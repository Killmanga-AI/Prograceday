using System.Net;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class AuthenticationTests : MvcTestBase
{
    [Fact]
    public async Task LoginPage_RendersForAnonymousVisitor()
    {
        using var response = await Client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Welcome back", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task InvalidCredentials_ShowApiError()
    {
        Api.State.LoginFails = true;

        using var response = await LoginAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid email or password.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ParticipantLogin_CreatesSecureSessionAndRedirectsToBrowse()
    {
        using var response = await LoginAsync("participant@example.com");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Events/Browse", response.Headers.Location?.OriginalString);

        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join(";", values)
            : "";
        Assert.Contains(".RaceDay.Session", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        using var page = await Client.GetAsync("/Events/MyEvents");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Fact]
    public async Task OrganiserLogin_RedirectsToDashboard()
    {
        using var response = await LoginAsync("organiser@example.com");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Organiser/Dashboard", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Logout_ClearsSessionAndProtectsPagesAgain()
    {
        await LoginAsParticipantAsync();
        var token = await GetAntiforgeryTokenAsync("/");

        using var logout = await Client.PostAsync("/Account/Logout", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);

        using var page = await Client.GetAsync("/Events/MyEvents");
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Contains("/Account/Login", page.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task LoginWithoutAntiforgeryToken_IsRejected()
    {
        using var response = await Client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = "participant@example.com",
                ["Password"] = "Password123!"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Api.State.LoginRequests);
    }

    [Fact]
    public async Task RegisterWithInvalidRole_IsRejectedByTheMvcValidator()
    {
        var token = await GetAntiforgeryTokenAsync("/Account/Register");

        using var response = await Client.PostAsync("/Account/Register", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["FullName"] = "Test Person",
                ["Email"] = "new@example.com",
                ["Password"] = "Password123!",
                ["Role"] = "Administrator",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Organiser or Participant", await response.Content.ReadAsStringAsync());
        Assert.False(Api.State.HasRequest("POST /api/auth/register"));
    }
}
