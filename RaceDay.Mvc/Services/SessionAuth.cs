using System.Globalization;
using Microsoft.AspNetCore.Http;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

public static class SessionKeys
{
    public const string Token = "Token";
    public const string UserId = "UserId";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string Role = "Role";
    public const string ExpiresAtUtc = "ExpiresAtUtc";
    public const string SessionExpired = "RaceDay.SessionExpired";
}

/// <summary>
/// Single place that reads and writes the authenticated user in the server-side session.
/// The API remains the authority for authorisation; these values only drive the UI and
/// early route protection.
/// </summary>
public static class SessionAuth
{
    public static void SignIn(HttpContext context, AuthResponse auth)
    {
        var lifetimeSeconds = auth.ExpiresInSeconds > 0 ? auth.ExpiresInSeconds : 7200;
        context.Session.SetString(SessionKeys.Token, auth.Token);
        context.Session.SetString(SessionKeys.UserId, auth.UserId.ToString(CultureInfo.InvariantCulture));
        context.Session.SetString(SessionKeys.FullName, auth.FullName);
        context.Session.SetString(SessionKeys.Email, auth.Email);
        context.Session.SetString(SessionKeys.Role, auth.Role);
        context.Session.SetString(
            SessionKeys.ExpiresAtUtc,
            DateTimeOffset.UtcNow.AddSeconds(lifetimeSeconds).ToString("O", CultureInfo.InvariantCulture));
    }

    public static void SignOut(HttpContext context) => context.Session.Clear();

    public static bool IsAuthenticated(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Session.GetString(SessionKeys.Token))) return false;

        var rawExpiry = context.Session.GetString(SessionKeys.ExpiresAtUtc);
        if (!DateTimeOffset.TryParse(
                rawExpiry,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
        {
            // A session without a recorded expiry cannot be trusted.
            context.Session.Clear();
            return false;
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            context.Session.Clear();
            return false;
        }

        return true;
    }

    public static string? Role(HttpContext context) => context.Session.GetString(SessionKeys.Role);

    public static string? FullName(HttpContext context) => context.Session.GetString(SessionKeys.FullName);

    public static int? UserId(HttpContext context) =>
        int.TryParse(context.Session.GetString(SessionKeys.UserId), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public static bool IsInRole(HttpContext context, string role) =>
        string.Equals(Role(context), role, StringComparison.OrdinalIgnoreCase);
}
