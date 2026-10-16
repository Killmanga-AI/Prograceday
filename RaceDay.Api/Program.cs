using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RaceDay API", Version = "v1", Description = "Event management API for organisers and participants." });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), includeControllerXmlComments: true);
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Paste the access token returned by /api/auth/login." });
    c.OperationFilter<RaceDay.Api.Swagger.AuthorizeCheckOperationFilter>();
});

var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase") || builder.Environment.IsEnvironment("Testing");
var inMemoryDatabaseName = builder.Configuration["InMemoryDatabaseName"] ?? "RaceDayDb";
var connection = builder.Configuration.GetConnectionString("RaceDay") ?? "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;";

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => {
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();

    if (useInMemoryDatabase)
    {
        db.Database.EnsureCreated();
    }

    var allowedRoles = new[] { "Organiser", "Participant" };
    var seededRoles = await db.Roles.Select(x => x.RoleName).ToListAsync();
    foreach (var roleName in allowedRoles)
    {
        if (!seededRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            db.Roles.Add(new Role { RoleName = roleName });
        }
    }

    await db.SaveChangesAsync();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { name = "RaceDay API", swagger = "/swagger" })).WithTags("Health").WithSummary("API health and documentation entry point.");
app.MapGet("/health", () => Results.Ok(new { status = "ok", environment = app.Environment.EnvironmentName })).WithTags("Health").WithSummary("Simple health check for local and CI verification.");

app.Run();

public partial class Program { }
