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

    /// <summary>What other dentists may do with this dentist's patients, appointments, treatments and prescriptions.</summary>
    public DataAccess ShareWithDentists { get; set; } = DataAccess.Edit;
    /// <summary>The same for assistants.</summary>
    public DataAccess ShareWithAssistants { get; set; } = DataAccess.Edit;
    /// <summary>Per-user exceptions to the two settings above.</summary>
    public List<DentistAccessGrant> AccessGrants { get; set; } = [];
}

/// <summary>How far another user may reach into a dentist's records.</summary>
public enum DataAccess
{
    /// <summary>The dentist's records and name do not appear at all.</summary>
    None = 0,
    View = 1,
    /// <summary>View, and also add, change and delete on the dentist's behalf.</summary>
    Edit = 2,
}

/// <summary>A per-user exception: this user gets exactly this access to the dentist's records.</summary>
public class DentistAccessGrant : Entity
{
    public int DentistId { get; set; }
    public Dentist Dentist { get; set; } = null!;
    public required string UserId { get; set; }
    public DataAccess Access { get; set; }
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

    /// <summary>Users may change their own name, e-mail (login) and phone under Hesabım.</summary>
    public bool UsersCanEditOwnProfile { get; set; } = true;
    /// <summary>Dentists may set their own sharing (who sees their records) under Hesabım; otherwise only the admin can.</summary>
    public bool DentistsManageOwnSharing { get; set; } = true;
}
