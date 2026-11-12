#!/usr/bin/env python3
"""Offline smoke tests for source overlay preparation. No full Gecko checkout needed."""
from __future__ import annotations

from pathlib import Path
import subprocess
import tempfile
import unittest

import apply_gecko_overlay as overlay


class OverlayTest(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / "firefox"
        self.source.mkdir()
        subprocess.run(["git", "init", "-q", str(self.source)], check=True)
        subprocess.run([
            "git", "-C", str(self.source), "-c", "user.name=Navi CI",
            "-c", "user.email=navi@example.invalid", "commit", "--allow-empty",
            "-q", "-m", "test source fixture"
        ], check=True)

        def put(path: str, content: str) -> None:
            p = self.source / path
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(content, encoding="utf-8")

        put("browser/moz.configure",
            'imply_option("MOZ_APP_VENDOR", "Mozilla")\n'
            'imply_option("MOZ_APP_ID", "{ec8030f7-c20a-464f-9b0e-13a3a9e97384}")\n')
        put("browser/themes/shared/browser-shared.css", ":root { color: red; }\n")
        base = "browser/branding/unofficial/"
        put(base + "configure.sh", "MOZ_APP_DISPLAYNAME=Nightly\n")
        put(base + "moz.build", "DIRS += ['content', 'locales']\n")
        put(base + "content/jar.mn", "browser.jar:\n  content/branding/about-logo.svg\n")
        put(base + "branding.nsi",
            '!define BrandFullNameInternal "Mozilla Developer Preview"\n'
            '!define CompanyName           "mozilla.org"\n'
            'https://www.mozilla.org https://support.mozilla.org\n')
        put(base + "pref/firefox-branding.js",
            'pref("app.update.url.manual", "https://nightly.mozilla.org");\n')
        put(base + "locales/en-US/brand.ftl", "-brand-full-name = Nightly\n")
        put(base + "locales/en-US/brand.properties", "brandFullName=Nightly\n")
        put(base + "content/about-logo.svg", "<svg/>")

    def test_branding_generated(self):
        result = overlay.prepare(self.source, allow_unpinned=True)
        self.assertIn("branding", result)
        branding = self.source / "browser/branding/navi"
        self.assertIn('MOZ_APP_DISPLAYNAME="Navi Navigator"',
                      (branding / "configure.sh").read_text("utf-8"))
        self.assertIn("Navi Navigator",
                      (branding / "locales/en-US/brand.ftl").read_text("utf-8"))
        self.assertIn('pref("app.update.enabled", false)',
                      (branding / "pref/firefox-branding.js").read_text("utf-8"))
        self.assertIn("Navi Navigator",
                      (branding / "branding.nsi").read_text("utf-8"))
        self.assertIn('Navi Navigator',
                      (self.source / "browser/moz.configure").read_text("utf-8"))
        self.assertIn("f6637d24-886a-4f2c-93ea-b2de570c13e8",
                      (self.source / "browser/moz.configure").read_text("utf-8"))
        css = (self.source / "browser/themes/shared/browser-shared.css").read_text("utf-8")
        self.assertEqual(css.count(overlay.START), 1)
        self.assertEqual(css.count(overlay.END), 1)
        self.assertIn("--navi-accent", css)
        self.assertEqual((branding / "default16.png").read_bytes()[:8],
                         b"\x89PNG\r\n\x1a\n")
        self.assertEqual((branding / "firefox.ico").read_bytes()[:4],
                         b"\x00\x00\x01\x00")
        self.assertIn("Navi", (branding / "content/about-wordmark.svg").read_text("utf-8"))

        # Running again should regenerate managed branding and replace, not
        # duplicate, the chrome CSS block.
        overlay.prepare(self.source, allow_unpinned=True)
        repeated_css = (self.source / "browser/themes/shared/browser-shared.css").read_text("utf-8")
        self.assertEqual(repeated_css.count(overlay.START), 1)
        self.assertEqual(repeated_css.count(overlay.END), 1)

    def test_reject_unpinned_revision(self):
        with self.assertRaisesRegex(RuntimeError, "Expected upstream Firefox"):
            overlay.prepare(self.source)
        self.assertFalse((self.source / "browser/branding/navi").exists())

    def test_refuse_unmanaged_brand(self):
        target = self.source / "browser/branding/navi"
        target.mkdir(parents=True)
        (target / "manual.txt").write_text("keep", encoding="utf-8")
        with self.assertRaisesRegex(RuntimeError, "unmanaged directory"):
            overlay.prepare(self.source, allow_unpinned=True)
        self.assertEqual((target / "manual.txt").read_text(), "keep")


if __name__ == "__main__":
    unittest.main(verbosity=2)
