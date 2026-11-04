using System.Globalization;
using Disci.Domain;

namespace Disci.Web.Services;

/// <summary>Turkish labels and formats used across the UI.</summary>
public static class Display
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>
    /// Patient number as staff know it: the DentaLineSuit protocol (or the Disci id), plus the DentaLine 2001 protocol
    /// when the patient also had a card there (that program numbered from 1 again).
    /// </summary>
    public static string Protocol(int id, int? legacyId, int? dl2001Protocol) =>
        $"#{legacyId ?? id}" + (dl2001Protocol is { } d ? $" · DL2001 #{d}" : "");

    public static string Money(decimal amount, string currency = Currencies.Try) => currency switch
    {
        Currencies.Try => amount.ToString("N2", Tr) + " ₺",
        Currencies.Usd => "$" + amount.ToString("N2", Tr),
        Currencies.Eur => "€" + amount.ToString("N2", Tr),
        _ => $"{amount.ToString("N2", Tr)} {currency}",
    };

    /// <summary>Amount written out for receipts: 1250,50 → "BinİkiYüzElliTL ElliKr".</summary>
    public static string MoneyInWords(decimal amount)
    {
        var lira = (long)Math.Floor(Math.Abs(amount));
        var kurus = (int)Math.Round((Math.Abs(amount) - lira) * 100);
        var text = Words(lira) + "TL";
        return kurus > 0 ? $"{text} {Words(kurus)}Kr" : text;
    }

    private static string Words(long n)
    {
        if (n == 0) return "Sıfır";
        string[] ones = ["", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz"];
        string[] tens = ["", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan"];
        string[] groups = ["", "Bin", "Milyon", "Milyar"];

        string Hundreds(int h) =>
            ((h / 100) switch { 0 => "", 1 => "Yüz", var x => ones[x] + "Yüz" }) + tens[h / 10 % 10] + ones[h % 10];

        var result = "";
        for (var g = 0; n > 0 && g < groups.Length; g++, n /= 1000)
        {
            var part = (int)(n % 1000);
            if (part == 0) continue;
            // Turkish says "Bin", not "BirBin".
            var words = g == 1 && part == 1 ? "" : Hundreds(part);
            result = words + groups[g] + result;
        }
        return result;
    }

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

    public static string Name(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "Randevulu",
        AppointmentStatus.Confirmed => "Teyitli",
        AppointmentStatus.Arrived => "Geldi, bekliyor",
        AppointmentStatus.InChair => "Koltukta",
        AppointmentStatus.Completed => "Tamamlandı",
        AppointmentStatus.NoShow => "Gelmedi",
        AppointmentStatus.Cancelled => "İptal",
        _ => status.ToString(),
    };

    /// <summary>Label for the one-click button that moves an appointment to this status.</summary>
    public static string Action(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Confirmed => "Teyit et",
        AppointmentStatus.Arrived => "Geldi",
        AppointmentStatus.InChair => "Koltuğa al",
        AppointmentStatus.Completed => "Bitir",
        AppointmentStatus.NoShow => "Gelmedi",
        AppointmentStatus.Cancelled => "İptal et",
        _ => Name(status),
    };

    /// <summary>CSS modifier used by badges and calendar blocks (see app.css, .st-*).</summary>
    public static string Css(AppointmentStatus status) => "st-" + status.ToString().ToLowerInvariant();

    public static readonly AppointmentStatus[] StatusFlow =
    [
        AppointmentStatus.Scheduled, AppointmentStatus.Confirmed, AppointmentStatus.Arrived,
        AppointmentStatus.InChair, AppointmentStatus.Completed, AppointmentStatus.NoShow, AppointmentStatus.Cancelled,
    ];

    public static string Initials(string? name)
    {
        var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpper(Tr),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpper(Tr),
        };
    }

    public static string Time(DateTime t) => t.ToString("HH:mm", Tr);

    public static string LongDate(DateOnly d) => d.ToString("d MMMM yyyy, dddd", Tr);

    public static string ShortDay(DateOnly d) => d.ToString("ddd d MMM", Tr);

    public static string MonthDay(DateOnly d) => d.ToString("d MMMM", Tr);

    /// <summary>Dentist colour for the calendar; a stable fallback palette when none is set.</summary>
    public static string Color(string? color, int id)
    {
        if (!string.IsNullOrWhiteSpace(color)) return color;
        string[] palette = ["#0d9488", "#6366f1", "#e11d48", "#d97706", "#0284c7", "#7c3aed", "#059669"];
        return palette[Math.Abs(id) % palette.Length];
    }

    /// <summary>
    /// A wa.me link that opens WhatsApp with the message typed in; the assistant still presses send, nothing is sent
    /// by the program. Null when the number does not look like a Turkish mobile.
    /// </summary>
    public static string? WhatsApp(string? phone, string message)
    {
        var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.StartsWith("90") && digits.Length == 12) { }
        else if (digits.StartsWith('0') && digits.Length == 11) digits = "9" + digits;
        else if (digits.StartsWith('5') && digits.Length == 10) digits = "90" + digits;
        else return null;
        return $"https://wa.me/{digits}?text={Uri.EscapeDataString(message)}";
    }

    public static string ReminderText(string name, DateTime start, string dentist) =>
        $"Sayın {name}, {start.ToString("d MMMM dddd", Tr)} saat {start:HH:mm} için {dentist} ile randevunuzu hatırlatırız. Teyit için bu mesaja yanıt verebilirsiniz.";

    public static string Address(Address a) =>
        string.Join(", ", new[] { a.Line1, a.Line2, a.District, a.City, a.PostalCode }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
