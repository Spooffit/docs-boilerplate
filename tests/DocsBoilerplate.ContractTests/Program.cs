using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Diagnostics;

// Separate child process: simulate a hard stop without a catch/finally rollback.
if (args.Length == 2 && args[0] == "--crash-during-update")
{
    var output = args[1];
    await UpdateTransaction.ApplyAsync(output,
        [
            new UpdateTransaction.Write("docs/overview.md", Encoding.UTF8.GetBytes("new overview"),
                true, DocsCli.Hash("original overview")),
            new UpdateTransaction.Write("docs/new/guide.md", Encoding.UTF8.GetBytes("new guide"),
                false, null),
            new UpdateTransaction.Write("docs.config.json", Encoding.UTF8.GetBytes("new manifest"),
                true, DocsCli.Hash("original manifest"))
        ], count =>
        {
            if (count == 2) Environment.Exit(73);
            return Task.CompletedTask;
        });
    return 91;
}

var passed = 0;
var failed = 0;
void Test(string name, Action check)
{
    try
    {
        check();
        Console.WriteLine($"PASS {name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex}");
        failed++;
    }
}
void Expect(bool condition, string detail)
{
    if (!condition) throw new Exception(detail);
}
void Throws<TException>(Action action) where TException : Exception
{
    try { action(); }
    catch (TException) { return; }
    throw new Exception($"Expected {typeof(TException).Name}.");
}

DocsCli.Profile P(string id, string[]? dependencies = null, string status = "available") =>
    new(id, id, status, dependencies ?? [], [$"docs/standards/{id}.md"], "backend");
DocsCli.Catalog Catalog(params DocsCli.Profile[] profiles) => new(profiles);

Test("transitive dependencies resolve without duplicates", () =>
{
    var selected = DocsCli.ResolveProfiles(["web", "web"], Catalog(P("base"), P("transport", ["base"]), P("web", ["transport", "base"])));
    Expect(selected.SetEquals(["web", "transport", "base"]), "Missing or duplicate dependency");
});
Test("mutually exclusive infrastructure profiles reject conflicting deploy choices", () =>
{
    var profiles = Catalog(
        new DocsCli.Profile("compose", "Compose", "available", [], [], "infra-deploy"),
        new DocsCli.Profile("kubernetes", "Kubernetes", "available", [], [], "infra-deploy"));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["compose", "kubernetes"], profiles));
    Expect(DocsCli.ResolveProfiles(["compose"], profiles).SetEquals(["compose"]), "Single deploy choice must remain usable");
});
Test("unit test frameworks and S3 clients are mutually exclusive", () =>
{
    var catalog = Catalog(
        new DocsCli.Profile("tunit", "TUnit", "available", [], [], "test-framework"),
        new DocsCli.Profile("xunit", "xUnit", "available", [], [], "test-framework"),
        new DocsCli.Profile("s3", "S3", "available", [], [], "infra-storage"),
        new DocsCli.Profile("minio", "MinIO client", "available", ["s3"], [], "infra-s3-client"),
        new DocsCli.Profile("aws", "AWS client", "available", ["s3"], [], "infra-s3-client"));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["tunit", "xunit"], catalog));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["minio", "aws"], catalog));
    Expect(DocsCli.ResolveProfiles(["minio"], catalog).SetEquals(["minio", "s3"]),
        "Single MinIO S3 client did not resolve storage dependency");
});
Test("broker and client selection are independent but complete", () =>
{
    var data = Catalog(
        new DocsCli.Profile("dotnet", ".NET", "available", [], [], "backend"),
        new DocsCli.Profile("messaging-rabbitmq", "RabbitMQ", "available", [], [], "messaging-transport"),
        new DocsCli.Profile("messaging-kafka", "Kafka", "available", [], [], "messaging-transport"),
        new DocsCli.Profile("masstransit", "MassTransit", "available", ["dotnet"], [], "messaging-client"),
        new DocsCli.Profile("wolverine-rabbitmq", "Wolverine RabbitMQ", "available",
            ["dotnet", "messaging-rabbitmq"], [], "messaging-client"),
        new DocsCli.Profile("messaging-organization-inline", "Inline", "available",
            ["dotnet"], [], "messaging-organization"));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["messaging-rabbitmq"], data));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["masstransit"], data));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["masstransit", "messaging-rabbitmq", "messaging-kafka"], data));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["messaging-organization-inline"], data));
    Expect(DocsCli.ResolveProfiles(["masstransit", "messaging-rabbitmq"], data)
        .SetEquals(["dotnet", "masstransit", "messaging-rabbitmq"]), "Independent broker and client did not resolve");
    Expect(DocsCli.ResolveProfiles(["wolverine-rabbitmq", "messaging-organization-inline"], data)
        .Contains("messaging-rabbitmq"), "Wolverine dependency did not select broker");
});
Test("wizard offers MassTransit with either broker without mixing transport clients", () =>
{
    var masstransit = new DocsCli.Profile("masstransit", "MassTransit", "available",
        ["dotnet"], [], "messaging-client");
    var rabbitWolverine = new DocsCli.Profile("wolverine-rabbitmq", "Wolverine RabbitMQ",
        "available", ["dotnet", "messaging-rabbitmq"], [], "messaging-client");
    var kafkaManual = new DocsCli.Profile("messaging-manual-kafka", "Manual Kafka",
        "available", ["dotnet", "messaging-kafka"], [], "messaging-client");
    Expect(DocsCli.SupportsMessagingTransport(masstransit, "messaging-rabbitmq") &&
           DocsCli.SupportsMessagingTransport(masstransit, "messaging-kafka"),
        "MassTransit must be offered for both supported brokers");
    Expect(DocsCli.SupportsMessagingTransport(rabbitWolverine, "messaging-rabbitmq") &&
           !DocsCli.SupportsMessagingTransport(rabbitWolverine, "messaging-kafka"),
        "Wolverine RabbitMQ must not leak into Kafka choices");
    Expect(DocsCli.SupportsMessagingTransport(kafkaManual, "messaging-kafka") &&
           !DocsCli.SupportsMessagingTransport(kafkaManual, "messaging-rabbitmq"),
        "Custom Kafka client must not leak into RabbitMQ choices");
});
Test("Dapper and specification patterns cannot be combined", () =>
{
    var data = Catalog(
        new DocsCli.Profile("dotnet", ".NET", "available", [], [], "backend"),
        new DocsCli.Profile("dapper", "Dapper", "available", ["dotnet"], [], "data-access"),
        new DocsCli.Profile("ef-core", "EF Core", "available", ["dotnet"], [], "data-access"),
        new DocsCli.Profile("specification-manual", "Specification", "available",
            ["ef-core"], [], "query-pattern"));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["dapper", "specification-manual"], data));
    Expect(DocsCli.ResolveProfiles(["specification-manual"], data)
        .SetEquals(["dotnet", "ef-core", "specification-manual"]), "Specification must use EF Core");
});
Test("PostgreSQL poolers and architecture options are exclusive", () =>
{
    var data = Catalog(
        new DocsCli.Profile("postgresql", "PostgreSQL", "available", [], [], "database"),
        new DocsCli.Profile("postgres-pooler-pgbouncer", "PgBouncer", "available",
            ["postgresql"], [], "postgres-pooler"),
        new DocsCli.Profile("postgres-pooler-odyssey", "Odyssey", "available",
            ["postgresql"], [], "postgres-pooler"),
        new DocsCli.Profile("microservices", "Microservices", "available", [], [], "architecture"),
        new DocsCli.Profile("architecture-hybrid", "Hybrid", "available", [], [], "architecture"));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(
        ["postgres-pooler-pgbouncer", "postgres-pooler-odyssey"], data));
    Throws<InvalidOperationException>(() => DocsCli.ResolveProfiles(["architecture-hybrid", "microservices"], data));
    Expect(DocsCli.ResolveProfiles(["postgres-pooler-pgbouncer"], data)
        .SetEquals(["postgresql", "postgres-pooler-pgbouncer"]), "Pooler must require PostgreSQL");
});
Test("unknown profile rejects", () => Throws<InvalidOperationException>(() =>
    DocsCli.ResolveProfiles(["missing"], Catalog(P("base")))));
