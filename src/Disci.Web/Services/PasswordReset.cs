using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Disci.Web.Services;

/// <summary>
/// Emergency access when the administrator password is lost: sets a new password on the account with this e-mail,
/// unlocks it, signs it out everywhere (new security stamp) and makes it an administrator. Creates the account when
/// there is none. Runs on the clinic computer only (it needs the database connection).
/// </summary>
public static class PasswordReset
{
    /// <summary>
    /// <c>Disci.Web.exe --parola-sifirla e-posta [--connection metin]</c>, called by parola-sifirla.ps1. The password comes
    /// from DISCI_YENI_PAROLA so it never appears on the command line. No web server is started.
    /// </summary>
    public static async Task<int> RunCommandAsync(string[] args)
    {
        string Option(string name, string fallback)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
        var email = Option("--parola-sifirla", "");
        var password = Environment.GetEnvironmentVariable("DISCI_YENI_PAROLA");
        if (string.IsNullOrWhiteSpace(email) || email.StartsWith("--", StringComparison.Ordinal) || string.IsNullOrEmpty(password))
        {
            Console.Error.WriteLine("Kullanım: parola-sifirla.ps1 betiğini çalıştırın (e-posta ve parola sorar).");
            return 2;
        }
        var connection = Option("--connection", Environment.GetEnvironmentVariable("DISCI_DB") ?? DisciDbContext.DefaultConnection);
        await using var db = new DisciDbContext(DisciDbContext.CreateOptions(connection));
        Console.WriteLine(await RunAsync(db, email, password));
        return 0;
    }

    public static async Task<string> RunAsync(DisciDbContext db, string email, string password)
    {
        email = email.Trim();
        var normalized = email.ToUpperInvariant();
        var hasher = new PasswordHasher<ApplicationUser>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized || u.NormalizedUserName == normalized);
        bool created = user is null;
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email, NormalizedUserName = normalized, Email = email, NormalizedEmail = normalized,
                EmailConfirmed = true, LockoutEnabled = true, FullName = "Yönetici",
            };
            db.Users.Add(user);
        }
        user.PasswordHash = hasher.HashPassword(user, password);
        user.SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Admin);
        if (adminRole is null)
        {
            adminRole = new IdentityRole(Roles.Admin) { NormalizedName = Roles.Admin.ToUpperInvariant() };
            db.Roles.Add(adminRole);
        }
        await db.SaveChangesAsync();
        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id))
        {
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }
        return created ? $"Yeni yönetici hesabı açıldı: {email}" : $"Parola değiştirildi, hesap kilidi açıldı ve yönetici yapıldı: {email}";
    }
}
