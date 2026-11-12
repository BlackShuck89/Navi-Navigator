# Navi Navigator milestone plan

## Foundation (current)

- [x] Repository initialized on main
- [x] Visual direction established: Netscape 7 organization, Netscape 8 Fusion skinning
- [x] Three preview skins: Modern, Classic and Fusion
- [x] Isolated Firefox profile preview launcher for Windows
- [x] Structural source validation and Windows profile-generation smoke test
- [x] Upstream development configuration and project architecture notes
- [ ] Confirm rendered appearance against current Firefox on Windows 11 (manual)
- [ ] Add licensed Navi mascot artwork and original full-size icon assets

## 0.1.0-alpha — first independently branded browser

- [x] Record a pinned Firefox upstream revision and create a reproducible experimental source overlay (compile not yet verified)
- [x] Generate initial Navi branding strings/icons and separate application GUID in upstream source
- [ ] Verify final executable name, version strings, installation and profile isolation on Windows
- [ ] Assemble a standalone, Windows x64 Gecko binary from source
- [ ] Integrate Navi's browser chrome and native menu/toolbar behavior
- [ ] Functional bookmarks, history, tabs, downloads, password management and extensions
- [ ] Check that security, sandboxing and update channels function as designed
- [ ] Produce a smoke-tested Windows installation package and build logs
- [ ] Introduce source attribution and third-party license bundle

## 0.2 — real skin manager

- [ ] Installable skin packages with a versioned manifest
- [ ] Live switching without modifying userChrome.css on disk
- [ ] Customizable toolbar geometry, graphics, textures and button layout
- [ ] Safe skin import: reject remote and privileged resources
- [ ] Skin gallery and migration support

## Later milestones

- [ ] Optional classic sidebar and expanded status bar
- [ ] Customizable throbber and mascot-aware browser error pages
- [ ] Session/workspace management
- [ ] Communicator-style add-ons: Mail, Composer, newsgroups
- [ ] Signed installers and secure update releases

**Definition of done:** do not claim a Windows executable exists until the executable itself has been compiled, launched and inspected. CI validation of preview CSS is not a browser build.


## Gecko-first pivot (November 2026)

The WebView2 prototype is now a legacy UX reference, not the implementation target.

- [x] Add a pinned native Gecko build configuration with distinct Navi app name, basename, and GUID
- [x] Add a source overlay that generates Navi-specific branding resources
- [x] Add a CI test applying the overlay to **real upstream Mozilla files**
- [x] Add manual Windows full-source build workflow for a properly provisioned dedicated runner
- [ ] **Compile and launch a full independent Gecko Windows build** (blocking)
- [ ] Verify the actual `navinavigator.exe`, application.ini metadata, and profile isolation
- [ ] Remove old-browser user-visible branding, audit NSIS, and package the installer
- [ ] Port the approved Navi Portal hero and skins to native Gecko browser chrome
- [ ] Provide signed downloads and a secure Gecko update channel before production use
