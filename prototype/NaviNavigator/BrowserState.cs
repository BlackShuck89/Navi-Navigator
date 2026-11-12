using System.Text.Json;

namespace NaviNavigator;

internal sealed record HistoryVisit(string Title, string Url, DateTimeOffset VisitedUtc);

internal sealed class BrowserSession
{
    public int SchemaVersion { get; set; } = 1;
    public List<string> Tabs { get; set; } = [];
    public int SelectedIndex { get; set; }
    public DateTimeOffset SavedUtc { get; set; } = DateTimeOffset.UtcNow;
}

internal static class BrowserStateStore
{
    private const int MaximumTabs = 40;
    private const int MaximumHistory = 2000;
    private static readonly JsonSerializerOptions Formatting = new() { WriteIndented = true };

    private static string SessionPath => Path.Combine(BrowserSettings.BaseDirectory, "session.json");
    private static string HistoryPath => Path.Combine(BrowserSettings.BaseDirectory, "history.json");

    internal static bool IsWebAddress(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var parsed) &&
        (parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeHttp);

    internal static BrowserSession? LoadSession()
    {
        try
        {
            if (!File.Exists(SessionPath) || new FileInfo(SessionPath).Length > 1024 * 1024)
                return null;
            var stored = JsonSerializer.Deserialize<BrowserSession>(File.ReadAllText(SessionPath));
            if (stored is null || stored.SchemaVersion != 1) return null;
            stored.Tabs = (stored.Tabs ?? [])
                .Where(x => x == "navi://home" || IsWebAddress(x))
                .Take(MaximumTabs).ToList();
            if (stored.Tabs.Count == 0) return null;
            stored.SelectedIndex = Math.Clamp(stored.SelectedIndex, 0, stored.Tabs.Count - 1);
            return stored;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or ArgumentException)
        {
            return null;
        }
    }

    internal static void SaveSession(IEnumerable<string> urls, int selectedIndex)
    {
        var tabs = urls.Where(u => u == "navi://home" || IsWebAddress(u))
            .Take(MaximumTabs).ToList();
        if (tabs.Count == 0) tabs.Add("navi://home");
        var snapshot = new BrowserSession
        {
            Tabs = tabs,
            SelectedIndex = Math.Clamp(selectedIndex, 0, tabs.Count - 1)
        };
        AtomicWrite(SessionPath, JsonSerializer.Serialize(snapshot, Formatting));
    }

    internal static List<HistoryVisit> LoadHistory()
    {
        try
        {
            if (!File.Exists(HistoryPath) || new FileInfo(HistoryPath).Length > 4 * 1024 * 1024)
                return [];
            var entries = JsonSerializer.Deserialize<List<HistoryVisit>>(File.ReadAllText(HistoryPath));
            return (entries ?? [])
                .Where(v => IsWebAddress(v.Url) && v.VisitedUtc <= DateTimeOffset.UtcNow.AddDays(1))
                .OrderByDescending(v => v.VisitedUtc)
                .Take(MaximumHistory)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or ArgumentException)
        {
            return [];
        }
    }

    internal static void SaveHistory(List<HistoryVisit> visits)
    {
        if (visits.Count > MaximumHistory)
            visits.RemoveRange(MaximumHistory, visits.Count - MaximumHistory);
        AtomicWrite(HistoryPath, JsonSerializer.Serialize(visits, Formatting));
    }

    private static void AtomicWrite(string path, string content)
    {
        Directory.CreateDirectory(BrowserSettings.BaseDirectory);
        var tempFile = path + ".tmp";
        File.WriteAllText(tempFile, content);
        File.Move(tempFile, path, overwrite: true);
    }
}
