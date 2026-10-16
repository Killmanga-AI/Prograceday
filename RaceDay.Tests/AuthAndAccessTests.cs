using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Tests;

public sealed class AuthAndAccessTests
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthAndAccessTests()
    {
        _factory = new RaceDayWebApplicationFactory();
    }

    [Fact]
    public async Task RegisterAndLogin_ReturnsJwtForParticipant()
    {
        using var client = _factory.CreateClient();
        var email = $"participant-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Password123!",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registrationBody = await registerResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", registrationBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password123!", registrationBody, StringComparison.Ordinal);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var json = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.Equal("Participant", json.RootElement.GetProperty("role").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";

        var first = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Original User",
            email,
            password = "Password123!",
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Second User",
            email,
            password = "Password456!",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task InvalidRole_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Role User",
            email = $"invalid-role-{Guid.NewGuid():N}@example.com",
            password = "Password123!",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidCredentials_ReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var email = $"bad-login-{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Bad Login User",
            email,
            password = "Password123!",
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PartOnePasswordPlaceholderIsRejectedAsInvalidCredentials()
    {
        using var client = _factory.CreateClient();
        var email = $"placeholder-{Guid.NewGuid():N}@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            var role = await db.Roles.SingleAsync(item => item.RoleName == "Participant");
            db.Users.Add(new User
            {
                FullName = "Legacy Placeholder",
                Email = email,
                RoleId = role.RoleId,
                PasswordHash = "HASHED_PASSWORD_1"
            });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantIsForbiddenFromCreatingEvents()
    {
        using var client = _factory.CreateClient();
        var email = $"organiser-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Participant User",
            email,
            password = "Password123!",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });

        var json = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = JsonContent.Create(new
            {
                eventName = "Test Event",
                description = "A test event",
                eventDate = DateTime.UtcNow.AddDays(7),
                location = "Cape Town",
                routeInfo = "Flat route",
                eventDistanceKm = 5m,
                eventType = "Run"
            })
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserCanUpdateOnlyTheirOwnProfileAndEmailRemainsUnique()
    {
        using var client = _factory.CreateClient();
        var email = $"profile-{Guid.NewGuid():N}@example.com";
        var existingEmail = $"existing-{Guid.NewGuid():N}@example.com";

        foreach (var accountEmail in new[] { email, existingEmail })
        {
            var register = await client.PostAsJsonAsync("/api/auth/register", new
            {
                fullName = "Profile User",
                email = accountEmail,
                password = "Password123!",
                role = "Participant"
            });
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = loginJson.RootElement.GetProperty("token").GetString();

        using var conflictingUpdate = new HttpRequestMessage(HttpMethod.Put, "/api/users/me")
        {
            Content = JsonContent.Create(new { fullName = "Profile User", email = existingEmail })
        };
        conflictingUpdate.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var conflictResponse = await client.SendAsync(conflictingUpdate);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        var changedEmail = $"updated-{Guid.NewGuid():N}@example.com";
        using var update = new HttpRequestMessage(HttpMethod.Put, "/api/users/me")
        {
            Content = JsonContent.Create(new { fullName = "Updated Profile", email = changedEmail })
        };
        update.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var profileResponse = await client.SendAsync(profileRequest);
        using var profile = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        Assert.Equal("Updated Profile", profile.RootElement.GetProperty("fullName").GetString());
        Assert.Equal(changedEmail, profile.RootElement.GetProperty("email").GetString());
        Assert.False(profile.RootElement.TryGetProperty("passwordHash", out _));
    }
}
