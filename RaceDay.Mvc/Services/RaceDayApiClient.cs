using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private void Authorize()
    {
        var token = accessor.HttpContext?.Session.GetString("Token");
        http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResult<T>> GetAsync<T>(string path)
    {
        Authorize();
        try { using var response = await http.GetAsync(path); return await Read<T>(response); }
        catch (Exception ex) { return new() { Success=false, Error=$"Could not reach the RaceDay API: {ex.Message}" }; }
    }
    public async Task<ApiResult<T>> PostAsync<T>(string path, object body)
    {
        Authorize();
        try { using var response = await http.PostAsJsonAsync(path, body, JsonOptions); return await Read<T>(response); }
        catch (Exception ex) { return new() { Success=false, Error=$"Could not reach the RaceDay API: {ex.Message}" }; }
    }
    public async Task<ApiResult<T>> PutAsync<T>(string path, object body)
    {
        Authorize();
        try { using var response = await http.PutAsJsonAsync(path, body, JsonOptions); return await Read<T>(response); }
        catch (Exception ex) { return new() { Success=false, Error=$"Could not reach the RaceDay API: {ex.Message}" }; }
    }
    public async Task<ApiResult<T>> DeleteAsync<T>(string path)
    {
        Authorize();
        try { using var response = await http.DeleteAsync(path); return await Read<T>(response); }
        catch (Exception ex) { return new() { Success=false, Error=$"Could not reach the RaceDay API: {ex.Message}" }; }
    }
    public async Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file)
    {
        Authorize();
        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);
            using var response = await http.PostAsync(path, content);
            return await Read<T>(response);
        }
        catch (Exception ex) { return new() { Success=false, Error=$"Upload failed: {ex.Message}" }; }
    }
    private static async Task<ApiResult<T>> Read<T>(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            string error = raw;
            try { using var doc=JsonDocument.Parse(raw); if(doc.RootElement.TryGetProperty("message",out var m)) error=m.GetString()??raw; } catch {}
            return new() { Success=false, Error=string.IsNullOrWhiteSpace(error)?response.ReasonPhrase:error, StatusCode=(int)response.StatusCode };
        }
        if (typeof(T) == typeof(bool)) return new() { Success=true, Data=(T)(object)true, StatusCode=(int)response.StatusCode };
        if (string.IsNullOrWhiteSpace(raw)) return new() { Success=true, StatusCode=(int)response.StatusCode };
        try { return new() { Success=true, Data=JsonSerializer.Deserialize<T>(raw,JsonOptions), StatusCode=(int)response.StatusCode }; }
        catch (Exception ex) { return new() { Success=false, Error=$"The API returned an unexpected response: {ex.Message}", StatusCode=(int)response.StatusCode }; }
    }
}
