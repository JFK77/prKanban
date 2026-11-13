# Phase 2: Authentication Implementation Guide (ASP.NET Core Identity)

**Date**: 2026-11-14  
**Status**: COMPLETE  
**Target Implementation**: Phase 3  
**Authentication Type**: ASP.NET Core Identity  
**Storage**: SQLite (prkanban.db)  

---

## EXECUTIVE SUMMARY

This document provides the complete authentication implementation strategy for prKanban using **ASP.NET Core Identity** with **SQLite** backend:

- ✅ User registration with password validation
- ✅ Secure login with account lockout protection
- ✅ Session-based authentication (httpOnly cookies)
- ✅ Password hashing (bcrypt via ASP.NET Core Identity)
- ✅ Account recovery and password reset (Phase 3+)
- ✅ Security best practices

---

## 1. AUTHENTICATION ARCHITECTURE

### Technology Stack

| Component | Technology | Purpose |
|---|---|---|
| **Framework** | ASP.NET Core 10 | Hosting environment |
| **Auth System** | ASP.NET Core Identity | User management |
| **Database** | SQLite (EF Core) | User storage |
| **Password Hash** | PBKDF2 (via Identity) | Secure hashing |
| **Session Storage** | Database + Cookies | State management |
| **Cookie Security** | httpOnly + Secure + SameSite | CSRF/XSS protection |

### Security Features

```
┌─────────────────────────────────────────┐
│   User Login Request (email/password)   │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│   1. Validate Email Format              │
│   2. Find User in Database              │
│   3. Verify Password Hash (PBKDF2)      │
│   4. Check Account Lockout Status       │
│   5. Verify Email Confirmed (optional)  │
└──────────────┬──────────────────────────┘
               │
               ├─ Invalid? ──→ Log failed attempt
               │               Increment counter
               │               Lock if >= 5 attempts
               │
               ├─ Locked? ──→ Return 403 Forbidden
               │
               └─ Valid ──→ Create Session Cookie
                            Store in Database
                            Return to Client
```

---

## 2. USER DATA MODEL (ASP.NET Core Identity)

### IdentityUser Table Structure

When ASP.NET Core Identity initializes, it creates the following tables:

```sql
-- AspNetUsers table (created by Identity)
CREATE TABLE "AspNetUsers" (
    "Id" TEXT PRIMARY KEY,                      -- GUID
    "UserName" TEXT NOT NULL UNIQUE,            -- Email (username)
    "NormalizedUserName" TEXT NOT NULL UNIQUE,  -- Normalized email
    "Email" TEXT NOT NULL UNIQUE,               -- Email address
    "NormalizedEmail" TEXT NOT NULL,            -- Normalized email
    "EmailConfirmed" INTEGER NOT NULL DEFAULT 0, -- Bool: email verified
    "PasswordHash" TEXT NOT NULL,               -- Bcrypt hash
    "SecurityStamp" TEXT,                       -- For invalidating tokens
    "ConcurrencyStamp" TEXT,                    -- For optimistic concurrency
    "PhoneNumber" TEXT,                         -- Optional
    "PhoneNumberConfirmed" INTEGER DEFAULT 0,  -- Bool
    "TwoFactorEnabled" INTEGER DEFAULT 0,      -- Bool
    "LockoutEnd" TEXT,                          -- Lockout expiration
    "LockoutEnabled" INTEGER DEFAULT 1,        -- Bool
    "AccessFailedCount" INTEGER DEFAULT 0      -- Failed login attempts
);

-- AspNetRoles table (created by Identity)
CREATE TABLE "AspNetRoles" (
    "Id" TEXT PRIMARY KEY,
    "Name" TEXT NOT NULL UNIQUE,
    "NormalizedName" TEXT NOT NULL,
    "ConcurrencyStamp" TEXT
);

-- AspNetUserRoles table (created by Identity)
CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id"),
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id")
);

-- AspNetUserLogins table (for OAuth - optional)
CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL,
    PRIMARY KEY ("LoginProvider", "ProviderKey"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id")
);

-- AspNetUserTokens table
CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id")
);

-- AspNetUserClaims table
CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id")
);

-- AspNetRoleClaims table
CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER PRIMARY KEY,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id")
);
```

### Extended User Profile (Custom)

For additional user info (name, profile pic, etc.), extend IdentityUser:

