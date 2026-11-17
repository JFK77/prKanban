using Ganss.Xss;

namespace PrKanban.Services;

public static class TaskDescriptionSanitizer
{
    public static string Sanitize(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(
        [
            "a", "b", "blockquote", "br", "div", "em", "h2", "h3",
            "i", "img", "li", "ol", "p", "strong", "u", "ul"
        ]);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["alt", "href", "src", "title", "width"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        return sanitizer.Sanitize(description);
    }
}
