using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// A deliberately scoped Markdown navigation parser, not a CommonMark or Docsify renderer.
/// Understands common inline links, ATX/Setext headings, and repeated heading slugs.
/// </summary>
internal static class MarkdownNavigation
{
    private static readonly Regex InlineLink = new(
        @"(?<!!)\[(?:\\.|[^\]\\\r\n])+\]\((?<target>[^)\r\n]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Heading = new(
        @"^ {0,3}#{1,6}[ \t]+(?<title>.+?)[ \t]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Setext = new(
        @"^ {0,3}(?:=+|-+)[ \t]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ExplicitAnchor = new(
        @"<a\s+[^>]*\b(?:id|name)\s*=\s*[""'](?<id>[^""']+)[""'][^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex MarkdownLinkLabel = new(
        @"!?\[(?<label>[^\]]+)\]\([^)]+\)", RegexOptions.Compiled);
    private static readonly Regex HtmlTag = new(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex InlineCode = new("`+(?<value>[^`]+)`+", RegexOptions.Compiled);

    internal static IEnumerable<string> InlineTargets(string markdown)
    {
        foreach (var line in VisibleLines(markdown))
            foreach (Match match in InlineLink.Matches(MaskInlineCode(line)))
                yield return match.Groups["target"].Value.Trim();
    }

    private static string MaskInlineCode(string line)
    {
        var result = line.ToCharArray();
        for (var i = 0; i < line.Length;)
        {
            if (line[i] != '`' || (i > 0 && line[i - 1] == '\\'))
            {
                i++;
                continue;
            }
            var start = i;
            var run = 0;
            while (i < line.Length && line[i] == '`')
            {
                run++;
                i++;
            }
            var end = -1;
            for (var j = i; j < line.Length;)
            {
                if (line[j] != '`') { j++; continue; }
                var k = j;
                while (k < line.Length && line[k] == '`') k++;
                if (k - j == run) { end = k; break; }
                j = k;
            }
            if (end < 0) continue;
            Array.Fill(result, ' ', start, end - start);
            i = end;
        }
        return new string(result);
    }

    internal static HashSet<string> Anchors(string markdown)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var duplicateCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        string? prior = null;
        foreach (var line in VisibleLines(markdown))
        {
            foreach (Match anchor in ExplicitAnchor.Matches(line))
                anchors.Add(anchor.Groups["id"].Value);
            var heading = Heading.Match(line);
            if (heading.Success)
            {
                AddHeading(heading.Groups["title"].Value);
                prior = null;
            }
            else if (Setext.IsMatch(line) && !string.IsNullOrWhiteSpace(prior))
            {
                AddHeading(prior);
                prior = null;
            }
            else
                prior = string.IsNullOrWhiteSpace(line) ? null : line;
        }
        return anchors;

        void AddHeading(string headingText)
        {
            var title = Regex.Replace(headingText.Trim(), @"[ \t]+#+[ \t]*$", "");
            var slug = Slug(title);
            if (slug.Length == 0) return;
            duplicateCounts.TryGetValue(slug, out var number);
            duplicateCounts[slug] = number + 1;
            anchors.Add(number == 0 ? slug : $"{slug}-{number}");
        }
    }

    internal static string Slug(string title)
    {
        title = MarkdownLinkLabel.Replace(title, "${label}");
        title = InlineCode.Replace(title, "${value}");
        title = HtmlTag.Replace(title, "");
        var result = new StringBuilder();
        foreach (var ch in title.ToLowerInvariant())
        {
            if (char.IsWhiteSpace(ch)) result.Append('-');
            else if (char.IsLetterOrDigit(ch) || ch is '-' or '_'
                || CharUnicodeInfo.GetUnicodeCategory(ch) is UnicodeCategory.NonSpacingMark
                    or UnicodeCategory.SpacingCombiningMark)
                result.Append(ch);
        }
        return result.ToString();
    }

    internal static (string Path, string? Fragment) SplitLocalTarget(string target)
    {
        // Supports <path with spaces.md> and an optional link title after the URL.
        if (target.StartsWith('<'))
        {
            var end = target.IndexOf('>');
            if (end > 0) target = target[1..end];
        }
        else
        {
            var whitespace = target.IndexOfAny([' ', '\t']);
            if (whitespace >= 0) target = target[..whitespace];
        }
        var hash = target.IndexOf('#');
        var path = hash < 0 ? target : target[..hash];
        var fragment = hash < 0 ? null : Uri.UnescapeDataString(target[(hash + 1)..]);
        return (Uri.UnescapeDataString(path), fragment);
    }

    private static IEnumerable<string> VisibleLines(string markdown)
    {
        char fence = '\0';
        int width = 0;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var indent = line.Length - line.TrimStart(' ').Length;
            var trimmed = line.TrimStart(' ');
            if (indent <= 3 && trimmed.Length >= 3 && (trimmed[0] is '`' or '~'))
            {
                var marker = trimmed[0];
                var count = 0;
                while (count < trimmed.Length && trimmed[count] == marker) count++;
                if (count >= 3)
                {
                    if (fence == '\0')
                    {
                        fence = marker;
                        width = count;
                        continue;
                    }
                    if (fence == marker && count >= width && string.IsNullOrWhiteSpace(trimmed[count..]))
                    {
                        fence = '\0';
                        width = 0;
                        continue;
                    }
                }
            }
            if (fence == '\0') yield return line;
        }
    }
}
