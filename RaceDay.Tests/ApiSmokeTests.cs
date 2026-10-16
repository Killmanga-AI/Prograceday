using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using Xunit;
namespace RaceDay.Tests;
public sealed class ApiSmokeTests : IClassFixture<RaceDayWebApplicationFactory> {
 private readonly RaceDayWebApplicationFactory _factory;
 public ApiSmokeTests(RaceDayWebApplicationFactory factory) => _factory=factory;
 [Fact] public async Task Root_ReturnsApiInformation(){ using var client=_factory.CreateClient(); var response=await client.GetAsync("/"); Assert.True(response.IsSuccessStatusCode); var body=await response.Content.ReadAsStringAsync(); Assert.Contains("RaceDay API",body); }
 [Fact] public async Task ProtectedProfile_RejectsAnonymousRequest(){ using var client=_factory.CreateClient(); var response=await client.GetAsync("/api/users/me"); Assert.Equal(System.Net.HttpStatusCode.Unauthorized,response.StatusCode); }
 [Fact] public async Task SwaggerDocumentsAllTwentyPlannedOperations(){using var client=_factory.CreateClient();var response=await client.GetAsync("/swagger/v1/swagger.json");Assert.True(response.IsSuccessStatusCode);using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var paths=json.RootElement.GetProperty("paths");var operationCount=paths.EnumerateObject().Where(path=>path.Name.StartsWith("/api/",StringComparison.Ordinal)).Sum(path=>path.Value.EnumerateObject().Count());Assert.Equal(20,operationCount);var createEvent=paths.GetProperty("/api/events").GetProperty("post");Assert.False(string.IsNullOrWhiteSpace(createEvent.GetProperty("summary").GetString()));Assert.True(createEvent.GetProperty("responses").TryGetProperty("201",out _));Assert.False(paths.GetProperty("/api/auth/register").GetProperty("post").TryGetProperty("security",out _));Assert.True(paths.GetProperty("/api/events").GetProperty("get").TryGetProperty("security",out _));}
}
