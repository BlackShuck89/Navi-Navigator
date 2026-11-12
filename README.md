# Navi Navigator

**Navi Navigator is being developed as a real Mozilla Firefox / Gecko-based browser.** It takes visual cues from the Netscape 7–8 transition while using an original Navi fox-and-compass identity. The long-term browser will be a separately branded native Firefox fork with modern web compatibility, traditional desktop controls and full skinning.

## Current status (November 2026)

**Gecko track — now the primary development path:** A pinned upstream Firefox revision, source-level rebranding script, independent application name/basename/GUID, generated original icons, Navi Modern browser-chrome CSS and a full native Windows build configuration. Continuous integration tests the overlay against *actual downloaded Mozilla source files*. The full Gecko executable is **not yet compiled or verified**. The first native Gecko release will be `0.1.0-alpha`.

**WebView2 track — legacy UI prototype:** Versions 0.0.1–0.0.5 demonstrated browsing, tab management, history, skinning and Navi Portal concepts as a separate Windows executable. These remain under `prototype/NaviNavigator/` but are **not the engine foundation going forward**. The current Gecko track neither embeds WebView2 nor depends on .NET.

## Gecko source integration

1. Set up a Windows 10/11 x64 Firefox developer environment using Mozilla's [official Windows build guide](https://firefox-source-docs.mozilla.org/setup/windows_build.html) — MozillaBuild, Visual Studio tooling, and 40 GB+ free disk.
2. Clone/bootstrap the official [Firefox source](https://github.com/mozilla-firefox/firefox) and check out the exact development revision in `config/upstream.json`.
3. In the MozillaBuild shell, run:

   ```bash
   bash /c/your/Navi-Navigator/scripts/Build-NaviGecko.sh /c/mozilla-source/firefox
   ```

The script checks for conflicting mozconfig files, generates Navi branding inside the Firefox source checkout, and runs `./mach build` and `./mach package`. **Use a disposable Firefox source checkout**: the integration modifies it in place. `./mach run` can be used for local smoke testing with a dedicated profile.

For the exact prerequisites, commands, source modifications, and build caveats, see **[docs/GECKO_BUILD.md](docs/GECKO_BUILD.md)**.

The GitHub Actions **Gecko source integration** workflow validates the patch against real upstream source data without pretending it compiled Gecko. A separate manual **Gecko Windows x64 full build** workflow is configured for a specially prepared self-hosted runner with the `navi-gecko` label; it will not run on ordinary GitHub-hosted Windows instances.

## Visual direction

- **Navi Modern:** Practical Netscape 7 architecture with restrained Netscape 8 polish; default for initial Gecko integration.
- **Navi Classic:** Compact traditional silver-blue desktop controls.
- **Navi Fusion:** Rich dark teal/navy colors, rounded tabs, glossy highlights.
- Consistent Navi mascot, compass and globe motifs. All visible Netscape logos/wordmarks are excluded from our generated branding.
- Complete Gecko skin switching, custom chrome buttons, Navi Portal and optional Communicator-style features are **future work**; the WebView2 versions are only references, not merged functionality.

## Source layout

| Location | Purpose |
| --- | --- |
| `scripts/apply_gecko_overlay.py` | Prepare pinned Firefox source, branding, icons, and browser chrome |
| `scripts/test_gecko_overlay.py` | Fast isolated regression tests |
| `scripts/test_gecko_upstream_snapshot.py` | Test against actual Mozilla files at pinned revision |
| `scripts/Build-NaviGecko.sh` | Full Windows Gecko build orchestration inside MozillaBuild |
| `config/upstream.json` | Exact upstream Firefox revision for reproducible development |
| `config/mozconfig.full` | Separate Navi app name and Gecko build configuration |
| `skins/` | Early Gecko chrome visual resources |
| `prototype/NaviNavigator/` | Historical WebView2 UI prototype; not part of Gecko build |

## Development and security

Navi's source patches deliberately **do not** turn off site isolation, HTTPS validation or other Firefox security boundaries. Experimental builds currently lack secure Navi code-signing, an update service, update-channel infrastructure, independent installer/profile testing, and formal QA; **do not use an unsigned test build as your primary browser for sensitive activity**.

Repository-authored tooling and documentation use MIT; copied Mozilla/Firefox source remains subject to its original licenses, principally MPL 2.0. Navi mascot and distinctive original branding assets are excluded from the MIT tooling license. Navi Navigator is not affiliated with Mozilla, Firefox, AOL, or Netscape.