Test("planned profiles cannot be selected", () => Throws<InvalidOperationException>(() =>
    DocsCli.ResolveProfiles(["future"], Catalog(P("future", status: "planned")))));
Test("cyclic dependencies reject", () => Throws<InvalidOperationException>(() =>
    DocsCli.ResolveProfiles(["alpha"], Catalog(P("alpha", ["beta"]), P("beta", ["alpha"])))));
Test("an unselected profile is not added", () =>
{
    var selected = DocsCli.ResolveProfiles(["alpha"], Catalog(P("alpha"), P("unrelated")));
    Expect(selected.SetEquals(["alpha"]), "Unexpected implicit profile");
});
Test("interactive profile picker includes all independently selectable profiles", () =>
{
    var catalog = Catalog(P("dotnet"), P("http-contracts"), P("ui-direct-dto-forms"),
        P("go", status: "planned"));
    var extras = DocsCli.RemainingInteractiveProfiles(catalog, ["dotnet"]);
    Expect(extras.SequenceEqual(["http-contracts", "ui-direct-dto-forms"]),
        "Independently selectable profiles disappeared from the catalog");
});
Test("wizard omits backend/frontend dependencies when both are skipped", () =>
{
    var choices = Catalog(
        new DocsCli.Profile("dotnet", ".NET", "available", [], [], "backend"),
        new DocsCli.Profile("blazor", "Blazor", "available", ["dotnet"], [], "frontend"),
        new DocsCli.Profile("bunit", "bUnit", "available", ["blazor"], [], "testing"),
        new DocsCli.Profile("http-contracts", "HTTP", "available", [], [], "web"),
        new DocsCli.Profile("object-storage", "Storage", "available", [], [], "infra-storage-policy"));
    var candidates = DocsCli.RemainingContextualProfiles(choices, ["dotnet", "blazor"], "none", "none");
    Expect(candidates.SequenceEqual(["http-contracts"]),
        "An unrelated backend/frontend was selected from the independent options step");
});
Test("skills resolve precisely", () =>
{
    var catalog = new DocsCli.SkillCatalog([new("review", "Review", "skills/review/SKILL.md", ".agents/skills/review/SKILL.md")]);
    Expect(DocsCli.ResolveSkills(["review"], catalog).SetEquals(["review"]), "Wrong skill resolution");
    Throws<InvalidOperationException>(() => DocsCli.ResolveSkills(["review", "review"], catalog));
    Throws<InvalidOperationException>(() => DocsCli.ResolveSkills(["unknown"], catalog));
});
Test("modules resolve precisely", () =>
{
    var catalog = new DocsCli.ModuleCatalog([new("testing", "Testing", [])]);
    Expect(DocsCli.ResolveModules(["testing"], catalog).SetEquals(["testing"]), "Wrong module resolution");
    Throws<InvalidOperationException>(() => DocsCli.ResolveModules(["testing", "testing"], catalog));
    Throws<InvalidOperationException>(() => DocsCli.ResolveModules(["missing"], catalog));
});
Test("directory indexes cover ancestors but not root", () =>
{
    var folders = DocsCli.DirectoryNames([
        "docs/standards/dotnet/testing.md",
        "docs/standards/dotnet/conventions.md",
        "docs/project/overview.md", "docs/index.md", "README.md"]);
    Expect(folders.SequenceEqual(["docs/project", "docs/standards", "docs/standards/dotnet"]),
        "Wrong directory index selection or ordering");
});
Test("root llms.txt only references installed documents", () =>
{
    var content = new Dictionary<string, string>
    {
        ["AGENTS.md"] = "# Agent instructions",
        ["docs/index.md"] = "# Home",
        ["docs/standards/dotnet/index.md"] = "# .NET",
    };
    var text = DocsCli.BuildLlmsIndex("Sample", content);
    Expect(text.Contains("docs/standards/dotnet/index.md", StringComparison.Ordinal), "Missing installed standard");
    Expect(!text.Contains("docs/templates/index.md", StringComparison.Ordinal), "Invented optional section");
    Expect(!text.Contains("docs/project/overview.md", StringComparison.Ordinal), "Invented absent file");
});
Test("llms.txt lists installed project areas without leaking absent modules", () =>
{
    var docs = new Dictionary<string, string>
    {
        ["AGENTS.md"] = "# Agent",
        ["docs/index.md"] = "# Home",
        ["docs/catalog.md"] = "# Catalog",
        ["docs/project/overview.md"] = "# Overview",
        ["docs/architecture/overview.md"] = "# Architecture",
        ["docs/architecture/api.md"] = "# Public API",
        ["docs/project/requirements.md"] = "# Requirements",
        ["docs/testing/strategy.md"] = "# Test strategy",
        ["docs/standards/http/index.md"] = "# HTTP",
        ["docs/templates/index.md"] = "# Templates"
    };
    var generated = DocsCli.BuildLlmsIndex("Sample", docs);
    Expect(generated.Contains("## Project areas", StringComparison.Ordinal), "Missing project area section");
    foreach (var path in new[] { "docs/architecture/api.md", "docs/project/requirements.md", "docs/testing/strategy.md" })
        Expect(generated.Contains($"]({path})", StringComparison.Ordinal), $"Missing installed module: {path}");
    Expect(generated.Contains("docs/standards/http/index.md", StringComparison.Ordinal), "Missing chosen standard group");
    Expect(generated.Contains("docs/templates/index.md", StringComparison.Ordinal), "Missing installed templates");
    Expect(!generated.Contains("docs/architecture/security.md", StringComparison.Ordinal), "Unselected security module leaked");
    Expect(!generated.Contains("docs/operations/runbook.md", StringComparison.Ordinal), "Unselected operations module leaked");
    Expect(generated.Split("](docs/catalog.md)", StringSplitOptions.None).Length == 2, "Duplicate catalog link");
    var replay = DocsCli.BuildLlmsIndex("Sample", docs);
    Expect(generated == replay, "Navigation order is not deterministic");

    var minimal = DocsCli.BuildLlmsIndex("Sample",
        new Dictionary<string, string> { ["AGENTS.md"] = "# Agent", ["docs/index.md"] = "# Home" });
    Expect(!minimal.Contains("## Project areas", StringComparison.Ordinal), "Empty optional section was generated");
    Expect(!minimal.Contains("docs/project/requirements.md", StringComparison.Ordinal), "Invented optional requirements");
});

