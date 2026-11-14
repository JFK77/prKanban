using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

namespace PrKanban.Services;

public sealed class AdminUserService(UserManager<ApplicationUser> users, PrKanbanContext database) : IAdminUserService
{
    public async Task<IReadOnlyList<AdminUserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var accounts = await users.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var result = new List<AdminUserResponse>(accounts.Count);
        foreach (var account in accounts)
            result.Add(await ToResponseAsync(account));
        return result;
    }

    public async Task<AdminUserResponse?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var account = await users.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        return account is null ? null : await ToResponseAsync(account);
    }

    public async Task<AdminUserWriteResult> CreateAsync(string actorId, CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (!await IsActiveAdminAsync(actorId))
            return new(403, "Administrator access is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return new(400, "Name is required.");

        var email = request.Email.Trim();
        var account = new ApplicationUser { Email = email, UserName = email, FullName = request.Name.Trim() };
        var result = await users.CreateAsync(account, request.Password);
        if (!result.Succeeded)
            return IdentityFailure(result);

        result = await users.AddToRoleAsync(account, "User");
        if (!result.Succeeded)
            return IdentityFailure(result);
        if (request.Role == "Admin")
        {
            result = await users.AddToRoleAsync(account, "Admin");
            if (!result.Succeeded)
                return IdentityFailure(result);
        }

        await transaction.CommitAsync(cancellationToken);
        return new(201, User: await ToResponseAsync(account));
    }

    public async Task<AdminUserWriteResult> UpdateAsync(string actorId, string id, UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (!await IsActiveAdminAsync(actorId))
            return new(403, "Administrator access is required.");
        var account = await users.FindByIdAsync(id);
        if (account is null)
            return new(404, "Account not found.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return new(400, "Name is required.");
        if (id == actorId && (!request.Active || request.Role != "Admin"))
            return new(409, "You cannot deactivate your own account or remove your own administrator role.");

        var email = request.Email.Trim();
        if (!string.Equals(account.Email, email, StringComparison.Ordinal))
            account.EmailConfirmed = false;
        account.Email = email;
        account.UserName = email;
        account.FullName = request.Name.Trim();
        var result = await users.UpdateAsync(account);
        if (!result.Succeeded)
            return IdentityFailure(result);

        var roles = await users.GetRolesAsync(account);
        if (!roles.Contains("User"))
        {
            result = await users.AddToRoleAsync(account, "User");
            if (!result.Succeeded)
                return IdentityFailure(result);
        }
        if (request.Role == "Admin" && !roles.Contains("Admin"))
            result = await users.AddToRoleAsync(account, "Admin");
        else if (request.Role == "User" && roles.Contains("Admin"))
            result = await users.RemoveFromRoleAsync(account, "Admin");
        if (!result.Succeeded)
            return IdentityFailure(result);

        var active = await AccountStatus.IsActiveAsync(users, account);
        if (active && !request.Active)
            result = await users.AddClaimAsync(account, AccountStatus.DisabledClaim());
        else if (!active && request.Active)
            result = await users.RemoveClaimAsync(account, AccountStatus.DisabledClaim());
        if (!result.Succeeded)
            return IdentityFailure(result);

        await transaction.CommitAsync(cancellationToken);
        return new(200, User: await ToResponseAsync(account));
    }

    private async Task<bool> IsActiveAdminAsync(string id)
    {
        var actor = await users.FindByIdAsync(id);
        return actor is not null && await AccountStatus.IsActiveAsync(users, actor) && await users.IsInRoleAsync(actor, "Admin");
    }

    private async Task<AdminUserResponse> ToResponseAsync(ApplicationUser user)
    {
        var role = await users.IsInRoleAsync(user, "Admin") ? "Admin" : "User";
        return new(user.Id, user.Email ?? "", user.FullName ?? "", role, await AccountStatus.IsActiveAsync(users, user));
    }

    private static AdminUserWriteResult IdentityFailure(IdentityResult result) =>
        new(400, string.Join(" ", result.Errors.Select(error => error.Description)));
}
