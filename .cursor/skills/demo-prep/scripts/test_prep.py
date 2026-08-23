#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import io
import tempfile
import unittest
import warnings
from contextlib import redirect_stdout
from pathlib import Path
from unittest.mock import MagicMock, patch


SCRIPT = Path(__file__).with_name("prep.py")
SPEC = importlib.util.spec_from_file_location("demo_prep", SCRIPT)
assert SPEC and SPEC.loader
prep = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(prep)


class DemoPrepTest(unittest.TestCase):
    def test_sdk_major(self) -> None:
        self.assertEqual(9, prep.sdk_major("9.0.100"))
        self.assertEqual(10, prep.sdk_major("10.0.0-preview"))
        self.assertIsNone(prep.sdk_major("unknown"))

    def test_doctor_accepts_dotnet_10_and_rejects_dotnet_9(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "src/eShop.AppHost/eShop.AppHost.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("")

            def which_side_effect(command: str) -> str | None:
                if command in {"dotnet", "docker"}:
                    return f"/usr/bin/{command}"
                return None

            dotnet_version = {"value": "10.0.100"}

            def run_side_effect(command: list[str], **kwargs: object) -> MagicMock:
                result = MagicMock()
                if command[:2] == ["dotnet", "--version"]:
                    result.returncode = 0
                    result.stdout = f"{dotnet_version['value']}\n"
                elif command[:2] == ["docker", "info"]:
                    result.returncode = 0
                return result

            with patch.object(prep.shutil, "which", side_effect=which_side_effect):
                with patch.object(prep.subprocess, "run", side_effect=run_side_effect):
                    self.assertEqual([], prep.doctor(root))
                    dotnet_version["value"] = "9.0.100"
                    issues = prep.doctor(root)
            self.assertEqual(
                [".NET 10 SDK required; found 9.0.100"],
                issues,
            )

    def test_doctor_reports_missing_runtime_without_requiring_tmux(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "src/eShop.AppHost/eShop.AppHost.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("")
            with patch.object(prep.shutil, "which", return_value=None):
                issues = prep.doctor(root)
            self.assertEqual(
                [
                    "the .NET 10 SDK is not installed",
                    "Docker is not installed",
                ],
                issues,
            )

    def test_start_reuses_ready_storefront(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = io.StringIO()
            with patch.object(prep, "web_ready", return_value=True):
                with patch.object(prep, "doctor") as doctor:
                    with redirect_stdout(output):
                        result = prep.start(Path(directory), timeout=1)
            self.assertEqual(0, result)
            self.assertIn("already running", output.getvalue())
            doctor.assert_not_called()

    def test_detects_database_create_race_and_apphost_exit(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / "apphost.log"
            log.write_text(
                'PostgresException: 42P04: database "identitydb" already exists\n'
            )
            self.assertTrue(prep.database_create_race(log))
            self.assertFalse(prep.apphost_exited(log))
            log.write_text("APPHOST_EXIT=1\n")
            self.assertFalse(prep.database_create_race(log))
            self.assertTrue(prep.apphost_exited(log))

    def test_start_fails_fast_when_apphost_exits_with_live_tmux_session(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            log = Path(directory) / "apphost.log"
            runner_checks: list[bool] = []
            apphost_checks = 0

            def runner_active_side_effect(_root: Path) -> bool:
                runner_checks.append(True)
                return len(runner_checks) > 1

            def launch_side_effect(_root: Path, log_file: Path) -> None:
                log_file.write_text("starting AppHost\n")

            def apphost_exited_side_effect(log_file: Path) -> bool:
                nonlocal apphost_checks
                apphost_checks += 1
                if apphost_checks >= 2:
                    log_file.write_text("starting AppHost\nAPPHOST_EXIT=1\n")
                    return True
                return False

            err = io.StringIO()
            with patch.object(prep, "web_ready", return_value=False):
                with patch.object(prep, "doctor", return_value=[]):
                    with patch.object(prep, "log_path", return_value=log):
                        with patch.object(
                            prep, "runner_active", side_effect=runner_active_side_effect
                        ):
                            with patch.object(
                                prep, "launch_apphost", side_effect=launch_side_effect
                            ):
                                with patch.object(
                                    prep, "apphost_exited", side_effect=apphost_exited_side_effect
                                ):
                                    with patch.object(prep.time, "sleep"):
                                        with patch.object(
                                            prep.time,
                                            "monotonic",
                                            side_effect=[0, 0.1, 0.2, 1000],
                                        ):
                                            with patch("sys.stderr", err):
                                                result = prep.start(root, timeout=300)

            self.assertEqual(1, result)
            self.assertIn("AppHost exited", err.getvalue())
            self.assertGreaterEqual(apphost_checks, 2)
            self.assertTrue(runner_checks[-1])

    def test_start_restarts_stale_runner_instead_of_skipping_launch(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            log = Path(directory) / "apphost.log"
            log.write_text("stale output\n")

            with patch.object(prep, "web_ready", side_effect=[False, True]):
                with patch.object(prep, "doctor", return_value=[]):
                    with patch.object(prep, "log_path", return_value=log):
                        with patch.object(prep, "runner_active", return_value=True):
                            with patch.object(prep, "launch_apphost") as launch:
                                with patch.object(prep, "restart_apphost") as restart:
                                    result = prep.start(root, timeout=300)

            restart.assert_called_once_with(root, log)
            launch.assert_not_called()
            self.assertEqual(0, result)

    def test_starts_without_tmux_and_tracks_the_process(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            home = root / "home"
            home.mkdir()
            log = home / "apphost.log"
            # The runner is detached on purpose, so nothing reaps the child here.
            warnings.simplefilter("ignore", ResourceWarning)
            with patch.object(prep, "tmux_available", return_value=False):
                with patch.object(prep, "log_path", return_value=log):
                    with patch.object(prep, "apphost_shell_command", return_value="sleep 30"):
                        prep.launch_apphost(root, log)
                        self.assertIsNotNone(prep.tracked_pid(root))
                        self.assertTrue(prep.runner_active(root))
                        prep.stop_apphost(root)
                        self.assertFalse(prep.runner_active(root))


if __name__ == "__main__":
    unittest.main()