Test("Russian agent index and selected-profile catalog keep canonical paths", () =>
{
    var files = new Dictionary<string, string>
    {
        ["AGENTS.md"] = "# Инструкции ИИ-агентам",
        ["docs/index.md"] = "# Русский проект",
        ["docs/catalog.md"] = "# Каталог документации",
        ["docs/project/overview.md"] = "# Обзор проекта",
        ["docs/standards/dotnet/testing.md"] = "# Тестирование .NET",
        ["docs/standards/dotnet/index.md"] = "# .NET"
    };
    var llms = DocsCli.BuildLlmsIndex("Русский проект", files, "ru");
    Expect(llms.Contains("## Основное", StringComparison.Ordinal), "Russian llms entry heading was not rendered");
    Expect(llms.Contains("## Справочник", StringComparison.Ordinal), "Russian llms reference heading was not rendered");
    Expect(llms.Contains("](docs/project/overview.md)", StringComparison.Ordinal),
        "Russian agent index changed canonical source paths");

    var profiles = new DocsCli.Catalog(
    [
        new DocsCli.Profile("dotnet", "English catalog profile label", "available", [],
            ["docs/standards/dotnet/testing.md"], "backend")
    ]);
    var index = DocsCli.BuildDocumentCatalog(files, ["dotnet"], profiles, "ru");
    Expect(index.Contains("# Каталог документации", StringComparison.Ordinal),
        "Russian document catalog heading was not rendered");
    Expect(index.Contains("**Тестирование .NET** (`dotnet`)", StringComparison.Ordinal),
        "Selected profile label was not derived from the localized document");
    Expect(index.Contains("](standards/dotnet/testing.md)", StringComparison.Ordinal),
        "Document catalog must retain language-neutral output paths");
    Expect(!index.Contains("English catalog profile label", StringComparison.Ordinal),
        "English source profile name leaked into Russian catalog");
});

