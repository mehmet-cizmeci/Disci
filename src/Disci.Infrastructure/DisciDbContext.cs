using Disci.Domain;
using Disci.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Disci.Infrastructure;

public class DisciDbContext(DbContextOptions<DisciDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{

    public const string DefaultConnection =
        @"Server=(localdb)\MSSQLLocalDB;Database=Disci;Trusted_Connection=True;TrustServerCertificate=True";

    // Identity picks its table layout from IdentityOptions in the application's services. The web app configures
    // version 3 (passkeys) itself; tools outside it must hand over the same setting or they build a different model.
    private static readonly IServiceProvider IdentitySchemaServices = new ServiceCollection()
        .Configure<IdentityOptions>(o => o.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
        .BuildServiceProvider();

    /// <summary>Options for code running outside the web app (importer, design-time tools).</summary>
    public static DbContextOptions<DisciDbContext> CreateOptions(string connection, int commandTimeoutSeconds = 30) =>
        new DbContextOptionsBuilder<DisciDbContext>()
            .UseSqlServer(connection, sql => sql.CommandTimeout(commandTimeoutSeconds))
            .UseApplicationServiceProvider(IdentitySchemaServices)
            .Options;

    public DbSet<Dentist> Dentists => Set<Dentist>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientRelative> PatientRelatives => Set<PatientRelative>();
    public DbSet<Anamnesis> Anamneses => Set<Anamnesis>();
    public DbSet<MedicalCondition> MedicalConditions => Set<MedicalCondition>();
    public DbSet<PatientMedicalCondition> PatientMedicalConditions => Set<PatientMedicalCondition>();
    public DbSet<ToothNote> ToothNotes => Set<ToothNote>();
    public DbSet<PatientPhoto> PatientPhotos => Set<PatientPhoto>();
    public DbSet<PatientNote> PatientNotes => Set<PatientNote>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<TreatmentSession> TreatmentSessions => Set<TreatmentSession>();
    public DbSet<TreatmentItem> TreatmentItems => Set<TreatmentItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpenseAccount> ExpenseAccounts => Set<ExpenseAccount>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PrescriptionTemplate> PrescriptionTemplates => Set<PrescriptionTemplate>();
    public DbSet<PrescriptionTemplateItem> PrescriptionTemplateItems => Set<PrescriptionTemplateItem>();
    public DbSet<Reminder> Reminders => Set<Reminder>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100);
            e.HasOne(x => x.Dentist).WithMany().HasForeignKey(x => x.DentistId);
        });

        b.Entity<Dentist>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.CalendarColor).HasMaxLength(9);
            LegacyIndex(e);
        });

        b.Entity<Patient>(e =>
        {
            e.Ignore(x => x.FullName);
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.NationalId).HasMaxLength(20);
            e.Property(x => x.BloodGroup).HasMaxLength(10);
            e.Property(x => x.MobilePhone).HasMaxLength(30);
            e.Property(x => x.HomePhone).HasMaxLength(30);
            e.Property(x => x.WorkPhone).HasMaxLength(30);
            e.Property(x => x.ReferrerPhone).HasMaxLength(30);
            e.Property(x => x.DiscountRate).HasPrecision(5, 2);
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.LegacyExtra).HasMaxLength(-1);
            e.OwnsOne(x => x.HomeAddress, a => AddressColumns(a, "Home"));
            e.OwnsOne(x => x.WorkAddress, a => AddressColumns(a, "Work"));
            e.HasOne(x => x.Anamnesis).WithOne(x => x.Patient).HasForeignKey<Anamnesis>(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.LastName, x.FirstName });
            e.HasIndex(x => x.MobilePhone);
            e.HasIndex(x => x.NationalId);
            LegacyIndex(e);
        });

        Child<PatientRelative>(b, x => x.Patient, p => p.Relatives);
        Child<PatientMedicalCondition>(b, x => x.Patient, p => p.MedicalConditions);
        Child<ToothNote>(b, x => x.Patient, p => p.ToothNotes);
        Child<PatientPhoto>(b, x => x.Patient, p => p.Photos);
        Child<PatientNote>(b, x => x.Patient, p => p.PatientNotes);

        b.Entity<Anamnesis>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(4000);
            e.Property(x => x.DrugAllergies).HasMaxLength(1000);
            e.Property(x => x.FoodAllergies).HasMaxLength(1000);
        });
        b.Entity<MedicalCondition>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
        });
        b.Entity<PatientMedicalCondition>(e => e.HasIndex(x => new { x.PatientId, x.MedicalConditionId }).IsUnique());
        b.Entity<ToothNote>(e =>
        {
            e.Property(x => x.Note).HasMaxLength(4000);
            e.HasIndex(x => new { x.PatientId, x.ToothNumber });
        });
        b.Entity<PatientPhoto>(e =>
        {
            e.Property(x => x.FilePath).HasMaxLength(400);
            LegacyIndex(e);
        });
        b.Entity<PatientNote>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(-1);
            e.Property(x => x.LegacyRtf).HasMaxLength(-1);
        });

        b.Entity<Procedure>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(20);
            e.HasIndex(x => x.Code).IsUnique();
        });
        b.Entity<PriceList>(e =>
        {
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.VatRate).HasPrecision(5, 2);
        });
        b.Entity<PriceListItem>(e =>
        {
            e.HasOne(x => x.PriceList).WithMany(x => x.Items).HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PriceListId, x.ProcedureId }).IsUnique();
        });
        b.Entity<AppointmentType>(e =>
        {
            e.Property(x => x.Color).HasMaxLength(9);
            LegacyIndex(e);
        });

        b.Entity<TreatmentSession>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.LegacyStatus).HasMaxLength(5);
            e.HasIndex(x => new { x.PatientId, x.Date });
            LegacyIndex(e);
        });
        b.Entity<TreatmentItem>(e =>
        {
            e.HasOne(x => x.TreatmentSession).WithMany(x => x.Items).HasForeignKey(x => x.TreatmentSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Currency).HasMaxLength(3);
            LegacyIndex(e);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.ExchangeRate).HasPrecision(18, 6);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.LegacyStatus).HasMaxLength(5);
            e.Property(x => x.CreatedByUserId).HasMaxLength(450);
            e.HasIndex(x => new { x.PatientId, x.Date });
            LegacyIndex(e);
        });
        b.Entity<ExpenseAccount>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            LegacyIndex(e);
        });
        b.Entity<Expense>(e =>
        {
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.LegacyStatus).HasMaxLength(5);
            e.HasIndex(x => x.Date);
            LegacyIndex(e);
        });

        b.Entity<Appointment>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.CreatedByUserId).HasMaxLength(450);
            e.HasIndex(x => new { x.DentistId, x.Start });
            LegacyIndex(e);
        });
        b.Entity<Prescription>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.CreatedByUserId).HasMaxLength(450);
            LegacyIndex(e);
        });
        b.Entity<Reminder>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.ForRole).HasMaxLength(50);
            e.Property(x => x.CreatedByUserId).HasMaxLength(450);
            e.HasIndex(x => new { x.IsDone, x.DueDate });
        });
        b.Entity<PrescriptionTemplate>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(1000);
        });
        b.Entity<PrescriptionTemplateItem>(e => e
            .HasOne(x => x.PrescriptionTemplate).WithMany(x => x.Items).HasForeignKey(x => x.PrescriptionTemplateId)
            .OnDelete(DeleteBehavior.Cascade));
        b.Entity<PrescriptionItem>(e => e
            .HasOne(x => x.Prescription).WithMany(x => x.Items).HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade));

        // Identity tables keep their own defaults; the rules below are for clinic data only.
        var domainTypes = b.Model.GetEntityTypes().Where(e => e.ClrType.Namespace == typeof(Patient).Namespace).ToList();

        // Only detail rows follow their parent; nothing clinical or financial disappears through a cascade by accident.
        foreach (var fk in domainTypes.SelectMany(e => e.GetForeignKeys()).Where(fk => !fk.IsOwnership))
        {
            var pair = (fk.DeclaringEntityType.ClrType, fk.PrincipalEntityType.ClrType);
            fk.DeleteBehavior = CascadingChildren.Contains(pair) ? DeleteBehavior.Cascade : DeleteBehavior.Restrict;
        }

        foreach (var p in domainTypes.SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null))
            p.SetMaxLength(200);
    }

    private static readonly HashSet<(Type Child, Type Parent)> CascadingChildren =
    [
        (typeof(PatientRelative), typeof(Patient)),
        (typeof(PatientMedicalCondition), typeof(Patient)),
        (typeof(ToothNote), typeof(Patient)),
        (typeof(PatientPhoto), typeof(Patient)),
        (typeof(PatientNote), typeof(Patient)),
        (typeof(Anamnesis), typeof(Patient)),
        (typeof(PriceListItem), typeof(PriceList)),
        (typeof(TreatmentItem), typeof(TreatmentSession)),
        (typeof(PrescriptionItem), typeof(Prescription)),
        (typeof(PrescriptionTemplateItem), typeof(PrescriptionTemplate)),
    ];

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampUpdates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        StampUpdates();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void StampUpdates()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;
    }

    private static void AddressColumns(OwnedNavigationBuilder<Patient, Address> a, string prefix)
    {
        a.Property(x => x.Line1).HasColumnName(prefix + "AddressLine1");
        a.Property(x => x.Line2).HasColumnName(prefix + "AddressLine2");
        a.Property(x => x.PostalCode).HasColumnName(prefix + "PostalCode").HasMaxLength(10);
        a.Property(x => x.District).HasColumnName(prefix + "District").HasMaxLength(100);
        a.Property(x => x.City).HasColumnName(prefix + "City").HasMaxLength(100);
    }

    private static void LegacyIndex<T>(EntityTypeBuilder<T> e) where T : class, ILegacyRecord =>
        e.HasIndex(x => x.LegacyId).IsUnique().HasFilter("[LegacyId] IS NOT NULL");

    private static void Child<T>(ModelBuilder b,
        System.Linq.Expressions.Expression<Func<T, Patient?>> parent,
        System.Linq.Expressions.Expression<Func<Patient, IEnumerable<T>?>> children) where T : class =>
        b.Entity<T>().HasOne(parent).WithMany(children).OnDelete(DeleteBehavior.Cascade);
}