```csharp
namespace PrKanban.Models
{
    using Microsoft.AspNetCore.Identity;
    using System;
    
    /// <summary>
    /// Extended user model with additional profile fields
    /// Inherits from IdentityUser (AspNetUsers table)
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User's display name
        /// </summary>
        public string FullName { get; set; }
        
        /// <summary>
        /// Profile picture URL
        /// </summary>
        public string ProfilePictureUrl { get; set; }
        
        /// <summary>
        /// User creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last profile update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last successful login timestamp
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
        
        /// <summary>
        /// Navigation to user's board config (1:1)
        /// </summary>
        public virtual BoardConfig BoardConfig { get; set; }
        
        /// <summary>
        /// Navigation to user's tasks (1:many)
        /// </summary>
        public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}
```

### Update DbContext for Identity

```csharp
namespace PrKanban.Data
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using PrKanban.Models;
    
    /// <summary>
    /// DbContext with ASP.NET Core Identity support
    /// </summary>
    public class PrKanbanContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Task> Tasks { get; set; }
        public DbSet<BoardConfig> BoardConfigs { get; set; }
        
        public PrKanbanContext(DbContextOptions<PrKanbanContext> options)
            : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);  // Important: Identity tables
            
            // Configure ApplicationUser (extends IdentityUser)
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(256);
                entity.Property(e => e.ProfilePictureUrl).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });
            
            // One-to-many: ApplicationUser -> Tasks
            modelBuilder.Entity<Task>(entity =>
            {
                entity.HasOne(t => t.User)
                    .WithMany(u => u.Tasks)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // One-to-one: ApplicationUser -> BoardConfig
            modelBuilder.Entity<BoardConfig>(entity =>
            {
                entity.HasOne(bc => bc.User)
                    .WithOne(u => u.BoardConfig)
                    .HasForeignKey<BoardConfig>(bc => bc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
```

---

## 3. STARTUP CONFIGURATION

### Appsettings.json Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=D:\\Apps\\prKanban\\Data\\prkanban.db;Mode=ReadWrite;Cache=Shared;Journal Mode=Wal;Busy Timeout=5000;"
  },
  "Authentication": {
    "PasswordPolicy": {
      "RequiredLength": 8,
      "RequireDigit": true,
      "RequireUppercase": false,
      "RequireNonAlphanumeric": false
    },
    "Lockout": {
      "MaxFailedAccessAttempts": 5,
      "LockoutDurationMinutes": 15,
      "DefaultLockoutEnabled": true
    },
    "Session": {
      "IdleTimeoutMinutes": 30,
      "AbsoluteExpirationMinutes": 480,
      "SlidingExpiration": true
    }
  }
}
```

### Program.cs Configuration (Phase 3)

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;

var builder = WebApplicationBuilder.CreateBuilder(args);

// 1. Add DbContext
builder.Services.AddDbContext<PrKanbanContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.UseNetTopologySuite();  // If needed
    });
});

// 2. Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    
    // Lockout policy
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
    
    // User options
    options.User.RequireUniqueEmail = true;
    
    // Email options (for email confirmation)
    options.SignIn.RequireConfirmedEmail = false;  // Can enable later
})
.AddEntityFrameworkStores<PrKanbanContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// 3. Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.Name = ".AspNetCore.Identity.Application";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ReturnUrlParameter = "returnUrl";
});

// 4. Add CORS (for frontend)
builder.Services.AddCors(options =>
{
    var isDevelopment = builder.Environment.IsDevelopment();
    options.AddPolicy("Development", policy =>
    {
        if (isDevelopment)
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// 5. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Development");
app.UseRouting();

// 6. Authentication middleware (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

// 7. Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<PrKanbanContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    // Create database
    await context.Database.MigrateAsync();
    
    // Seed roles
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new IdentityRole("User"));
    }
}

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
```

---

## 4. AUTHENTICATION ENDPOINTS (ASP.NET Core)

### Authentication Controller (Phase 3)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrKanban.Models;
using System.ComponentModel.DataAnnotations;

