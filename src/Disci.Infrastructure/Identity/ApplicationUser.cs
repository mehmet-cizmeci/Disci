using Disci.Domain;
using Microsoft.AspNetCore.Identity;

namespace Disci.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    /// <summary>Set for dentists' own accounts; assistants and admins usually have none.</summary>
    public int? DentistId { get; set; }
    public Dentist? Dentist { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Screen language chosen by the user ("tr", "en"); null = Turkish.</summary>
    public string? Language { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Dentist = "Hekim";
    public const string Assistant = "Asistan";

    public static readonly string[] All = [Admin, Dentist, Assistant];

    public static string DisplayName(string role) => role switch
    {
        Admin => "Yönetici",
        Dentist => "Diş hekimi",
        Assistant => "Asistan",
        _ => role,
    };
}
