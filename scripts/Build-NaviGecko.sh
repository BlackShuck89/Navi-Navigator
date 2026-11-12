#!/usr/bin/env bash
# Real Mozilla source build. Run from MozillaBuild's shell on Windows x64.
# Usage: bash /c/Navi-Navigator/scripts/Build-NaviGecko.sh /c/mozilla-source/firefox
set -euo pipefail
repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source_dir="${1:-}"
if [[ -z "$source_dir" || ! -f "$source_dir/mach" ]]; then
  echo "Usage: bash scripts/Build-NaviGecko.sh /path/to/bootstrapped/firefox-source" >&2
  exit 2
fi

source_dir="$(cd "$source_dir" && pwd)"
if [[ -e "$source_dir/mozconfig" || -e "$source_dir/.mozconfig" ]]; then
  echo "A mozconfig already exists in the Firefox checkout." >&2
  echo "Use a dedicated disposable source checkout, or remove the old mozconfig manually." >&2
  exit 3
fi
if [[ -n "${MOZCONFIG:-}" ]]; then
  echo "MOZCONFIG is already set: $MOZCONFIG" >&2
  exit 3
fi
export MOZCONFIG="$repo_dir/config/mozconfig.full"

echo "Preparing Navi changes in the Mozilla source checkout at: $source_dir"
python3 "$repo_dir/scripts/apply_gecko_overlay.py" --firefox-source "$source_dir"
cd "$source_dir"
echo "Building actual Firefox/Gecko with Navi branding."
./mach build
./mach package
echo "Gecko compiled and packaged; inspect obj-navi-gecko/dist."
echo "Use ./mach run with a dedicated test profile to verify launcher and identity."
echo "This has no trusted Navi update channel: do not use it for sensitive browsing."
