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

- [ ] Pin a supported Firefox upstream revision and create a reproducible patch set
- [ ] Implement Navi application identity, icon resources, version strings, profile namespace
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
