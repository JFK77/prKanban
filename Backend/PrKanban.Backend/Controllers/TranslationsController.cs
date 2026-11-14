using Microsoft.AspNetCore.Mvc;
using PrKanban.Migrations;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Provides the application translation catalogue.</summary>
[ApiController]
[Route("api/v1/translations")]
public sealed class TranslationsController(ITranslationService translations) : ControllerBase
{
    /// <summary>Returns the current texts with defaults for removed built-in keys.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IReadOnlyList<TranslationResponse>>> Get(CancellationToken cancellationToken)
    {
        var catalog = SeedUiTranslations.Entries.ToDictionary(
            item => item.Key,
            item => new TranslationResponse(item.Key, item.Category, item.German, item.English));
        catalog["admin.translationDescription"] = new("admin.translationDescription", "Administration",
            AddLanguageHelp.German, AddLanguageHelp.English);
        catalog["board.noTasks"] = new("board.noTasks", "Board",
            AddLanguageHelp.EmptyBoardGerman, AddLanguageHelp.EmptyBoardEnglish);
        catalog["auth.accountLocked"] = new("auth.accountLocked", "Anmeldung",
            AddLanguageHelp.LockedGerman, AddLanguageHelp.LockedEnglish);
        catalog["auth.accountInactive"] = new("auth.accountInactive", "Anmeldung",
            AddLanguageHelp.InactiveGerman, AddLanguageHelp.InactiveEnglish);
        foreach (var item in await translations.GetAllAsync(cancellationToken))
            catalog[item.Key] = item;
        return Ok(catalog.Values.ToList());
    }
}
