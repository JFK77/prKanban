using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Models;
using PrKanban.Migrations;
using PrKanban.Services;
using System.Text.RegularExpressions;

namespace PrKanban.Controllers;

/// <summary>Administration of translation keys and texts.</summary>
[ApiController]
[Route("api/v1/admin/translations")]
[Authorize]
public sealed class AdminTranslationsController(UserManager<ApplicationUser> users, ITranslationService translations) : ControllerBase
{
    private async Task<bool> IsAdminAsync()
    {
        var user = await users.GetUserAsync(User);
        return user is not null && await users.IsInRoleAsync(user, "Admin");
    }

    private static bool HasRequiredPlaceholders(string key, TranslationRequest request)
    {
        var defaults = SeedUiTranslations.Entries.FirstOrDefault(entry => entry.Key == key);
        if (defaults.Key is null) return true;
        static HashSet<string> Placeholders(string value) =>
            Regex.Matches(value, @"\{[A-Za-z][A-Za-z0-9]*\}")
                .Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
        var required = Placeholders(defaults.German);
        return required.SetEquals(Placeholders(request.German)) &&
               required.SetEquals(Placeholders(request.English));
    }

    /// <summary>Lists all translations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync()) return StatusCode(403);
        return Ok(await translations.GetAllAsync(cancellationToken));
    }

    /// <summary>Returns a translation key.</summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync()) return StatusCode(403);
        var result = await translations.GetAsync(key, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a translation key.</summary>
    [HttpPost("{key}")]
    public async Task<IActionResult> Create(string key, TranslationRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync()) return StatusCode(403);
        if (key.Length > 120 || !Regex.IsMatch(key, @"^[A-Za-z0-9][A-Za-z0-9_.-]*$"))
            return Problem(statusCode: 400, detail: "Invalid translation key.");
        if (!HasRequiredPlaceholders(key, request))
            return Problem(statusCode: 400, detail: "Translation placeholders must match the built-in text in both languages.");
        if (!await translations.CreateAsync(key, request, cancellationToken))
            return Problem(statusCode: 409, detail: "Translation key already exists.");
        var result = await translations.GetAsync(key, cancellationToken);
        return CreatedAtAction(nameof(Get), new { key }, result);
    }

    /// <summary>Updates an existing translation.</summary>
    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, TranslationRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync()) return StatusCode(403);
        if (!HasRequiredPlaceholders(key, request))
            return Problem(statusCode: 400, detail: "Translation placeholders must match the built-in text in both languages.");
        var result = await translations.UpdateAsync(key, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Deletes an existing translation.</summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key, CancellationToken cancellationToken)
    {
        if (!await IsAdminAsync()) return StatusCode(403);
        return await translations.DeleteAsync(key, cancellationToken) ? NoContent() : NotFound();
    }
}
