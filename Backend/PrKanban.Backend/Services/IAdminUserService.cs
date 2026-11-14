using System.ComponentModel.DataAnnotations;

namespace PrKanban.Services;

/// <summary>Account data visible to administrators.</summary>
public sealed record AdminUserResponse(string Id, string Email, string Name, string Role, bool Active);

/// <summary>Details for creating an account.</summary>
public sealed record CreateAdminUserRequest
{
    [Required, EmailAddress, StringLength(256)] public required string Email { get; init; }
    [Required, StringLength(256)] public required string Name { get; init; }
    [Required, StringLength(100, MinimumLength = 8)] public required string Password { get; init; }
    [Required, RegularExpression("^(Admin|User)$")] public required string Role { get; init; }
}

/// <summary>Changes to an existing account.</summary>
public sealed record UpdateAdminUserRequest
{
    [Required, EmailAddress, StringLength(256)] public required string Email { get; init; }
    [Required, StringLength(256)] public required string Name { get; init; }
    [Required, RegularExpression("^(Admin|User)$")] public required string Role { get; init; }
    [Required] public required bool Active { get; init; }
}

public sealed record AdminUserWriteResult(int StatusCode, string? Error = null, AdminUserResponse? User = null);

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminUserResponse?> GetAsync(string id, CancellationToken cancellationToken);
    Task<AdminUserWriteResult> CreateAsync(string actorId, CreateAdminUserRequest request, CancellationToken cancellationToken);
    Task<AdminUserWriteResult> UpdateAsync(string actorId, string id, UpdateAdminUserRequest request, CancellationToken cancellationToken);
}
