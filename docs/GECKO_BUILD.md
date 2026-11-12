# Gecko-first development and native Windows build

Navi Navigator's primary target is a **separately branded Firefox / Gecko application**, not a .NET or WebView2 shell.

## Status

The repo now contains a real source-integration layer that modifies a pinned Firefox checkout. It has deterministic branding, application identity, generated icons and early CSS. CI includes a **real-upstream layout test**, downloading actual Mozilla files from the pinned revision and running the patch generator. A green result proves the source overlay matches those files, **not that Firefox has compiled or launched**.

No Gecko-based Navi binary exists until the full Windows build has succeeded and the result has been inspected. An installed Firefox and a skin preview are not a substitute.

## Native build requirements

Mozilla documents [Windows build prerequisites](https://firefox-source-docs.mozilla.org/setup/windows_build.html) including MozillaBuild, a supported Visual Studio C++ environment, at least **40 GB free disk**, and 8 GB+ RAM recommended. Build on Windows 10/11 x64, ideally with 64 GB+ free. Standard GitHub-hosted Windows agents are not assumed large enough for the full Gecko build.

From **MozillaBuild's shell**, follow Mozilla's instructions to bootstrap the real [mozilla-firefox/firefox](https://github.com/mozilla-firefox/firefox) repository. Keep Navi's repository in a separate directory.

Use a dedicated Firefox checkout with **no existing mozconfig** and check out the **exact** upstream revision declared in `config/upstream.json`:

```bash
cd /c/mozilla-source/firefox
git fetch origin
git checkout eebfc2395a938361f2ecaf6107f3767c1df8d04e
```

Then call the Navi build script from within the same MozillaBuild shell (edit paths to your actual directories):

```bash
bash /c/Navi-Navigator/scripts/Build-NaviGecko.sh /c/mozilla-source/firefox
```

The script uses `MOZCONFIG` to point at `config/mozconfig.full`; it does **not** replace a local `.mozconfig` or `mozconfig` file. It patches the Firefox checkout, then invokes:

```bash
./mach build
./mach package
```

Output should appear under `obj-navi-gecko/dist`. Check for a distinct executable, product branding, `application.ini`, and a Windows installer. This has to be **verified from the actual artifacts**, not inferred from a passing source-level test.

Run using a **dedicated test profile**, never your existing Firefox profile. Carefully inspect `./mach run --help` on the compiled checkout before supplying profile flags.

## Source changes applied

- The officially supplied **unofficial branding folder** is copied to `browser/branding/navi` and its source MPL obligations retained.
- Navi product and vendor identifiers, installer display labels, local brand Fluent strings, links, wordmark and Windows .ICO / .PNG assets replace developer-preview defaults.
- The build option `--with-app-basename=NaviNavigator` sets a distinct application name used in the profile/system identity; `--with-app-name=navinavigator` sets the executable name; `MOZ_APP_ID` is changed to a Navi-specific GUID. Those values must still be tested on Windows before guaranteeing zero collision.
- Navi Modern's initial CSS is applied to the Gecko browser chrome; Classic/Fusion switching from WebView2 is **not** magically carried across.
- The experimental updater preferences are disabled because **no Navi security update service exists**. This is a serious constraint: do not distribute a browser that users might mistakenly rely on for security patches.

The integration is *not* a source fork with every Firefox literal renamed. Some identifiers and file names inherited from Gecko remain internally (per the original licenses), and product-branded UI needs auditing.

## CI validation

```bash
python3 scripts/test_gecko_overlay.py
python3 scripts/test_gecko_upstream_snapshot.py
```

The second test downloads genuine Mozilla branding files and related configure/style sources from the pinned revision, builds a disposable sparse tree and checks that Navi's overlay creates coherent branding and still resolves the jar manifest's assets. It needs internet access to GitHub and **does not** compile Gecko. The mock fixture tests remain for fast regression checks.

## Full-source GitHub Actions

`.github/workflows/gecko-windows-full.yml` defines a manual compilation workflow for a **self-hosted** Windows runner with labels `self-hosted`, `windows`, `x64`, and `navi-gecko`. It requires MozillaBuild at `C:\mozilla-build` and `NAVI_FIREFOX_SOURCE` pointed at a bootstrap-complete checkout. A runner must be provisioned, available and appropriately trusted before this can start. It is not a scheduled build.

Release engineering comes after compilation: icons, code signing, license bundle, integrated Navi start page, profile isolation, normal updater behavior, trustworthy update distribution, crash telemetry controls, and visual testing at multiple DPI levels.

## Upstream maintenance

The commit in `config/upstream.json` is a **development snapshot**, not a long-term supported security release. Before a public Navi release, pin an actively maintained upstream release, rerun the real-upstream integration tests, audit API changes, and keep Gecko fixes arriving promptly.
