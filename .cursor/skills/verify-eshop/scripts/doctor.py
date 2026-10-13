#!/usr/bin/env python3
"""Read-only: is the running eShop stack worth driving?

Exit 0 when every check passes, 1 otherwise. Never starts, stops, or mutates anything.
"""
from __future__ import annotations

import importlib.util
import json
import os
import subprocess
import tempfile
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], text=True, capture_output=True, check=True).stdout.strip())
PREP = ROOT / ".cursor/skills/demo-prep/scripts/prep.py"
IDENTITY_DISCOVERY = "http://localhost:5223/.well-known/openid-configuration"
DCP_LABEL = "com.microsoft.developer.usvc-dev.name"


def load_prep():
    spec = importlib.util.spec_from_file_location("prep", PREP)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def http_ok(url: str) -> bool:
    try:
        with urllib.request.urlopen(url, timeout=10) as response:
            return response.status == 200
    except (OSError, urllib.error.URLError):
        return False


def dcp_containers() -> list[str]:
    result = subprocess.run(
        ["docker", "ps", "--filter", f"label={DCP_LABEL}", "--format", "{{.Names}}"],
        text=True,
        capture_output=True,
    )
    return result.stdout.split() if result.returncode == 0 else []


def drop_temp_browsers_path() -> None:
    """Agent sandboxes export PLAYWRIGHT_BROWSERS_PATH under the system temp dir.

    That cache is per session and usually empty, so Playwright reports chromium missing
    while the durable default cache has it. Mirror of prep.py's NUGET_PACKAGES handling;
    verify.sh applies the same rule before launching the browser.
    """
    ambient = os.environ.get("PLAYWRIGHT_BROWSERS_PATH")
    if not ambient:
        return
    if Path(tempfile.gettempdir()).resolve() in Path(ambient).resolve().parents:
        os.environ.pop("PLAYWRIGHT_BROWSERS_PATH")


def playwright_ready() -> tuple[bool, str]:
    drop_temp_browsers_path()
    probe = (
        "const {chromium}=require('playwright-core');const fs=require('fs');"
        "const p=chromium.executablePath();process.stdout.write(JSON.stringify({p,ok:fs.existsSync(p)}))"
    )
    result = subprocess.run(["node", "-e", probe], cwd=ROOT, text=True, capture_output=True)
    if result.returncode != 0:
        return False, "node_modules missing; run `npm ci` in the repo root"
    info = json.loads(result.stdout)
    if not info["ok"]:
        return False, f"chromium not installed at {info['p']}; run `npx playwright install chromium`"
    return True, info["p"]


def main() -> int:
    prep = load_prep()
    checks: list[tuple[str, bool, str]] = []

    issues = prep.doctor(ROOT)
    checks.append(("prereqs (.NET 10, Docker daemon)", not issues, "; ".join(issues) or "ok"))

    ready = prep.web_ready(timeout=15)
    checks.append(("storefront http://localhost:5045 renders catalog + Blazor script", ready, "ok" if ready else "not ready; run scripts/launch.sh or `prep.py status`"))

    checks.append(("identity http://localhost:5223 discovery document", http_ok(IDENTITY_DISCOVERY), "ok" if http_ok(IDENTITY_DISCOVERY) else "Identity.API not answering on the http profile port"))

    pid = prep.tracked_pid(ROOT)
    session = prep.session_exists()
    owner = f"tmux session {prep.SESSION}" if session else f"pid {pid} (pid file)" if pid else "untracked"
    checks.append(("AppHost runner known to demo-prep", session or pid is not None, owner if (session or pid) else "port 5045 is served by a process this skill did not start; do not stop it"))

    names = dcp_containers()
    have = {prefix: any(n.startswith(prefix + "-") for n in names) for prefix in ("postgres", "eventbus", "redis")}
    checks.append(("Aspire containers postgres/eventbus/redis", all(have.values()), ", ".join(names) or "none with DCP label"))

    ok, detail = playwright_ready()
    checks.append(("Playwright + chromium", ok, detail))

    crashed = prep.crashed_services()
    checks.append(("no crashed Aspire services", not crashed, "ok" if not crashed else ", ".join(f"{n} exit {c}" for n, c, _ in crashed)))

    failed = 0
    for name, passed, detail in checks:
        print(f"[{'ok' if passed else 'FAIL'}] {name}: {detail}")
        failed += 0 if passed else 1
    print(f"log: {prep.log_path(ROOT)}")
    print(f"evidence root: {os.environ.get('ESHOP_VERIFY_EVIDENCE_DIR', Path.home() / '.cursor/verify-evidence/eshop')}")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
