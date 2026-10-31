using System.Globalization;
using Disci.Domain;

namespace Disci.Web.Services;

/// <summary>Turkish labels and formats used across the UI.</summary>
public static class Display
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public static string Money(decimal amount, string currency = Currencies.Try) => currency switch
    {
        Currencies.Try => amount.ToString("N2", Tr) + " ₺",
        Currencies.Usd => "$" + amount.ToString("N2", Tr),
        Currencies.Eur => "€" + amount.ToString("N2", Tr),
        _ => $"{amount.ToString("N2", Tr)} {currency}",
    };

    public static string Date(DateOnly? date) => date?.ToString("dd.MM.yyyy", Tr) ?? "";

    public static string Age(DateOnly? birth)
    {
        if (birth is not { } b) return "";
        var today = DateOnly.FromDateTime(DateTime.Today);
        int age = today.Year - b.Year - (today < b.AddYears(today.Year - b.Year) ? 1 : 0);
        return age is >= 0 and < 130 ? $"{age} yaş" : "";
    }

    public static string Name(TreatmentKind kind) => kind switch
    {
        TreatmentKind.Finding => "Bulgu",
        TreatmentKind.Planned => "Plan",
        TreatmentKind.Completed => "Yapıldı",
        _ => kind.ToString(),
    };

    public static string Name(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Nakit",
        PaymentMethod.CreditCard => "Kredi kartı",
        PaymentMethod.BankTransfer => "Havale/EFT",
        PaymentMethod.Cheque => "Çek/Senet",
        _ => "Diğer",
    };

    public static string Name(Gender gender) => gender switch
    {
        Gender.Female => "Kadın",
        Gender.Male => "Erkek",
        _ => "",
    };

    public static string Name(PhotoKind kind) => kind switch
    {
        PhotoKind.IntraOral => "Ağız içi",
        PhotoKind.Face => "Yüz",
        PhotoKind.XRay => "Röntgen",
        _ => "Diğer",
    };

    public static string Name(RelativeType type) => type switch
    {
        RelativeType.EmergencyContact => "Yakını",
        RelativeType.Father => "Baba",
        RelativeType.Mother => "Anne",
        RelativeType.Spouse => "Eşi",
        _ => "Diğer",
    };

    public static string Address(Address a) =>
        string.Join(", ", new[] { a.Line1, a.Line2, a.District, a.City, a.PostalCode }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
