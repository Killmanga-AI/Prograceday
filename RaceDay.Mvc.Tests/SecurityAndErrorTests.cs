using System.Net;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class SecurityAndErrorTests : MvcTestBase
{
    [Fact]
    public async Task UnknownPath_ReturnsFriendlyNotFoundPage()
    {
        using var response = await Client.GetAsync("/this-page-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("could not find", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResponsesCarryBaselineSecurityHeaders()
    {
        using var response = await Client.GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task AuthenticatedPages_AreNotCached()
    {
        await LoginAsParticipantAsync();

        using var response = await Client.GetAsync("/Profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Cache-Control", out var values));
        Assert.Contains("no-store", string.Join(",", values));
    }
}
