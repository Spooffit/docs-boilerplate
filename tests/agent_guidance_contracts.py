#!/usr/bin/env python3
"""Check that generated agent guidance is usable without an installed CLI.

The generated repository contains documentation, not the generator binary.
These checks guard the user-facing instructions against contradictions and
unsourced business claims; they do not certify factual correctness.
"""
from __future__ import annotations

from pathlib import Path
import re
import sys

BARE_COMMAND = re.compile(
    r"(?<![\w./-])docs-boilerplate\s+(?:init|update|validate|recover|list|doctor)\b"
)
SOURCE_COMMAND = "dotnet run --project src/DocsBoilerplate.Cli -- "
GUIDANCE = [
    Path("AGENTS.md"),
    Path("docs/standards/documentation.md"),
]


def check(root: Path) -> list[str]:
    errors: list[str] = []
    targets = GUIDANCE + sorted((root / ".agents" / "skills").glob("*/SKILL.md"))
    for relative in targets:
        path = root / relative
        if not path.is_file():
            errors.append(f"{root}: missing agent guidance {relative}")
            continue
        content = path.read_text(encoding="utf-8")
        if BARE_COMMAND.search(content):
            errors.append(f"{root}: undocumented installed CLI invocation in {relative}")
        # Every CLI reference must use the source-checkout invocation above.
    agent = (root / "AGENTS.md").read_text(encoding="utf-8")
    for needle in (
        SOURCE_COMMAND + "recover --output",
        SOURCE_COMMAND + "validate --output",
        "status: active",
        "docs/catalog.md",
        "Not yet verified",
    ):
        if needle not in agent:
            errors.append(f"{root}: missing expected source/evidence guidance {needle!r}")
    if (root / "src").exists():
        errors.append(f"{root}: generator sources leaked into the generated repository")
    return errors


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: agent_guidance_contracts.py GENERATED_DIR [GENERATED_DIR ...]", file=sys.stderr)
        return 2
    errors: list[str] = []
    for arg in sys.argv[1:]:
        folder = Path(arg)
        if not folder.is_dir():
            errors.append(f"{arg}: generated repository does not exist")
            continue
        errors.extend(check(folder))
    for error in errors:
        print("ERROR:", error, file=sys.stderr)
    print(f"Agent guidance contracts: {len(sys.argv)-1} generated repositories; {len(errors)} findings.")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
