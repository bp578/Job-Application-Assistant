using System.Net;
using System.Text.RegularExpressions;

namespace JobAssistant.Api.Ingestion;

/// <summary>Turns job-board HTML descriptions into plain text for storage and AI prompts.</summary>
public static partial class HtmlText
{
    public static string? ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // Greenhouse returns HTML that is itself HTML-escaped ("&lt;p&gt;"), so decode before stripping tags.
        var decoded = WebUtility.HtmlDecode(html);
        var withLineBreaks = BlockEndTag().Replace(decoded, "\n");
        var withoutTags = AnyTag().Replace(withLineBreaks, " ");

        // Decode again for entities that were inside the markup (e.g. "&amp;amp;" -> "&amp;" -> "&").
        var text = WebUtility.HtmlDecode(withoutTags);
        text = HorizontalWhitespace().Replace(text, " ");
        text = SpaceBeforePunctuation().Replace(text, "$1");
        text = LineBreakWithPadding().Replace(text, "\n");

        return text.Trim();
    }

    [GeneratedRegex(@"<\s*(br|/p|/div|/li|/h[1-6])\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEndTag();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t ]+")]
    private static partial Regex HorizontalWhitespace();

    // Removing inline tags like </strong> can leave "APIs ." — pull punctuation back onto the word.
    [GeneratedRegex(@" +([.,;:!?)])")]
    private static partial Regex SpaceBeforePunctuation();

    [GeneratedRegex(@"\s*\n\s*")]
    private static partial Regex LineBreakWithPadding();
}
