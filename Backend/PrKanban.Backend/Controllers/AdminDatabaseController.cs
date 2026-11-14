using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Models;
using PrKanban.Services;

namespace PrKanban.Controllers;

/// <summary>Read-only database information for administrators.</summary>
[ApiController]
[Route("api/v1/admin/database")]
[Authorize]
public sealed class AdminDatabaseController(
    UserManager<ApplicationUser> users,
    IDatabaseInformationService databaseInformation) : ControllerBase
{
    /// <summary>Returns the database provider and location.</summary>
    [HttpGet]
    [ProducesResponseType<DatabaseInformationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(User);
        if (user is null)
            return Unauthorized();
        if (!await users.IsInRoleAsync(user, "Admin"))
            return StatusCode(StatusCodes.Status403Forbidden);

        return Ok(databaseInformation.GetInformation());
    }
}
