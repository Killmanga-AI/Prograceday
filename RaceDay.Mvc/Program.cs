using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionExpiryFilter>();
});
builder.Services.AddScoped<SessionExpiryFilter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

// These option delegates run after the host has finished building, so configuration
// from appsettings, environment variables, user secrets and test hosts is all applied.
builder.Services.AddSession(options =>
{
    var secureCookies = ResolveSecureCookies();
    var idleMinutes = builder.Configuration.GetValue("Session:IdleMinutes", 30);

    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(Math.Clamp(idleMinutes, 1, 480));
});

builder.Services.AddAntiforgery(options =>
{
    var secureCookies = ResolveSecureCookies();

    options.Cookie.Name = ".RaceDay.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddHttpClient<RaceDayApiClient>((services, client) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var configuredUrl = configuration["RaceDayApi:BaseUrl"] ?? "http://localhost:5171/";

    if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var baseUri) ||
        (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            "RaceDayApi:BaseUrl must be an absolute HTTP or HTTPS URL, for example http://localhost:5171/.");
    }

    client.BaseAddress = new Uri(configuredUrl.EndsWith('/') ? configuredUrl : configuredUrl + "/");
    var timeoutSeconds = configuration.GetValue("RaceDayApi:TimeoutSeconds", 30);
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120));
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

bool ResolveSecureCookies()
{
    // Tests run over plain HTTP inside the in-memory host, so request-scoped cookies
    // are required there. Deployed environments default to secure cookies.
    if (builder.Environment.IsEnvironment("Testing")) return false;
    return builder.Configuration.GetValue("Session:SecureCookies", !builder.Environment.IsDevelopment());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing") &&
    app.Configuration.GetValue("Https:Redirect", true))
{
    app.UseHttpsRedirection();
}

// Baseline browser hardening for every response, including error pages.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data: https: http:; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "script-src 'self' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";
    await next();
});

// Status pages must run before routing so the re-executed request is routed again.
app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Authenticated pages must never be cached by a shared browser or proxy. Headers are
// written before the response starts so Razor output is not disrupted.
app.Use(async (context, next) =>
{
    if (!string.IsNullOrEmpty(context.Session.GetString(SessionKeys.Token)))
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        context.Response.Headers.Pragma = "no-cache";
    }

    await next();
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/health", () => Results.Ok(new { status = "ok", application = "RaceDay.Mvc" }));

app.Run();

public partial class Program { }
