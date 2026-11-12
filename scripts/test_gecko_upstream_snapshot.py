#!/usr/bin/env python3
"""Integration test: apply Navi's Gecko overlay to REAL pinned Mozilla source files.

This is deliberately *not* a Gecko compile. It fetches the relevant files
from Mozilla's exact pinned revision, builds a minimal disposable source tree,
and checks the overlay against the real upstream layout (not hand-made mocks).
No third-party Python packages needed.
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request

import apply_gecko_overlay as overlay

REPO = "mozilla-firefox/firefox"
REV = overlay.LOCK["revision"]
BASE = f"https://raw.githubusercontent.com/{REPO}/{REV}/"
API = f"https://api.github.com/repos/{REPO}/contents/"
REAL_FILES = [
    "browser/moz.configure",
    "browser/themes/shared/browser-shared.css",
    "js/moz.configure",
    "build/moz.configure/init.configure",
]
MAX_FILE_BYTES = 5 * 1024 * 1024
MAX_FILES = 100


def download(url: str, *, json_api: bool = False) -> bytes:
    headers = {"User-Agent": "NaviNavigator-gecko-upstream-validation"}
    if json_api:
        headers["Accept"] = "application/vnd.github+json"
        token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
        if token:
            headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(url, headers=headers)
    with urllib.request.urlopen(request, timeout=35) as response:
        raw = response.read(MAX_FILE_BYTES + 1)
    if len(raw) > MAX_FILE_BYTES:
        raise RuntimeError(f"Upstream file exceeds expected size: {url}")
    return raw


def write_remote(root: Path, path: str) -> None:
    target = root / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(download(BASE + path))


def fetch_branding_dir(root: Path, remote_dir: str, tally: list[str]) -> None:
    url = API + remote_dir + "?ref=" + REV
    items = json.loads(download(url, json_api=True).decode("utf-8"))
    if not isinstance(items, list):
        raise RuntimeError(f"Expected directory listing: {remote_dir}")
    for item in items:
        name = item["name"]
        path = item["path"]
        if not re.fullmatch(r"[A-Za-z0-9_.@-]+", name):
            raise RuntimeError("Unexpected source path name: " + path)
        if len(tally) > MAX_FILES:
            raise RuntimeError("Branding tree unexpectedly large; refusing broad fetch")
        if item["type"] == "dir":
            fetch_branding_dir(root, path, tally)
        elif item["type"] == "file":
            write_remote(root, path)
            tally.append(path)
        else:
            raise RuntimeError(f"Unexpected source entry {item['type']}: {path}")


def main() -> None:
    with tempfile.TemporaryDirectory(prefix="navi-real-gecko-") as folder:
        src = Path(folder) / "firefox"
        src.mkdir()
        subprocess.run(["git", "init", "-q", str(src)], check=True)
        subprocess.run([
            "git", "-C", str(src), "-c", "user.name=Navi CI",
            "-c", "user.email=navi-ci@example.invalid", "commit", "-q",
            "--allow-empty", "-m", "Temporary source snapshot test"
        ], check=True)
        for file in REAL_FILES:
            write_remote(src, file)
        branding_files: list[str] = []
        fetch_branding_dir(src, "browser/branding/unofficial", branding_files)
        print(f"Downloaded {len(branding_files)} actual Mozilla branding files"
              f" and {len(REAL_FILES)} source/config files from {REV}", flush=True)

        app_name_opts = (src / "js/moz.configure").read_text("utf-8")
        app_basename_opts = (src / "build/moz.configure/init.configure").read_text("utf-8")
        assert '--with-app-name' in app_name_opts
        assert '--with-app-basename' in app_basename_opts

        manifest = src / "browser/branding/unofficial/content/jar.mn"
        text = manifest.read_text("utf-8")
        assert "about-logo.svg" in text and "about-wordmark.svg" in text

        result = overlay.prepare(src, allow_unpinned=True)
        navi = src / "browser/branding/navi"
        assert navi.is_dir()
        assert result["revision"] == overlay.commit_id(src)
        assert "Navi Navigator" in (navi / "locales/en-US/brand.ftl").read_text("utf-8")
        assert "Navi Navigator" in (navi / "branding.nsi").read_text("utf-8")
        assert "f6637d24-886a-4f2c-93ea-b2de570c13e8" in (
            src / "browser/moz.configure").read_text("utf-8")
        assert (navi / "default256.png").read_bytes().startswith(b"\x89PNG")
        assert (navi / "firefox.ico").read_bytes()[:4] == b"\x00\x00\x01\x00"
        assert (navi / "content/about-wordmark.svg").read_text("utf-8").count("Navi") >= 1

        # Verify that everything Mozilla's existing jar manifest references is
        # still available in the new branding directory.
        for line in text.splitlines():
            line = line.strip()
            if not line or line.startswith("#") or line.startswith("%") or line.endswith(":"):
                continue
            parts = line.split()
            if len(parts) < 1 or not parts[0].startswith("content/branding/"):
                continue
            file_ref = parts[-1]
            if file_ref.startswith("(") and file_ref.endswith(")"):
                source_path = (navi / "content" / file_ref[1:-1]).resolve()
            else:
                # jar.mn uses the destination path (content/branding/foo)
                # when no explicit source is present. Its actual source file
                # is relative to the directory containing jar.mn.
                source_path = navi / "content" / Path(file_ref).name
            if not source_path.is_file():
                raise AssertionError(f"Generated branding missing jar asset: {source_path}")

        # Repeated generation must be safe and must not duplicate source CSS.
        before_css = (src / "browser/themes/shared/browser-shared.css").read_bytes()
        overlay.prepare(src, allow_unpinned=True)
        assert (src / "browser/themes/shared/browser-shared.css").read_bytes() == before_css
        assert (navi / ".navi-generated").is_file()

        print("PASS: Navi Gecko integration matches real pinned Mozilla source"
              " layout, brand jar assets, icon resources and configuration.", flush=True)


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, RuntimeError, ValueError, urllib.error.URLError) as e:
        print(f"FAIL: real upstream integration check: {e}", file=sys.stderr)
        raise SystemExit(1)
