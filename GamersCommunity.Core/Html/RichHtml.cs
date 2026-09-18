using System.Text.RegularExpressions;
using Ganss.Xss;

namespace GamersCommunity.Core.Html;

/// <summary>
/// Sanitizes user-authored HTML for rich-text fields (guild wall, presentations, etc.).
/// </summary>
public static class RichHtml
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    private static readonly Regex StripTags = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Returns null when input is null, whitespace, or empty after sanitization.</summary>
    public static string? SanitizeOptional(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var sanitized = Sanitizer.Sanitize(html.Trim());
        return IsEffectivelyEmpty(sanitized) ? null : sanitized;
    }

    /// <summary>Sanitizes required content; returns trimmed HTML or null if blank after sanitization.</summary>
    public static string? SanitizeRequired(string? html) => SanitizeOptional(html);

    public static bool IsEffectivelyEmpty(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return true;

        var text = ToPlainText(html);
        return string.IsNullOrWhiteSpace(text);
    }

    /// <summary>Plain text for search snippets, notifications, and length hints.</summary>
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var decoded = StripTags.Replace(html, " ");
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
                 {
                     "p", "br", "strong", "b", "em", "i", "u", "s", "ul", "ol", "li", "span", "a", "h2", "h3",
                 })
            sanitizer.AllowedTags.Add(tag);

        sanitizer.AllowedAttributes.Clear();
        foreach (var attr in new[] { "style", "href", "target", "rel", "class" })
            sanitizer.AllowedAttributes.Add(attr);

        sanitizer.AllowedCssProperties.Clear();
        foreach (var prop in new[] { "color", "background-color", "text-align" })
            sanitizer.AllowedCssProperties.Add(prop);

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");

        sanitizer.AllowedAtRules.Clear();
        sanitizer.UriAttributes.Add("href");

        return sanitizer;
    }
}
