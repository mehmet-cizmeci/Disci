using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Disci.Web.Services;

/// <summary>
/// Brings the database up to date and makes sure the roles exist. The first administrator is created only
/// from configuration (Seed:AdminEmail / Seed:AdminPassword, e.g. user-secrets), never from a hard-coded value.
/// </summary>
public sealed class IdentitySeeder(
    DisciDbContext db,
    RoleManager<IdentityRole> roles,
    UserManager<ApplicationUser> users,
    IConfiguration config,
    ILogger<IdentitySeeder> log)
{
    public async Task RunAsync()
    {
        if (config.GetValue("Database:MigrateOnStartup", true))
        {
            await SchemaUpgrades.ApplyAsync(db);
            await db.Database.MigrateAsync();
        }

        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        if (await users.Users.AnyAsync())
            return;

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            log.LogWarning("Hiç kullanıcı yok. İlk yönetici için Seed:AdminEmail ve Seed:AdminPassword ayarlayın.");
            return;
        }

        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Yönetici" };
        var result = await users.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            log.LogError("İlk yönetici oluşturulamadı: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }
        await users.AddToRoleAsync(admin, Roles.Admin);
        log.LogInformation("İlk yönetici oluşturuldu: {Email}", email);
    }
}
