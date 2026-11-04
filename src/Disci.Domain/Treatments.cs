namespace Disci.Domain;

public enum TreatmentKind
{
    /// <summary>Mevcut durum / teşhis.</summary>
    Finding = 1,
    Planned = 2,
    Completed = 3,
}

/// <summary>One visit or chart entry for a patient (DentaLineSuit DLORALTD).</summary>
public class TreatmentSession : Entity, ILegacySourceRecord
{
    public int? LegacyId { get; set; }
    public string? LegacySource { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    /// <summary>Null for imported records: the old system did not store who did the work.</summary>
    public int? DentistId { get; set; }
    public Dentist? Dentist { get; set; }
    public DateOnly Date { get; set; }
    public TreatmentKind Kind { get; set; }
    public string? Notes { get; set; }
    public string? LegacyStatus { get; set; }
    public List<TreatmentItem> Items { get; set; } = [];
}

/// <summary>A procedure on one tooth (DentaLineSuit DLOR_DET).</summary>
public class TreatmentItem : Entity, ILegacySourceRecord
{
    public int? LegacyId { get; set; }
    public string? LegacySource { get; set; }
    public int TreatmentSessionId { get; set; }
    public TreatmentSession TreatmentSession { get; set; } = null!;
    public int? ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }
    public required string Description { get; set; }
    /// <summary>FDI notation; null for whole-mouth procedures.</summary>
    public int? ToothNumber { get; set; }
    /// <summary>Tooth surfaces as a bit mask, as stored by the old system.</summary>
    public int SurfaceMask { get; set; }
    public decimal Price { get; set; }
    public decimal VatAmount { get; set; }
    public string Currency { get; set; } = Currencies.Try;
    public int? LegacyTbdr { get; set; }
}
