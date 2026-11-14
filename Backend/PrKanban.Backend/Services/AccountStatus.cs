using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PrKanban.Models;

namespace PrKanban.Services;

public static class AccountStatus
{
    public const string DisabledClaimType = "prkanban:disabled";

    public static async Task<bool> IsActiveAsync(UserManager<ApplicationUser> users, ApplicationUser user)
    {
        var claims = await users.GetClaimsAsync(user);
        return !claims.Any(claim => claim.Type == DisabledClaimType && claim.Value == "true");
    }

    public static Claim DisabledClaim() => new(DisabledClaimType, "true");
}
