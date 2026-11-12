using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Disci.Web.Services;

/// <summary>
/// Looks for a newer Disci release on GitHub and hands an install request to the computer's update task.
///
/// The program itself never installs anything: it runs under a restricted service account and must not be able to
/// replace its own files. It only writes <c>istek.json</c> (which version to install) into the update folder. The
/// update task (Windows: scheduled task "Disci Guncelleme" running guncelle.ps1 as SYSTEM; Linux/macOS: cron +
/// guncelle.sh) downloads that release itself from the fixed GitHub address, checks its SHA256, takes a backup,
/// installs it, rolls back if the new version does not start, and reports in <c>durum.json</c>.
/// </summary>
public sealed class UpdateService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<UpdateService> log, TimeProvider clock)
{
    public const string HttpClientName = "github";

    private ReleaseInfo? _latest;
    private DateTimeOffset _checkedAt;
    private string? _lastError;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public string Repository => config["Update:Repository"] is { Length: > 0 } r ? r : "mehmet-cizmeci/Disci";
    public bool Enabled => config.GetValue("Update:Enabled", true);

    /// <summary>Where the program and the update task exchange files; next to the photo folder unless configured.</summary>
    public string Folder => config["Update:Folder"] is { Length: > 0 } f ? f
        : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(config["FileStorage:Root"] ?? "data/files")) ?? ".", "Guncelleme");

    public static Version CurrentVersion { get; } = ParseVersion(
        typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion) ?? new Version(0, 0, 0);

    public ReleaseInfo? Latest => _latest;
    public string? LastError => _lastError;
    public DateTimeOffset CheckedAt => _checkedAt;
    public bool UpdateAvailable => _latest is { } l && l.Version > CurrentVersion;

    /// <summary>Asks GitHub for the latest release; cached for six hours unless forced.</summary>
    public async Task<ReleaseInfo?> CheckAsync(bool force = false, CancellationToken ct = default)
    {
        if (!Enabled) return null;
        if (!force && _latest is not null && clock.GetUtcNow() - _checkedAt < TimeSpan.FromHours(6)) return _latest;
        await _gate.WaitAsync(ct);
        try
        {
            var http = httpFactory.CreateClient(HttpClientName);
            using var response = await http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", ct);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("GitHub boş yanıt verdi.");
            _latest = new ReleaseInfo(dto.TagName ?? "", ParseVersion(dto.TagName) ?? new Version(0, 0, 0), dto.Name ?? dto.TagName ?? "",
                dto.Body ?? "", dto.HtmlUrl ?? $"https://github.com/{Repository}/releases", dto.PublishedAt);
            _lastError = null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            _lastError = "GitHub'a ulaşılamadı. İnternet bağlantısını kontrol edin.";
            log.LogWarning(e, "Update check failed");
        }
        finally
        {
            _checkedAt = clock.GetUtcNow();
            _gate.Release();
        }
        return _latest;
    }

    /// <summary>Leaves the install request for the update task.</summary>
    public async Task RequestInstallAsync(ReleaseInfo release, string requestedBy)
    {
        Directory.CreateDirectory(Folder);
        var request = new UpdateRequest(release.Tag, requestedBy, clock.GetUtcNow());
        await File.WriteAllTextAsync(Path.Combine(Folder, "istek.json"), JsonSerializer.Serialize(request, Json));
    }

    public async Task CancelRequestAsync()
    {
        var path = Path.Combine(Folder, "istek.json");
        if (File.Exists(path)) File.Delete(path);
        await Task.CompletedTask;
    }

    public UpdateRequest? PendingRequest() => Read<UpdateRequest>("istek.json");

    /// <summary>What the update task last reported; null if it never ran (task not installed).</summary>
    public UpdateStatus? Status() => Read<UpdateStatus>("durum.json");

    public bool AutoInstall => Read<UpdateSettings>("ayar.json")?.Automatic ?? false;

    public async Task SetAutoInstallAsync(bool on)
    {
        Directory.CreateDirectory(Folder);
        await File.WriteAllTextAsync(Path.Combine(Folder, "ayar.json"), JsonSerializer.Serialize(new UpdateSettings(on), Json));
    }

    private T? Read<T>(string name) where T : class
    {
        try
        {
            var path = Path.Combine(Folder, name);
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) : null;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>"v1.2.3", "1.2.3", "1.2.3+abc" → 1.2.3.</summary>
    public static Version? ParseVersion(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().TrimStart('v', 'V');
        var cut = s.IndexOfAny(['+', '-', ' ']);
        if (cut >= 0) s = s[..cut];
        return Version.TryParse(s, out var v) ? new Version(v.Major, v.Minor, Math.Max(0, v.Build)) : null;
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
        [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }
    }

    public static void ConfigureClient(HttpClient http)
    {
        http.Timeout = TimeSpan.FromSeconds(15);
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Disci-Updater", CurrentVersion.ToString(3)));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }
}

public sealed record ReleaseInfo(string Tag, Version Version, string Name, string Notes, string Url, DateTimeOffset? PublishedAt);

public sealed record UpdateRequest(string Tag, string RequestedBy, DateTimeOffset RequestedAt);

/// <summary>Written by the update task. State: "bekliyor", "indiriliyor", "kuruluyor", "tamam", "hata".</summary>
public sealed record UpdateStatus(string State, string? Message, string? Version, DateTimeOffset? Time);

public sealed record UpdateSettings(bool Automatic);

/// <summary>Checks once shortly after start and then twice a day, so the admin sees "new version" without asking.</summary>
public sealed class UpdateCheckWorker(UpdateService updates) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!updates.Enabled) return;
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(12));
            do { await updates.CheckAsync(force: true, stoppingToken); }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { }
    }
}
