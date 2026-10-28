using System.Security.Cryptography;
using System.Text;
using System.Linq;

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
DocsCli.Catalog Catalog(params DocsCli.Profile[] profiles) => new(1, profiles);

Test("transitive dependencies resolve without duplicates", () =>
{
    var selected = DocsCli.ResolveProfiles(["web", "web"], Catalog(P("base"), P("transport", ["base"]), P("web", ["transport", "base"])));
    Expect(selected.SetEquals(["web", "transport", "base"]), "Missing or duplicate dependency");
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
Test("skills resolve precisely", () =>
{
    var catalog = new DocsCli.SkillCatalog(1, [new("review", "Review", "skills/review/SKILL.md", ".agents/skills/review/SKILL.md")]);
    Expect(DocsCli.ResolveSkills(["review"], catalog).SetEquals(["review"]), "Wrong skill resolution");
    Throws<InvalidOperationException>(() => DocsCli.ResolveSkills(["review", "review"], catalog));
    Throws<InvalidOperationException>(() => DocsCli.ResolveSkills(["unknown"], catalog));
});
Test("modules resolve precisely", () =>
{
    var catalog = new DocsCli.ModuleCatalog(1, [new("testing", "Testing", [])]);
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
    var text = DocsCli.BuildLlmsIndex("Sample", "Summary", content);
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
    var generated = DocsCli.BuildLlmsIndex("Sample", "Summary", docs);
    Expect(generated.Contains("## Project areas", StringComparison.Ordinal), "Missing project area section");
    foreach (var path in new[] { "docs/architecture/api.md", "docs/project/requirements.md", "docs/testing/strategy.md" })
        Expect(generated.Contains($"]({path})", StringComparison.Ordinal), $"Missing installed module: {path}");
    Expect(generated.Contains("docs/standards/http/index.md", StringComparison.Ordinal), "Missing chosen standard group");
    Expect(generated.Contains("docs/templates/index.md", StringComparison.Ordinal), "Missing installed templates");
    Expect(!generated.Contains("docs/architecture/security.md", StringComparison.Ordinal), "Unselected security module leaked");
    Expect(!generated.Contains("docs/operations/runbook.md", StringComparison.Ordinal), "Unselected operations module leaked");
    Expect(generated.Split("](docs/catalog.md)", StringSplitOptions.None).Length == 2, "Duplicate catalog link");
    var replay = DocsCli.BuildLlmsIndex("Sample", "Summary", docs);
    Expect(generated == replay, "Navigation order is not deterministic");

    var minimal = DocsCli.BuildLlmsIndex("Sample", "Summary",
        new Dictionary<string, string> { ["AGENTS.md"] = "# Agent", ["docs/index.md"] = "# Home" });
    Expect(!minimal.Contains("## Project areas", StringComparison.Ordinal), "Empty optional section was generated");
    Expect(!minimal.Contains("docs/project/requirements.md", StringComparison.Ordinal), "Invented optional requirements");
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
    Expect(first == DocumentMetadata.Prepare("docs/standards/rules.md", first), "Metadata was inserted twice");
    Expect(first.Contains("kind: standard", StringComparison.Ordinal), "Wrong metadata kind");
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
