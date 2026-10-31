namespace Disci.Domain;

public enum ProcedureKind
{
    Treatment = 1,
    /// <summary>Teşhis/bulgu (kayıp diş, implant mevcut, ...): recorded, not billed.</summary>
    Finding = 2,
}

public class Procedure : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public ProcedureKind Kind { get; set; }
    public bool ShowOnToothChart { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PriceList : Entity
{
    public required string Name { get; set; }
    public string? LegacyCode { get; set; }
    public string Currency { get; set; } = Currencies.Try;
    public decimal? VatRate { get; set; }
    public bool IsDefault { get; set; }
    public List<PriceListItem> Items { get; set; } = [];
}

public class PriceListItem : Entity
{
    public int PriceListId { get; set; }
    public PriceList PriceList { get; set; } = null!;
    public int ProcedureId { get; set; }
    public Procedure Procedure { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public decimal? VatAmount { get; set; }
    /// <summary>Price charged to the patient, VAT included.</summary>
    public decimal Price { get; set; }
}

public class Medication : Entity
{
    public string? Code { get; set; }
    public required string Name { get; set; }
    public string? Dose { get; set; }
    public string? Usage { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AppointmentType : Entity, ILegacyRecord
{
    public int? LegacyId { get; set; }
    public required string Name { get; set; }
    public int DurationMinutes { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}
