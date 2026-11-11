using System.Text.Json;

namespace NaviNavigator;

internal sealed record Bookmark(string Title, string Url);

internal sealed class BrowserSettings
{
    public string Skin { get; set; } = "modern";
    // Null means use the selected skin's preferred icon/text mode.
    public bool? ShowToolbarLabels { get; set; }
    public bool ShowPersonalBar { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public List<Bookmark> Bookmarks { get; set; } = [];

    internal static string BaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Navi Navigator", "Standalone Preview");

    private static string SettingsFile => Path.Combine(BaseDirectory, "settings.json");

    internal static BrowserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                return JsonSerializer.Deserialize<BrowserSettings>(File.ReadAllText(SettingsFile)) ?? new();
            }
        }
        catch (Exception) { /* Recover from damaged settings with defaults. */ }
        return new();
    }

    internal void Save()
    {
        Directory.CreateDirectory(BaseDirectory);
        var temporary = SettingsFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, SettingsFile, true);
    }
}
