# Navi Navigator

> A Gecko-based browser project inspired by the transition from Netscape 7 to Netscape 8, with its own Navi the Navigator identity.

**Status:** pre-alpha / development preview. There is **not yet a standalone Navi Navigator executable**. The first runnable preview applies Navi's interface skins to a separate Firefox profile, so browsing is handled by a current, locally installed Firefox. This is a stepping stone toward a fully rebranded Firefox source build, not a finished fork.

## Design direction

- **Navi Modern** — default: Netscape 7's functional layout with restrained Netscape 8 Fusion polish.
- **Navi Classic** — compact, brighter, conventional 2000s-style browser chrome.
- **Navi Fusion** — darker teal/navy gradients, stronger highlights and rounded tabs.
- Traditional menu bar, visible bookmarks toolbar, status feedback and configurable chrome are long-term priorities.
- Skin appearance and toolbar layout are separate features. The preview currently changes appearance, not toolbar placement.
- Gecko provides modern standards compatibility and Firefox's ongoing security foundation.

The name and visual design are original to Navi Navigator; no Netscape or Mozilla logos are included. Navi is the project's teal-and-navy fox mascot, with compass and celestial motifs.

## Try the development skin preview (Windows)

Requires an **installed Firefox** and Windows PowerShell 5.1 or PowerShell 7.

```powershell
git clone https://github.com/BlackShuck89/Navi-Navigator.git
cd Navi-Navigator
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-NaviPreview.ps1 -Skin modern
```

Choose `-Skin classic` or `-Skin fusion` to try the alternatives. Use `-NoLaunch` to generate and inspect a profile without opening Firefox. Use `-FirefoxPath "C:\\Program Files\\Mozilla Firefox\\firefox.exe"` for a nonstandard installation. Profiles are created in `%LOCALAPPDATA%\Navi Navigator\PreviewProfiles\` (or specify `-ProfileRoot`). Your existing Firefox profile is **not** modified.

This preview deliberately uses Firefox's supported profile launch options and its optional `userChrome.css` customization mechanism. CSS selectors are internal Firefox implementation details and may break on upstream changes; our validation workflow checks file structure, not rendering compatibility. The preview is not rebranded and must not be distributed as a standalone Navi binary.

## Source layout

| Path | Purpose |
|---|---|
| `skins/base.css` | Shared browser-chrome treatment |
| `skins/{modern,classic,fusion}.css` | Three original Navi skins |
| `skins/manifest.json` | Skin registry and color palettes |
| `scripts/Start-NaviPreview.ps1` | Isolated Firefox profile and launcher |
| `scripts/validate.mjs` | Dependency-free structural validation |
| `config/mozconfig.artifact` | Developer artifact-build configuration |
| `docs/` | Architecture, skin contracts, roadmap |
| `.github/workflows/` | Source validation and Windows profile smoke test |

## Developing against Firefox source

For a real Gecko fork, use [Mozilla's official Windows build guide](https://firefox-source-docs.mozilla.org/setup/windows_build.html) to install MozillaBuild and bootstrap [mozilla-firefox/firefox](https://github.com/mozilla-firefox/firefox). Mozilla currently recommends at least **40 GB free disk space** for Windows source builds. Start with an artifact build to iterate on frontend chrome; a later full source build will be necessary for application naming, icon branding, installers and update infrastructure. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Validation

```bash
node scripts/validate.mjs
```

No Node runtime is required by the browser itself; Node is only used for repository checks. GitHub Actions validates the skin files and produces a Windows preview-profile smoke test. **A green CI check does not mean a Navi executable has been compiled.**

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md). Version `0.1.0-alpha` is reserved for the first genuinely independently branded, buildable Gecko application; this repository is currently the pre-alpha foundation.

## Licensing and attribution

Repository-authored scripts, CSS, and documentation are available under the MIT license (see `LICENSE`). Mozilla Firefox source code and components remain under their respective licenses, principally the Mozilla Public License 2.0. The Navi mascot artwork and project branding are **not** covered by the MIT code license; contact the project owner before reusing them. Netscape and Firefox are trademarks of their respective owners, with no affiliation claimed.
