using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Event management API for organisers and participants."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /api/auth/login."
    });
    c.OperationFilter<RaceDay.Api.Swagger.AuthorizeCheckOperationFilter>();
});

var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase") ||
                          builder.Environment.IsEnvironment("Testing");
var inMemoryDatabaseName = builder.Configuration["InMemoryDatabaseName"] ?? "RaceDayDb";
var connection = builder.Configuration.GetConnectionString("RaceDay") ??
                 "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<RaceDayContext>(options =>
{
    if (useInMemoryDatabase)
    {
        options.UseInMemoryDatabase(inMemoryDatabaseName);
    }
    else
    {
        options.UseSqlServer(connection);
    }
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) && builder.Environment.IsEnvironment("Testing"))
{
    jwtKey = "TEST_ONLY_SIGNING_KEY_FOR_AUTOMATED_TESTS_2026";
}
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Configure Jwt:Key with a secret containing at least 32 characters.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaceDay",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "RaceDayClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<AzureBlobStorageService>();
builder.Services.AddScoped<LocalBlobStorageService>();
builder.Services.AddScoped<IBlobStorageService>(services =>
{
    // Resolve at request time so configuration supplied by the host (environment
    // variables, test factories, key vault providers) is always applied.
    var configuration = services.GetRequiredService<IConfiguration>();
    var provider = configuration["Media:Provider"] ?? "Local";
    if (provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
        return services.GetRequiredService<AzureBlobStorageService>();
    if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        return services.GetRequiredService<LocalBlobStorageService>();
    throw new InvalidOperationException("Media:Provider must be either 'Local' or 'Azure'.");
});

var app = builder.Build();
var mediaProvider = app.Configuration["Media:Provider"] ?? "Local";

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();

    if (useInMemoryDatabase)
    {
        await db.Database.EnsureCreatedAsync();
    }
    else if (builder.Configuration.GetValue("Database:AutoMigrate", false))
    {
        await MigrateWithRetryAsync(
            db,
            app.Logger,
            builder.Configuration.GetValue("Database:MigrateRetrySeconds", 5),
            builder.Configuration.GetValue("Database:MigrateMaxAttempts", 12));
    }

    await SeedRolesAsync(db, app.Logger);
}

if (app.Environment.IsDevelopment() ||
    app.Environment.IsEnvironment("Testing") ||
    builder.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (builder.Configuration.GetValue("Https:Redirect", true))
{
    app.UseHttpsRedirection();
}

if (mediaProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    var mediaRoot = MediaStoragePaths.ResolveRoot(builder.Configuration, app.Environment);
    Directory.CreateDirectory(mediaRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaRoot),
        RequestPath = MediaStoragePaths.RequestPath
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { name = "RaceDay API", swagger = "/swagger" }))
    .WithTags("Health")
    .WithSummary("API health and documentation entry point.");

app.MapGet("/health", async (RaceDayContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "ok", database = "connected", storage = mediaProvider })
            : Results.Json(new { status = "degraded", database = "unreachable", storage = mediaProvider }, statusCode: 503);
    }
    catch
    {
        return Results.Json(new { status = "degraded", database = "unreachable", storage = mediaProvider }, statusCode: 503);
    }
}).WithTags("Health").WithSummary("Readiness check for the API, database and media provider.");

app.Run();

static async Task MigrateWithRetryAsync(
    RaceDayContext db,
    ILogger logger,
    int retrySeconds,
    int maxAttempts)
{
    retrySeconds = Math.Clamp(retrySeconds, 1, 60);
    maxAttempts = Math.Clamp(maxAttempts, 1, 60);

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                ex,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {Delay} seconds.",
                attempt,
                maxAttempts,
                retrySeconds);
            await Task.Delay(TimeSpan.FromSeconds(retrySeconds));
        }
    }
}

static async Task SeedRolesAsync(RaceDayContext db, ILogger logger)
{
    try
    {
        var allowedRoles = new[] { "Organiser", "Participant" };
        var seededRoles = await db.Roles.Select(role => role.RoleName).ToListAsync();
        var missing = allowedRoles
            .Where(role => !seededRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count == 0) return;

        db.Roles.AddRange(missing.Select(role => new Role { RoleName = role }));
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded roles: {Roles}", string.Join(", ", missing));
    }
    catch (Exception ex)
    {
        logger.LogWarning(
            ex,
            "Role seeding was skipped because the database is not available. Start SQL Server and restart the API.");
    }
}

public partial class Program { }
