using System.Globalization;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Disci.Web.Components;
using Disci.Web.Components.Account;
using Disci.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

// Maintenance command for parola-sifirla.ps1: resets an administrator password and exits without starting the web server.
if (args.Length > 0 && args[0] == "--parola-sifirla")
    return await PasswordReset.RunCommandAsync(args);

// A published copy (wwwroot next to the exe) serves from its own folder whatever the working directory is;
// a Windows service starts in System32. During development the default (project folder) is kept.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot")) ? AppContext.BaseDirectory : null,
});

// Runs as a Windows service on the clinic computer; a no-op when started from a console.
builder.Host.UseWindowsService(o => o.ServiceName = "Disci");

// The service account may have no user profile, so sign-in cookie keys are kept in a known folder;
// without this everyone is signed out whenever the service restarts.
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("Disci")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    if (OperatingSystem.IsWindows())
        dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => o.DetailedErrors = builder.Environment.IsDevelopment());

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Interactive components get short-lived contexts from the factory; Identity uses the scoped context it also registers.
builder.Services.AddDbContextFactory<DisciDbContext>(options => options.UseSqlServer(connectionString), ServiceLifetime.Scoped);
builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // No e-mail service yet: accounts are created by an administrator, not self-registered.
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<TurkishIdentityErrors>()
    .AddEntityFrameworkStores<DisciDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.Section));
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ClinicEvents>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ClinicInfo>();
builder.Services.AddSingleton<FirstRunSetup>();
builder.Services.AddScoped<SetupService>();
builder.Services.AddSingleton<UserGuide>();
builder.Services.AddHttpClient(UpdateService.HttpClientName, UpdateService.ConfigureClient);
builder.Services.AddSingleton<UpdateService>();
builder.Services.AddHostedService<UpdateCheckWorker>();

var app = builder.Build();

var turkish = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkish),
    SupportedCultures = [turkish],
    SupportedUICultures = [turkish],
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Explicit so that re-executed status-code requests (e.g. /not-found) also pass through them.
app.UseAuthentication();
app.UseAuthorization();
app.UseScreenLanguage();
app.UseFirstRunSetup();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapPatientFiles();
app.MapUserGuideFiles();
app.MapScreenLanguage();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().RunAsync();

app.Run();
return 0;

/// <summary>Visible to the integration tests (WebApplicationFactory).</summary>
public partial class Program;
