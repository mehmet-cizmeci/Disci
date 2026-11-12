using System.Security.Claims;
using System.Text.Encodings.Web;
using Disci.Domain;
using Disci.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Disci.Web.Tests;

/// <summary>
/// Runs the real app against a throw-away LocalDB database filled with made-up data, deleted afterwards.
/// Requests carrying the X-Test-Role header are signed in as a fake user with that role; no account or password is involved.
/// </summary>
public class DisciWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string RoleHeader = "X-Test-Role";

    private readonly string _database = $"Disci_Tests_{Guid.NewGuid():N}";
    private readonly string _filesRoot = Path.Combine(Path.GetTempPath(), "disci-tests-" + Guid.NewGuid().ToString("N"));

    public TestData Data { get; private set; } = default!;

    /// <summary>"Development" serves static web assets (blazor.web.js) the way `dotnet run` does.</summary>
    public string EnvironmentName { get; init; } = "Testing";
    /// <summary>True only for the tests of the first-run setup form; the others start without accounts and must not be redirected.</summary>
    public bool FirstRunSetupEnabled { get; init; }

    private string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={_database};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        // Never create the real first administrator (user-secrets) in a test database.
        builder.UseSetting("Seed:AdminEmail", "");
        builder.UseSetting("Seed:AdminPassword", "");
        // The tests start without accounts; the first-run setup page has its own tests.
        builder.UseSetting("FirstRunSetup:Enabled", FirstRunSetupEnabled ? "true" : "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("FileStorage:Root", _filesRoot);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        // Tests never contact GitHub.
        builder.UseSetting("Update:Enabled", "false");
        builder.UseSetting("Update:Folder", Path.Combine(_filesRoot, "Guncelleme"));
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    public async Task InitializeAsync()
    {
        // Touching Services starts the app, which creates the database and roles.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DisciDbContext>();
        Data = await TestData.SeedAsync(db, _filesRoot);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<DisciDbContext>().Database.EnsureDeletedAsync();
        if (Directory.Exists(_filesRoot))
            Directory.Delete(_filesRoot, recursive: true);
        await base.DisposeAsync();
    }

    public HttpClient ClientAs(string? role)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (role is not null) client.DefaultRequestHeaders.Add(RoleHeader, role);
        return client;
    }
}

/// <summary>Invented patients, treatments and payments; nothing here comes from a real clinic.</summary>
public sealed record TestData(int PatientId, string LastName, int PhotoId, int PatientCount, int DentistId, int PaymentId, int TomorrowAppointmentId, int PrescriptionId)
{
    public static async Task<TestData> SeedAsync(DisciDbContext db, string filesRoot)
    {
        var dentist = new Dentist { FullName = "Deneme Hekim" };
        var procedure = new Procedure { Code = "TST01", Name = "Deneme dolgu", Category = "Deneme", Kind = ProcedureKind.Treatment };
        var today = DateOnly.FromDateTime(DateTime.Today);

        var main = new Patient
        {
            FirstName = "Deneme",
            LastName = "HASTAOĞLU",
            MobilePhone = "0 555 000 0001",
            BirthDate = new DateOnly(1980, 1, 1),
            RegisteredOn = today.AddYears(-1),
            PrimaryDentist = dentist,
        };
        main.TreatmentSessions.Add(new TreatmentSession
        {
            Date = today.AddDays(-3),
            Kind = TreatmentKind.Completed,
            Dentist = dentist,
            Items = [new TreatmentItem { Procedure = procedure, Description = "Deneme dolgu", ToothNumber = 16, Price = 1500 }],
        });
        main.Payments.Add(new Payment { Date = today, Method = PaymentMethod.Cash, Amount = 1000 });
        main.Anamnesis = new Anamnesis { DrugAllergies = "Penisilin" };
        main.Photos.Add(new PatientPhoto { TakenOn = today, Kind = PhotoKind.IntraOral, FilePath = "patients/test/photo.jpg" });

        var others = new[]
        {
            new Patient { FirstName = "Örnek", LastName = "KİŞİ", RegisteredOn = today },
            new Patient { FirstName = "Arşiv", LastName = "KAYDI", RegisteredOn = today, IsArchived = true },
        };

        var priceList = new PriceList { Name = "Deneme tarifesi", IsDefault = true };
        priceList.Items.Add(new PriceListItem { Procedure = procedure, BasePrice = 1500, Price = 1500 });

        // Tomorrow 10:00, not yet confirmed: should appear on the confirmation-call list.
        var tomorrow = new Appointment
        {
            Patient = main,
            Dentist = dentist,
            Start = DateTime.Today.AddDays(1).AddHours(10),
            End = DateTime.Today.AddDays(1).AddHours(10).AddMinutes(30),
        };
        var overdueTask = new Reminder { Title = "Deneme görevi: laboratuvarı ara", DueDate = today.AddDays(-1), Patient = main };

        var medication = new Medication { Name = "Deneme Amoksisilin", Dose = "1000 mg", Usage = "Günde 2 kez 1 tablet" };
        var prescription = new Prescription
        {
            Patient = main,
            Dentist = dentist,
            Date = today,
            Diagnosis = "Deneme tanısı",
            Items = [new PrescriptionItem { Medication = medication, MedicationName = medication.Name, Dose = medication.Dose, Usage = medication.Usage, Quantity = 2 }],
        };

        var template = new PrescriptionTemplate
        {
            Name = "Deneme şablonu",
            Diagnosis = "Deneme şablon tanısı",
            Items = [new PrescriptionTemplateItem { Medication = medication, MedicationName = medication.Name, Dose = medication.Dose, Quantity = 1 }],
        };

        db.AddRange(main, others[0], others[1], priceList, tomorrow, overdueTask, prescription, template);
        await db.SaveChangesAsync();

        // A tiny valid JPEG header is enough for the file endpoint test.
        var photoPath = Path.Combine(filesRoot, "patients", "test", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(photoPath)!);
        await File.WriteAllBytesAsync(photoPath, [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0xFF, 0xD9]);

        return new TestData(main.Id, main.LastName, main.Photos[0].Id, PatientCount: 2, dentist.Id, main.Payments[0].Id, tomorrow.Id, prescription.Id);
    }
}

public sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // The header is for HttpClient tests; the cookie lets a browser drive the test server by hand.
        if (!Request.Headers.TryGetValue(DisciWebFactory.RoleHeader, out var role)
            && !Request.Cookies.TryGetValue("test-role", out var cookieRole))
            return Task.FromResult(AuthenticateResult.NoResult());
        if (string.IsNullOrEmpty(role)) role = Request.Cookies["test-role"];

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, Request.Cookies["test-name"] ?? "test@disci.local"),
            new Claim(ClaimTypes.Role, role.ToString()),
        ], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Mirror the cookie scheme: anonymous users are sent to the login page.
        Response.Redirect("/Account/Login");
        return Task.CompletedTask;
    }
}
