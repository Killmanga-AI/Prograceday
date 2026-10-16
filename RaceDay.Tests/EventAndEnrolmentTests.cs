using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

public sealed class EventAndEnrolmentTests
{
    private readonly RaceDayWebApplicationFactory _factory = new();

    [Fact]
    public async Task OrganiserCanCreateAndUpdateOwnEventWithDistanceAndType()
    {
        using var client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Organiser");
        var participantToken = await RegisterAndLoginAsync(client, "Participant");
        var eventId = await CreateEventAsync(client, token);
        var categoryId = await CreateCategoryAsync(client, token, eventId, 10);

        using var enrolment = AuthorizedRequest(
            HttpMethod.Post,
            $"/api/categories/{categoryId}/enrolments",
            participantToken);
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(enrolment)).StatusCode);

        using var update = AuthorizedRequest(HttpMethod.Put, $"/api/events/{eventId}", token, new
        {
            eventName = "Updated Race",
            description = "Updated description",
            eventDate = DateTime.UtcNow.AddDays(14),
            location = "Durban",
            routeInfo = "Coastal loop",
            eventDistanceKm = 21.1m,
            eventType = "Run"
        });
        var updateResponse = await client.SendAsync(update);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var result = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal(21.1m, result.RootElement.GetProperty("eventDistanceKm").GetDecimal());
        Assert.Equal("Run", result.RootElement.GetProperty("eventType").GetString());

        using var delete = AuthorizedRequest(HttpMethod.Delete, $"/api/events/{eventId}", token);
        var deleteResponse = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var getDeleted = AuthorizedRequest(HttpMethod.Get, $"/api/events/{eventId}", token);
        var deletedResponse = await client.SendAsync(getDeleted);
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task DifferentOrganiserCannotManageAnotherOrganisersEvent()
    {
        using var client = _factory.CreateClient();
        var ownerToken = await RegisterAndLoginAsync(client, "Organiser");
        var otherToken = await RegisterAndLoginAsync(client, "Organiser");
        var eventId = await CreateEventAsync(client, ownerToken);

        using var update = AuthorizedRequest(HttpMethod.Put, $"/api/events/{eventId}", otherToken, new
        {
            eventName = "Attempted takeover",
            description = "Should not be saved",
            eventDate = DateTime.UtcNow.AddDays(14),
            location = "Cape Town",
            routeInfo = "Road route",
            eventDistanceKm = 10m,
            eventType = "Walk"
        });

        var response = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var categoryRequest = AuthorizedRequest(HttpMethod.Post, $"/api/events/{eventId}/categories", otherToken, new
        {
            categoryName = "10km",
            distanceKm = 10m,
            maxParticipants = 100
        });
        var categoryResponse = await client.SendAsync(categoryRequest);
        Assert.Equal(HttpStatusCode.Forbidden, categoryResponse.StatusCode);
    }

    [Fact]
    public async Task CategoryOwnerCanUpdateAndDeleteCategory()
    {
        using var client = _factory.CreateClient();
        var ownerToken = await RegisterAndLoginAsync(client, "Organiser");
        var otherToken = await RegisterAndLoginAsync(client, "Organiser");
        var eventId = await CreateEventAsync(client, ownerToken);
        var categoryId = await CreateCategoryAsync(client, ownerToken, eventId, 100);

        using var update = AuthorizedRequest(HttpMethod.Put, $"/api/categories/{categoryId}", ownerToken, new
        {
            categoryName = "Half Marathon",
            distanceKm = 21.1m,
            maxParticipants = 150
        });
        var updateResponse = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var forbiddenUpdate = AuthorizedRequest(HttpMethod.Put, $"/api/categories/{categoryId}", otherToken, new
        {
            categoryName = "Takeover",
            distanceKm = 1m,
            maxParticipants = 10
        });
        var forbiddenResponse = await client.SendAsync(forbiddenUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var delete = AuthorizedRequest(HttpMethod.Delete, $"/api/categories/{categoryId}", ownerToken);
        var deleteResponse = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrolButDuplicateAndFullCategoryAreRejected()
    {
        using var client = _factory.CreateClient();
        var organiserToken = await RegisterAndLoginAsync(client, "Organiser");
        var participantToken = await RegisterAndLoginAsync(client, "Participant");
        var secondParticipantToken = await RegisterAndLoginAsync(client, "Participant");
        var eventId = await CreateEventAsync(client, organiserToken);
        var categoryId = await CreateCategoryAsync(client, organiserToken, eventId, 1);

        using var firstRequest = AuthorizedRequest(HttpMethod.Post, $"/api/categories/{categoryId}/enrolments", participantToken);
        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        using var firstEnrollment = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        Assert.Equal(categoryId, firstEnrollment.RootElement.GetProperty("categoryId").GetInt32());
        var enrolmentId = firstEnrollment.RootElement.GetProperty("enrolmentId").GetInt32();

        using var organiserListRequest = AuthorizedRequest(HttpMethod.Get, $"/api/events/{eventId}/enrolments", organiserToken);
        var organiserListResponse = await client.SendAsync(organiserListRequest);
        Assert.Equal(HttpStatusCode.OK, organiserListResponse.StatusCode);
        using var organiserList = JsonDocument.Parse(await organiserListResponse.Content.ReadAsStringAsync());
        Assert.Single(organiserList.RootElement.EnumerateArray());

        using var duplicateRequest = AuthorizedRequest(HttpMethod.Post, $"/api/categories/{categoryId}/enrolments", participantToken);
        var duplicateResponse = await client.SendAsync(duplicateRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var otherParticipantCancel = AuthorizedRequest(HttpMethod.Delete, $"/api/enrolments/{enrolmentId}", secondParticipantToken);
        var forbiddenCancel = await client.SendAsync(otherParticipantCancel);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCancel.StatusCode);

        using var fullRequest = AuthorizedRequest(HttpMethod.Post, $"/api/categories/{categoryId}/enrolments", secondParticipantToken);
        var fullResponse = await client.SendAsync(fullRequest);
        Assert.Equal(HttpStatusCode.BadRequest, fullResponse.StatusCode);

        using var mineRequest = AuthorizedRequest(HttpMethod.Get, "/api/users/me/enrolments", participantToken);
        var mineResponse = await client.SendAsync(mineRequest);
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        using var mine = JsonDocument.Parse(await mineResponse.Content.ReadAsStringAsync());
        Assert.Single(mine.RootElement.EnumerateArray());
        Assert.Equal(categoryId, mine.RootElement[0].GetProperty("categoryId").GetInt32());

        using var ownCancel = AuthorizedRequest(HttpMethod.Delete, $"/api/enrolments/{enrolmentId}", participantToken);
        var cancelResponse = await client.SendAsync(ownCancel);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        using var afterCancelRequest = AuthorizedRequest(HttpMethod.Post, $"/api/categories/{categoryId}/enrolments", secondParticipantToken);
        var afterCancel = await client.SendAsync(afterCancelRequest);
        Assert.Equal(HttpStatusCode.Created, afterCancel.StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotCreateAnEventEvenWithValidToken()
    {
        using var client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Participant");

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/events", token, new
        {
            eventName = "Forbidden event",
            description = "Not allowed",
            eventDate = DateTime.UtcNow.AddDays(7),
            location = "Cape Town",
            routeInfo = "Road",
            eventDistanceKm = 5m,
            eventType = "Run"
        });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidEventDateDistanceAndTypeAreRejected()
    {
        using var client = _factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Organiser");

        using var request = AuthorizedRequest(HttpMethod.Post, "/api/events", token, new
        {
            eventName = "Invalid event",
            description = "Invalid values",
            eventDate = DateTime.MinValue,
            location = "Cape Town",
            routeInfo = "Route",
            eventDistanceKm = 0m,
            eventType = "Swim"
        });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserCanRecordResultAndParticipantCanViewOwnResult()
    {
        using var client = _factory.CreateClient();
        var organiserToken = await RegisterAndLoginAsync(client, "Organiser");
        var otherOrganiserToken = await RegisterAndLoginAsync(client, "Organiser");
        var participantToken = await RegisterAndLoginAsync(client, "Participant");
        var eventId = await CreateEventAsync(client, organiserToken);
        var categoryId = await CreateCategoryAsync(client, organiserToken, eventId, 5);

        using var enrolRequest = AuthorizedRequest(HttpMethod.Post, $"/api/categories/{categoryId}/enrolments", participantToken);
        var enrolResponse = await client.SendAsync(enrolRequest);
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);
        using var enrol = JsonDocument.Parse(await enrolResponse.Content.ReadAsStringAsync());
        var enrolmentId = enrol.RootElement.GetProperty("enrolmentId").GetInt32();

        using var resultRequest = AuthorizedRequest(HttpMethod.Post, $"/api/enrolments/{enrolmentId}/results", organiserToken, new
        {
            finishTime = "00:35:12",
            position = 1
        });
        var resultResponse = await client.SendAsync(resultRequest);
        Assert.Equal(HttpStatusCode.Created, resultResponse.StatusCode);

        using var forbiddenResultRequest = AuthorizedRequest(HttpMethod.Post, $"/api/enrolments/{enrolmentId}/results", otherOrganiserToken, new
        {
            finishTime = "00:40:00",
            position = 2
        });
        var forbiddenResultResponse = await client.SendAsync(forbiddenResultRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResultResponse.StatusCode);

        using var duplicateRequest = AuthorizedRequest(HttpMethod.Post, $"/api/enrolments/{enrolmentId}/results", organiserToken, new
        {
            finishTime = "00:36:00",
            position = 2
        });
        var duplicateResponse = await client.SendAsync(duplicateRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var mineRequest = AuthorizedRequest(HttpMethod.Get, "/api/users/me/results", participantToken);
        var mineResponse = await client.SendAsync(mineRequest);
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        using var results = JsonDocument.Parse(await mineResponse.Content.ReadAsStringAsync());
        Assert.Single(results.RootElement.EnumerateArray());
        Assert.Equal(enrolmentId, results.RootElement[0].GetProperty("enrolmentId").GetInt32());
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Account",
            email,
            password = "Password123!",
            role
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<int> CreateEventAsync(HttpClient client, string token)
    {
        using var request = AuthorizedRequest(HttpMethod.Post, "/api/events", token, new
        {
            eventName = "RaceDay Integration Event",
            description = "Test event",
            eventDate = DateTime.UtcNow.AddDays(7),
            location = "Cape Town",
            routeInfo = "Flat loop",
            eventDistanceKm = 10m,
            eventType = "Run"
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("eventId").GetInt32();
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client, string token, int eventId, int maxParticipants)
    {
        using var request = AuthorizedRequest(HttpMethod.Post, $"/api/events/{eventId}/categories", token, new
        {
            categoryName = "10km",
            distanceKm = 10m,
            maxParticipants
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("categoryId").GetInt32();
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }
}
