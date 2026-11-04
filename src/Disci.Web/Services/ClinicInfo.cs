using Disci.Domain;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Disci.Web.Services;

/// <summary>The clinic's name and contact details: the saved profile, or the Clinic section of the settings file until one is saved.</summary>
public sealed class ClinicInfo(IDbContextFactory<DisciDbContext> dbFactory, IConfiguration config)
{
    public async Task<ClinicProfile> GetAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ClinicProfiles.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(ct) ?? FromConfig();
    }

    public async Task SaveAsync(ClinicProfile values, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.ClinicProfiles.OrderBy(c => c.Id).FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new ClinicProfile { Name = values.Name };
            db.ClinicProfiles.Add(row);
        }
        row.Name = values.Name.Trim();
        row.Address = Clean(values.Address);
        row.Phone = Clean(values.Phone);
        row.Email = Clean(values.Email);
        row.TaxOffice = Clean(values.TaxOffice);
        row.TaxNo = Clean(values.TaxNo);
        await db.SaveChangesAsync(ct);
    }

    private ClinicProfile FromConfig() => new()
    {
        Name = Clean(config["Clinic:Name"]) ?? "Diş Kliniği",
        Address = Clean(config["Clinic:Address"]),
        Phone = Clean(config["Clinic:Phone"]),
    };

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>
/// First-run state: until the first account exists, the program shows the setup form instead of the sign-in page.
/// The form only answers requests from the computer the program runs on, so nobody else on the network can claim
/// the administrator account first.
/// </summary>
public sealed class FirstRunSetup(IServiceScopeFactory scopes, IConfiguration config)
{
    private volatile bool _done;

    /// <summary>FirstRunSetup:Enabled = false turns the redirect off (the integration tests start without accounts).</summary>
    public bool Enabled => config.GetValue("FirstRunSetup:Enabled", true);

    public async Task<bool> IsNeededAsync()
    {
        if (_done) return false;
        using var scope = scopes.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.Users.AnyAsync()) _done = true;
        return !_done;
    }

    public void MarkDone() => _done = true;

    /// <summary>The program's own computer. TestServer requests have no address and count as local.</summary>
    public static bool IsLocal(HttpContext http)
    {
        var ip = http.Connection.RemoteIpAddress;
        return ip is null || System.Net.IPAddress.IsLoopback(ip)
            || (http.Connection.LocalIpAddress is { } local && ip.Equals(local));
    }
}

public static class FirstRunSetupExtensions
{
    /// <summary>Sends the home and sign-in pages to /kurulum while there are no accounts.</summary>
    public static IApplicationBuilder UseFirstRunSetup(this IApplicationBuilder app) => app.Use(async (http, next) =>
    {
        var path = http.Request.Path.Value ?? "";
        if ((path == "/" || path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase))
            && HttpMethods.IsGet(http.Request.Method)
            && http.User.Identity?.IsAuthenticated != true
            && http.RequestServices.GetRequiredService<FirstRunSetup>().Enabled
            && await http.RequestServices.GetRequiredService<FirstRunSetup>().IsNeededAsync())
        {
            http.Response.Redirect(http.Request.PathBase + "/kurulum");
            return;
        }
        await next();
    });
}

/// <summary>Creates the clinic profile, the first dentist and the administrator account in one go.</summary>
public sealed class SetupService(
    DisciDbContext db,
    UserManager<ApplicationUser> users,
    ClinicInfo clinic,
    FirstRunSetup state)
{
    public async Task<IReadOnlyList<string>> RunAsync(SetupModel m)
    {
        if (await users.Users.AnyAsync())
            return ["Kurulum daha önce tamamlanmış. Giriş sayfasını kullanın."];
        if (m.Password != m.ConfirmPassword)
            return ["Parolalar aynı değil."];

        await using var tx = await db.Database.BeginTransactionAsync();
        Dentist? dentist = null;
        if (!string.IsNullOrWhiteSpace(m.DentistName))
        {
            dentist = new Dentist { FullName = m.DentistName.Trim(), Email = m.Email.Trim(), WorkStart = new TimeOnly(9, 0), WorkEnd = new TimeOnly(18, 0) };
            db.Dentists.Add(dentist);
            await db.SaveChangesAsync();
        }

        var admin = new ApplicationUser
        {
            UserName = m.Email.Trim(),
            Email = m.Email.Trim(),
            EmailConfirmed = true,
            FullName = m.FullName.Trim(),
            DentistId = dentist?.Id,
        };
        var result = await users.CreateAsync(admin, m.Password);
        if (!result.Succeeded)
            return result.Errors.Select(e => e.Description).ToList();
        await users.AddToRoleAsync(admin, Roles.Admin);
        if (dentist is not null) await users.AddToRoleAsync(admin, Roles.Dentist);
        await tx.CommitAsync();

        await clinic.SaveAsync(new ClinicProfile { Name = m.ClinicName, Address = m.Address, Phone = m.Phone, Email = m.ClinicEmail });
        state.MarkDone();
        return [];
    }
}

public sealed class SetupModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Klinik adını yazın.")]
    public string ClinicName { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Klinik e-postası geçerli değil.")]
    public string? ClinicEmail { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Adınızı ve soyadınızı yazın.")]
    public string FullName { get; set; } = "";
    /// <summary>When the administrator is also a dentist, a dentist record is created for the calendar.</summary>
    public string? DentistName { get; set; }
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "E-posta adresini yazın (giriş adı olarak kullanılır).")]
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "E-posta adresi geçerli değil.")]
    public string Email { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Parola yazın.")]
    [System.ComponentModel.DataAnnotations.MinLength(8, ErrorMessage = "Parola en az 8 karakter olmalı.")]
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