Test("document catalog lists only selected technology", () =>
{
    var content = new Dictionary<string, string>
    {
        ["docs/index.md"] = "# Home",
        ["docs/standards/dotnet/testing.md"] = "# Testing",
    };
    var text = DocsCli.BuildDocumentCatalog(content, ["dotnet"], Catalog(P("dotnet"), P("blazor")));
    Expect(text.Contains("dotnet", StringComparison.Ordinal), "Missing chosen technology");
    Expect(!text.Contains("blazor", StringComparison.Ordinal), "Unselected technology leaked");
    Expect(text.Contains("standards/dotnet/testing.md", StringComparison.Ordinal), "Missing installed document");
});
Test("discoverability identifies missing catalog and local index entries", () =>
{
    var docs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["docs/index.md"] = "# Home",
        ["docs/catalog.md"] = "# Catalog\n- [Home](index.md)\n- [Index](project/index.md)\n- [Overview](project/overview.md)",
        ["docs/project/index.md"] = "# Project\n- [Overview](overview.md)",
        ["docs/project/overview.md"] = "# Overview",
        ["docs/project/new-guide.md"] = "# New guide"
    };
    var owned = new HashSet<string>(docs.Keys.Where(p => p != "docs/project/new-guide.md"),
        StringComparer.OrdinalIgnoreCase);
    var missing = DocumentDiscovery.FindMissingLinks(docs, owned);
    Expect(missing.Count == 2 && missing.Any(x => x.Contains("docs/catalog.md"))
        && missing.Any(x => x.Contains("docs/project/index.md")),
        "An unindexed authored document escaped discovery checks");

    docs["docs/project/index.md"] += "\n- [New guide](new-guide.md#overview)";
    missing = DocumentDiscovery.FindMissingLinks(docs, owned);
    Expect(missing.Count == 1 && missing[0].Contains("docs/catalog.md"),
        "Local index repair incorrectly replaced catalog discovery");
    docs["docs/catalog.md"] += "\n- [New guide](project/new-guide.md)";
    Expect(DocumentDiscovery.FindMissingLinks(docs, owned).Count == 0,
        "Fully indexed new document was rejected");

    docs["docs/project/renamed.md"] = docs["docs/project/new-guide.md"];
    docs.Remove("docs/project/new-guide.md");
    missing = DocumentDiscovery.FindMissingLinks(docs, owned);
    Expect(missing.Count == 2, "Moving a user document left navigation silently stale");
    docs["docs/catalog.md"] = docs["docs/catalog.md"].Replace("new-guide.md", "renamed.md");
    docs["docs/project/index.md"] = docs["docs/project/index.md"].Replace("new-guide.md", "renamed.md");
    Expect(DocumentDiscovery.FindMissingLinks(docs, owned).Count == 0,
        "Updated navigation after a move was rejected");

    // Removed profile sources are retained on disk, but no longer advertised
    // by the selected technology catalog. This must not hide newly authored docs.
    docs["docs/project/retired.md"] = "# Old material";
    var retained = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "docs/project/retired.md" };
    Expect(DocumentDiscovery.FindMissingLinks(docs, owned, retained).Count == 0,
        "Explicitly retained retired source was treated as a new author page");
    Expect(DocumentDiscovery.FindMissingLinks(docs, owned).Count == 2,
        "Omitting the retained manifest must restore discoverability checks");
});
Test("authored nested directory indexes require a parent link", () =>
{
    var docs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["docs/index.md"] = "# Home",
        ["docs/catalog.md"] = "# Catalog\n- [Home](index.md)\n- [Project](project/index.md)\n- [Notes](project/notes/index.md)\n- [Guide](project/notes/guide.md)",
        ["docs/project/index.md"] = "# Project",
        ["docs/project/notes/index.md"] = "# Notes\n- [Guide](guide.md)",
        ["docs/project/notes/guide.md"] = "# Guide"
    };
    var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "docs/index.md", "docs/catalog.md", "docs/project/index.md"
    };
    var missing = DocumentDiscovery.FindMissingLinks(docs, owned);
    Expect(missing.Count == 1 && missing[0].Contains("docs/project/index.md")
        && missing[0].Contains("docs/project/notes/index.md"),
        "An authored directory escaped parent-index discovery");

    docs["docs/project/index.md"] += "\n- [Notes](notes/index.md)";
    Expect(DocumentDiscovery.FindMissingLinks(docs, owned).Count == 0,
        "Complete nested navigation was rejected");

    docs["docs/project/notes/index.md"] = "# Notes";
    missing = DocumentDiscovery.FindMissingLinks(docs, owned);
    Expect(missing.Count == 1 && missing[0].Contains("docs/project/notes/index.md")
        && missing[0].Contains("docs/project/notes/guide.md"),
        "An authored page without its local index entry escaped validation");
});
Test("catalog links with escaped bracket labels are discoverable", () =>
{
    const string navigation = @"# Catalog
- [Feature: \[Concise name\]](templates/feature.md)
- [Technical: \[Boundary\]](templates/technical-feature.md)";
    var links = MarkdownNavigation.InlineTargets(navigation).ToArray();
    Expect(links.SequenceEqual(["templates/feature.md", "templates/technical-feature.md"]),
        "Escaped square brackets in generated titles hid valid Markdown links");
    var resolved = DocumentDiscovery.LinkedPaths("docs/catalog.md", navigation);
    Expect(resolved.SetEquals(["docs/templates/feature.md", "docs/templates/technical-feature.md"]),
        "Catalog parsing omitted escaped labels");
});

