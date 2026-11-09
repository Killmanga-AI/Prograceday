using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.Mvc.Tests;

public abstract class MvcTestBase : IAsyncLifetime
{
    private static readonly Regex TokenPattern = new(
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
        RegexOptions.Compiled);

    protected StubRaceDayApi Api { get; private set; } = null!;
    protected MvcWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Api = await StubRaceDayApi.StartAsync();
        Factory = new MvcWebApplicationFactory(Api.BaseUrl);
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        await Api.DisposeAsync();
    }

    protected async Task<string> GetAntiforgeryTokenAsync(string url)
    {
        using var response = await Client.GetAsync(url);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenPattern.Match(html);
        Assert.True(match.Success, $"Antiforgery token was not rendered on {url}.");
        return match.Groups[1].Value;
    }

    protected async Task<HttpResponseMessage> LoginAsync(
        string email = "participant@example.com",
        string password = "Password123!")
    {
        var token = await GetAntiforgeryTokenAsync("/Account/Login");
        return await Client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
    }

    protected async Task LoginAsParticipantAsync() =>
        Assert.Equal(System.Net.HttpStatusCode.Redirect, (await LoginAsync("participant@example.com")).StatusCode);

    protected async Task LoginAsOrganiserAsync() =>
        Assert.Equal(System.Net.HttpStatusCode.Redirect, (await LoginAsync("organiser@example.com")).StatusCode);

    protected static MultipartFormDataContent Multipart(
        Dictionary<string, string> fields,
        string fileField,
        string fileName,
        string contentType,
        byte[] bytes)
    {
        var content = new MultipartFormDataContent();
        foreach (var field in fields)
            content.Add(new StringContent(field.Value), field.Key);

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, fileField, fileName);
        return content;
    }

    protected static byte[] PngBytes() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
    ];

    protected static Dictionary<string, string> ValidEventFields(string token) => new()
    {
        ["EventName"] = "New Coastal 10 km",
        ["Description"] = "A flat coastal route.",
        ["EventDate"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm"),
        ["Location"] = "Durban",
        ["RouteInfo"] = "Promenade out and back",
        ["EventDistanceKm"] = "10",
        ["EventType"] = "Run",
        ["__RequestVerificationToken"] = token
    };
}
