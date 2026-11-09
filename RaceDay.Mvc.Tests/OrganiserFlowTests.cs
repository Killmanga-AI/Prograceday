using System.Net;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class OrganiserFlowTests : MvcTestBase
{
    [Fact]
    public async Task Dashboard_DisplaysScopedMetrics()
    {
        await LoginAsOrganiserAsync();

        using var response = await Client.GetAsync("/Organiser/Dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Organiser dashboard", html);
        Assert.Contains("15", html);
        Assert.Contains("Cape Town Marathon", html);
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_ShowsValidationAndSkipsTheApi()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/CreateEvent");

        var fields = ValidEventFields(token);
        fields["EventName"] = "";

        using var response = await Client.PostAsync("/Organiser/CreateEvent", new FormUrlEncodedContent(fields));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("required", html, StringComparison.OrdinalIgnoreCase);
        Assert.False(Api.State.HasRequest("POST /api/events"));
    }

    [Fact]
    public async Task CreateEvent_WithValidData_CreatesItAndUploadsTheBanner()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/CreateEvent");

        using var content = Multipart(
            ValidEventFields(token),
            "Banner",
            "banner.png",
            "image/png",
            PngBytes());

        using var response = await Client.PostAsync("/Organiser/CreateEvent", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Organiser/EventCategories/11", response.Headers.Location?.OriginalString);
        Assert.True(Api.State.HasRequest("POST /api/events"));
        Assert.True(Api.State.HasRequest("POST /api/events/11/banner"));
    }

    [Fact]
    public async Task UpdateCategory_SendsValidatedChangesToTheApi()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/EventCategories/10");

        using var response = await Client.PostAsync("/Organiser/UpdateCategory", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["CategoryId"] = "5",
                ["EventId"] = "10",
                ["CategoryName"] = "10 km Open",
                ["DistanceKm"] = "10",
                ["MaxParticipants"] = "150",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(Api.State.HasRequest("PUT /api/categories/5"));
    }

    [Fact]
    public async Task AddResult_PostsFinishTimeForTheSelectedEnrolment()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/Results/10");

        using var response = await Client.PostAsync("/Organiser/AddResult?eventId=10", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["EnrolmentId"] = "77",
                ["FinishTime"] = "00:35:12",
                ["Position"] = "1",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(Api.State.HasRequest("POST /api/enrolments/77/results"));
    }

    [Fact]
    public async Task Results_RejectsAnEmptyFinishTime()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/Results/10");

        using var response = await Client.PostAsync("/Organiser/AddResult?eventId=10", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["EnrolmentId"] = "77",
                ["FinishTime"] = "00:00:00",
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(Api.State.HasRequest("POST /api/enrolments/77/results"));
    }
}