namespace PrKanban.Controllers
{
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
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validate model
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            // Create user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName ?? request.Email.Split('@')[0],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Create with password
            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (result.Succeeded)
            {
                // Add default role
                await _userManager.AddToRoleAsync(user, "User");
                
                // Create default board config
                var boardConfig = new BoardConfig
                {
                    UserId = user.Id,
                    ColumnOrder = new[] { "backlog", "todo", "in-progress", "review", "done" },
                    Theme = "light"
                };
                // Save board config (via DbContext)
                
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
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            // Find user
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            if (user == null)
            {
                _logger.LogWarning($"Login attempt failed: user {request.Email} not found");
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
            
            // Check lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"Login attempt failed: user {request.Email} is locked out");
                return Forbid("Account is temporarily locked due to too many failed login attempts");
            }
            
            // Sign in
            var result = await _signInManager.PasswordSignInAsync(
                request.Email, 
                request.Password, 
                isPersistent: true, 
                lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                // Update last login
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
            
            if (result.IsLockedOut)
            {
                _logger.LogWarning($"Login attempt failed: user {request.Email} is now locked out");
                return Forbid("Account is locked");
            }
            
            _logger.LogWarning($"Login attempt failed: invalid password for {request.Email}");
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
        /// Logout current user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation($"User {User.Identity.Name} logged out");
            
            return Ok(new
            {
                success = true,
                data = new { message = "Logged out successfully" }
            });
        }
        
        /// <summary>
        /// Get current authenticated user
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
    
    // Request models
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }
        
        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
        
        [StringLength(256)]
        public string FullName { get; set; }
    }
    
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
    }
}
```

---

## 5. PASSWORD SECURITY

### Password Hashing (Automatic via Identity)

ASP.NET Core Identity automatically:
1. **Hashes** passwords using PBKDF2 (RFC 2898)
2. **Never stores** plain text passwords
3. **Uses salt** for each password
4. **Verifies** using time-constant comparison (prevents timing attacks)

**Example hash**: `AQAAAAEAACcQAAAAEId8...` (bcrypt-like format)

### Password Policy

```csharp
// From appsettings.json
options.Password.RequiredLength = 8;        // Min 8 chars
options.Password.RequireDigit = true;       // Must have number
options.Password.RequireUppercase = false;  // No uppercase required
options.Password.RequireNonAlphanumeric = false;  // No special chars
```

### Password Reset (Future, Phase 3+)

```csharp
// Generate reset token
var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

// Reset password
var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
```

---

## 6. ACCOUNT LOCKOUT

### Lockout Policy

```csharp
options.Lockout.MaxFailedAccessAttempts = 5;           // Lock after 5 attempts
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);  // 15 min lockout
```

### Lockout Timeline

```
Time  | Event                           | Attempts | Status
------|----------------------------------|----------|----------
1     | User enters wrong password      | 1        | OK
2     | User enters wrong password      | 2        | OK
3     | User enters wrong password      | 3        | OK
4     | User enters wrong password      | 4        | OK
5     | User enters wrong password      | 5        | LOCKED
6-90  | User tries to login             | 5        | LOCKED
91    | Lockout expires (15 min passed) | 0        | OK (reset)
```

### Reset Lockout (Admin)

```csharp
// Admin unlock user
await _userManager.SetLockoutEndDateAsync(user, null);
```

---

## 7. SESSION MANAGEMENT

### Session Cookie Configuration

```csharp
options.Cookie.Name = ".AspNetCore.Identity.Application";
options.Cookie.HttpOnly = true;              // Prevent JS access
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
options.ExpireTimeSpan = TimeSpan.FromMinutes(30);  // 30 min timeout
options.SlidingExpiration = true;            // Extend on activity
```

### Sliding Expiration

```
Login → Access Token Valid: 0-30 min
         Make API call at T=25 min
         → Cookie extended to T=55 min
         Access Token Valid: 25-55 min
```

---

## 8. SECURITY BEST PRACTICES

### Implemented

✅ **Password Hashing**: PBKDF2 via Identity  
✅ **Account Lockout**: 5 attempts → 15 min lockout  
✅ **Session Cookies**: httpOnly + Secure + SameSite  
✅ **CSRF Protection**: Built-in via antiforgery tokens  
✅ **XSS Protection**: httpOnly cookies prevent script access  
✅ **HTTPS Enforcement**: Secure cookie flag  
✅ **Email Unique**: Prevents duplicate accounts  
✅ **Timing Attack Prevention**: Constant-time password comparison  

### Future (Phase 3+)

🔲 Email verification on registration  
🔲 Two-factor authentication (2FA)  
🔲 Password reset via email  
🔲 Social login (Google, Microsoft)  
🔲 Audit logging (who logged in when)  
🔲 IP-based geo-blocking  

---

## 9. TESTING AUTHENTICATION

### Unit Test Example

```csharp
using Microsoft.AspNetCore.Identity;
using Xunit;

