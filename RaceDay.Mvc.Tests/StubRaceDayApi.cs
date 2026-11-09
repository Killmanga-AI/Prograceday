using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RaceDay.Mvc.Tests;

public sealed class StubApiState
{
    private readonly object _sync = new();
    private readonly List<string> _requests = [];

    public bool LoginFails { get; set; }
    public bool FailAllRequests { get; set; }
    public bool SessionExpired { get; set; }
    public int UploadStatus { get; set; } = StatusCodes.Status200OK;
    public string UploadMessage { get; set; } = "Upload rejected.";

    public int LoginRequests
    {
        get { lock (_sync) return _requests.Count(request => request == "POST /api/auth/login"); }
    }

    public void Record(HttpContext context)
    {
        lock (_sync)
        {
            _requests.Add($"{context.Request.Method} {context.Request.Path}");
        }
    }

    public bool HasRequest(string methodAndPath)
    {
        lock (_sync) return _requests.Contains(methodAndPath);
    }

    public int CountRequests(string methodAndPath)
    {
        lock (_sync) return _requests.Count(request => request == methodAndPath);
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_sync) return _requests.ToArray();
    }
}

/// <summary>
/// A real Kestrel-hosted stub of the RaceDay API. Running it on 127.0.0.1 proves the MVC
/// application performs genuine HTTP calls (serialization, bearer headers, multipart
/// uploads and error handling) rather than being tested against a fake HttpClient.
/// </summary>
public sealed class StubRaceDayApi : IAsyncDisposable
{
    private readonly WebApplication _app;

    private StubRaceDayApi(WebApplication app, string baseUrl, StubApiState state)
    {
        _app = app;
        BaseUrl = baseUrl;
        State = state;
    }

    public string BaseUrl { get; }
    public StubApiState State { get; }

