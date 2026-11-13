using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Models;
using System.ComponentModel.DataAnnotations;

namespace PrKanban.Controllers
{
    /// <summary>
    /// Authentication endpoints (register, login, logout, user info)
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/v1/auth/register - Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName ?? request.Email.Split('@')[0],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                _logger.LogInformation($"User {user.Email} registered successfully");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = user.Id,
                        email = user.Email,
                        message = "User registered successfully"
                    }
                });
            }

            return BadRequest(new
            {
                success = false,
                error = new
                {
                    code = "REGISTRATION_FAILED",
                    message = string.Join(", ", result.Errors.Select(e => e.Description))
                }
            });
        }

        /// <summary>
        /// POST /api/v1/auth/login - Login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning($"Login failed: user {request.Email} not found");
                return Unauthorized(new
                {
                    success = false,
                    error = new
                    {
                        code = "INVALID_CREDENTIALS",
                        message = "Email or password is incorrect"
                    }
                });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(403, new
                {
                    success = false,
                    error = new
                    {
                        code = "ACCOUNT_LOCKED",
                        message = "Account is temporarily locked"
                    }
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                request.Email,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation($"User {request.Email} logged in successfully");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = user.Id,
                        email = user.Email,
                        name = user.FullName,
                        message = "Logged in successfully"
                    }
                });
            }

            _logger.LogWarning($"Login failed for {request.Email}: invalid password");
            return Unauthorized(new
            {
                success = false,
                error = new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "Email or password is incorrect"
                }
            });
        }

        /// <summary>
        /// POST /api/v1/auth/logout - Logout current user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation($"User {User.Identity?.Name} logged out");

            return Ok(new
            {
                success = true,
                data = new { message = "Logged out successfully" }
            });
        }

        /// <summary>
        /// GET /api/v1/auth/user - Get current authenticated user
        /// </summary>
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = user.Id,
                    email = user.Email,
                    name = user.FullName,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                }
            });
        }
    }

    // Request DTOs
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(256)]
        public string? FullName { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
