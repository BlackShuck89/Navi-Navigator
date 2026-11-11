using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NaviNavigator;

/// <summary>
/// Versioned, declarative skin package. No script or code is loaded from skins.
/// Colors, chrome geometry, optional toolbar texture and PNG icons are the skin surface.
/// </summary>
internal sealed class NaviSkin
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Top { get; set; } = "#EAF1F5";
    public string Bottom { get; set; } = "#B7CBD5";
    public string Foreground { get; set; } = "#193349";
    public string Accent { get; set; } = "#128B94";
    public string AccentLight { get; set; } = "#88D6D5";
    public string TabTop { get; set; } = "#FFFFFF";
    public string TabBottom { get; set; } = "#CCDCE6";
    public string Border { get; set; } = "#7B9CAA";
    public string MenuBackground { get; set; } = "#F6F9FA";
    public string MenuForeground { get; set; } = "#18354C";
    public string SurfaceEffect { get; set; } = "gloss";
    public string TabShape { get; set; } = "sloped";
    public int ToolbarHeight { get; set; } = 46;
    public int TabHeight { get; set; } = 34;
    public int TabWidth { get; set; } = 206;
    public int IconSize { get; set; } = 24;
    public int CornerRadius { get; set; } = 7;
    public bool ShowButtonLabels { get; set; } = true;
    public string ToolbarTexture { get; set; } = "";

    [JsonIgnore] public string SourceDirectory { get; set; } = "";
    [JsonIgnore] public Color TopColor => Parse(Top);
    [JsonIgnore] public Color BottomColor => Parse(Bottom);
    [JsonIgnore] public Color ForegroundColor => Parse(Foreground);
    [JsonIgnore] public Color AccentColor => Parse(Accent);
    [JsonIgnore] public Color AccentLightColor => Parse(AccentLight);
    [JsonIgnore] public Color TabTopColor => Parse(TabTop);
    [JsonIgnore] public Color TabBottomColor => Parse(TabBottom);
    [JsonIgnore] public Color BorderColor => Parse(Border);
    [JsonIgnore] public Color MenuBackgroundColor => Parse(MenuBackground);
    [JsonIgnore] public Color MenuForegroundColor => Parse(MenuForeground);

    private static Color Parse(string hex) => ColorTranslator.FromHtml(hex);

    internal static NaviSkin Fallback => new()
    {
        Id = "modern",
        Name = "Navi Modern (fallback)"
    };

    internal bool IsValid(out string error)
    {
        if (SchemaVersion != 1 || !Regex.IsMatch(Id, "^[a-z][a-z0-9-]{1,31}$"))
        {
            error = "Unsupported schemaVersion or invalid skin ID";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 60 ||
            Description.Length > 250 ||
            !new[] { "gloss", "bevel", "matte" }.Contains(SurfaceEffect) ||
            !new[] { "sloped", "rounded", "square" }.Contains(TabShape))
        {
            error = "Unsupported skin name, finish or tab shape";
            return false;
        }
        if (ToolbarHeight is < 30 or > 75 || TabHeight is < 25 or > 58 ||
            TabWidth is < 135 or > 300 || IconSize is < 16 or > 42 ||
            CornerRadius is < 0 or > 22)
        {
            error = "Skin geometry out of supported bounds";
            return false;
        }
        string[] colors = [Top, Bottom, Foreground, Accent, AccentLight, TabTop,
            TabBottom, Border, MenuBackground, MenuForeground];
        if (colors.Any(c => c is null || !Regex.IsMatch(c, "^#[0-9a-fA-F]{6}$")))
        {
            error = "Invalid color; use #RRGGBB";
            return false;
        }
        if (!string.IsNullOrEmpty(ToolbarTexture) &&
            (ToolbarTexture != Path.GetFileName(ToolbarTexture) ||
             !ToolbarTexture.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
        {
            error = "ToolbarTexture must be a local PNG filename";
            return false;
        }
        error = "";
        return true;
    }

    internal string? GetIconPath(string key)
    {
        // Keys are compiled in the browser; no untrusted arbitrary path is accepted.
        if (!SkinCatalog.IconKeys.Contains(key)) return null;
        string full = Path.Combine(SourceDirectory, "icons", key + ".png");
        return File.Exists(full) ? full : null;
    }

    internal string? GetTexturePath()
    {
        if (string.IsNullOrWhiteSpace(ToolbarTexture)) return null;
        string full = Path.Combine(SourceDirectory, ToolbarTexture);
        return File.Exists(full) ? full : null;
    }
}

internal sealed class SkinCatalog
{
    internal static readonly HashSet<string> IconKeys = new(StringComparer.Ordinal)
    {
        "back", "forward", "reload", "stop", "home", "go", "tab", "bookmark", "compass"
    };

    internal IReadOnlyList<NaviSkin> All => _skins;
    internal IReadOnlyList<string> Warnings => _warnings;
    private readonly List<NaviSkin> _skins = [];
    private readonly List<string> _warnings = [];

    internal static string UserSkinDirectory =>
        Path.Combine(BrowserSettings.BaseDirectory, "Skins");

    internal SkinCatalog() => Reload();

    internal void Reload()
    {
        _skins.Clear();
        _warnings.Clear();

        // Built-in themes are portable assets next to the exe, not hardcoded palettes.
        LoadDirectory(Path.Combine(AppContext.BaseDirectory, "Skins"));
        LoadDirectory(UserSkinDirectory);

        if (_skins.Count == 0)
        {
            _skins.Add(NaviSkin.Fallback);
            _warnings.Add("No valid skin packages found. Using built-in fallback colors.");
        }
    }

    internal NaviSkin Get(string? id) =>
        _skins.FirstOrDefault(s => s.Id == id) ??
        _skins.FirstOrDefault(s => s.Id == "modern") ??
        _skins[0];

    internal void EnsureUserDirectory() => Directory.CreateDirectory(UserSkinDirectory);

    private void LoadDirectory(string location)
    {
        if (!Directory.Exists(location)) return;
        foreach (var dir in Directory.EnumerateDirectories(location).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var manifest = Path.Combine(dir, "skin.json");
            if (!File.Exists(manifest)) continue;
            try
            {
                if (new FileInfo(manifest).Length > 64 * 1024)
                    throw new InvalidDataException("Manifest exceeds 64 KiB");

                var skin = JsonSerializer.Deserialize<NaviSkin>(
                    File.ReadAllText(manifest),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidDataException("Empty manifest");
                if (!skin.IsValid(out var problem)) throw new InvalidDataException(problem);
                if (_skins.Any(s => s.Id == skin.Id))
                    throw new InvalidDataException("Duplicate ID; rename this custom skin");

                skin.SourceDirectory = dir;
                _skins.Add(skin);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or JsonException or InvalidDataException or ArgumentException)
            {
                _warnings.Add(Path.GetFileName(dir) + ": " + ex.Message);
            }
        }
    }
}
