namespace Disci.Domain;

public enum AppointmentStatus
{
    Scheduled = 1,
    Confirmed = 2,
    Arrived = 3,
    Completed = 4,
    NoShow = 5,
    Cancelled = 6,
}

public class Appointment : Entity, ILegacyRecord
{
    public int? LegacyId { get; set; }
    /// <summary>Null for a slot booked for someone not yet registered (see GuestName).</summary>
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public int DentistId { get; set; }
    public Dentist Dentist { get; set; } = null!;
    public int? AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int? Chair { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
}

public class Prescription : Entity, ILegacyRecord
{
    public int? LegacyId { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    /// <summary>The dentist the prescription is issued under.</summary>
    public int DentistId { get; set; }
    public Dentist Dentist { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    /// <summary>Who typed it in; may be an assistant writing on the dentist's behalf.</summary>
    public string? CreatedByUserId { get; set; }
    public List<PrescriptionItem> Items { get; set; } = [];
}

public class PrescriptionItem : Entity
{
    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;
    public int? MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public required string MedicationName { get; set; }
    public string? Dose { get; set; }
    public string? Usage { get; set; }
    public int Quantity { get; set; } = 1;
}
