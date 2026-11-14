using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public interface IUserLanguageService
{
    Task<string> GetAsync(ApplicationUser user);
    Task<IdentityResult> SetAsync(ApplicationUser user, string language);
}

public sealed class UserLanguageService(UserManager<ApplicationUser> users, PrKanbanContext database) : IUserLanguageService
{
    private const string ClaimType = "prkanban:language";

    public async Task<string> GetAsync(ApplicationUser user)
    {
        var claims = (await users.GetClaimsAsync(user)).Where(c => c.Type == ClaimType).ToList();
        if (claims.Count > 1 || claims.Any(c => c.Value is not ("de" or "en")))
            throw new InvalidOperationException("The user has invalid language preferences.");
        return claims.Count == 0 ? "de" : claims[0].Value;
    }

    public async Task<IdentityResult> SetAsync(ApplicationUser user, string language)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        var claims = (await users.GetClaimsAsync(user)).Where(c => c.Type == ClaimType).ToList();
        if (claims.Count > 1)
            throw new InvalidOperationException("The user has multiple language preferences.");
        var result = claims.Count == 0
            ? await users.AddClaimAsync(user, new Claim(ClaimType, language))
            : claims[0].Value == language
                ? IdentityResult.Success
                : await users.ReplaceClaimAsync(user, claims[0], new Claim(ClaimType, language));
        if (result.Succeeded)
            await transaction.CommitAsync();
        return result;
    }
}
