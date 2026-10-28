#!/usr/bin/env python3
"""Offline JSON Schema contracts for catalog inputs and generated project manifests."""
from __future__ import annotations

from copy import deepcopy
from pathlib import Path
from tempfile import TemporaryDirectory
import json
import subprocess

from jsonschema import Draft202012Validator

ROOT = Path(__file__).resolve().parents[1]
SCHEMAS = {
    "profiles": ("catalog/profiles.json", "schemas/profiles.schema.json"),
    "modules": ("catalog/modules.json", "schemas/modules.schema.json"),
    "skills": ("catalog/skills.json", "schemas/skills.schema.json"),
}


def load(path: str):
    return json.loads((ROOT / path).read_text(encoding="utf-8"))


def assert_invalid(validator, value, description):
    if validator.is_valid(value):
        raise AssertionError(f"Schema allowed invalid {description}")


def run():
    for kind, (sample, schema_file) in SCHEMAS.items():
        schema = load(schema_file)
        Draft202012Validator.check_schema(schema)
        validator = Draft202012Validator(schema)
        data = load(sample)
        validator.validate(data)
        unexpected = deepcopy(data)
        unexpected["unspecified"] = "ignore-this"
        assert_invalid(validator, unexpected, f"{kind}: unknown fields")
        missing = deepcopy(data)
        missing.pop("schemaVersion")
        assert_invalid(validator, missing, f"{kind}: missing version")
        print(f"{kind}: valid and negative schema scenarios passed")

    with TemporaryDirectory(prefix="docs-boilerplate-schema-") as temporary:
        output = Path(temporary) / "project"
        subprocess.run([
            "dotnet", "run", "--no-build", "--configuration", "Release",
            "--project", "src/DocsBoilerplate.Cli", "--", "init",
            "--name", "SchemaTest", "--description", "Generated config validation",
            "--output", str(output), "--profiles", "dotnet,blazor",
            "--skills", "docs-review", "--modules", "testing", "--site", "docsify",
            "--non-interactive"
        ], cwd=ROOT, check=True)
        config_schema = load("schemas/docs-config.schema.json")
        Draft202012Validator.check_schema(config_schema)
        validator = Draft202012Validator(config_schema)
        data = json.loads((output / "docs.config.json").read_text(encoding="utf-8"))
        validator.validate(data)

        retired = deepcopy(data)
        retired["RetiredFiles"] = ["docs/standards/dotnet/retired.md"]
        validator.validate(retired)
        duplicate_retired = deepcopy(retired)
        duplicate_retired["RetiredFiles"].append("docs/standards/dotnet/retired.md")
        assert_invalid(validator, duplicate_retired, "duplicate retired paths")
        bad_retired = deepcopy(retired)
        bad_retired["RetiredFiles"] = ["../outside.md"]
        assert_invalid(validator, bad_retired, "unsafe retired path")

        wrong_site = deepcopy(data)
        wrong_site["Site"] = "unsupported-site"
        assert_invalid(validator, wrong_site, "unrecognized site")
        wrong_name = deepcopy(data)
        wrong_name["Name"] = ""
        assert_invalid(validator, wrong_name, "empty project name")
        wrong_path = deepcopy(data)
        wrong_path["GeneratedFiles"].append("../escape.md")
        assert_invalid(validator, wrong_path, "unsafe path")
        wrong_hash = deepcopy(data)
        first = next(iter(wrong_hash["GeneratedHashes"]))
        wrong_hash["GeneratedHashes"][first] = "invalid-hash"
        assert_invalid(validator, wrong_hash, "invalid hash")
        unknown = deepcopy(data)
        unknown["UndocumentedField"] = 123
        assert_invalid(validator, unknown, "unknown config property")
        print("docs-config: real generated manifest and negative scenarios passed")


if __name__ == "__main__":
    run()
