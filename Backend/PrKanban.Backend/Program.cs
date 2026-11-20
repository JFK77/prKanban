using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PrKanban.Data;
using PrKanban.Models;
using PrKanban.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDatabaseInformationService, DatabaseInformationService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<IUserLanguageService, UserLanguageService>();
builder.Services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
builder.Services.AddScoped<ITaskMoveService, TaskMoveService>();
builder.Services.AddScoped<IProjectBoardService, ProjectBoardService>();
builder.Services.AddScoped<IProjectHierarchyService, ProjectHierarchyService>();

// 1. Add DbContext
builder.Services.AddDbContext<PrKanbanContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
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
    
    // User options
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PrKanbanContext>()
.AddDefaultTokenProviders();

// 3. Add Authentication (Cookie-based)
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return System.Threading.Tasks.Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return System.Threading.Tasks.Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return System.Threading.Tasks.Task.CompletedTask;
    };
});

// 4. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        // Allow localhost on all common dev ports
        policy.WithOrigins(
            "http://localhost:3000",  // Frontend dev server (Vite, CRA)
            "http://localhost:5000",  // Backend API (this server)
            "http://localhost:5173",  // Vite default
            "http://127.0.0.1:3000",
            "http://127.0.0.1:5000",
            "http://127.0.0.1:5173",
            "file://",                // Local file:// protocol for desktop apps
            "http://localhost"        // Without port
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();       // Required for httpOnly cookies
    });
});

// 5. Add Controllers
builder.Services.AddControllers();

// 6. Add Swagger for API docs
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-create database and initialize roles
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PrKanbanContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Attempting database initialization...");
        
        // Migrations create the database and keep the schema history consistent.
        dbContext.Database.Migrate();
        _ = dbContext.AppTranslations.AsNoTracking().Take(1).ToList();
        logger.LogInformation("✅ Database migrations applied");
        
        // Create default roles
        string[] roleNames = { "Admin", "User" };
        IdentityResult roleResult;
        
        foreach (var roleName in roleNames)
        {
            var roleExist = roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult();
            if (!roleExist)
            {
                roleResult = roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                if (roleResult.Succeeded)
                {
                    logger.LogInformation($"✅ Created role: {roleName}");
                }
            }
            else
            {
                logger.LogInformation($"ℹ️  Role already exists: {roleName}");
            }
        }
    }
}
catch (Exception ex)
{
    app.Logger.LogError($"❌ Database initialization failed: {ex.Message}");
    app.Logger.LogError($"Stack trace: {ex.StackTrace}");
    throw;
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable static files and serve default document (index.html)
var staticFileOptions = new StaticFileOptions();
app.UseStaticFiles(staticFileOptions);

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.UseCors("Development");
app.UseRouting();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        await using var statusScope = context.RequestServices.CreateAsyncScope();
        var users = statusScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.GetUserAsync(context.User);
        if (user is null || !await AccountStatus.IsActiveAsync(users, user))
        {
            await context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>().SignOutAsync();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
    await next();
});
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
