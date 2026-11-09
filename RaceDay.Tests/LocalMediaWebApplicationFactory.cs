using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>
/// Runs the API with the filesystem media provider so uploads, storage and retrieval can
/// be verified end to end without Azure credentials.
/// </summary>
public sealed class LocalMediaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayMediaTests-{Guid.NewGuid():N}";

    public string MediaRoot { get; } = Path.Combine(
        Path.GetTempPath(),
        "raceday-media-tests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InMemoryDatabaseName"] = _databaseName,
                ["Jwt:Key"] = "TEST_ONLY_SIGNING_KEY_FOR_AUTOMATED_TESTS_2026",
                ["Media:Provider"] = "Local",
                ["Media:LocalPath"] = MediaRoot,
                ["Media:PublicBaseUrl"] = "http://localhost:5099"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayContext>>();
            services.RemoveAll<RaceDayContext>();
            services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (Directory.Exists(MediaRoot)) Directory.Delete(MediaRoot, true);
        }
        catch (IOException)
        {
            // A test host may still hold a handle; the temp folder is safe to leave behind.
        }
    }
}
