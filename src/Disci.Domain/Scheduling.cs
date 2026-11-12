namespace Disci.Domain;

public enum AppointmentStatus
{
    Scheduled = 1,
    Confirmed = 2,
    Arrived = 3,
    Completed = 4,
    NoShow = 5,
    Cancelled = 6,
    /// <summary>Hekim hastayı koltuğa aldı (Arrived'dan sonra, Completed'dan önce).</summary>
    InChair = 7,
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
    /// <summary>When reception checked the patient in; drives the waiting-room timer.</summary>
    public DateTime? ArrivedAt { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
}

/// <summary>Görev / hatırlatma: a to-do for the front desk or a dentist, optionally about a patient.</summary>
public class Reminder : Entity
{
    public required string Title { get; set; }
    public string? Notes { get; set; }
    public DateOnly DueDate { get; set; }
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
    /// <summary>Role the task is meant for (Asistan, Hekim); null = everyone.</summary>
    public string? ForRole { get; set; }
    public bool IsDone { get; set; }
    public DateTime? DoneAt { get; set; }
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

/// <summary>Reçete şablonu: a ready set of medicines ("Çekim sonrası") added to a prescription with one click.</summary>
public class PrescriptionTemplate : Entity
{
    public required string Name { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PrescriptionTemplateItem> Items { get; set; } = [];
}

public class PrescriptionTemplateItem : Entity
{
    public int PrescriptionTemplateId { get; set; }
    public PrescriptionTemplate PrescriptionTemplate { get; set; } = null!;
    public int? MedicationId { get; set; }
    public Medication? Medication { get; set; }
    public required string MedicationName { get; set; }
    public string? Dose { get; set; }
    public string? Usage { get; set; }
    public int Quantity { get; set; } = 1;
}
