using System.Net;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class AuthorisationTests : MvcTestBase
{
    [Fact]
    public async Task AnonymousParticipantPage_RedirectsToLoginWithReturnUrl()
    {
        using var response = await Client.GetAsync("/Events/MyEvents");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.Contains("/Account/Login", location);
        Assert.Contains("returnUrl", location);
    }

    [Fact]
    public async Task ParticipantCannotOpenTheOrganiserDashboard()
    {
        await LoginAsParticipantAsync();

        using var response = await Client.GetAsync("/Organiser/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OrganiserCannotOpenParticipantPages()
    {
        await LoginAsOrganiserAsync();

        using var response = await Client.GetAsync("/Events/MyEvents");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/AccessDenied", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ParticipantCannotPostToOrganiserActionsEvenWithAntiforgeryToken()
    {
        await LoginAsParticipantAsync();
        var token = await GetAntiforgeryTokenAsync("/Events/Browse");

        using var response = await Client.PostAsync("/Organiser/CreateEvent", new FormUrlEncodedContent(
            ValidEventFields(token)));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/AccessDenied", response.Headers.Location?.OriginalString);
        Assert.False(Api.State.HasRequest("POST /api/events"));
    }

    [Fact]
    public async Task ExpiredApiToken_RedirectsToLoginAndClearsTheSession()
    {
        await LoginAsOrganiserAsync();
        Api.State.SessionExpired = true;

        using var expired = await Client.GetAsync("/Organiser/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
        Assert.Contains("/Account/Login", expired.Headers.Location!.OriginalString);

        // The session was cleared, so the next request is rejected before reaching the API.
        Api.State.SessionExpired = false;
        using var again = await Client.GetAsync("/Organiser/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, again.StatusCode);
        Assert.Contains("/Account/Login", again.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AccessDeniedPage_ReturnsForbiddenStatusCode()
    {
        using var response = await Client.GetAsync("/Home/AccessDenied");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Access denied", await response.Content.ReadAsStringAsync());
    }
}
