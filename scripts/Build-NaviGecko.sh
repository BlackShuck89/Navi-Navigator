#!/usr/bin/env bash
# Run inside MozillaBuild's start-shell.bat shell after Firefox bootstrap.
# Usage: bash scripts/Build-NaviGecko.sh /c/mozilla-source/firefox
set -euo pipefail
repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_dir="${1:-}"
if [[ -z "$source_dir" || ! -f "$source_dir/mach" ]]; then
  echo "Usage: bash scripts/Build-NaviGecko.sh /path/to/firefox-source" >&2
  exit 2
fi
source_dir="$(cd "$source_dir" && pwd)"
echo "Preparing Firefox source at $source_dir"
python3 "$repo_dir/scripts/apply_gecko_overlay.py" --firefox-source "$source_dir"
if [[ -e "$source_dir/mozconfig" || -e "$source_dir/.mozconfig" ]]; then
  echo "Refusing to replace an existing mozconfig. Move it aside first." >&2
  exit 3
fi
cp "$repo_dir/config/mozconfig.full" "$source_dir/mozconfig"
cd "$source_dir"
echo "Running an experimental full Gecko build (may require significant resources)."
./mach build
./mach package
echo "Navi build and package commands returned success."
echo "Inspect the obj-navi/dist directory and verify application branding."
echo "Do NOT distribute until identity, profile separation, update path and QA are validated."
