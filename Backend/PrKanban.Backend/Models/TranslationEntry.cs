namespace PrKanban.Models;

public sealed class TranslationEntry
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string German { get; set; } = string.Empty;
    public string English { get; set; } = string.Empty;
}
