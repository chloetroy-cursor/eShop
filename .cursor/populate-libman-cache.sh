#!/usr/bin/env bash
# Populates LibMan's offline "cdnjs" cache from the npm registry.
#
# The eShop Identity.API restores client-side libraries (jquery, bootstrap, ...)
# via LibMan's cdnjs provider at build time. In network-restricted environments
# cdnjs.cloudflare.com / api.cdnjs.com are unreachable, but registry.npmjs.org is.
# The same libraries live on npm, so we pre-seed LibMan's on-disk cache with the
# exact files (and the small metadata JSON files LibMan validates against) so the
# committed libman.json restores fully offline without any code changes.
set -euo pipefail

# LibMan cache lives under LocalApplicationData (~/.local/share on Linux).
CACHE_DIR="${LOCALAPPDATA:-$HOME/.local/share}/.librarymanager/cache/cdnjs"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

fetch_npm() { # <pkg> <version> -> extracts tarball to $WORK/<pkg>-<version>/package
  local pkg="$1"
  local ver="$2"
  local dest="$WORK/$pkg-$ver"
  mkdir -p "$dest"
  curl -sSL "https://registry.npmjs.org/$pkg/-/$pkg-$ver.tgz" -o "$dest.tgz"
  tar xzf "$dest.tgz" -C "$dest"
}

# seed <cdnjs-lib> <version> <default-file> <files-csv> <npm-pkg> <src-prefix>
# copies each <file> from npm <src-prefix>/<file> into the cdnjs cache and writes metadata.
seed() {
  local lib="$1"
  local ver="$2"
  local deffile="$3"
  local files="$4"
  local npmpkg="$5"
  local prefix="$6"
  local base="$CACHE_DIR/$lib"
  local vdir="$base/$ver"
  mkdir -p "$vdir"
  IFS=',' read -ra arr <<< "$files"
  local files_json=""
  local first=1
  for f in "${arr[@]}"; do
    mkdir -p "$vdir/$(dirname "$f")"
    cp "$WORK/$npmpkg/package/$prefix/$f" "$vdir/$f"
    if [ $first -eq 1 ]; then files_json="\"$f\""; first=0; else files_json="$files_json,\"$f\""; fi
  done
  printf '{"filename":"%s","versions":["%s"]}' "$deffile" "$ver" > "$base/metadata.json"
  printf '{"files":[%s]}' "$files_json" > "$base/$ver-metadata.json"
  echo "  seeded $lib@$ver (${#arr[@]} files)"
}

echo "Populating LibMan cdnjs cache at $CACHE_DIR"
fetch_npm jquery 3.6.3
fetch_npm bootstrap 5.2.3
fetch_npm jquery-validation-unobtrusive 4.0.0
fetch_npm jquery-validation 1.19.5

seed jquery 3.6.3 "jquery.min.js" \
  "jquery.js,jquery.min.js,jquery.min.map" \
  jquery-3.6.3 dist
seed bootstrap 5.2.3 "js/bootstrap.min.js" \
  "css/bootstrap.css,css/bootstrap.css.map,css/bootstrap.min.css,css/bootstrap.min.css.map,js/bootstrap.bundle.js,js/bootstrap.bundle.min.js" \
  bootstrap-5.2.3 dist
seed jquery-validation-unobtrusive 4.0.0 "jquery.validate.unobtrusive.min.js" \
  "jquery.validate.unobtrusive.js,jquery.validate.unobtrusive.min.js" \
  jquery-validation-unobtrusive-4.0.0 dist
seed jquery-validate 1.19.5 "jquery.validate.min.js" \
  "jquery.validate.js,jquery.validate.min.js" \
  jquery-validation-1.19.5 dist

echo "LibMan cdnjs cache populated."
