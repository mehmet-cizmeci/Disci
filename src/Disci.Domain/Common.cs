namespace Disci.Domain;

public abstract class Entity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Records carried over from DentaLineSuit keep their original key for traceability.</summary>
public interface ILegacyRecord
{
    int? LegacyId { get; set; }
}

public static class Currencies
{
    public const string Try = "TRY";
    public const string Usd = "USD";
    public const string Eur = "EUR";
}

public enum Gender
{
    Unknown = 0,
    Female = 1,
    Male = 2,
}

public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    BankTransfer = 3,
    Cheque = 4,
    Other = 9,
}

public class Address
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
}
