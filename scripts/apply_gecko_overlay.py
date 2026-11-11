#!/usr/bin/env python3
"""Prepare a pinned Firefox checkout for an independently branded Navi build.

Only modifies a disposable *upstream source checkout*, not installed Firefox.
No third-party Python modules are required. This is an experimental Gecko
integration layer, not a guarantee that the full browser builds successfully.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
LOCK = json.loads((ROOT / "config" / "upstream.json").read_text("utf-8"))
START = "/* BEGIN NAVI CHROME OVERLAY - generated */"
END = "/* END NAVI CHROME OVERLAY */"
GUID = "{f6637d24-886a-4f2c-93ea-b2de570c13e8}"


def replace_once(text: str, old: str, new: str, name: str) -> str:
    old_count = text.count(old)
    new_count = text.count(new)
    if old_count == 0 and new_count == 1:
        # Already patched by an earlier Navi run.
        return text
    if old_count != 1 or new_count != 0:
        raise RuntimeError(
            f"{name}: expected exactly one unmodified occurrence of {old!r}, "
            f"found {old_count} originals and {new_count} Navi replacements; "
            "upstream layout may have changed"
        )
    return text.replace(old, new, 1)


def commit_id(src: Path) -> str:
    try:
        return subprocess.check_output(
            ["git", "-C", str(src), "rev-parse", "HEAD"],
            text=True,
            stderr=subprocess.STDOUT,
        ).strip()
    except (OSError, subprocess.CalledProcessError) as ex:
        raise RuntimeError(f"Cannot read upstream Git revision: {ex}") from ex


def svg_wordmark() -> str:
    # Generated text is browser UI, not a typeface/font asset.
    return '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 630 100" role="img" aria-label="Navi Navigator">
  <text x="0" y="69" fill="#62c8c5" font-size="68" font-weight="bold" font-family="Segoe UI,Arial,sans-serif">Navi</text>
  <text x="160" y="69" fill="#e9f6f6" font-size="52" font-family="Segoe UI,Arial,sans-serif">Navigator</text>
</svg>
'''


def render_app_icon(size: int) -> bytes:
    """Write a small original compass as a dependency-free RGBA PNG."""
    import math
    import struct
    import zlib

    def blend(a, b, f):
        return int(a * (1 - f) + b * f)

    scanlines = bytearray()
    center = (size - 1) / 2
    for y in range(size):
        scanlines.append(0)
        for x in range(size):
            dx, dy = (x - center) / (size / 2), (y - center) / (size / 2)
            r = math.hypot(dx, dy)
            if r > .96:
                pixel = (0, 0, 0, 0)
            elif r > .88:
                pixel = (183, 230, 229, 255)
            elif r > .78:
                pixel = (21, 45, 65, 255)
            else:
                pixel = (25, 116, 119, 255)
                if .70 < r < .75:
                    pixel = (124, 201, 203, 255)
                # Compass needles: cardinal direction spikes
                north = abs(dx) < .11 * (1 - abs(dy) / .83) and -.77 < dy < 0
                south = abs(dx) < .11 * (1 - abs(dy) / .83) and 0 < dy < .77
                east = abs(dy) < .11 * (1 - abs(dx) / .83) and 0 < dx < .77
                west = abs(dy) < .11 * (1 - abs(dx) / .83) and -.77 < dx < 0
                if north or south or east or west:
                    pixel = (237, 248, 248, 255) if dx + dy < .02 else (106, 90, 205, 255)
                if r < .09:
                    pixel = (11, 39, 55, 255)
            scanlines.extend(pixel)

    def chunk(tag: bytes, data: bytes) -> bytes:
        return (
            struct.pack(">I", len(data))
            + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        )

    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(bytes(scanlines), 9))
        + chunk(b"IEND", b"")
    )


def render_ico(images: dict[int, bytes]) -> bytes:
    import struct
    sizes = sorted(s for s in images if s in (16, 32, 48, 64, 128, 256))
    header = struct.pack("<HHH", 0, 1, len(sizes))
    directory = bytearray()
    payload = bytearray()
    offset = 6 + 16 * len(sizes)
    for size in sizes:
        data = images[size]
        directory.extend(struct.pack(
            "<BBBBHHII", 0 if size == 256 else size,
            0 if size == 256 else size, 0, 0, 1, 32, len(data), offset
        ))
        payload.extend(data)
        offset += len(data)
    return header + directory + payload


def prepare(src: Path, allow_unpinned: bool = False) -> dict[str, str]:
    src = src.resolve()
    if not (src / "browser" / "moz.configure").is_file():
        raise RuntimeError(f"Not a Firefox source checkout: {src}")
    revision = commit_id(src)
    expected = LOCK["revision"]
    if revision != expected and not allow_unpinned:
        raise RuntimeError(
            f"Expected upstream Firefox {expected}, got {revision}. "
            "Checkout the pinned revision, or use --allow-unpinned for experimental updates."
        )

    unofficial = src / "browser" / "branding" / "unofficial"
    target = src / "browser" / "branding" / "navi"
    if not (unofficial / "configure.sh").is_file():
        raise RuntimeError("Firefox unofficial branding layout not found")

    # Prepare changes up front; reject upstream mismatches before writing.
    moz_file = src / "browser" / "moz.configure"
    source_text = moz_file.read_text("utf-8")
    moz_text = replace_once(
        source_text,
        'imply_option("MOZ_APP_VENDOR", "Mozilla")',
        'imply_option("MOZ_APP_VENDOR", "Navi Navigator")',
        "browser/moz.configure",
    )
    moz_text = replace_once(
        moz_text,
        'imply_option("MOZ_APP_ID", "{ec8030f7-c20a-464f-9b0e-13a3a9e97384}")',
        f'imply_option("MOZ_APP_ID", "{GUID}")',
        "browser/moz.configure",
    )

    shared = src / "browser" / "themes" / "shared" / "browser-shared.css"
    if not shared.is_file():
        raise RuntimeError("Upstream browser-shared.css not found")
    css = shared.read_text("utf-8")
    if START in css and END in css:
        css = css[:css.index(START)] + css[css.index(END) + len(END):]
    elif START in css or END in css:
        raise RuntimeError("Broken Navi overlay block in browser-shared.css")
    css = css.rstrip() + "\n\n" + START + "\n"
    css += (ROOT / "skins" / "base.css").read_text("utf-8") + "\n"
    css += (ROOT / "skins" / "modern.css").read_text("utf-8") + "\n"
    css += END + "\n"

    # Never silently reset a customized branding directory.
    if target.exists():
        marker = target / ".navi-generated"
        if not marker.is_file():
            raise RuntimeError(f"Refusing to overwrite an unmanaged directory: {target}")
        shutil.rmtree(target)
    shutil.copytree(unofficial, target)

    (target / "configure.sh").write_text(
        '''# Navi Navigator branding for independently built Gecko previews.
MOZ_APP_DISPLAYNAME="Navi Navigator"
MOZ_MACBUNDLE_ID="org.navinavigator.browser"
''', "utf-8"
    )
    ftl_path = target / "locales" / "en-US" / "brand.ftl"
    ftl_path.write_text(
        '''-brand-shorter-name = Navi
-brand-short-name = Navi Navigator
-brand-shortcut-name = Navi
-brand-full-name = Navi Navigator
-brand-product-name = Navi Navigator
-vendor-short-name = Navi Navigator
trademarkInfo = { " " }
''', "utf-8"
    )
    (target / "locales" / "en-US" / "brand.properties").write_text(
        "brandShorterName=Navi\nbrandShortName=Navi Navigator\nbrandFullName=Navi Navigator\n", "utf-8"
    )
    installer = target / "branding.nsi"
    installer_text = installer.read_text("utf-8")
    installer_text = installer_text.replace("Mozilla Developer Preview", "Navi Navigator")
    installer_text = installer_text.replace('!define CompanyName           "mozilla.org"',
                                             '!define CompanyName           "Navi Navigator"')
    installer_text = installer_text.replace("https://www.mozilla.org", "https://github.com/BlackShuck89/Navi-Navigator")
    installer_text = installer_text.replace("https://support.mozilla.org", "https://github.com/BlackShuck89/Navi-Navigator/issues")
    installer.write_text(installer_text, "utf-8")

    prefs = target / "pref" / "firefox-branding.js"
    prefs_text = prefs.read_text("utf-8")
    prefs_text = prefs_text.replace("https://nightly.mozilla.org", "https://github.com/BlackShuck89/Navi-Navigator/releases")
    prefs_text += '''\n// Development builds have NO secure Navi update server yet.
pref("app.update.enabled", false);
'''
    prefs.write_text(prefs_text, "utf-8")

    artwork = (ROOT / "assets" / "navi-compass.svg").read_text("utf-8")
    content = target / "content"
    (content / "about-logo.svg").write_text(artwork, "utf-8")
    (content / "about-wordmark.svg").write_text(svg_wordmark(), "utf-8")
    (content / "firefox-wordmark.svg").write_text(svg_wordmark(), "utf-8")
    (content / "aboutDialog.css").write_text(
        '''#aboutDialogContainer { background-color: #152d41; color: #e7f5f5; color-scheme: dark; }
#bottomBox { background-color: #0c2c2e; }
#rightBox { margin-inline: 30px; padding-top: 64px; }
''', "utf-8"
    )

    png_sizes = (16, 22, 24, 32, 48, 64, 128, 256)
    images = {size: render_app_icon(size) for size in png_sizes}
    for size in png_sizes:
        (target / f"default{size}.png").write_bytes(images[size])
    ico = render_ico(images)
    for name in ("firefox.ico", "firefox64.ico", "newtab.ico", "newwindow.ico",
                 "pbmode.ico", "document.ico", "document_pdf.ico"):
        (target / name).write_bytes(ico)
    for name in ("about-logo.png", "about-logo@2x.png", "about-logo-private.png",
                 "about-logo-private@2x.png", "about.png"):
        (content / name).write_bytes(images[256])
    for name, size in {
        "VisualElements_150.png": 128,
        "VisualElements_70.png": 64,
        "PrivateBrowsing_150.png": 128,
        "PrivateBrowsing_70.png": 64,
        "background.png": 256,
    }.items():
        (target / name).write_bytes(images[size])

    (target / ".navi-generated").write_text(
        "Generated by Navi Navigator/scripts/apply_gecko_overlay.py\n"
        f"Upstream: {revision}\n", "utf-8"
    )
    moz_file.write_text(moz_text, "utf-8")
    shared.write_text(css, "utf-8")

    return {"revision": revision, "branding": str(target), "css": str(shared),
            "moz_configure": str(moz_file)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--firefox-source", type=Path, required=True)
    parser.add_argument("--allow-unpinned", action="store_true",
                        help="Unsafe for releases; permits experimental upstream revisions")
    args = parser.parse_args()
    try:
        result = prepare(args.firefox_source, args.allow_unpinned)
    except Exception as exc:
        print(f"Navi source overlay failed: {exc}", file=sys.stderr)
        return 1
    print(json.dumps(result, indent=2))
    print("Source prepared. Next: copy config/mozconfig.full to <firefox>/.mozconfig, then run ./mach build.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
