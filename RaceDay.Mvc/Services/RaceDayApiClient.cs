using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Typed HTTP client for the RaceDay REST API. A bearer token from the server-side
/// session is attached to every request; the API is the only component that talks
/// to SQL Server or blob storage.
/// </summary>
public sealed class RaceDayApiClient(
    HttpClient http,
    IHttpContextAccessor accessor,
    ILogger<RaceDayApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    public Task<ApiResult<T>> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, cancellationToken);

    public Task<ApiResult<T>> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, path, body, cancellationToken);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, cancellationToken);

    public async Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file, CancellationToken cancellationToken = default)
    {
        var context = accessor.HttpContext;
        var token = context?.Session.GetString(SessionKeys.Token);

        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", Path.GetFileName(file.FileName));

            using var request = new HttpRequestMessage(HttpMethod.Post, Normalize(path)) { Content = content };
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return await ReadResponseAsync<T>(response, context, token, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timeout while uploading {Path} to the RaceDay API.", path);
            return Failure<T>("The RaceDay API did not respond in time. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network failure while uploading {Path}.", path);
            return Failure<T>("Could not reach the RaceDay API. Please try again shortly.");
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        var token = context?.Session.GetString(SessionKeys.Token);

        try
        {
            using var request = new HttpRequestMessage(method, Normalize(path));
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
                request.Content = JsonContent.Create(body, options: JsonOptions);

            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return await ReadResponseAsync<T>(response, context, token, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timeout while calling {Method} {Path} on the RaceDay API.", method, path);
            return Failure<T>("The RaceDay API did not respond in time. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network failure while calling {Method} {Path}.", method, path);
            return Failure<T>("Could not reach the RaceDay API. Please try again shortly.");
        }
    }

    private async Task<ApiResult<T>> ReadResponseAsync<T>(
        HttpResponseMessage response,
        HttpContext? context,
        string? token,
        CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var status = (int)response.StatusCode;

        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(token) && context is not null)
        {
            // The API rejected an existing token: clear the stale session and let the
            // global filter send the user back to the sign-in page with a message.
            context.Items[SessionKeys.SessionExpired] = true;
            SessionAuth.SignOut(context);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = status,
                Error = ExtractError(raw, response)
            };
        }

        if (typeof(T) == typeof(bool))
            return new ApiResult<T> { Success = true, StatusCode = status, Data = (T)(object)true };

        if (string.IsNullOrWhiteSpace(raw))
            return new ApiResult<T> { Success = true, StatusCode = status };

        try
        {
            var data = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            return new ApiResult<T> { Success = true, StatusCode = status, Data = data };
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Unexpected JSON payload from the RaceDay API ({Path}).", response.RequestMessage?.RequestUri);
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = status,
                Error = "The RaceDay API returned a response that could not be read."
            };
        }
    }

    private static string ExtractError(string raw, HttpResponseMessage response)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var document = JsonDocument.Parse(raw);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in new[] { "message", "title", "detail" })
                    {
                        if (document.RootElement.TryGetProperty(property, out var value) &&
                            value.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(value.GetString()))
                        {
                            return value.GetString()!;
                        }
                    }

                    if (document.RootElement.TryGetProperty("errors", out var errors) &&
                        errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
                                ? property.Value.EnumerateArray().Select(item => item.GetString())
                                : [property.Value.GetString()])
                            .Where(message => !string.IsNullOrWhiteSpace(message));
                        var combined = string.Join(" ", messages);
                        if (!string.IsNullOrWhiteSpace(combined)) return combined;
                    }
                }
            }
            catch (JsonException)
            {
                // Not JSON; fall through to a safe message below.
            }

            var trimmed = raw.Trim();
            if (trimmed.Length <= 300 && !trimmed.StartsWith('<'))
                return trimmed;
        }

        return $"The RaceDay API returned {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }

    private static string Normalize(string path) => path.TrimStart('/');

    private static ApiResult<T> Failure<T>(string message) => new() { Success = false, Error = message };
}
