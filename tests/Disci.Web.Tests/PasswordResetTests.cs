using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Disci.LegacyImport.Import;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>The emergency administrator password reset (deploy/kurulum/parola-sifirla.ps1).</summary>
public sealed class PasswordResetTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    [Fact]
    public async Task Creates_then_resets_an_administrator_the_program_accepts()
    {
        using (var scope = factory.Services.CreateScope())
            Assert.StartsWith("Yeni yönetici", await PasswordReset.RunAsync(scope.ServiceProvider.GetRequiredService<DisciDbContext>(), "acil@ornek.test", "IlkParola1"));
        using (var scope = factory.Services.CreateScope())
            Assert.StartsWith("Parola değiştirildi", await PasswordReset.RunAsync(scope.ServiceProvider.GetRequiredService<DisciDbContext>(), "ACIL@ornek.test", "YeniParola2"));

        using var check = factory.Services.CreateScope();
        var users = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync("acil@ornek.test");
        Assert.NotNull(user);
        Assert.True(await users.CheckPasswordAsync(user, "YeniParola2"));
        Assert.False(await users.CheckPasswordAsync(user, "IlkParola1"));
        Assert.True(await users.IsInRoleAsync(user, Roles.Admin));
    }
}
