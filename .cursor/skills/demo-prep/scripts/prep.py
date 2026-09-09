#!/usr/bin/env python3
"""Start and inspect the eShop visual demo stack."""
from __future__ import annotations

import argparse
import json
import os
import shutil
import signal
import ssl
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
from pathlib import Path

SESSION = "eshop-fe-demo"
WEB_URL = "http://localhost:5045"
BLAZOR_SCRIPT_URL = f"{WEB_URL}/_framework/blazor.web.js"
DASHBOARD_URL = "http://localhost:18848"
TMUX_CONFIG = Path("/exec-daemon/tmux.portal.conf")
DCP_CONTAINER_LABEL = "com.microsoft.developer.usvc-dev.name"


def repository_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        check=True,
        text=True,
        capture_output=True,
    )
    return Path(result.stdout.strip()).resolve()


def tmux_available() -> bool:
    return shutil.which("tmux") is not None


def tmux_command(*args: str) -> list[str]:
    command = ["tmux"]
    if TMUX_CONFIG.exists():
        command.extend(["-f", str(TMUX_CONFIG)])
    command.extend(args)
    return command


def session_exists() -> bool:
    if not tmux_available():
        return False
    return (
        subprocess.run(
            tmux_command("has-session", "-t", f"={SESSION}"),
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        ).returncode
        == 0
    )


def pid_path(root: Path) -> Path:
    return log_path(root).with_suffix(".pid")


def tracked_pid(root: Path) -> int | None:
    path = pid_path(root)
    if not path.exists():
        return None
    try:
        pid = int(path.read_text().strip())
    except ValueError:
        return None
    try:
        os.kill(pid, 0)
    except OSError:
        return None
    return pid


def runner_active(root: Path) -> bool:
    return session_exists() or tracked_pid(root) is not None


def asset_served(url: str, timeout: float = 10.0) -> bool:
    try:
        with urllib.request.urlopen(url, timeout=timeout) as response:
            return response.status == 200
    except (OSError, urllib.error.URLError):
        return False


def web_ready(timeout: float = 45.0) -> bool:
    try:
        with urllib.request.urlopen(WEB_URL, timeout=timeout) as response:
            if response.status >= 500:
                return False
            body = response.read().decode("utf-8", errors="replace")
    except (OSError, urllib.error.URLError):
        return False
    # The home page stream-renders, so headers and the page shell arrive before the
    # catalog-api call resolves. Status alone stays 200 even when that call fails.
    if "catalog-items" not in body:
        return False
    # The streamed markup above only reaches the DOM once this script applies it. Static
    # web assets resolve through absolute content roots baked in at build time, so a
    # stale package cache 404s it and the browser sits on "Loading..." forever while
    # every server-side check here still passes.
    return asset_served(BLAZOR_SCRIPT_URL)


def sdk_major(version: str) -> int | None:
    try:
        return int(version.split(".", 1)[0])
    except (ValueError, IndexError):
        return None


def doctor(root: Path) -> list[str]:
    issues: list[str] = []
    if not (root / "src/eShop.AppHost/eShop.AppHost.csproj").exists():
        issues.append("run /demo-prep from the eShop repository")

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


def dcp_executables() -> list[dict]:
    """Per-resource state from Aspire's orchestrator.

    Service stdout/stderr goes to the dashboard and to DCP-owned files, never to the
    AppHost console, so a crashed service is invisible in the AppHost log.
    """
    configs = sorted(
        Path(tempfile.gettempdir()).glob("aspire-dcp*/kubeconfig"),
        key=lambda path: path.stat().st_mtime,
        reverse=True,
    )
    for config in configs:
        server = token = None
        for line in config.read_text(errors="replace").splitlines():
            line = line.strip()
            if line.startswith("server:"):
                server = line.split(":", 1)[1].strip()
            elif line.startswith("token:"):
                token = line.split(":", 1)[1].strip()
        if not server or not token:
            continue
        request = urllib.request.Request(
            f"{server}/apis/usvc-dev.developer.microsoft.com/v1/executables",
            headers={"Authorization": f"Bearer {token}"},
        )
        try:
            with urllib.request.urlopen(
                request, timeout=5, context=ssl._create_unverified_context()
            ) as response:
                return json.load(response).get("items", [])
        except (OSError, urllib.error.URLError, ValueError):
            continue
    return []


def crashed_services() -> list[tuple[str, int, str | None]]:
    crashed = []
    for item in dcp_executables():
        status = item.get("status", {})
        exit_code = status.get("exitCode")
        if status.get("state") == "Finished" and exit_code not in (None, 0):
            name = item.get("metadata", {}).get("name", "unknown")
            crashed.append((name, exit_code, status.get("stdErrFile")))
    return crashed


