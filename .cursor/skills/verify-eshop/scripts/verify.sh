#!/usr/bin/env bash
# Drive one or more feature recipes against the running stack and collect evidence.
#
#   verify.sh                              every recipe in recipes/
#   verify.sh catalog-browse sign-in       named recipes (file stem under recipes/)
#   verify.sh --spec-dir /tmp/my-recipes   ad-hoc specs that import ../recipes/fixtures
#   verify.sh --headed catalog-browse      watch the browser
#   verify.sh --grep "Paid"                Playwright title filter
#
# Each run gets its own evidence directory, printed first and last. Runs doctor first
# and refuses to drive a stack that fails it (override with --skip-doctor).
set -euo pipefail

HERE=$(cd "$(dirname "$0")/.." && pwd)
ROOT=$(git rev-parse --show-toplevel)
source "$HERE/scripts/env.sh"
RUN_ID=$(date -u +%Y%m%dT%H%M%SZ)
EVIDENCE_ROOT="${ESHOP_VERIFY_EVIDENCE_ROOT:-$HOME/.cursor/verify-evidence/eshop}"
export ESHOP_VERIFY_EVIDENCE_DIR="${ESHOP_VERIFY_EVIDENCE_DIR:-$EVIDENCE_ROOT/$RUN_ID}"

skip_doctor=0
pw_args=()
recipes=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-doctor) skip_doctor=1 ;;
    --spec-dir) export ESHOP_VERIFY_SPEC_DIR="$(cd "$2" && pwd)"; shift ;;
    --headed|--debug|--ui) pw_args+=("$1") ;;
    --grep|-g) pw_args+=("--grep" "$2"); shift ;;
    --*) pw_args+=("$1") ;;
    *) recipes+=("$1") ;;
  esac
  shift
done

mkdir -p "$ESHOP_VERIFY_EVIDENCE_DIR"
echo "evidence: $ESHOP_VERIFY_EVIDENCE_DIR"

if [[ $skip_doctor -eq 0 ]]; then
  python3 "$HERE/scripts/doctor.py" | tee "$ESHOP_VERIFY_EVIDENCE_DIR/doctor.txt"
  if [[ ${PIPESTATUS[0]} -ne 0 ]]; then
    echo "doctor failed; not driving. Fix the stack or pass --skip-doctor." >&2
    exit 1
  fi
fi

files=()
for r in ${recipes[@]+"${recipes[@]}"}; do
  files+=("${r%.spec.ts}.spec.ts")
done

cd "$ROOT"
set +e
# macOS ships bash 3.2, where "${empty[@]}" trips set -u; this expansion does not.
npx playwright test -c "$HERE/playwright.config.ts" ${pw_args[@]+"${pw_args[@]}"} ${files[@]+"${files[@]}"}
status=$?
set -e

echo
echo "evidence: $ESHOP_VERIFY_EVIDENCE_DIR"
find "$ESHOP_VERIFY_EVIDENCE_DIR" -type f ! -path '*/playwright-output/*' | sort
exit $status
