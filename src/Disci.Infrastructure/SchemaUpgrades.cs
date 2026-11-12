using Microsoft.EntityFrameworkCore;

namespace Disci.Infrastructure;

/// <summary>
/// One-time renames that EF migrations cannot express, run before <c>Migrate</c>. A database created before 1.3.1
/// recorded migration 20261104110401 and the second legacy-number column under older names; this brings them to the
/// current names (migration id, Patients.SecondProtocol and its index, LegacySource values) so EF sees the database as
/// up to date. Does nothing on new or already converted databases.
/// </summary>
public static class SchemaUpgrades
{
    private const string Sql = """
        IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
           AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
                       WHERE MigrationId LIKE N'20261104110401[_]%' AND MigrationId <> N'20261104110401_SecondLegacySource')
        BEGIN
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            UPDATE [__EFMigrationsHistory] SET MigrationId = N'20261104110401_SecondLegacySource'
            WHERE MigrationId LIKE N'20261104110401[_]%';

            DECLARE @old sysname = (SELECT TOP 1 name FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[Patients]') AND name LIKE N'%Protocol' AND name <> N'SecondProtocol');
            IF @old IS NOT NULL
            BEGIN
                DECLARE @ix sysname = (SELECT TOP 1 i.name FROM sys.indexes i
                    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE i.object_id = OBJECT_ID(N'[Patients]') AND c.name = @old);
                IF @ix IS NOT NULL EXEC(N'DROP INDEX ' + @ix + N' ON [Patients]');
                DECLARE @from nvarchar(300) = N'Patients.' + @old;
                EXEC sp_rename @from, N'SecondProtocol', N'COLUMN';
                EXEC(N'CREATE UNIQUE INDEX [IX_Patients_SecondProtocol] ON [Patients]([SecondProtocol]) WHERE [SecondProtocol] IS NOT NULL');
            END

            EXEC(N'UPDATE [PatientPhotos] SET LegacySource = N''IKINCI'' WHERE LegacySource IS NOT NULL AND LegacySource <> N''IKINCI'';
                   UPDATE [Payments] SET LegacySource = N''IKINCI'' WHERE LegacySource IS NOT NULL AND LegacySource <> N''IKINCI'';
                   UPDATE [TreatmentItems] SET LegacySource = N''IKINCI'' WHERE LegacySource IS NOT NULL AND LegacySource <> N''IKINCI'';
                   UPDATE [TreatmentSessions] SET LegacySource = N''IKINCI'' WHERE LegacySource IS NOT NULL AND LegacySource <> N''IKINCI'';');

            COMMIT;
        END
        """;

    public static async Task ApplyAsync(DisciDbContext db)
    {
        // A brand-new install has no database yet; Migrate creates it.
        if (!db.Database.IsSqlServer() || !await db.Database.CanConnectAsync()) return;
        await db.Database.ExecuteSqlRawAsync(Sql);
    }
}
