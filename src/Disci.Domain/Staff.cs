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

/// <summary>
/// The clinic's own details, printed on receipts and prescriptions. One row, filled in by the first-run setup form
/// and editable under Klinik ayarları.
/// </summary>
public class ClinicProfile : Entity
{
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNo { get; set; }
}
