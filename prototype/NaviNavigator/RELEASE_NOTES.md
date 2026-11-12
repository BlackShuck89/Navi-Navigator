# Navi Navigator 0.0.5 — Browser functionality update

**Focus:** core browsing, not portal design. This technical preview adds automatic restoration of tabs (including active-tab selection), periodic session checkpoints, a searchable local history manager, recently closed tab recovery, and a tab context menu.

- **Session:** Saves a list of up to 40 opened HTTP/HTTPS pages and internal Navi Portal tabs. Recovery is enabled by default and can be disabled under Tools. Internal page content is recreated on launch.
- **History:** Go → History or Ctrl+H opens a searchable native history manager with open/delete/clear controls. Only successful HTTP/HTTPS navigations are recorded, with the browser's best-available page title.
- **Tabs:** Right-click any tab to duplicate it, close it or close other tabs. Ctrl+Shift+T reopens a recently closed tab (up to ten while the process is running). Ctrl+Tab cycles between tabs.
- **Reliability:** Browser state is saved atomically to JSON in the existing Navi Navigator data directory. Failed or invalid session entries are skipped on restore.
- **Portal:** Retains the approved portal redesign and supports the bundled illustration in Assets/navi-approved-hero.webp. The raw CI artifact may need the approved artwork added separately.

**Engine:** Still Microsoft Edge WebView2/Chromium. This is an unsigned Windows x64 test build without an automatic security updater; avoid sensitive browsing.

Existing bookmarks, skin preferences, and WebView2 profile location are preserved.
