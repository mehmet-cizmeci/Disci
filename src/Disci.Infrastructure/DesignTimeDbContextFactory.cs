using Microsoft.EntityFrameworkCore.Design;

namespace Disci.Infrastructure;

/// <summary>Used by `dotnet ef`; override the target with the DISCI_DB environment variable.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DisciDbContext>
{
    public DisciDbContext CreateDbContext(string[] args) =>
        new(DisciDbContext.CreateOptions(Environment.GetEnvironmentVariable("DISCI_DB") ?? DisciDbContext.DefaultConnection));
}
