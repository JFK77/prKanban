using System.ComponentModel.DataAnnotations;

namespace PrKanban.Services;

/// <summary>One translation key and its German and English texts.</summary>
public sealed record TranslationResponse(string Key, string Category, string German, string English);

/// <summary>Values for a translation.</summary>
public sealed record TranslationRequest
{
    [Required, StringLength(100)] public required string Category { get; init; }
    [Required, StringLength(2048)] public required string German { get; init; }
    [Required, StringLength(2048)] public required string English { get; init; }
}

public interface ITranslationService
{
    Task<IReadOnlyList<TranslationResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<TranslationResponse?> GetAsync(string key, CancellationToken cancellationToken);
    Task<bool> CreateAsync(string key, TranslationRequest request, CancellationToken cancellationToken);
    Task<TranslationResponse?> UpdateAsync(string key, TranslationRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken);
}
