# Navi Navigator 0.0.2 — Interface and skinning technical preview

**Rendering backend:** Microsoft Edge WebView2 (Chromium), NOT Gecko/Firefox. The Gecko-source integration remains a separate development effort.

This visual update draws from Netscape 7 and 8:

- Separate navigation, location and optional personal toolbars.
- Original icon renderer with compass, directional, reload, home and bookmark controls.
- Custom tab outlines: sloped Modern, compact square Classic, and rounded Fusion.
- Redesigned gradients, bevels, toolbar sizing, typography, and status bar.
- Skin packages from versioned `skin.json` files with optional PNG icon/toolbar textures; live switching and reloading.
- Offline Navi Portal with a new illustration of Navi as a teal fox wearing the compass emblem; no Netscape logos.
- Existing tabs, browsing, downloads, bookmarks and user profile directory preserved.

This ZIP contains a portable Windows x64 executable. Extract the whole archive and run `NaviNavigator.exe`. Requires the WebView2 Evergreen Runtime; .NET is bundled. Unsigned, without automatic updates, and not recommended for sensitive browsing.

See `prototype/NaviNavigator/SKINNING.md` in the repository for skin-format documentation.