Test("discovery links honor relative paths and ignore examples", () =>
{
    var linked = DocumentDiscovery.LinkedPaths("docs/project/index.md", """
        [local](overview.md#purpose)
        [sibling](../architecture/overview.md)
        [encoded](My%20Topic.md)
        [outside](../../../not-a-project-doc.md)
        [web](https://example.com/docs)
        Inline `[fiction](missing.md)`
        ~~~md
        [also fictional](not-real.md)
        ~~~
        """);
    Expect(linked.Contains("docs/project/overview.md"), "Local path lost");
    Expect(linked.Contains("docs/architecture/overview.md"), "Relative parent path lost");
    Expect(linked.Contains("docs/project/My Topic.md"), "Encoded link lost");
    Expect(!linked.Contains("not-a-project-doc.md") && !linked.Contains("docs/project/missing.md")
        && !linked.Contains("docs/project/not-real.md"), "Invalid or example path was included");
});

Test("Markdown heading anchors are scoped, duplicated, and Unicode-safe", () =>
{
    const string source = """
        # Overview

        ## One **Important** Section

        ## Repeat
        ## Repeat
        ## Repeat!

        Paragraph title
        ------------

        <a id="explicit-id"></a>
        <a name="explicit-name"></a>

        ~~~md
        ## Hidden header
        [Bad](missing.md#nope)
        ~~~
        """;
    var anchors = MarkdownNavigation.Anchors(source);
    foreach (var key in new[] { "overview", "one-important-section", "repeat", "repeat-1", "repeat-2",
        "paragraph-title", "explicit-id", "explicit-name" })
        Expect(anchors.Contains(key), $"Heading anchor missing: {key}");
    Expect(!anchors.Contains("hidden-header"), "Heading in fenced code became an anchor");
    var unicodeSlug = MarkdownNavigation.Slug("Über C# & .NET");
    Expect(unicodeSlug == "über-c--net", $"Incorrect Unicode/punctuation slug: {unicodeSlug}");
});

Test("Markdown link targets retain fragments and omit fenced code examples", () =>
{
    const string source = """
        Read [Section](guide.md#one-important-section) and [same page](#overview).
        Open [encoded](guide.md#über-caf%C3%A9).
        Inline `[example](missing.md#not-real)` is not a real link.

        ~~~markdown
        [Example only](missing.md#fictitious)
        ~~~
        """;
    var links = MarkdownNavigation.InlineTargets(source).ToArray();
    Expect(links.Length == 3, "Inline link parser included fenced content or skipped links");
    Expect(links[0] == "guide.md#one-important-section", "Lost real fragment");
    Expect(links[1] == "#overview", "Lost same-page fragment");
    var (path, fragment) = MarkdownNavigation.SplitLocalTarget(links[2]);
    Expect(path == "guide.md" && fragment == "über-café", "Percent-encoded fragment was not decoded");
    var (anglePath, angleFragment) = MarkdownNavigation.SplitLocalTarget("<guides/my guide.md#section> \"Title\"");
    Expect(anglePath == "guides/my guide.md" && angleFragment == "section", "Angle-bracket path not supported");
});

Test("SHA-256 hashes are stable and use UTF-8", () =>
    Expect(DocsCli.Hash("abc") == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
        "SHA-256 does not match known test vector"));
