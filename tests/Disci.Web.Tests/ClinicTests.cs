using System.Net;
using Disci.Domain;
using Disci.Infrastructure;
using Disci.Infrastructure.Identity;
using Disci.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Web.Tests;

/// <summary>Front-desk screens, money rules and booking rules.</summary>
public sealed class ClinicTests(DisciWebFactory factory) : IClassFixture<DisciWebFactory>
{
    private static async Task<string> Page(HttpClient client, string url) =>
        WebUtility.HtmlDecode(await client.GetStringAsync(url));

    [Theory]
    [InlineData("/randevular", "Randevular")]
    [InlineData("/bildirimler", "Bildirimler ve görevler")]
    [InlineData("/hastalar/yeni", "Yeni hasta")]
    public async Task Front_desk_pages_open_for_assistants(string url, string heading)
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), url);
        Assert.Contains(heading, html);
    }

    [Theory]
    [InlineData("/kasa")]
    [InlineData("/fiyatlar")]
    public async Task Finance_pages_are_for_admins_and_dentists(string url)
    {
        Assert.Equal(HttpStatusCode.OK, (await factory.ClientAs(Roles.Dentist).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientAs(Roles.Assistant).GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Settings_are_for_admins_only()
    {
        var html = await Page(factory.ClientAs(Roles.Admin), "/ayarlar");
        Assert.Contains("Deneme Hekim", html);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientAs(Roles.Dentist).GetAsync("/ayarlar")).StatusCode);
    }

    [Fact]
    public async Task Price_list_shows_seeded_price()
    {
        var html = await Page(factory.ClientAs(Roles.Admin), "/fiyatlar");
        Assert.Contains("Deneme tarifesi", html);
        Assert.Contains("Deneme dolgu", html);
    }

    [Fact]
    public async Task Cash_report_lists_this_months_collection()
    {
        var html = await Page(factory.ClientAs(Roles.Admin), "/kasa");
        Assert.Contains(factory.Data.LastName, html);
        Assert.Contains("1.000,00 ₺", html);
    }

    [Fact]
    public async Task Patient_page_shows_finance_summary_and_actions()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), $"/hastalar/{factory.Data.PatientId}");
        Assert.Contains("Toplam ücret", html);
        Assert.Contains("1.500,00 ₺", html);
        Assert.Contains("Ödeme al", html);
        Assert.Contains("Arşive kaldır", html);
    }

    [Fact]
    public async Task Receipt_prints_amount_and_words()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), $"/makbuz/{factory.Data.PaymentId}");
        Assert.Contains("TAHSİLAT MAKBUZU", html);
        Assert.Contains("1.000,00 ₺", html);
        Assert.Contains("BinTL", html);
    }

    [Fact]
    public async Task Debtor_filter_lists_patients_who_owe()
    {
        var html = await Page(factory.ClientAs(Roles.Assistant), "/hastalar?filtre=borclu");
        Assert.Contains(factory.Data.LastName, html);
        Assert.DoesNotContain("KİŞİ", html);
    }

    [Fact]
    public async Task Ledger_balance_is_completed_treatments_minus_payments()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
        Assert.Equal(500m, await Ledger.BalanceAsync(db, factory.Data.PatientId));
    }

    [Fact]
    public async Task Overlapping_booking_for_the_same_dentist_is_a_conflict()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
        var start = DateTime.Today.AddDays(1).AddHours(10).AddMinutes(15);

        var overlapping = new Appointment { DentistId = factory.Data.DentistId, GuestName = "Misafir", Start = start, End = start.AddMinutes(30) };
        var conflicts = await Scheduling.ConflictsAsync(db, overlapping);
        Assert.Contains(conflicts, c => c.Id == factory.Data.TomorrowAppointmentId);

        var afterwards = new Appointment { DentistId = factory.Data.DentistId, GuestName = "Misafir", Start = start.AddMinutes(15), End = start.AddMinutes(45) };
        Assert.Empty(await Scheduling.ConflictsAsync(db, afterwards));
    }

    [Fact]
    public async Task Notifications_list_unconfirmed_bookings_and_overdue_tasks()
    {
        using var scope = factory.Services.CreateScope();
        var notices = await scope.ServiceProvider.GetRequiredService<NotificationService>().GetAsync();
        Assert.Contains(notices, n => n.Kind == NoticeKind.Unconfirmed && n.AppointmentId == factory.Data.TomorrowAppointmentId);
        Assert.Contains(notices, n => n.Kind == NoticeKind.Task && n.Tone == NoticeTone.Danger);
    }

    [Fact]
    public void Arrival_is_stamped_and_cleared_when_moved_back()
    {
        var a = new Appointment { Start = DateTime.Today, End = DateTime.Today.AddMinutes(30) };
        var now = new DateTime(2026, 1, 1, 10, 5, 0);
        Scheduling.SetStatus(a, AppointmentStatus.Arrived, now);
        Assert.Equal(now, a.ArrivedAt);
        Scheduling.SetStatus(a, AppointmentStatus.InChair, now.AddMinutes(10));
        Assert.Equal(now, a.ArrivedAt);
        Scheduling.SetStatus(a, AppointmentStatus.Scheduled, now);
        Assert.Null(a.ArrivedAt);
    }

    [Theory]
    [InlineData("1000", "BinTL")]
    [InlineData("1250.50", "BinİkiYüzElliTL ElliKr")]
    [InlineData("21000", "YirmiBirBinTL")]
    [InlineData("101", "YüzBirTL")]
    [InlineData("2000000", "İkiMilyonTL")]
    public void Amounts_are_written_in_turkish(string amount, string expected) =>
        Assert.Equal(expected, Display.MoneyInWords(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("0 555 000 0001", "905550000001")]
    [InlineData("5550000001", "905550000001")]
    [InlineData("+90 555 000 00 01", "905550000001")]
    public void WhatsApp_links_use_international_numbers(string phone, string expected) =>
        Assert.StartsWith($"https://wa.me/{expected}?text=", Display.WhatsApp(phone, "Merhaba"));

    [Fact]
    public void WhatsApp_link_is_skipped_for_landlines_too_short() => Assert.Null(Display.WhatsApp("212 555", "x"));
}