    public static async Task<StubRaceDayApi> StartAsync()
    {
        var state = new StubApiState();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(state);

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            state.Record(context);
            if (state.FailAllRequests)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { message = "The RaceDay API is unavailable." });
                return;
            }

            await next();
        });

        MapEndpoints(app, state);

        await app.StartAsync();
        return new StubRaceDayApi(app, app.Urls.First(), state);
    }

    private static void MapEndpoints(WebApplication app, StubApiState state)
    {
        app.MapPost("/api/auth/login", async (HttpContext context) =>
        {
            var body = await context.Request.ReadFromJsonAsync<LoginBody>();
            if (state.LoginFails || body is null || body.Password != "Password123!")
                return Results.Json(new { message = "Invalid email or password." }, statusCode: 401);

            var organiser = body.Email.StartsWith("organiser", StringComparison.OrdinalIgnoreCase);
            var role = organiser ? "Organiser" : "Participant";
            return Results.Ok(new
            {
                token = "stub-token",
                userId = organiser ? 2 : 1,
                fullName = organiser ? "Olive Organiser" : "Pat Participant",
                email = body.Email,
                role,
                expiresInSeconds = 3600
            });
        });

        app.MapPost("/api/auth/register", () => Results.Created("/api/users/me", new { userId = 3 }));

        app.MapGet("/api/users/me", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new
            {
                userId = 1,
                fullName = "Pat Participant",
                email = "participant@example.com",
                role = "Participant",
                profilePictureUrl = (string?)null
            });
        });

        app.MapPut("/api/users/me", async (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            var body = await context.Request.ReadFromJsonAsync<ProfileBody>();
            return Results.Ok(new
            {
                userId = 1,
                fullName = body?.FullName ?? "Pat Participant",
                email = body?.Email ?? "participant@example.com",
                profilePictureUrl = (string?)null
            });
        });

        app.MapPost("/api/users/me/profile-picture", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            if (state.UploadStatus != StatusCodes.Status200OK)
                return Results.Json(new { message = state.UploadMessage }, statusCode: state.UploadStatus);
            return Results.Ok(new { userId = 1, profilePictureUrl = "http://localhost:5055/media/profile.png" });
        });

        app.MapGet("/api/events", () => Results.Ok(new[] { EventPayload(10) }));
        app.MapGet("/api/events/{id:int}", (int id) => id == 10 ? Results.Ok(EventPayload(10)) : Results.NotFound());

        app.MapGet("/api/events/{id:int}/categories", (int id) => id == 10
            ? Results.Ok(new[]
            {
                new { categoryId = 5, eventId = 10, categoryName = "10 km Open", distanceKm = 10m, maxParticipants = 100, enrolledCount = 12 },
                new { categoryId = 6, eventId = 10, categoryName = "5 km Fun Run", distanceKm = 5m, maxParticipants = 1, enrolledCount = 1 }
            })
            : Results.NotFound());

        app.MapPost("/api/categories/{id:int}/enrolments", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Created($"/api/enrolments/{id}", new { enrolmentId = 77, participantId = 1, categoryId = id, status = "Registered" });
        });

        app.MapGet("/api/users/me/enrolments", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new[]
            {
                new
                {
                    enrolmentId = 77,
                    categoryId = 5,
                    category = "10 km Open",
                    eventId = 10,
                    @event = "Cape Town Marathon",
                    eventDate = DateTime.UtcNow.AddDays(20),
                    eventLocation = "Cape Town",
                    eventType = "Run",
                    eventBannerImageUrl = (string?)null,
                    distanceKm = 10m,
                    enrolmentDate = DateTime.UtcNow.AddDays(-2),
                    status = "Registered"
                }
            });
        });

        app.MapDelete("/api/enrolments/{id:int}", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.NoContent();
        });

        app.MapGet("/api/users/me/results", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new[]
            {
                new
                {
                    resultId = 31,
                    enrolmentId = 77,
                    eventId = 10,
                    @event = "Cape Town Marathon",
                    eventDate = DateTime.UtcNow.AddDays(-3),
                    eventLocation = "Cape Town",
                    category = "10 km Open",
                    distanceKm = 10m,
                    totalFinishers = 4,
                    finishTime = "00:42:15",
                    position = 2,
                    recordedAt = DateTime.UtcNow.AddDays(-3)
                }
            });
        });

        app.MapGet("/api/organiser/dashboard", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new
            {
                totalEvents = 3,
                upcomingEvents = 2,
                pastEvents = 1,
                totalEnrolments = 15,
                resultsRecorded = 6,
                recentEnrolments = new[]
                {
                    new { enrolmentId = 77, participant = "Pat Participant", @event = "Cape Town Marathon", category = "10 km Open", enrolmentDate = DateTime.UtcNow.AddDays(-2), status = "Registered" }
                },
                upcomingEventList = new[]
                {
                    new { eventId = 10, eventName = "Cape Town Marathon", eventDate = DateTime.UtcNow.AddDays(20), location = "Cape Town", enrolments = 12 }
                }
            });
        });

        app.MapGet("/api/organiser/events", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new[]
            {
                new { eventId = 10, eventName = "Cape Town Marathon", eventDate = DateTime.UtcNow.AddDays(20), location = "Cape Town", eventType = "Run", eventDistanceKm = 42.2m, bannerImageUrl = (string?)null, categories = 2, enrolments = 12 }
            });
        });

        app.MapPost("/api/events", (HttpContext context) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Created("/api/events/11", EventPayload(11));
        });

        app.MapPut("/api/events/{id:int}", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(EventPayload(id));
        });

        app.MapDelete("/api/events/{id:int}", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.NoContent();
        });

        app.MapPost("/api/events/{id:int}/banner", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            if (state.UploadStatus != StatusCodes.Status200OK)
                return Results.Json(new { message = state.UploadMessage }, statusCode: state.UploadStatus);
            return Results.Ok(new { eventId = id, bannerImageUrl = "http://localhost:5055/media/banner.png" });
        });

        app.MapPost("/api/events/{id:int}/categories", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Created($"/api/categories/{id}", new { categoryId = id, eventId = 10, categoryName = "21 km", distanceKm = 21m, maxParticipants = 50 });
        });

        app.MapPut("/api/categories/{id:int}", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new { categoryId = id, eventId = 10, categoryName = "21 km", distanceKm = 21m, maxParticipants = 50 });
        });

        app.MapDelete("/api/categories/{id:int}", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.NoContent();
        });

        app.MapGet("/api/events/{id:int}/enrolments", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new[]
            {
                new { enrolmentId = 77, participantId = 1, participantName = "Pat Participant", participantEmail = "participant@example.com", categoryId = 5, categoryName = "10 km Open", enrolmentDate = DateTime.UtcNow.AddDays(-2), status = "Registered", hasResult = false }
            });
        });

        app.MapGet("/api/events/{id:int}/results", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Ok(new[]
            {
                new { resultId = 31, enrolmentId = 77, participant = "Pat Participant", category = "10 km Open", finishTime = "00:42:15", position = 2, eventDate = DateTime.UtcNow.AddDays(-3) }
            });
        });

        app.MapPost("/api/enrolments/{id:int}/results", (HttpContext context, int id) =>
        {
            var guard = Guard(context, state);
            if (guard is not null) return guard;
            return Results.Created($"/api/enrolments/{id}/results", new { resultId = 32, enrolmentId = id });
        });
    }

    private static IResult? Guard(HttpContext context, StubApiState state)
    {
        if (state.SessionExpired)
            return Results.Json(new { message = "The access token has expired." }, statusCode: StatusCodes.Status401Unauthorized);
        return null;
    }

    private static object EventPayload(int id) => new
    {
        eventId = id,
        organiserId = 2,
        eventName = "Cape Town Marathon",
        description = "A coastal city marathon.",
        eventDate = DateTime.UtcNow.AddDays(20),
        location = "Cape Town",
        routeInfo = "Flat coastal loop",
        eventDistanceKm = 42.2m,
        eventType = "Run",
        bannerImageUrl = (string?)null,
        createdAt = DateTime.UtcNow.AddDays(-30)
    };

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private sealed record LoginBody(string Email, string Password);
    private sealed record ProfileBody(string FullName, string Email);
}
