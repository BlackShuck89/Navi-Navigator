# Navi Navigator architecture

## Current reality

This repository is a development overlay, **not** a forked copy of Firefox. Its preview launcher creates a dedicated Firefox profile with a generated userChrome.css and user.js. It launches an installed, up-to-date Firefox browser with the selected appearance skin. Firefox still retains its own brand, installer, executable name, menus, extension handling, security features and updates.

The overlay is immediately useful for testing toolbar geometry, tab shape, border treatments, gradients and readability without consuming tens of gigabytes of upstream source.

## Target browser architecture

- Rendering and web standards: Firefox / Gecko / SpiderMonkey, updated from Mozilla's upstream security releases.
- Application chrome: Navi-owned frontend customizations; preserve upstream browser security and isolation.
- Skin manager: data-driven visual resources (icons, colors, gradients, CSS, images) independent of the user-selected toolbar layout. Runtime skin switching is a *planned feature*, not implemented by this preview.
- Browser profile: isolated Navi-specific profile path, preferences, migrator and a separate application identity in a future proper fork.
- Packaging: rebranded Windows x64 installer and full source/licensing disclosure; do not redistribute Mozilla's Firefox application branding under a renamed executable.
- Other suite tools: optional independently packaged Mail, Composer, and related services after core browsing is stable.

## Real fork bootstrap on Windows

1. Install MozillaBuild from Mozilla, plus Visual Studio C++ tools as documented.
2. Follow https://firefox-source-docs.mozilla.org/setup/windows_build.html to bootstrap the official https://github.com/mozilla-firefox/firefox repository.
3. For frontend prototyping, place config/mozconfig.artifact from this repository into the root of the Firefox source checkout as .mozconfig.
4. Inside the MozillaBuild shell, run ./mach build then ./mach run.
5. Develop Navi chrome and branding changes as tracked patches against an **explicitly pinned upstream revision**; test patch application during CI before compiling.
6. Move to a full non-artifact build for a separately branded Navi binary, application IDs, icons, signing, installers and update infrastructure.

Mozilla's Windows guide recommends at least 40 GB available disk. A stock GitHub-hosted Windows runner is not presumed adequate for a full Firefox compile. We will not label a profile preview or an upstream Firefox build as a Navi-branded executable.

## Security boundaries

- Do not disable Enhanced Tracking Protection, process isolation or certificate validation to achieve a nostalgic visual effect.
- Imported user skins may not execute privileged JavaScript, load remote resources or grant host permissions.
- Keep customization limited to visual browser-chrome assets wherever practical. CSS support depends on upstream selectors and can require revision.
- Never update an existing user Firefox profile in place during development.
- Treat upstream tracking as part of the product; Gecko security patches are more important than skin fidelity.

## Initial components

skins/manifest.json defines available variants. skins/base.css provides common structure; skins/<id>.css overrides colors and detail. scripts/Start-NaviPreview.ps1 generates a Firefox-only preview profile; scripts/validate.mjs performs dependency-free structural checks. See THEMING.md for the proposed extensibility contract.

## Branding and provenance

The product is Navi Navigator with Navi the Navigator as mascot: a teal and navy fox character with compass, celestial and globe motifs. A non-mascot compass mark is a temporary repository placeholder. Original character artwork should be added only with the owner's approval and a clearly stated asset license. Firefox source licensing and project-specific code licensing must remain distinct.
