namespace Disci.Web.Tests;

/// <summary>
/// Manual helper, skipped in normal runs: serves the app with made-up data on http://localhost:5222 for 10 minutes
/// so interactive pages can be tried in a browser without a real account. Remove Skip, run only this test, open
/// the site, run <c>document.cookie = 'test-role=Admin; path=/'</c> in the browser console and reload.
/// </summary>
public sealed class BrowserTestServer
{
    [Fact(Skip = "El ile çalıştırılır; açıklamaya bakın.")]
    public async Task Serve()
    {
        await using var factory = new DisciWebFactory { EnvironmentName = "Development" };
        factory.UseKestrel(5222);
        factory.StartServer();
        await factory.InitializeAsync();
        await Task.Delay(TimeSpan.FromMinutes(10));
        await ((IAsyncLifetime)factory).DisposeAsync();
    }
}
