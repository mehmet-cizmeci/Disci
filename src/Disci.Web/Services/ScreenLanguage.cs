namespace Disci.Web.Services;

/// <summary>
/// Screen language (Turkish or English). Kept per browser in the disci-dil cookie; signed-in users also store it in
/// their account (ApplicationUser.Language), which sets the cookie again at sign-in on any computer. English is shown by
/// wwwroot/i18n (see App.razor); number and date formats stay Turkish so entries and reports keep working.
/// </summary>
public static class ScreenLanguage
{
    public const string Cookie = "disci-dil";
    public static readonly string[] Supported = ["tr", "en"];

    public static string Normalize(string? code) => code is "en" ? "en" : "tr";

    public static string From(HttpContext? http) => Normalize(http?.Request.Cookies[Cookie]);

    public static void Set(HttpResponse response, string? code)
    {
        if (response.HasStarted) return;
        response.Cookies.Append(Cookie, Normalize(code), new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(400), SameSite = SameSiteMode.Lax, HttpOnly = true, IsEssential = true,
        });
    }

    /// <summary>
    /// A signed-in user on a browser without the cookie (new computer, any sign-in method) gets the language saved in
    /// their account; the language applies from the next page they open.
    /// </summary>
    public static IApplicationBuilder UseScreenLanguage(this IApplicationBuilder app) => app.Use(async (http, next) =>
    {
        if (http.User.Identity?.IsAuthenticated == true && !http.Request.Cookies.ContainsKey(Cookie)
            && http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value is { } id)
        {
            var factory = http.RequestServices.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<Disci.Infrastructure.DisciDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            var language = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                db.Users.Where(u => u.Id == id).Select(u => u.Language));
            Set(http.Response, language);
        }
        await next();
    });

    /// <summary>/dil/en or /dil/tr: switch language and go back (used on the sign-in page).</summary>
    public static IEndpointConventionBuilder MapScreenLanguage(this IEndpointRouteBuilder app) =>
        app.MapGet("/dil/{code}", (HttpContext http, string code, string? geri) =>
        {
            Set(http.Response, code);
            // Only local paths, never an outside address.
            var target = geri is { Length: > 0 } g && g.StartsWith('/') && !g.StartsWith("//") && !g.StartsWith("/\\") ? g : "/";
            return Results.LocalRedirect(target);
        }).AllowAnonymous();
}
