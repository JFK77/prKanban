using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public sealed class TranslationService(PrKanbanContext database) : ITranslationService
{
    public async Task<IReadOnlyList<TranslationResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await database.AppTranslations.AsNoTracking().OrderBy(x => x.Key)
            .Select(x => new TranslationResponse(x.Key, x.Category, x.German, x.English))
            .ToListAsync(cancellationToken);

    public async Task<TranslationResponse?> GetAsync(string key, CancellationToken cancellationToken) =>
        await database.AppTranslations.AsNoTracking().Where(x => x.Key == key)
            .Select(x => new TranslationResponse(x.Key, x.Category, x.German, x.English))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> CreateAsync(string key, TranslationRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.AppTranslations.AnyAsync(x => x.Key == key, cancellationToken))
            return false;
        database.AppTranslations.Add(new TranslationEntry
        {
            Key = key, Category = request.Category.Trim(),
            German = request.German.Trim(), English = request.English.Trim()
        });
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<TranslationResponse?> UpdateAsync(string key, TranslationRequest request, CancellationToken cancellationToken)
    {
        var entry = await database.AppTranslations.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (entry is null) return null;
        entry.Category = request.Category.Trim();
        entry.German = request.German.Trim();
        entry.English = request.English.Trim();
        await database.SaveChangesAsync(cancellationToken);
        return new(entry.Key, entry.Category, entry.German, entry.English);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var entry = await database.AppTranslations.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (entry is null) return false;
        database.AppTranslations.Remove(entry);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }
}
