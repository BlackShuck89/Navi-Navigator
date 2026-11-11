# Navi Navigator skinning system (v1)

This release treats interface skins as **independent packages**, not merely color presets. The Windows prototype reads a declarative `skin.json` manifest and applies it to its navigation toolbar, location bar, personal bar, status bar, buttons and tab strip. This system is part of the WebView2 prototype; a future Gecko build will require a separate chrome-integration port.

## Built-in skins

- **Navi Modern** — sloped early-2000s tabs, balanced gray-blue gradient chrome, compass colors, icon-and-label buttons.
- **Navi Classic** — square tabs, compact toolbars, beveled silver surfaces, Tahoma-style typography.
- **Navi Fusion** — rounded tabs, dark blue/teal glossy surfaces, taller icon-only primary navigation toolbar.

Switch immediately using **View → Skins**. The selected ID is saved alongside existing bookmarks in `%LOCALAPPDATA%\Navi Navigator\Standalone Preview\settings.json`, preserving old user settings. **View → Toolbars** controls personal/status bar visibility and an optional override for icon captions.

## Install a user skin

Choose **View → Skins → Open Skins Folder**. Inside the opened directory, create a unique subfolder such as `my-ocean-theme` and place a `skin.json` in it. Then choose **Reload Skin Packages**.

You can copy one of the built-in manifests from `Skins/modern/skin.json` and change its `Id` and `Name` fields. User skins **must have a unique ID**; built-ins cannot be overridden in place. The application skips invalid packages and keeps the existing skin usable.

This is a deliberately limited format: **no scripts, external CSS, remote assets, or executable extensions are loaded through the skin manager.** It is a visual-only feature.

## Manifest fields

```json
{
  "SchemaVersion": 1,
  "Id": "my-ocean-theme",
  "Name": "My Ocean Theme",
  "Description": "A personal Navigator-style skin",
  "Top": "#EEF6F8",
  "Bottom": "#ABCBD1",
  "Foreground": "#173B4C",
  "Accent": "#0F8796",
  "AccentLight": "#92DBDA",
  "TabTop": "#FFFFFF",
  "TabBottom": "#C9E5EC",
  "Border": "#739AA5",
  "MenuBackground": "#F6F9FA",
  "MenuForeground": "#18354C",
  "SurfaceEffect": "gloss",
  "TabShape": "sloped",
  "ToolbarHeight": 46,
  "TabHeight": 34,
  "TabWidth": 206,
  "IconSize": 24,
  "CornerRadius": 7,
  "ShowButtonLabels": true,
  "ToolbarTexture": ""
}
```

`SurfaceEffect` is `gloss`, `bevel`, or `matte`. `TabShape` is `sloped`, `square`, or `rounded`. All colors must be six-digit hexadecimal values. Geometry has bounded numeric ranges in `SkinCatalog.cs`, to prevent malicious or broken layouts.

For optional icon replacements, create an `icons` subfolder containing local PNG files with the following fixed names:

`back.png`, `forward.png`, `reload.png`, `stop.png`, `home.png`, `go.png`, `tab.png`, `bookmark.png`, `compass.png`.

Only provided icon files are replaced. Missing icons fall back to Navi's original vector-drawn graphics. Files should have transparent backgrounds and be designed for 24–32 pixel display. The loader checks PNG signatures and limits image dimensions and file size.

`ToolbarTexture` can point to a PNG in the same skin folder, such as `"ToolbarTexture": "waves.png"`. Remote URLs, absolute paths, parent-directory traversal and non-PNG files are rejected. The bitmap is tiled across applicable native toolstrips.

## Scope and limitations

The three themes change window chrome. They do **not** style web pages themselves, install a new browser engine, change OS-level title-bar controls, or yet support arbitrary drag-and-drop toolbar layout editing. Extensions, broader preferences and window chrome integration into the future Gecko fork remain future work.

The page at `navi://home` is application-generated and has its own branding. It is re-rendered when skins switch. The offline portal includes an original stylized character illustration and Navi's compass; it contains no legacy Netscape marks. When artwork is unavailable, the portal displays a compass fallback.

If you discover a theme that renders unreadable text or clips controls at a given DPI, report Windows version, scale percentage, selected skin, and a screenshot in the repository's GitHub Issues.

## 0.0.3 branding consistency

The three built-in palettes now use Navi's teal/navy colors (not Netscape trademarks). The portal uses its own offline Navi compass SVG and responds to the selected appearance package. A new branded About dialog shows Navi and the product version. Custom skin packages retain the exact same schema; no migration is necessary.
