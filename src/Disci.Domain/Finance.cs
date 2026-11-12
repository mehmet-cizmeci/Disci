namespace Disci.Domain;

public class Payment : Entity, ILegacySourceRecord
{
    public int? LegacyId { get; set; }
    public string? LegacySource { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateOnly? DueDate { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Currencies.Try;
    public decimal ExchangeRate { get; set; } = 1;
    public string? BankName { get; set; }
    public string? ReceiptNo { get; set; }
    public string? Description { get; set; }
    public bool IsCancelled { get; set; }
    /// <summary>Status carried over from the old program (O/C); meaning of "C" not confirmed, so it is kept but not interpreted.</summary>
    public string? LegacyStatus { get; set; }
    public string? CreatedByUserId { get; set; }
}

/// <summary>Gider hesabı / tedarikçi (laboratuvar, kira, ...).</summary>
public class ExpenseAccount : Entity, ILegacyRecord
{
    public int? LegacyId { get; set; }
    public required string Name { get; set; }
    public string? Kind { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNo { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Expense : Entity, ILegacyRecord
{
    public int? LegacyId { get; set; }
    public int? ExpenseAccountId { get; set; }
    public ExpenseAccount? ExpenseAccount { get; set; }
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PaidOn { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public decimal? BankFee { get; set; }
    public string Currency { get; set; } = Currencies.Try;
    public string? Description { get; set; }
    public string? LegacyStatus { get; set; }
}
