using System.Net;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class ParticipantFlowTests : MvcTestBase
{
    [Fact]
    public async Task Browse_RendersEventsProvidedByTheApi()
    {
        using var response = await Client.GetAsync("/Events/Browse");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cape Town Marathon", html);
        Assert.Contains("Cape Town", html);
        Assert.Contains("Run", html);
    }

    [Fact]
    public async Task Browse_WhenApiFails_ShowsAUserFacingError()
    {
        Api.State.FailAllRequests = true;

        using var response = await Client.GetAsync("/Events/Browse");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The RaceDay API is unavailable.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Details_ShowsCategoriesAndFullState()
    {
        await LoginAsParticipantAsync();
        using var response = await Client.GetAsync("/Events/Details/10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("10 km Open", html);
        Assert.Contains("5 km Fun Run", html);
        Assert.Contains("Category full", html);
    }

    [Fact]
    public async Task Enrol_PostsToTheApiAndRedirectsToTheEvent()
    {
        await LoginAsParticipantAsync();
        var token = await GetAntiforgeryTokenAsync("/Events/Details/10");

        using var response = await Client.PostAsync("/Events/Enrol", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["categoryId"] = "5",
                ["eventId"] = "10",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Events/Details/10", response.Headers.Location?.OriginalString);
        Assert.True(Api.State.HasRequest("POST /api/categories/5/enrolments"));
    }

    [Fact]
    public async Task Cancel_PostsDeleteForTheCurrentUsersEnrolment()
    {
        await LoginAsParticipantAsync();
        var token = await GetAntiforgeryTokenAsync("/Events/MyEvents");

        using var response = await Client.PostAsync("/Events/Cancel", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["id"] = "77",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Events/MyEvents", response.Headers.Location?.OriginalString);
        Assert.True(Api.State.HasRequest("DELETE /api/enrolments/77"));
    }

    [Fact]
    public async Task MyResults_RendersFinishTimePositionAndFinishers()
    {
        await LoginAsParticipantAsync();

        using var response = await Client.GetAsync("/Events/MyResults");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("00:42:15", html);
        Assert.Contains("2 / 4", html);
        Assert.Contains("Cape Town Marathon", html);
    }

    [Fact]
    public async Task MyEvents_ShowsRegisteredEnrolment()
    {
        // The stub always returns one enrolment, so this covers the populated view instead.
        await LoginAsParticipantAsync();

        using var response = await Client.GetAsync("/Events/MyEvents");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("10 km Open", html);
        Assert.Contains("Registered", html);
    }

}
