NAVI NAVIGATOR 0.0.4 - WINDOWS TECHNICAL PREVIEW
================================================

This is a test build for Windows 10/11 x64.

INSTALL / RUN
1. Extract the full ZIP archive into a folder.
2. Run NaviNavigator.exe (not an installer).
3. The program is unsigned, so Windows SmartScreen may warn about it.
   Only run builds from a source you trust.
4. Microsoft Edge WebView2 Evergreen Runtime must be installed.
   The .NET runtime is included in the archive.

NEW IN THIS BUILD
- Refined Netscape 7/8-inspired browser chrome, separate navigation and
  location toolbars, optional personal bookmarks bar, and redesigned tabs.
- Navi Modern, Classic and Fusion skins now change more than colors:
  geometry, toolbar density, button labels, tab outlines, gradients,
  icon art and icon sizing.
- Skin packages have individual versioned skin.json manifests plus
  optional local PNG icons and toolbar textures.
- The Navi Portal features a teal-and-white fox portrayal of Navi,
  with a new compass emblem rather than any Netscape branding.

SKIN CUSTOMIZATION
View > Skins > Open Skins Folder opens your personal skin directory.
Copy a folder containing skin.json there. Choose Reload Skin Packages.
Refer to prototype/NaviNavigator/SKINNING.md in the source repository.
Installed skins cannot run code; they provide colors, geometry,
optional local PNG assets.

BROWSER CONTROLS
- Ctrl+L URL entry; Ctrl+T new tab; Ctrl+W close tab
- Ctrl+D bookmark; F5 reload; Alt+Left/Right navigation
- View > Toolbars to show/hide the personal bar or status bar.
- View > Skins to select or reload themes.

ENGINE / SECURITY
This proof-of-concept uses Microsoft WebView2 (Chromium), NOT Gecko.
Do not use for banking, passwords or other sensitive activities.
It is unsigned, has no update mechanism, and has not received a
comprehensive security audit.

Settings and WebView2 browsing data are stored in:
%LOCALAPPDATA%\Navi Navigator\Standalone Preview

This update reuses that folder, so existing bookmarks and skin
preferences from v0.0.1 should be preserved.

Source: https://github.com/BlackShuck89/Navi-Navigator

BRANDING AND APPEARANCE CHANGES IN 0.0.4
- Navi's teal/white/navy reference-sheet markings reinforced in mascot SVG.
- Dedicated original compass-and-globe emblem replaces older generic star marks.
- Portal colors and content now respond to the selected skin.
- A branded About window includes Navi and the product version.
- Skin palette tokens have been aligned with Navi's canonical colors.
- No Netscape logos or wordmarks are included in branded application assets.


NAVI PORTAL v0.0.4
- Large split-layout portal hero featuring the approved Navi illustration.
- Expanded Navi Services sidebar, portal link modules, skins preview, and lower information panels.
- Distinct portal styles for Modern, Classic and Fusion; responsive narrower layout.
- The browser looks for Assets/navi-approved-hero.webp, using a legacy vector fallback if absent.
- The full preview ZIP must contain the approved hero art at that exact relative path.
- All browser navigation features and existing user profiles remain unchanged.
