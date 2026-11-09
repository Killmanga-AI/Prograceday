using System.Net;
using System.Text;
using Xunit;

namespace RaceDay.Mvc.Tests;

public sealed class UploadValidationTests : MvcTestBase
{
    [Fact]
    public async Task CreateEvent_RejectsPdfBannersBeforeCallingTheApi()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/CreateEvent");

        using var content = Multipart(
            ValidEventFields(token),
            "Banner",
            "route-map.pdf",
            "application/pdf",
            Encoding.UTF8.GetBytes("%PDF-1.7 fake"));

        using var response = await Client.PostAsync("/Organiser/CreateEvent", content);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Only JPG, PNG and WebP images are accepted.", html);
        Assert.False(Api.State.HasRequest("POST /api/events"));
    }

    [Fact]
    public async Task CreateEvent_RejectsOversizedBannersBeforeCallingTheApi()
    {
        await LoginAsOrganiserAsync();
        var token = await GetAntiforgeryTokenAsync("/Organiser/CreateEvent");
        var oversized = new byte[6 * 1024 * 1024];
        PngBytes().CopyTo(oversized, 0);

        using var content = Multipart(
            ValidEventFields(token),
            "Banner",
            "huge.png",
            "image/png",
            oversized);

        using var response = await Client.PostAsync("/Organiser/CreateEvent", content);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("5 MB or smaller", html);
        Assert.False(Api.State.HasRequest("POST /api/events"));
    }

    [Fact]
    public async Task Profile_WithValidPicture_UploadsAndRedirects()
    {
        await LoginAsParticipantAsync();
        var token = await GetAntiforgeryTokenAsync("/Profile");

        using var content = Multipart(
            new Dictionary<string, string>
            {
                ["FullName"] = "Pat Participant",
                ["Email"] = "participant@example.com",
                ["__RequestVerificationToken"] = token
            },
            "Picture",
            "avatar.png",
            "image/png",
            PngBytes());

        using var response = await Client.PostAsync("/Profile", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(Api.State.HasRequest("PUT /api/users/me"));
        Assert.True(Api.State.HasRequest("POST /api/users/me/profile-picture"));
    }

    [Fact]
    public async Task Profile_WithInvalidPictureType_StopsBeforeTheUpload()
    {
        await LoginAsParticipantAsync();
        var token = await GetAntiforgeryTokenAsync("/Profile");

        using var content = Multipart(
            new Dictionary<string, string>
            {
                ["FullName"] = "Pat Participant",
                ["Email"] = "participant@example.com",
                ["__RequestVerificationToken"] = token
            },
            "Picture",
            "avatar.txt",
            "text/plain",
            Encoding.UTF8.GetBytes("not an image"));

        using var response = await Client.PostAsync("/Profile", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Only JPG, PNG and WebP images are accepted.", await response.Content.ReadAsStringAsync());
        Assert.False(Api.State.HasRequest("PUT /api/users/me"));
    }
}