Test("metadata is added once and remains valid", () =>
{
    var input = "# Rules\n\nApplies to .NET.";
    var first = DocumentMetadata.Prepare("docs/standards/rules.md", input);
    Expect(DocumentMetadata.IsValid(first, false), "Generated frontmatter invalid");
    Expect(DocumentMetadata.DocumentId(first) is not null, "Stable document ID missing");
    Expect(first == DocumentMetadata.Prepare("docs/standards/rules.md", first), "Metadata was inserted twice");
    Expect(DocumentMetadata.DocumentId(DocumentMetadata.Prepare("docs/project/new.md", "# New")) != DocumentMetadata.DocumentId(first),
        "Two different documents received the same stable ID");
    Expect(first.Contains("kind: standard", StringComparison.Ordinal), "Wrong metadata kind");
});
Test("root README is readable on GitHub without leaking frontmatter", () =>
{
    var readme = "# Example\n\nA descriptive landing page.";
    Expect(DocumentMetadata.Prepare("README.md", readme) == readme,
        "Root README received injected YAML metadata");
    Expect(DocumentMetadata.Prepare("docs/README.md", readme).StartsWith("---\n", StringComparison.Ordinal),
        "Nested README improperly bypassed normal document metadata");
    Throws<InvalidOperationException>(() => DocumentMetadata.Prepare(
        "README.md", "---\ntitle: Example\n---\n\n# Example"));
    Throws<InvalidOperationException>(() => DocumentMetadata.Prepare(
        "README.md", "No top-level heading"));
});
Test("invalid or duplicate metadata fields are rejected", () =>
{
    Expect(!DocumentMetadata.IsValid("# No frontmatter", false), "Missing metadata allowed");
    var duplicate = "---\ntitle: A\ntitle: B\ndescription: C\nkind: index\nstatus: active\n---\n\n# A";
    Expect(!DocumentMetadata.IsValid(duplicate, false), "Duplicate metadata allowed");
    Throws<InvalidOperationException>(() => DocumentMetadata.Prepare("docs/index.md", "---\ninvalid\n---\n# Broken"));
});
Test("agent skill metadata stays distinct from ordinary documents", () =>
{
    var skill = "---\nname: review\ndescription: Review documentation\n---\n\n# Review";
    Expect(DocumentMetadata.IsValid(skill, true), "Valid skill metadata rejected");
    Expect(!DocumentMetadata.IsValid(skill, false), "Skill metadata accepted as normal page");
    Throws<InvalidOperationException>(() => DocumentMetadata.Prepare(".agents/skills/review/SKILL.md", "# No skill metadata"));
});
Test("portable document naming rejects malformed Markdown names", () =>
{
    DocumentMetadata.AssertFileName("docs/my-feature.md");
    DocumentMetadata.AssertFileName("docs/index.md");
    DocumentMetadata.AssertFileName("README.md");
    DocumentMetadata.AssertFileName("SKILL.md");
    Throws<InvalidOperationException>(() => DocumentMetadata.AssertFileName("docs/My_Feature.md"));
    Throws<InvalidOperationException>(() => DocumentMetadata.AssertFileName("docs/MyFeature.md"));
});
Test("path containment rejects traversal and platform metacharacters", () =>
{
    using var temporary = new TempDirectory();
    var child = SafeFiles.Child(temporary.Path, "docs/project/index.md");
    Expect(child.StartsWith(temporary.Path, OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal), "Legitimate child outside root");
    foreach (var invalid in new[] { "../secret.md", "docs/../../secret", "/absolute.md", "docs\\evil.md", "docs/C:evil.md", "docs//file.md" })
        Throws<IOException>(() => SafeFiles.Child(temporary.Path, invalid));
});
Test("forced process exit leaves a recoverable journal and preserves the original manifest", () =>
{
    using var tmp = new TempDirectory();
    Directory.CreateDirectory(Path.Combine(tmp.Path, "docs"));
    var overview = Path.Combine(tmp.Path, "docs", "overview.md");
    var manifest = Path.Combine(tmp.Path, "docs.config.json");
    File.WriteAllText(overview, "original overview");
    File.WriteAllText(manifest, "original manifest");
    var info = new ProcessStartInfo("dotnet")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    info.ArgumentList.Add(System.Reflection.Assembly.GetExecutingAssembly().Location);
    info.ArgumentList.Add("--crash-during-update");
    info.ArgumentList.Add(tmp.Path);
    using var child = Process.Start(info) ?? throw new Exception("Could not launch fault child");
    if (!child.WaitForExit(30000))
    {
        child.Kill(entireProcessTree: true);
        throw new Exception("Fault child did not exit");
    }
    Expect(child.ExitCode == 73, $"Expected forced exit 73, got {child.ExitCode}");
    Expect(UpdateRecovery.HasPending(tmp.Path), "Hard stop did not leave a journal");
    Expect(File.ReadAllText(overview) == "new overview", "First write was not applied");
    Expect(File.Exists(Path.Combine(tmp.Path, "docs", "new", "guide.md")),
        "New file was not applied before termination");
    Expect(File.ReadAllText(manifest) == "original manifest", "Manifest must be written last");
    Throws<IOException>(() => UpdateRecovery.AssertNoPending(tmp.Path));
    var result = UpdateRecovery.RecoverAsync(tmp.Path).GetAwaiter().GetResult();
    Expect(result == UpdateRecovery.Outcome.RolledBack, "Interrupted update was not rolled back");
    Expect(File.ReadAllText(overview) == "original overview", "Recovery lost original contents");
    Expect(File.ReadAllText(manifest) == "original manifest", "Recovery changed the original manifest");
    Expect(!Directory.Exists(Path.Combine(tmp.Path, "docs", "new")), "Recovery retained new directories");
    Expect(!UpdateRecovery.HasPending(tmp.Path), "Recovery left journal behind");
});
Test("recovery finalizes an entirely written update after interrupted cleanup", () =>
{
    using var tmp = new TempDirectory();
    var first = Path.Combine(tmp.Path, "page.md");
    var manifest = Path.Combine(tmp.Path, "docs.config.json");
    File.WriteAllText(first, "original");
    File.WriteAllText(manifest, "old manifest");
    var writes = new[]
    {
        new UpdateTransaction.Write("page.md", Encoding.UTF8.GetBytes("updated"), true, DocsCli.Hash("original")),
        new UpdateTransaction.Write("docs.config.json", Encoding.UTF8.GetBytes("new manifest"), true, DocsCli.Hash("old manifest"))
    };
    UpdateRecovery.PrepareAsync(tmp.Path, writes).GetAwaiter().GetResult();
    for (var i = 0; i < writes.Length; i++)
        UpdateRecovery.ApplyStagedAsync(tmp.Path, i, writes[i].Path, DocsCli.Hash(
            Encoding.UTF8.GetString(writes[i].Content)), true).GetAwaiter().GetResult();
    Expect(UpdateRecovery.RecoverAsync(tmp.Path).GetAwaiter().GetResult() ==
        UpdateRecovery.Outcome.AlreadyCompleted, "Fully committed update was incorrectly undone");
    Expect(File.ReadAllText(first) == "updated" && File.ReadAllText(manifest) == "new manifest",
        "Successful update was undone after process loss");
    Expect(!UpdateRecovery.HasPending(tmp.Path), "Completed transaction left its journal");
});
Test("independent edit after process loss blocks recovery before any writes", () =>
{
    using var tmp = new TempDirectory();
    var first = Path.Combine(tmp.Path, "one.md");
    var other = Path.Combine(tmp.Path, "two.md");
    File.WriteAllText(first, "before one");
    File.WriteAllText(other, "before two");
    var writes = new[]
    {
        new UpdateTransaction.Write("one.md", Encoding.UTF8.GetBytes("after one"), true, DocsCli.Hash("before one")),
        new UpdateTransaction.Write("two.md", Encoding.UTF8.GetBytes("after two"), true, DocsCli.Hash("before two"))
    };
    UpdateRecovery.PrepareAsync(tmp.Path, writes).GetAwaiter().GetResult();
    UpdateRecovery.ApplyStagedAsync(tmp.Path, 0, "one.md", DocsCli.Hash("after one"), true).GetAwaiter().GetResult();
    File.WriteAllText(other, "owner changed the second page");
    Throws<IOException>(() => UpdateRecovery.RecoverAsync(tmp.Path).GetAwaiter().GetResult());
    Expect(File.ReadAllText(first) == "after one", "Recovery modified a file despite conflict");
    Expect(File.ReadAllText(other) == "owner changed the second page", "Newer edit was overwritten");
    Expect(UpdateRecovery.HasPending(tmp.Path), "Recovery deleted evidence of a conflict");
});
Test("interrupted staging without a complete plan cannot be mistaken for a safe rollback", () =>
{
    using var tmp = new TempDirectory();
    var pending = Path.Combine(tmp.Path, UpdateRecovery.DirectoryName);
    Directory.CreateDirectory(pending);
    File.WriteAllText(Path.Combine(pending, "unrecognized.txt"), "must be preserved");
    Throws<IOException>(() => UpdateRecovery.RecoverAsync(tmp.Path).GetAwaiter().GetResult());
    Expect(File.Exists(Path.Combine(pending, "unrecognized.txt")),
        "Unrecognized staging data was erased without owner review");
});
Test("recovery can resume when cleanup had already deleted an unused backup", () =>
{
    using var tmp = new TempDirectory();
    var one = Path.Combine(tmp.Path, "one.md");
    var two = Path.Combine(tmp.Path, "two.md");
    File.WriteAllText(one, "before one");
    File.WriteAllText(two, "before two");
    var writes = new[]
    {
        new UpdateTransaction.Write("one.md", Encoding.UTF8.GetBytes("after one"), true, DocsCli.Hash("before one")),
        new UpdateTransaction.Write("two.md", Encoding.UTF8.GetBytes("after two"), true, DocsCli.Hash("before two"))
    };
    UpdateRecovery.PrepareAsync(tmp.Path, writes).GetAwaiter().GetResult();
    UpdateRecovery.ApplyStagedAsync(tmp.Path, 1, "two.md", DocsCli.Hash("after two"), true).GetAwaiter().GetResult();
    File.Delete(Path.Combine(tmp.Path, UpdateRecovery.DirectoryName, "before", "000000.bin"));
    Expect(UpdateRecovery.RecoverAsync(tmp.Path).GetAwaiter().GetResult() ==
        UpdateRecovery.Outcome.RolledBack, "Recovery needed an already unnecessary snapshot");
    Expect(File.ReadAllText(one) == "before one" && File.ReadAllText(two) == "before two",
        "Partial cleanup recovery did not restore the original state");
});

