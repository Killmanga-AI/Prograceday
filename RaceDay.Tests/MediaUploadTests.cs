using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace RaceDay.Tests;

public sealed class MediaUploadTests
{
    [Fact]
    public async Task BannerUpload_StoresTheFileAndServesItBack()
    {
        using var factory = new LocalMediaWebApplicationFactory();
        using var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Organiser");
        var eventId = await CreateEventAsync(client, token);

        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(PngBytes());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "banner.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/banner") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var url = json.RootElement.GetProperty("bannerImageUrl").GetString();
        Assert.NotNull(url);
        Assert.StartsWith("http://localhost:5099/media/", url);

        var stored = Directory.GetFiles(factory.MediaRoot);
        Assert.Single(stored);

        using var served = await client.GetAsync(new Uri(url!).AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/png", served.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes(), await served.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task BannerUpload_RejectsContentThatDoesNotMatchTheDeclaredType()
    {
        using var factory = new LocalMediaWebApplicationFactory();
        using var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Organiser");
        var eventId = await CreateEventAsync(client, token);

        using var content = new MultipartFormDataContent();
        var fake = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not a png"));
        fake.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fake, "file", "banner.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/banner") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(Directory.Exists(factory.MediaRoot) && Directory.GetFiles(factory.MediaRoot).Length > 0);
    }

    [Fact]
    public async Task BannerUpload_RejectsFilesLargerThanFiveMegabytes()
    {
        using var factory = new LocalMediaWebApplicationFactory();
        using var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Organiser");
        var eventId = await CreateEventAsync(client, token);

        using var content = new MultipartFormDataContent();
        var oversized = new byte[6 * 1024 * 1024];
        PngBytes().CopyTo(oversized, 0);
        var image = new ByteArrayContent(oversized);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "huge.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/banner") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.BadRequest,
            $"Expected 413 or 400 but received {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task ProfilePictureUpload_PersistsTheUrlOnTheProfile()
    {
        using var factory = new LocalMediaWebApplicationFactory();
        using var client = factory.CreateClient();
        var token = await RegisterAndLoginAsync(client, "Participant");

        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(PngBytes());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "avatar.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/profile-picture") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var profileResponse = await client.SendAsync(profileRequest);
        using var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());

        Assert.StartsWith("http://localhost:5099/media/", profile.RootElement.GetProperty("profilePictureUrl").GetString());
    }

    [Fact]
    public async Task UploadEndpoints_RequireAuthenticationAndTheOrganiserRole()
    {
        using var factory = new LocalMediaWebApplicationFactory();
        using var client = factory.CreateClient();
        var organiserToken = await RegisterAndLoginAsync(client, "Organiser");
        var participantToken = await RegisterAndLoginAsync(client, "Participant");
        var eventId = await CreateEventAsync(client, organiserToken);

        using (var anonymous = new MultipartFormDataContent())
        {
            var image = new ByteArrayContent(PngBytes());
            image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            anonymous.Add(image, "file", "banner.png");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/banner") { Content = anonymous };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using (var asParticipant = new MultipartFormDataContent())
        {
            var image = new ByteArrayContent(PngBytes());
            image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            asParticipant.Add(image, "file", "banner.png");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/banner") { Content = asParticipant };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task AnotherOrganiserCannotReplaceTheBanner()
    {
        using var factory = new LocalMediaWebApplicationFactory();
        using var client = factory.CreateClient();
        var ownerToken = await RegisterAndLoginAsync(client, "Organiser");
        var otherToken = await RegisterAndLoginAsync(client, "Organiser");
        var eventId = await CreateEventAsync(client, ownerToken);

        using var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(PngBytes());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "banner.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/events/{eventId}/banner") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static byte[] PngBytes() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
    ];

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Media Test Account",
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = JsonContent.Create(new
            {
                eventName = "Media Test Event",
                description = "Upload coverage",
                eventDate = DateTime.UtcNow.AddDays(9),
                location = "Cape Town",
                routeInfo = "Loop",
                eventDistanceKm = 5m,
                eventType = "Run"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("eventId").GetInt32();
    }
}
