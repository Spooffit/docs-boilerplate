#!/usr/bin/env python3
"""Check opt-in technical examples, then optionally build and broker-test EventBus.

The EventBus example is extracted directly from its Markdown standard into a
temporary .NET solution. It is neither a generated product nor a maintained
demonstration application.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
HTTP_DOC = ROOT / "catalog/docs/standards/http/etag-concurrency.md"
BUS_DOC = ROOT / "catalog/docs/standards/dotnet/eventbus-composition.md"
NAMING_DOC = ROOT / "catalog/docs/standards/dotnet/test-naming-convention.md"


def verify_http():
    text = HTTP_DOC.read_text(encoding="utf-8")
    cases = dict(re.findall(r"^```http scenario=([a-z]+)\n(.*?)^```", text, re.M | re.S))
    assert set(cases) == {"read", "success", "stale", "weak", "missing", "conflict"}, cases.keys()
    current_etag = '"rev-7"'
    current_value = "before"
    for scenario in ("read", "success", "stale", "weak", "missing", "conflict"):
        lines = cases[scenario].splitlines()
        request = lines[0]
        response_index = next(i for i, line in enumerate(lines) if line.startswith("HTTP/1.1 "))
        response = lines[response_index]
        headers = dict(line.split(": ", 1) for line in lines[1:response_index] if ": " in line)
        response_headers = dict(line.split(": ", 1) for line in lines[response_index + 1:] if ": " in line)
        assert request.endswith(" /resources/42 HTTP/1.1"), (scenario, request)
        assert headers.get("Host") == "api.example.test", scenario
        assert response.startswith("HTTP/1.1 "), scenario
        status = int(response.split()[1])
        if scenario == "read":
            assert request.startswith("GET ") and status == 200
            assert response_headers["ETag"] == current_etag
            continue
        assert request.startswith("PUT "), scenario
        tag = headers.get("If-Match")
        if tag is None:
            expected = 428  # The sample explicitly requires preconditions.
        elif tag.startswith("W/") or tag != current_etag:
            expected = 412  # If-Match uses strong comparison, including weak-tag rejection.
        elif scenario == "conflict":
            expected = 409  # A separate application-level conflict despite matching tag.
        else:
            expected = 204
            current_etag = '"rev-8"'
            current_value = "after"
            assert response_headers["ETag"] == current_etag
        assert status == expected, (scenario, status, expected)
        if scenario != "success":
            assert current_etag == '"rev-8"' and current_value == "after", scenario
    print("PASS: six HTTP If-Match scenarios and state transitions")


def verify_naming():
    text = NAMING_DOC.read_text(encoding="utf-8")
    assert "MethodUnderTest_Scenario_ExpectedBehavior" in text
    names = re.findall(r"public (?:async )?(?:void|Task) ([A-Za-z][A-Za-z0-9_]*)\(", text)
    assert len(names) >= 3, names
    assert all(re.fullmatch(r"[A-Za-z][A-Za-z0-9]*_[A-Za-z][A-Za-z0-9]*_[A-Za-z][A-Za-z0-9]*", name) for name in names), names
    print("PASS: opt-in test naming examples")


def extract_bus_files(target_root: Path):
    text = BUS_DOC.read_text(encoding="utf-8")
    snippets = re.findall(r"^```(xml|csharp) file=([^\s]+)\n(.*?)^```", text, re.M | re.S)
    expected = {
        "Integration/EventBus/Contracts/Contracts.csproj",
        "Integration/EventBus/Contracts/ExampleEvent.cs",
        "Integration/EventBus/RabbitMq/RabbitMq.csproj",
        "Integration/EventBus/RabbitMq/EventBusTopology.cs",
        "Integration/EventBus/Default/Default.csproj",
        "Integration/EventBus/Default/EventBusRegistration.cs",
        "App/App.csproj",
        "App/Program.cs",
    }
    assert len(snippets) == len(expected), snippets
    assert {path for _, path, _ in snippets} == expected
    for language, path, body in snippets:
        destination = (target_root / path).resolve()
        assert destination.is_relative_to(target_root.resolve()), path
        assert path.endswith(".csproj") if language == "xml" else path.endswith(".cs"), path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(body.rstrip() + "\n", encoding="utf-8")
    contracts = ElementTree.parse(target_root / "Integration/EventBus/Contracts/Contracts.csproj")
    rabbit = ElementTree.parse(target_root / "Integration/EventBus/RabbitMq/RabbitMq.csproj")
    default = ElementTree.parse(target_root / "Integration/EventBus/Default/Default.csproj")
    assert not contracts.findall(".//ProjectReference") and not contracts.findall(".//PackageReference")
    rabbit_refs = {x.get("Include") for x in rabbit.findall(".//ProjectReference")}
    default_refs = {x.get("Include") for x in default.findall(".//ProjectReference")}
    assert rabbit_refs == {"../Contracts/Contracts.csproj"}, rabbit_refs
    assert default_refs == {"../Contracts/Contracts.csproj", "../RabbitMq/RabbitMq.csproj"}, default_refs
    packages = [(x.get("Include"), x.get("Version")) for x in rabbit.findall(".//PackageReference")]
    assert packages == [("MassTransit.RabbitMQ", "8.5.12")], packages
    topology = (target_root / "Integration/EventBus/RabbitMq/EventBusTopology.cs").read_text()
    app = (target_root / "App/Program.cs").read_text()
    assert "ExchangeType = \"topic\"" in topology
    assert "UseRoutingKeyFormatter" in topology
    assert "ConfigureConsumeTopology = false" in app
    assert "RoutingKey = \"examples.*\"" in app
    print("PASS: complete EventBus snippets, project references and explicit topic binding")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--run-eventbus", action="store_true", help="Build and run the extracted example against disposable RabbitMQ on localhost:5673")
    args = parser.parse_args()
    verify_http()
    verify_naming()
    with tempfile.TemporaryDirectory(prefix="docs-eventbus-") as temporary:
        root = Path(temporary)
        extract_bus_files(root)
        if args.run_eventbus:
            subprocess.run(["dotnet", "run", "--project", str(root / "App/App.csproj"), "--configuration", "Release"], check=True, timeout=180)
            print("PASS: EventBus compiled and routed a real message using RabbitMQ")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, subprocess.CalledProcessError, subprocess.TimeoutExpired) as exc:
        print(f"FAIL optional profile contract: {exc}", file=sys.stderr)
        raise SystemExit(1)
