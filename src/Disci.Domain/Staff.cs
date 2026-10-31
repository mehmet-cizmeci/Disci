namespace Disci.Domain;

/// <summary>Hekim. A login account points at its dentist through ApplicationUser.DentistId.</summary>
public class Dentist : Entity, ILegacyRecord
{
    public int? LegacyId { get; set; }
    public required string FullName { get; set; }
    public string? Specialty { get; set; }
    public string? DiplomaNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public TimeOnly? WorkStart { get; set; }
    public TimeOnly? WorkEnd { get; set; }
    public string? CalendarColor { get; set; }
    public bool IsActive { get; set; } = true;
}
