from __future__ import annotations

import hashlib
import importlib.util
import json
import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEMO = ROOT / "demo/incident"


def module(name: str):
    spec = importlib.util.spec_from_file_location(name, DEMO / f"{name}.py")
    loaded = importlib.util.module_from_spec(spec)
    assert spec.loader
    spec.loader.exec_module(loaded)
    return loaded


class DemoHarnessTests(unittest.TestCase):
    def test_generator_is_deterministic(self):
        subprocess.run([sys.executable, str(DEMO / "generate_telemetry.py")], cwd=ROOT, check=True)
        before = hashlib.sha256((DEMO / "generated/traces.json").read_bytes()).digest()
        subprocess.run([sys.executable, str(DEMO / "generate_telemetry.py")], cwd=ROOT, check=True)
        self.assertEqual(before, hashlib.sha256((DEMO / "generated/traces.json").read_bytes()).digest())

    def test_health_and_request_queries(self):
        query = module("query_telemetry")
        self.assertGreater(query.health()["after_release"]["error_rate"], 0.15)
        result = query.request("req-order-7f3a0000")
        self.assertEqual(9, result["attempts"])
        self.assertFalse(result["backoff_present"])

    def test_seeded_policy_fails(self):
        result = subprocess.run([sys.executable, str(DEMO / "check_retry_policy.py")], cwd=ROOT)
        self.assertEqual(1, result.returncode)

    def test_bounded_exponential_policy_passes(self):
        check = module("check_retry_policy")
        policy = check.inspect_policy(
            "public int RetryCount { get; set; } = 3;",
            """
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential
            """,
        )
        self.assertTrue(check.is_safe(*policy))

    def test_metadata_contract(self):
        metadata = json.loads((DEMO / "generated/metadata.json").read_text())
        self.assertEqual("mon-ordering-publish-errors", metadata["monitor"]["id"])
        self.assertEqual("2026-08-18T20:04:00Z", metadata["deploy"]["started_at"])


if __name__ == "__main__":
    unittest.main()
