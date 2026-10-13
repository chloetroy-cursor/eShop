#!/usr/bin/env bash
# Tear down what a verification run created. Evidence is never removed.
#
#   cleanup.sh          empty the verification user's bag; stop the AppHost only if
#                       launch.sh started it (marker file); leave persistent containers
#   cleanup.sh --keep-stack   skip the AppHost stop even when we started it
set -euo pipefail

HERE=$(cd "$(dirname "$0")/.." && pwd)
ROOT=$(git rev-parse --show-toplevel)
source "$HERE/scripts/env.sh"
STATE_DIR="${ESHOP_VERIFY_STATE_DIR:-$HOME/.cursor/verify-evidence/eshop/.state}"
PREP="$ROOT/.cursor/skills/demo-prep/scripts/prep.py"
keep_stack=0
[[ "${1:-}" == "--keep-stack" ]] && keep_stack=1

# Residue: the verification user's basket lives in Redis and outlives the run. Drain it
# through the real cart form; if the storefront is already down there is nothing to drain.
if python3 "$PREP" status >/dev/null 2>&1; then
  export ESHOP_VERIFY_EVIDENCE_DIR="${ESHOP_VERIFY_EVIDENCE_DIR:-$HOME/.cursor/verify-evidence/eshop/cleanup-$(date -u +%Y%m%dT%H%M%SZ)}"
  (cd "$ROOT" && ESHOP_VERIFY_SPEC_DIR="$HERE/scripts/cleanup" npx playwright test -c "$HERE/playwright.config.ts" --reporter=line) \
    || echo "warning: could not empty the bag through the UI; run scripts/side-effects.sh basket to inspect" >&2
else
  echo "storefront not up; skipping bag drain"
fi

# Instances: only the AppHost this skill started. Postgres/RabbitMQ/Redis containers are
# ContainerLifetime.Persistent by design and stay for the next run.
if [[ -f "$STATE_DIR/started-apphost" ]]; then
  if [[ $keep_stack -eq 1 ]]; then
    echo "keeping the stack (--keep-stack); marker left in place"
  else
    python3 "$PREP" stop
    rm -f "$STATE_DIR/started-apphost"
  fi
else
  echo "AppHost was not started by this skill; leaving it running"
fi

echo "evidence retained under ${ESHOP_VERIFY_EVIDENCE_ROOT:-$HOME/.cursor/verify-evidence/eshop}"
