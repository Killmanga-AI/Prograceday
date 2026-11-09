using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RaceDay.Mvc.Tests;

public sealed class MvcWebApplicationFactory(string apiBaseUrl) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RaceDayApi:BaseUrl"] = apiBaseUrl,
                ["RaceDayApi:TimeoutSeconds"] = "10",
                ["Https:Redirect"] = "false",
                ["Session:SecureCookies"] = "false",
                ["Session:IdleMinutes"] = "30"
            });
        });
    }
}