Test("update rollback restores all prior bytes, removes additions and cleans directories", () =>
{
    foreach (var failAfter in new[] { 2, 3 })
    {
        using var tmp = new TempDirectory();
        var projectDir = Path.Combine(tmp.Path, "docs", "project");
        Directory.CreateDirectory(projectDir);
        var originalPath = Path.Combine(projectDir, "overview.md");
        var configPath = Path.Combine(tmp.Path, "docs.config.json");
        var userPath = Path.Combine(projectDir, "personal.txt");
        File.WriteAllText(originalPath, "original page");
        File.WriteAllText(configPath, "original manifest");
        File.WriteAllText(userPath, "do not touch");
        var writes = new[]
        {
            new UpdateTransaction.Write("docs/project/overview.md",
                Encoding.UTF8.GetBytes("new page"), true, DocsCli.Hash("original page")),
            new UpdateTransaction.Write("docs/new/deep/feature.md",
                Encoding.UTF8.GetBytes("new feature"), false, null),
            new UpdateTransaction.Write("docs.config.json",
                Encoding.UTF8.GetBytes("new manifest"), true, DocsCli.Hash("original manifest"))
        };
        IOException? failure = null;
        try
        {
            UpdateTransaction.ApplyAsync(tmp.Path, writes, count =>
                count == failAfter
                    ? Task.FromException(new InvalidOperationException("injected post-write failure"))
                    : Task.CompletedTask).GetAwaiter().GetResult();
        }
        catch (IOException ex) { failure = ex; }

        Expect(failure is not null && failure.Message.Contains("Completed changes were rolled back"),
            "Fault was not reported as successfully rolled back");
        Expect(File.ReadAllText(originalPath) == "original page", "Changed template file was not restored");
        Expect(File.ReadAllText(configPath) == "original manifest", "Manifest changed after failure");
        Expect(File.ReadAllText(userPath) == "do not touch", "User-owned file was modified by rollback");
        Expect(!Directory.Exists(Path.Combine(tmp.Path, "docs", "new")),
            "Rollback left newly created directories");
        Expect(!Directory.EnumerateFiles(tmp.Path, ".docs-boilerplate-*.tmp",
            SearchOption.AllDirectories).Any(), "Temporary files survived rollback");
    }
});
Test("update rollback also handles stale hashes after earlier writes", () =>
{
    using var tmp = new TempDirectory();
    var userPath = Path.Combine(tmp.Path, "existing.md");
    File.WriteAllText(userPath, "owner's latest revision");
    var writes = new[]
    {
        new UpdateTransaction.Write("new/guide.md", Encoding.UTF8.GetBytes("new content"), false, null),
        new UpdateTransaction.Write("existing.md", Encoding.UTF8.GetBytes("upstream"), true,
            DocsCli.Hash("obsolete baseline"))
    };
    IOException? failure = null;
    try { UpdateTransaction.ApplyAsync(tmp.Path, writes).GetAwaiter().GetResult(); }
    catch (IOException ex) { failure = ex; }
    Expect(failure is not null && failure.Message.Contains("File changed after planning"),
        "Stale expected fingerprint did not block write");
    Expect(File.ReadAllText(userPath) == "owner's latest revision", "Concurrent user content was overwritten");
    Expect(!Directory.Exists(Path.Combine(tmp.Path, "new")),
        "Earlier successful writes were not reversed");
});
Test("rollback refuses to erase a later concurrent change", () =>
{
    using var tmp = new TempDirectory();
    var page = Path.Combine(tmp.Path, "owned.md");
    File.WriteAllText(page, "baseline");
    IOException? failure = null;
    try
    {
        UpdateTransaction.ApplyAsync(tmp.Path,
            [new UpdateTransaction.Write("owned.md", Encoding.UTF8.GetBytes("new upstream"),
                true, DocsCli.Hash("baseline"))],
            _ =>
            {
                File.WriteAllText(page, "edited after generator write");
                return Task.FromException(new InvalidOperationException("fault after external modification"));
            }).GetAwaiter().GetResult();
    }
    catch (IOException ex) { failure = ex; }
    Expect(failure is not null && failure.Message.Contains("Rollback was incomplete"),
        "Concurrent edit was not reported as an incomplete rollback");
    Expect(File.ReadAllText(page) == "edited after generator write",
        "Rollback erased a later independent edit");
});
Test("successful update transaction applies old and new files", () =>
{
    using var tmp = new TempDirectory();
    var owned = Path.Combine(tmp.Path, "owned.md");
    File.WriteAllText(owned, "before");
    UpdateTransaction.ApplyAsync(tmp.Path,
        [
            new UpdateTransaction.Write("owned.md", Encoding.UTF8.GetBytes("after"), true,
                DocsCli.Hash("before")),
            new UpdateTransaction.Write("added/subdir/new.md", Encoding.UTF8.GetBytes("created"), false, null)
        ]).GetAwaiter().GetResult();
    Expect(File.ReadAllText(owned) == "after", "Expected replacement was missing");
    Expect(File.ReadAllText(Path.Combine(tmp.Path, "added", "subdir", "new.md")) == "created",
        "Expected addition was missing");
});

