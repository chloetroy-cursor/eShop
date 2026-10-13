#!/usr/bin/env bash
# Bring the eShop Aspire stack up for verification and remember whether we started it.
#
#   launch.sh            start (or adopt an already-running stack), wait until ready
#   launch.sh --timeout 900
#
# Delegates to the repo's demo-prep launcher, which refuses to start a second AppHost
# when the storefront already answers. Writes a marker under the state dir only when
# this call actually started the stack, so cleanup.sh never stops someone else's.
set -euo pipefail

ROOT=$(git rev-parse --show-toplevel)
STATE_DIR="${ESHOP_VERIFY_STATE_DIR:-$HOME/.cursor/verify-evidence/eshop/.state}"
PREP="$ROOT/.cursor/skills/demo-prep/scripts/prep.py"
mkdir -p "$STATE_DIR"

if ! command -v npx >/dev/null || [[ ! -d "$ROOT/node_modules/@playwright/test" ]]; then
  echo "installing Playwright deps (npm ci)"
  (cd "$ROOT" && npm ci --no-audit --no-fund >/dev/null)
fi

out=$(python3 "$PREP" start "$@" 2>&1 | tee /dev/stderr) || { echo "launch failed" >&2; exit 1; }
if grep -q "ESHOP DEMO READY (already running)" <<<"$out"; then
  echo "adopted an already-running stack; cleanup.sh will leave it up"
else
  date -u +%Y-%m-%dT%H:%M:%SZ > "$STATE_DIR/started-apphost"
  echo "started the stack; marker: $STATE_DIR/started-apphost"
fi
