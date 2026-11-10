# Navi skinning contract (preview v1)

Navi deliberately distinguishes **skin** from **layout**. Skin changes presentation; layout changes toolbar placement, sidebar availability and which controls appear. The preview implements skins only.

## Skin data

The repository's skins/manifest.json has schemaVersion 1, a defaultSkin ID and an array of skins. Each skin contains an ID, friendly name, CSS filename, explanation and a palette with six hex values: surface, surfaceHigh, surfaceLow, accent, accentBright, text.

The preview composes skins/base.css and skins/<skin>.css into chrome/userChrome.css in a disposable Firefox profile. Each skin sets shared Navi CSS custom properties, with base.css applying those properties to Firefox browser chrome.

Current skins:
- modern: practical silver-blue layout with teal highlights and restrained depth
- classic: compact, rectangular, bright chrome
- fusion: dark navy/teal gradients and stronger rounded details

## Current limitations

The browser's modern underlying Firefox tab placement, menu affordances, extension UI and window controls are not replaced. CSS can change their appearance within upstream constraints. Firefox changes internal selectors over time. The standalone project needs source-level browser chrome integration rather than relying forever on userChrome.css.

A valid manifest and balanced CSS do not guarantee pixel-perfect rendering. Test in a real browser across active/inactive window states, multiple DPI settings, dark/light operating-system modes, long tab titles, and keyboard navigation.

## Future installable skins

A stable package format is planned but is NOT implemented. Proposed requirements:
- a versioned, declarative manifest
- local SVG/PNG icons and textures, with documented licensing
- color tokens and limited chrome CSS rather than arbitrary privileged scripts
- explicit minimum/maximum Navi compatibility
- sandboxed resource loading; no external URLs and no CSS-driven data exfiltration
- profile-independent theme selection and safe fallback to Navi Modern

Skin packages must never disable browser security protections or bypass extension permission prompts. Custom layouts will use a separate layout contract.
