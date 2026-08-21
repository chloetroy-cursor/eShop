#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import io
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest.mock import patch


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

    def test_doctor_reports_missing_runtime(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "src/eShop.AppHost/eShop.AppHost.csproj"
            project.parent.mkdir(parents=True)
            project.write_text("")
            with patch.object(prep.shutil, "which", return_value=None):
                issues = prep.doctor(root)
            self.assertEqual(
                [
                    "tmux is not installed",
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


if __name__ == "__main__":
    unittest.main()
