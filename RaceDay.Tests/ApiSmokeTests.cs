using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;
namespace RaceDay.Tests;
public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>> {
 private readonly WebApplicationFactory<Program> _factory;
 public ApiSmokeTests(WebApplicationFactory<Program> factory) => _factory=factory;
 [Fact] public async Task Root_ReturnsApiInformation(){ using var client=_factory.CreateClient(); var response=await client.GetAsync("/"); Assert.True(response.IsSuccessStatusCode); var body=await response.Content.ReadAsStringAsync(); Assert.Contains("RaceDay API",body); }
 [Fact] public async Task ProtectedProfile_RejectsAnonymousRequest(){ using var client=_factory.CreateClient(); var response=await client.GetAsync("/api/users/me"); Assert.Equal(System.Net.HttpStatusCode.Unauthorized,response.StatusCode); }
 [Fact] public async Task SwaggerIsAvailableInDevelopment(){using var client=_factory.WithWebHostBuilder(b=>b.UseSetting("ASPNETCORE_ENVIRONMENT","Development")).CreateClient();var response=await client.GetAsync("/swagger/v1/swagger.json");Assert.True(response.IsSuccessStatusCode);}
}
