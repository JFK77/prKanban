using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Models;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Administration of user accounts.</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize]
public sealed class AdminUsersController(UserManager<ApplicationUser> users, IAdminUserService adminUsers) : ControllerBase
{
    private async Task<ApplicationUser?> CurrentAdminAsync()
    {
        var current = await users.GetUserAsync(User);
        return current is not null && await users.IsInRoleAsync(current, "Admin") ? current : null;
    }

    /// <summary>Lists all accounts.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (await CurrentAdminAsync() is null) return StatusCode(403);
        return Ok(await adminUsers.GetAllAsync(cancellationToken));
    }

    /// <summary>Returns one account.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        if (await CurrentAdminAsync() is null) return StatusCode(403);
        var account = await adminUsers.GetAsync(id, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }

    /// <summary>Creates an account.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var current = await CurrentAdminAsync();
        if (current is null) return StatusCode(403);
        var result = await adminUsers.CreateAsync(current.Id, request, cancellationToken);
        return result.User is null
            ? Problem(statusCode: result.StatusCode, detail: result.Error)
            : CreatedAtAction(nameof(Get), new { id = result.User.Id }, result.User);
    }

    /// <summary>Updates an account without deleting its tasks.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var current = await CurrentAdminAsync();
        if (current is null) return StatusCode(403);
        var result = await adminUsers.UpdateAsync(current.Id, id, request, cancellationToken);
        return result.User is null
            ? Problem(statusCode: result.StatusCode, detail: result.Error)
            : Ok(result.User);
    }
}
