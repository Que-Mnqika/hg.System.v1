using HGTSWebApi.Data;
using HGTSWebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Helper function moved to the top - FIXED position
static bool IsPostgresConnection(string? connectionString)
{
    return !string.IsNullOrWhiteSpace(connectionString) &&
        (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
         connectionString.Contains("5432", StringComparison.OrdinalIgnoreCase));
}

static bool HasAnyDashboardCategory(ClaimsPrincipal user, params string[] allowedCategories)
{
    var categories = user.Claims
        .Where(claim => string.Equals(claim.Type, "UserCategory", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(claim.Type, "userCategory", StringComparison.OrdinalIgnoreCase) ||
                        claim.Type == ClaimTypes.Role)
        .Select(claim => claim.Value);

    return categories.Any(category => allowedCategories.Any(allowed =>
        string.Equals(category, allowed, StringComparison.OrdinalIgnoreCase)));
}

// Add DbContext
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (IsPostgresConnection(defaultConnection))
    {
        options.UseNpgsql(defaultConnection);
        return;
    }

    //options.UseSqlServer(defaultConnection);
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEverything",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "1Q2W3E4R5T6Y7U8I9O0PAZSXDCFVGBHN")),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ================================================================
// AUTHORIZATION POLICIES FOR ROLE-BASED ACCESS
// ================================================================
builder.Services.AddAuthorization(options =>
{
    // System Admin can do ANYTHING - full access to all endpoints and all data
    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireAssertion(context =>
            HasAnyDashboardCategory(context.User, "SYSTEM_ADMIN")));

    // Admin or higher (includes System Admin)
    options.AddPolicy("AdminOrHigher", policy =>
        policy.RequireAssertion(context =>
            HasAnyDashboardCategory(context.User, "SYSTEM_ADMIN", "ADMIN", "Admin")));

    // Backwards-compatible alias used by existing controllers
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
            HasAnyDashboardCategory(context.User, "SYSTEM_ADMIN", "ADMIN", "Admin")));

    // Dispatcher role
    options.AddPolicy("DispatcherOnly", policy =>
        policy.RequireAssertion(context =>
            HasAnyDashboardCategory(context.User, "SYSTEM_ADMIN", "ADMIN", "Admin", "DISPATCHER", "Dispatcher")));

    options.AddPolicy("Operations", policy =>
        policy.RequireAssertion(context =>
            HasAnyDashboardCategory(context.User, "SYSTEM_ADMIN", "ADMIN", "Admin", "OPERATIONS", "Operations", "DISPATCHER", "Dispatcher")));

    // Any authenticated user
    options.AddPolicy("AuthenticatedUser", policy =>
        policy.RequireAssertion(context =>
            context.User.Identity.IsAuthenticated));
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICredentialService, CredentialService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITripSwapService, TripSwapService>();
builder.Services.AddHostedService<TripStatusService>();

var app = builder.Build();

// Configure URLs
app.Urls.Clear();
app.Urls.Add("http://localhost:5277");
app.Urls.Add("http://0.0.0.0:5277");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HGTS API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowEverything");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Root endpoint
app.MapGet("/", () => "HGTS API is running!");

// Map controllers
app.MapControllers();

// ================================================================
// NO DATABASE SEEDING - DATA ALREADY EXISTS
// ================================================================
Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         HGTS API - BUS BOARDING SYSTEM                      ║");
Console.WriteLine("║         Status: RUNNING                                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine($"📍 Local: http://localhost:5277");
Console.WriteLine($"📍 Network: http://0.0.0.0:5277");
Console.WriteLine($"📋 Swagger: http://localhost:5277/swagger");
Console.WriteLine("");

app.Run();
