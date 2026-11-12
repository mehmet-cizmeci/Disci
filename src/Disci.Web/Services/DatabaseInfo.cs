using Microsoft.Data.SqlClient;

namespace Disci.Web.Services;

/// <summary>
/// Which SQL Server the program uses, for the Klinik ayarları page (never the password). The connection is changed by
/// the installer's script (veritabani-ayarla.ps1 / .sh), not by the program: the program runs under a restricted account
/// and cannot rewrite its own settings file.
/// </summary>
public sealed record DatabaseInfo(string Server, string Database, string Authentication, bool InContainer)
{
    public static DatabaseInfo From(IConfiguration config)
    {
        var b = new SqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection") ?? "");
        var auth = b.IntegratedSecurity ? "Windows kimlik doğrulaması" : $"SQL kullanıcısı: {b.UserID}";
        var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        // In the Linux/macOS Docker setup the built-in SQL Server is the "sql" container.
        var server = inContainer && b.DataSource == "sql" ? "Disci ile birlikte kurulan SQL Server (Docker)" : b.DataSource;
        return new DatabaseInfo(server, b.InitialCatalog, auth, inContainer);
    }

    public string HowToChange => InContainer
        ? "Değiştirmek için kurulum klasöründe: ./veritabani-ayarla.sh"
        : @"Değiştirmek için Disci'nin kurulu olduğu bilgisayarda yönetici PowerShell'de: C:\Disci\betikler\veritabani-ayarla.ps1";
}