def database_create_race() -> bool:
    for _, _, stderr_file in crashed_services():
        if not stderr_file:
            continue
        path = Path(stderr_file)
        if path.exists() and "42P04" in path.read_text(errors="replace"):
            return True
    return False


def apphost_exited(log: Path) -> bool:
    return log.exists() and "APPHOST_EXIT=" in log.read_text(errors="replace")


def durable_nuget_packages() -> Path | None:
    """A replacement for an ambient package cache that will not outlive the build.

    The build records absolute package paths as static web asset content roots, so a
    cache under the system temp dir (an agent sandbox hands out a fresh one per
    session) leaves the storefront serving 404s for `_framework/blazor.web.js` once
    that directory is reclaimed. Only override a temp cache; a caller who points at
    their own durable one keeps it and avoids a needless re-restore.
    """
    ambient = os.environ.get("NUGET_PACKAGES")
    if not ambient:
        return None
    if Path(tempfile.gettempdir()).resolve() not in Path(ambient).resolve().parents:
        return None
    return Path.home() / ".nuget" / "packages"


def apphost_shell_command(log: Path) -> str:
    packages = durable_nuget_packages()
    environment = "ESHOP_USE_HTTP_ENDPOINTS=1 "
    if packages is not None:
        environment += f"NUGET_PACKAGES={shlex_quote(packages)} "
    return (
        "set -o pipefail; "
        f"{environment}"
        "dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj "
        f"2>&1 | tee -a {shlex_quote(log)}; "
        f"printf 'APPHOST_EXIT=%s\\n' \"$?\" | tee -a {shlex_quote(log)}"
    )


def launch_apphost(root: Path, log: Path) -> None:
    command = apphost_shell_command(log)
    if tmux_available():
        if not session_exists():
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
        subprocess.run(
            tmux_command("send-keys", "-t", f"{SESSION}:0.0", command, "C-m"),
            check=True,
        )
        return

    process = subprocess.Popen(
        ["bash", "-lc", command],
        cwd=root,
        stdin=subprocess.DEVNULL,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        start_new_session=True,
    )
    pid_path(root).write_text(f"{process.pid}\n")


def stop_apphost(root: Path) -> None:
    if session_exists():
        subprocess.run(
            tmux_command("send-keys", "-t", f"{SESSION}:0.0", "C-c"),
            check=True,
        )
    pid = tracked_pid(root)
    if pid is not None:
        try:
            os.killpg(os.getpgid(pid), signal.SIGTERM)
        except OSError:
            pass
    pid_path(root).unlink(missing_ok=True)


def restart_apphost(root: Path, log: Path) -> None:
    stop_apphost(root)
    time.sleep(3)
    # Scope the teardown to DCP-managed containers; a bare `docker ps -aq` would remove
    # every container on the machine, including ones unrelated to this demo.
    result = subprocess.run(
        ["docker", "ps", "-aq", "--filter", f"label={DCP_CONTAINER_LABEL}"],
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
    launch_apphost(root, log)


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

    if runner_active(root):
        restart_apphost(root, log)
    else:
        log.write_text("")
        launch_apphost(root, log)

    deadline = time.monotonic() + timeout
    recovered_database_race = False
    while time.monotonic() < deadline:
        if web_ready():
            print("ESHOP DEMO READY")
            print_urls(root)
            return 0
        if database_create_race() and not recovered_database_race:
            print("Retrying AppHost after an Aspire database-create race")
            restart_apphost(root, log)
            recovered_database_race = True
            continue
        if apphost_exited(log) or not runner_active(root):
            print(f"DEMO PREP FAILED: AppHost exited; inspect {log}", file=sys.stderr)
            return 1
        time.sleep(2)

    print(f"DEMO PREP TIMED OUT after {timeout}s; inspect {log}", file=sys.stderr)
    report_crashed_services()
    return 1


def report_crashed_services() -> None:
    for name, exit_code, stderr_file in crashed_services():
        print(f"- {name} exited with code {exit_code}", file=sys.stderr)
        if stderr_file:
            print(f"  stderr: {stderr_file}", file=sys.stderr)


def shlex_quote(path: Path) -> str:
    import shlex

    return shlex.quote(str(path))


def status(root: Path) -> int:
    state = "ready" if web_ready() else "starting" if runner_active(root) else "stopped"
    print(f"status: {state}")
    print_urls(root)
    if state != "ready":
        report_crashed_services()
    return 0 if state == "ready" else 1


def stop(root: Path) -> int:
    if not runner_active(root):
        print("ESHOP DEMO ALREADY STOPPED")
        return 0
    stop_apphost(root)
    if session_exists():
        subprocess.run(tmux_command("kill-session", "-t", f"={SESSION}"), check=True)
    print("ESHOP DEMO STOPPED")
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
    return stop(root)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        print(f"DEMO PREP FAILED: {error}", file=sys.stderr)
        raise SystemExit(1)
