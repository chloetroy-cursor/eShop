#!/usr/bin/env bash
set -euo pipefail

# Lever for Catalog migration slices. Prefer unit/characterization tests;
# fall back to functional tests only when Docker is available.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

UNIT_PROJ="tests/Catalog.UnitTests/Catalog.UnitTests.csproj"
FUNC_PROJ="tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj"

run_and_report() {
  local path_label="$1"
  shift
  echo "check-catalog: path=$path_label"
  echo "check-catalog: running: $*"
  set +e
  "$@"
  local ec=$?
  set -e
  echo "check-catalog: path=$path_label exit_code=$ec"
  exit "$ec"
}

if [[ -f "$UNIT_PROJ" ]]; then
  run_and_report "A:Catalog.UnitTests" dotnet test "$UNIT_PROJ"
fi

if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  if [[ -f "$FUNC_PROJ" ]]; then
    run_and_report "B:Catalog.FunctionalTests" dotnet test "$FUNC_PROJ"
  fi
fi

echo "check-catalog: path=C:unavailable exit_code=2" >&2
echo "No Catalog unit tests found and Docker is unavailable for functional tests." >&2
echo "Add unit/characterization tests via the Characterize then extract skill," >&2
echo "or start Docker and re-run for Catalog.FunctionalTests." >&2
exit 2
