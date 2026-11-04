namespace Disci.Domain;

public class Patient : Entity, ILegacyRecord
{
    /// <summary>DentaLineSuit PROTOKOL number.</summary>
    public int? LegacyId { get; set; }
    /// <summary>PROTOKOL number in DentaLine 2001 (separate numbering; the same number can belong to another patient in DentaLineSuit).</summary>
    public int? Dl2001Protocol { get; set; }
    public required string FirstName { get; set; }
    public string LastName { get; set; } = "";
    public string FullName => string.IsNullOrEmpty(LastName) ? FirstName : $"{FirstName} {LastName}";

    public string? NationalId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? BirthPlace { get; set; }
    public Gender Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? Nationality { get; set; }
    public string? Occupation { get; set; }

    public string? MobilePhone { get; set; }
    public string? HomePhone { get; set; }
    public string? WorkPhone { get; set; }
    public string? Email { get; set; }
    public string? SecondaryEmail { get; set; }
    public Address HomeAddress { get; set; } = new();
    public Address WorkAddress { get; set; } = new();

    public DateOnly RegisteredOn { get; set; }
    public string? ReferredBy { get; set; }
    public string? ReferrerPhone { get; set; }
    public string? FamilyDoctor { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsuranceNo { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNo { get; set; }
    public decimal? DiscountRate { get; set; }
    public string? Notes { get; set; }

    /// <summary>Null = shared pool, otherwise the patient's own dentist.</summary>
    public int? PrimaryDentistId { get; set; }
    public Dentist? PrimaryDentist { get; set; }
    public int? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    /// <summary>Old-system fields with no column here, kept as JSON so nothing is lost.</summary>
    public string? LegacyExtra { get; set; }
    public bool IsArchived { get; set; }

    public Anamnesis? Anamnesis { get; set; }
    public List<PatientRelative> Relatives { get; set; } = [];
    public List<PatientMedicalCondition> MedicalConditions { get; set; } = [];
    public List<ToothNote> ToothNotes { get; set; } = [];
    public List<PatientPhoto> Photos { get; set; } = [];
    public List<PatientNote> PatientNotes { get; set; } = [];
    public List<TreatmentSession> TreatmentSessions { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    /// <summary>Further old numbers of this patient: the same person had more than one card (e.g. twice in DentaLine 2001).</summary>
    public List<PatientLegacyNumber> LegacyNumbers { get; set; } = [];
}

/// <summary>An additional old-system protocol number of a patient whose duplicate cards were merged on import.</summary>
public class PatientLegacyNumber : Entity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    /// <summary>A <see cref="LegacySources"/> value.</summary>
    public required string Source { get; set; }
    public int Number { get; set; }
}

public enum RelativeType
{
    EmergencyContact = 1,
    Father = 2,
    Mother = 3,
    Spouse = 4,
    Other = 9,
}

public class PatientRelative : Entity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public RelativeType Type { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Occupation { get; set; }
}

/// <summary>Anamnez: free-text part; yes/no findings are PatientMedicalCondition rows.</summary>
public class Anamnesis : Entity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string? Notes { get; set; }
    public string? DrugAllergies { get; set; }
    public string? FoodAllergies { get; set; }
    public DateOnly? PregnancyDate { get; set; }
    public string? ExtraOralOther { get; set; }
    public string? IntraOralOther { get; set; }
}

public class MedicalCondition : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PatientMedicalCondition : Entity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int MedicalConditionId { get; set; }
    public MedicalCondition MedicalCondition { get; set; } = null!;
    public string? Note { get; set; }
}

public class ToothNote : Entity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    /// <summary>FDI notation: 11-48 permanent, 51-85 deciduous.</summary>
    public int ToothNumber { get; set; }
    public required string Note { get; set; }
}

public enum PhotoKind
{
    IntraOral = 1,
    Face = 2,
    XRay = 3,
    Other = 9,
}

public class PatientPhoto : Entity, ILegacySourceRecord
{
    public int? LegacyId { get; set; }
    public string? LegacySource { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateOnly TakenOn { get; set; }
    public PhotoKind Kind { get; set; }
    /// <summary>1-4 = quadrant, 5 = whole mouth.</summary>
    public int? Jaw { get; set; }
    public int? ToothNumber { get; set; }
    public string? Description { get; set; }
    /// <summary>Relative to the file storage root.</summary>
    public required string FilePath { get; set; }
}

public class PatientNote : Entity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public required string Text { get; set; }
    public string? LegacyRtf { get; set; }
}
