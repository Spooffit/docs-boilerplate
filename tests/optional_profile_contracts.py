#!/usr/bin/env python3
"""Validate optional HTTP ETag and .NET naming examples without product fixtures."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HTTP_DOC = ROOT / "catalog/docs/standards/http/etag-concurrency.md"
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



def main() -> int:
    verify_http()
    verify_naming()
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL optional profile contract: {exc}", file=sys.stderr)
        raise SystemExit(1)