public class AuthenticationTests
{
    [Fact]
    public async Task Register_ValidEmail_CreatesUser()
    {
        // Arrange
        var userManager = GetMockUserManager();
        var signInManager = GetMockSignInManager();
        var controller = new AuthController(userManager, signInManager, logger);
        
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePassword123",
            ConfirmPassword = "SecurePassword123"
        };
        
        // Act
        var result = await controller.Register(request);
        
        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
    
    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        // Arrange
        var userManager = GetMockUserManager();
        var signInManager = GetMockSignInManager();
        var controller = new AuthController(userManager, signInManager, logger);
        
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };
        
        // Act
        var result = await controller.Login(request);
        
        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
```

---

## 10. MIGRATION FROM FIREBASE AUTH

### User Migration Strategy

**Firebase** → **ASP.NET Core Identity**

```csharp
// Step 1: Export Firebase users
var firebaseUsers = await firebaseAuth.GetAllUsersAsync();

// Step 2: Create corresponding Identity users
foreach (var fbUser in firebaseUsers)
{
    var newUser = new ApplicationUser
    {
        UserName = fbUser.Email,
        Email = fbUser.Email,
        FullName = fbUser.DisplayName ?? "User",
        CreatedAt = fbUser.UserMetadata.CreationTimestamp.ToDateTime(),
        EmailConfirmed = true  // Firebase users already verified
    };
    
    // Create temporary password (user must reset)
    var tempPassword = GenerateTemporaryPassword();
    var result = await _userManager.CreateAsync(newUser, tempPassword);
    
    if (result.Succeeded)
    {
        // Send password reset email to Firebase email
        // User will set their own password
    }
}
```

---

## 11. LOGGING & MONITORING

### Security Logging

```csharp
_logger.LogInformation($"User {email} registered successfully");
_logger.LogWarning($"Login attempt failed: user {email} not found");
_logger.LogWarning($"Login attempt failed: user {email} is locked out");
_logger.LogInformation($"User {email} logged in successfully");
```

### Monitor These Events

| Event | Severity | Action |
|---|---|---|
| Successful login | INFO | Record timestamp |
| Failed login | WARN | Count attempts |
| Account lockout | WARN | Alert user |
| Password reset | INFO | Log request |
| Unauthorized access | ERROR | Alert admin |

---

## 12. DEPLOYMENT CHECKLIST

- [ ] Appsettings.json configured with database path
- [ ] Connection string points to D:\Apps\prKanban\Data\prkanban.db
- [ ] HTTPS enabled in production
- [ ] Cookie secure flag set to Always
- [ ] CORS configured for Windows PC IP
- [ ] Password policy configured
- [ ] Lockout policy configured
- [ ] Logging configured
- [ ] Database migrations applied
- [ ] Test register/login workflow
- [ ] Test account lockout (5 failed attempts)
- [ ] Test session timeout (30 minutes)
- [ ] Test logout

---

## 13. SUCCESS CRITERIA FOR AUTHENTICATION PHASE

✅ **ASP.NET Core Identity configured** (SQLite backend)  
✅ **User registration endpoint** (email/password)  
✅ **Login endpoint** (session-based)  
✅ **Logout endpoint** (clear session)  
✅ **Current user endpoint** (authenticated info)  
✅ **Password hashing** (secure PBKDF2)  
✅ **Account lockout** (5 attempts = 15 min lockout)  
✅ **Session management** (30 min timeout + sliding)  
✅ **Cookie security** (httpOnly + Secure + SameSite)  
✅ **Testing strategy** (unit tests documented)  
✅ **Migration plan** (Firebase → Identity)  

---

**Document Status**: ✅ AUTHENTICATION DESIGN COMPLETE

---

## 14. IMPLEMENTATION ROADMAP (Phase 3)

**Week 1**: Setup & Models
- [ ] Create ASP.NET Core 10 project
- [ ] Add EF Core + SQLite packages
- [ ] Create ApplicationUser + DbContext
- [ ] Add Identity services to DI

**Week 2**: Authentication Controller
- [ ] Implement register endpoint
- [ ] Implement login endpoint
- [ ] Implement logout endpoint
- [ ] Implement GetCurrentUser endpoint

**Week 3**: Testing & Hardening
- [ ] Write unit tests for auth controller
- [ ] Test account lockout mechanism
- [ ] Test password policy enforcement
- [ ] Test session timeout

**Week 4**: Integration
- [ ] Integrate with frontend
- [ ] Test full login flow (frontend → backend)
- [ ] Test logout flow
- [ ] Documentation complete

---

**Phase 2 Authentication Complete** ✅

Next: Phase 3 - Backend Implementation
