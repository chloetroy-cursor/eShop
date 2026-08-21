#!/usr/bin/env python3
"""Start and inspect the eShop visual demo stack."""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

SESSION = "eshop-fe-demo"
WEB_URL = "http://localhost:5045"
DASHBOARD_URL = "http://localhost:18848"
TMUX_CONFIG = Path("/exec-daemon/tmux.portal.conf")


def repository_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        check=True,
        text=True,
        capture_output=True,
    )
    return Path(result.stdout.strip()).resolve()


def tmux_command(*args: str) -> list[str]:
    command = ["tmux"]
    if TMUX_CONFIG.exists():
        command.extend(["-f", str(TMUX_CONFIG)])
    command.extend(args)
    return command


def session_exists() -> bool:
    return (
        subprocess.run(
            tmux_command("has-session", "-t", f"={SESSION}"),
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        ).returncode
        == 0
    )


def web_ready() -> bool:
    try:
        with urllib.request.urlopen(WEB_URL, timeout=2) as response:
            return response.status < 500
    except (OSError, urllib.error.URLError):
        return False


def sdk_major(version: str) -> int | None:
    try:
        return int(version.split(".", 1)[0])
    except (ValueError, IndexError):
        return None


def doctor(root: Path) -> list[str]:
    issues: list[str] = []
    if not (root / "src/eShop.AppHost/eShop.AppHost.csproj").exists():
        issues.append("run /demo-prep from the eShop repository")

    if shutil.which("tmux") is None:
        issues.append("tmux is not installed")

    if shutil.which("dotnet") is None:
        issues.append("the .NET 10 SDK is not installed")
    else:
        result = subprocess.run(
            ["dotnet", "--version"],
            text=True,
            capture_output=True,
        )
        version = result.stdout.strip()
        if result.returncode != 0 or sdk_major(version) != 10:
            issues.append(f".NET 10 SDK required; found {version or 'unknown'}")

    if shutil.which("docker") is None:
        issues.append("Docker is not installed")
    elif (
        subprocess.run(
            ["docker", "info"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        ).returncode
        != 0
    ):
        issues.append("Docker is installed but the daemon is not running")
    return issues


def log_path(root: Path) -> Path:
    return Path.home() / ".cursor" / "demo-logs" / f"{root.name}-apphost.log"


def print_urls(root: Path) -> None:
    print(f"storefront: {WEB_URL}")
    print(f"aspire dashboard: {DASHBOARD_URL}")
    print(f"log: {log_path(root)}")


def database_create_race(log: Path) -> bool:
    if not log.exists():
        return False
    text = log.read_text(errors="replace")
    return "42P04" in text and 'database "' in text and "already exists" in text


def apphost_exited(log: Path) -> bool:
    return log.exists() and "APPHOST_EXIT=" in log.read_text(errors="replace")


def launch_apphost(log: Path) -> None:
    command = (
        "set -o pipefail; "
        "ESHOP_USE_HTTP_ENDPOINTS=1 "
        "dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj "
        f"2>&1 | tee -a {shlex_quote(log)}; "
        f"printf 'APPHOST_EXIT=%s\\n' \"$?\" | tee -a {shlex_quote(log)}"
    )
    subprocess.run(
        tmux_command("send-keys", "-t", f"{SESSION}:0.0", command, "C-m"),
        check=True,
    )


def restart_apphost(log: Path) -> None:
    subprocess.run(
        tmux_command("send-keys", "-t", f"{SESSION}:0.0", "C-c"),
        check=True,
    )
    time.sleep(3)
    result = subprocess.run(
        ["docker", "ps", "-aq"],
        check=True,
        text=True,
        capture_output=True,
    )
    containers = result.stdout.split()
    if containers:
        subprocess.run(["docker", "rm", "-f", *containers], check=True)
    if log.exists():
        shutil.copyfile(log, log.with_suffix(".previous.log"))
    log.write_text("DEMO_PREP_RESTART\n")
    launch_apphost(log)


def start(root: Path, timeout: int) -> int:
    if web_ready():
        print("ESHOP DEMO READY (already running)")
        print_urls(root)
        return 0

    issues = doctor(root)
    if issues:
        print("DEMO PREP BLOCKED", file=sys.stderr)
        for issue in issues:
            print(f"- {issue}", file=sys.stderr)
        return 2

    log = log_path(root)
    log.parent.mkdir(parents=True, exist_ok=True)

    if not session_exists():
        log.write_text("")
        subprocess.run(
            tmux_command(
                "new-session",
                "-d",
                "-s",
                SESSION,
                "-c",
                str(root),
                "--",
                os.environ.get("SHELL", "bash"),
                "-l",
            ),
            check=True,
        )
        launch_apphost(log)
    else:
        restart_apphost(log)

    deadline = time.monotonic() + timeout
    recovered_database_race = False
    while time.monotonic() < deadline:
        if web_ready():
            print("ESHOP DEMO READY")
            print_urls(root)
            return 0
        if database_create_race(log) and not recovered_database_race:
            print("Retrying AppHost after an Aspire database-create race")
            restart_apphost(log)
            recovered_database_race = True
            continue
        if apphost_exited(log):
            print(f"DEMO PREP FAILED: AppHost exited; inspect {log}", file=sys.stderr)
            return 1
        if not session_exists():
            print(f"DEMO PREP FAILED: AppHost exited; inspect {log}", file=sys.stderr)
            return 1
        time.sleep(2)

    print(f"DEMO PREP TIMED OUT after {timeout}s; inspect {log}", file=sys.stderr)
    return 1


def shlex_quote(path: Path) -> str:
    import shlex

    return shlex.quote(str(path))


def status(root: Path) -> int:
    state = "ready" if web_ready() else "starting" if session_exists() else "stopped"
    print(f"status: {state}")
    print_urls(root)
    return 0 if state == "ready" else 1


def stop() -> int:
    if session_exists():
        subprocess.run(tmux_command("kill-session", "-t", f"={SESSION}"), check=True)
        print("ESHOP DEMO STOPPED")
    else:
        print("ESHOP DEMO ALREADY STOPPED")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=("doctor", "start", "status", "stop"))
    parser.add_argument("--timeout", type=int, default=300)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    root = repository_root()
    if args.action == "doctor":
        issues = doctor(root)
        if issues:
            for issue in issues:
                print(f"- {issue}")
            return 2
        print("DEMO PREP DOCTOR: OK")
        return 0
    if args.action == "start":
        return start(root, args.timeout)
    if args.action == "status":
        return status(root)
    return stop()


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        print(f"DEMO PREP FAILED: {error}", file=sys.stderr)
        raise SystemExit(1)