Test("update path preflight rejects case-only files and directory aliases", () =>
{
    var entries = new[]
    {
        "docs",
        "docs/standards",
        "docs/standards/dotnet",
        "docs/standards/dotnet/USER-NOTE.md",
        "docs/Guides",
        "docs/Guides/index.md",
        "docs/standards/dotnet/existing.md"
    };
    Expect(UpdatePathSafety.FindCaseOnlyCollision(
        "docs/standards/dotnet/user-note.md", entries) == "docs/standards/dotnet/USER-NOTE.md",
        "Case-only sibling file collision was missed");
    Expect(UpdatePathSafety.FindCaseOnlyCollision(
        "docs/guides/new.md", entries) == "docs/Guides",
        "Case-only ancestor directory collision was missed");
    Expect(UpdatePathSafety.FindCaseOnlyCollision(
        "docs/standards/dotnet/new.md", entries) is null,
        "A non-colliding file was falsely rejected");
    Expect(UpdatePathSafety.FindCaseOnlyCollision(
        "docs/standards/dotnet/existing.md", entries) is null,
        "Exact casing must be handled by ordinary collision logic");
    Expect(UpdatePathSafety.FindBlockingFileAncestor(
        "docs/notes/guide.md", ["docs/notes", "docs/standards/summary.md"]) == "docs/notes",
        "A user file blocking a needed directory was missed");
    Expect(UpdatePathSafety.FindBlockingFileAncestor(
        "docs/standards/dotnet/new.md", ["docs/standards/summary.md"]) is null,
        "Unrelated user file falsely blocked an update");
});
Test("safe enumeration includes directories without changing file-only enumeration", () =>
{
    using var tmp = new TempDirectory();
    var folder = Path.Combine(tmp.Path, "EmptyFolder");
    Directory.CreateDirectory(folder);
    var file = Path.Combine(tmp.Path, "document.md");
    File.WriteAllText(file, "# Example");
    Expect(!SafeFiles.Enumerate(tmp.Path).Contains(folder),
        "Directory leaked into existing file-only enumeration");
    var entries = SafeFiles.EnumerateEntries(tmp.Path).ToArray();
    Expect(entries.Contains(folder) && entries.Contains(file),
        "Update preflight omitted files or empty directories");
});

Test("symlinked path segment is rejected when platform permits creation", () =>
{
    using var temporary = new TempDirectory();
    var external = System.IO.Path.Combine(temporary.Path, "external");
    Directory.CreateDirectory(external);
    var symlink = System.IO.Path.Combine(temporary.Path, "linked");
    try { Directory.CreateSymbolicLink(symlink, external); }
    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
    {
        Console.WriteLine("SKIP symlink test (insufficient platform permissions)");
        return;
    }
    Throws<IOException>(() => SafeFiles.Child(temporary.Path, "linked/inside.md"));
});

Console.WriteLine($"{passed} passed, {failed} failed.");
return failed == 0 ? 0 : 1;

sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "docs-boilerplate-contract-" + Guid.NewGuid().ToString("N"));
    public TempDirectory() => Directory.CreateDirectory(Path);
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
