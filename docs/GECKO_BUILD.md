# Gecko source integration — next development milestone

This is the first actual *source-level* Navi integration. The original preview merely loads CSS into an installed Firefox. The source overlay can prepare a pinned upstream Firefox checkout with Navi-specific branding, application identity settings, icon resources and the Navi Modern chrome style.

**It has not been compiled into a working Navi browser yet.** Build success, Windows executable identity, installer registration, first-run behavior, updater behavior and separation from a normal Firefox installation require verification.

## Pinned upstream

The first integration targets Mozilla's official Git repository at the exact revision recorded in config/upstream.json. This is a reproducibility mechanism, **not** an endorsement of this revision for a public release. Any public browser must track Mozilla security updates and receive its own security-maintenance process.

The source overlay is intentionally stored as code in Navi's own small repository; we do not copy Firefox's giant source tree into Navi Navigator.

## Windows development build

1. Set up [MozillaBuild and Firefox development dependencies](https://firefox-source-docs.mozilla.org/setup/windows_build.html). Mozilla recommends 40 GB+ free space and 8 GB+ RAM.
2. Within the MozillaBuild shell, bootstrap/clone the Firefox source following the official guide.
3. Navigate to that Firefox source directory and `git checkout` the exact revision in `config/upstream.json`. Do this only in a disposable source checkout, not a checkout containing unrelated work.
4. In the MozillaBuild shell, invoke the Navi script by pointing it at the Firefox checkout:

   ```bash
   bash /c/path/to/Navi-Navigator/scripts/Build-NaviGecko.sh /c/mozilla-source/firefox
   ```

The build script refuses to overwrite an existing mozconfig, prepares the new branding, writes its own mozconfig and runs `./mach build` followed by `./mach package`.

If you want to inspect changes before compiling:

```bash
python3 /c/path/to/Navi-Navigator/scripts/apply_gecko_overlay.py --firefox-source /c/mozilla-source/firefox
git -C /c/mozilla-source/firefox diff --stat
```

For a build from a newer Firefox revision, the overlay provides `--allow-unpinned` for *experimentation only*. That override does not establish compatibility.

## What the source overlay changes

- Creates `browser/branding/navi` by copying Firefox's standard **unofficial** branding sources (MPL 2.0), then replaces the primary brand strings, Windows icon files, product logos, and first-pass installer labels.
- Assigns a Navi-specific application vendor and GUID in `browser/moz.configure`.
- Applies the Navi Modern skin at the end of `browser/themes/shared/browser-shared.css`, integrating it into compiled Gecko browser chrome rather than requiring userChrome.css.
- Adds branding preferences indicating that the experimental build **does not have its own secure update service**.
- Uses `--with-branding=browser/branding/navi` and `--with-app-basename=NaviNavigator` in `config/mozconfig.full`.

This is a first-pass rebrand. Some Firefox-internal file names and ancillary resources still need to be customized or audited. The executable's exact generated name is not asserted until a full build is complete.

## Testing and limits

`python3 scripts/test_gecko_overlay.py` creates an isolated fake Mozilla source tree and verifies manifest substitutions, icons and the stylesheet patch. These tests do not compile Gecko. Existing GitHub Actions tests also check preview skin syntax and Windows profile generation.

The current overlay does not produce release-ready binaries and does not provide code signing, reliable automated security updates, complete Firefox trademark removal, a safe skin package manager, or production user-data migration. Do **not** use a locally built preview as a daily browser for sensitive activity.

The original Navi mascot artwork has not been committed to the source tree; the project uses a generated original compass placeholder.

## Later automated builds

A full Windows GitHub Actions build requires appropriately provisioned Windows build machines, MozillaBuild, large storage, toolchains, caches and Windows installer validation. We intentionally do not claim that the existing GitHub-hosted preview jobs build Gecko. When reliable machines are available, the pinned overlay and full mozconfig are ready to be consumed by that build workflow.
