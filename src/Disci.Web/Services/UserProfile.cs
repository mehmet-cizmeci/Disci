using Disci.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Disci.Web.Services;

/// <summary>
/// Changes a user's name, e-mail and phone. The e-mail is also the login name, so both move together; there is no
/// mail server, so the address is taken as confirmed rather than sending a confirmation link.
/// Used by the admin's user list and by the user's own Hesabım page.
/// </summary>
public static class UserProfile
{
    public static async Task<IdentityResult> UpdateAsync(UserManager<ApplicationUser> users, ApplicationUser user,
        string fullName, string email, string? phone)
    {
        user.FullName = fullName.Trim();
        email = email.Trim();
        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) || user.UserName != email)
        {
            if (await users.FindByEmailAsync(email) is { } other && other.Id != user.Id)
                return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Bu e-posta ile bir kullanıcı zaten var." });
            user.Email = email;
            user.UserName = email;
            user.EmailConfirmed = true;
        }
        user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        return await users.UpdateAsync(user);
    }
}
