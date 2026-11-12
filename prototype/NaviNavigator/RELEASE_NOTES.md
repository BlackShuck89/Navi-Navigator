# Navi Navigator 0.0.4 — Portal visual redesign

This update rebuilds the local Navi Portal to match the richer early-2000s browser concept: a widescreen Navi hero, stronger masthead and original compass mark, expanded sidebar, news/project/download modules, a skin comparison panel, and a branded footer. Modern, Classic, and Fusion each receive distinctive portal treatments.

The page embeds original approved Navi character artwork as `Assets/navi-approved-hero.webp` when present. The application can fall back to its existing SVG if the image is unavailable; a complete preview package needs to include the new WebP alongside the executable. The About page uses the same artwork. There are no old Netscape wordmarks or logos in the new artwork.

Bookmarks and the previous user profile path are preserved. The underlying renderer remains Microsoft WebView2 (Chromium); the Gecko/Firefox fork is not built into this preview. No code signing or update service is included. Avoid sensitive browsing.
