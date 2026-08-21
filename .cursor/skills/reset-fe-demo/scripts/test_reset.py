#!/usr/bin/env python3
from __future__ import annotations

import subprocess
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).with_name("reset.py")


def run(cwd: Path, *command: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        command,
        cwd=cwd,
        check=check,
        text=True,
        capture_output=True,
    )


class ResetFeDemoTest(unittest.TestCase):
    def test_replaces_only_clean_demo_worktree_and_preserves_branches(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            temp = Path(directory)
            remote = temp / "remote.git"
            seed = temp / "seed"
            clone = temp / "clone"
            target = temp / "demo"

            run(temp, "git", "init", "--bare", str(remote))
            run(temp, "git", "init", "-b", "main", str(seed))
            run(seed, "git", "config", "user.name", "Demo Test")
            run(seed, "git", "config", "user.email", "demo@example.com")
            (seed / "README.md").write_text("fresh\n")
            (seed / "Makefile").write_text("demo-reset:\n\t@true\n")
            run(seed, "git", "add", "README.md", "Makefile")
            run(seed, "git", "commit", "-m", "seed")
            run(seed, "git", "remote", "add", "origin", str(remote))
            run(seed, "git", "push", "-u", "origin", "main")
            run(remote, "git", "symbolic-ref", "HEAD", "refs/heads/main")
            run(temp, "git", "clone", str(remote), str(clone))

            first = run(
                clone,
                "python3",
                str(SCRIPT),
                "--target",
                str(target),
            )
            self.assertIn("FE DEMO READY", first.stdout)
            first_branch = run(target, "git", "branch", "--show-current").stdout.strip()
            self.assertEqual("", run(target, "git", "status", "--porcelain").stdout)
            upstream = run(
                target,
                "git",
                "rev-parse",
                "--abbrev-ref",
                "@{upstream}",
                check=False,
            )
            self.assertNotEqual(0, upstream.returncode)

            (target / "scratch.txt").write_text("do not discard\n")
            blocked = run(
                clone,
                "python3",
                str(SCRIPT),
                "--target",
                str(target),
                check=False,
            )
            self.assertEqual(1, blocked.returncode)
            self.assertIn("scratch.txt", blocked.stderr)
            self.assertTrue((target / "scratch.txt").exists())

            (target / "scratch.txt").unlink()
            second = run(
                clone,
                "python3",
                str(SCRIPT),
                "--target",
                str(target),
            )
            self.assertIn("FE DEMO READY", second.stdout)
            second_branch = run(target, "git", "branch", "--show-current").stdout.strip()
            self.assertNotEqual(first_branch, second_branch)
            branches = run(clone, "git", "branch", "--format=%(refname:short)").stdout
            self.assertIn(first_branch, branches)
            self.assertIn(second_branch, branches)


if __name__ == "__main__":
    unittest.main()
