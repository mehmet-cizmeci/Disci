using Microsoft.AspNetCore.Identity;

namespace Disci.Web.Services;

/// <summary>Turkish texts for the account errors staff can see (password rules, duplicate names).</summary>
public sealed class TurkishIdentityErrors : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Parola en az {length} karakter olmalı." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Parolada en az bir rakam (0-9) olmalı." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Parolada en az bir küçük harf olmalı." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Parolada en az bir büyük harf olmalı." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Parolada en az bir simge (ör. ! veya *) olmalı." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Parolada en az {uniqueChars} farklı karakter olmalı." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Parola yanlış." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"'{userName}' adıyla bir kullanıcı zaten var." };
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"'{email}' e-postasıyla bir kullanıcı zaten var." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"'{userName}' kullanıcı adı olarak kullanılamaz." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"'{email}' geçerli bir e-posta adresi değil." };
    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = $"Kullanıcı zaten '{role}' rolünde." };
}
