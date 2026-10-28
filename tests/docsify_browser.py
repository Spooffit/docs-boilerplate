#!/usr/bin/env python3
"""Real Docsify rendering/routing checks against a newly generated repository.

Requires Playwright + Chromium in the test environment only.
No browser packages, test files or static build output enter the generated project.
"""
from __future__ import annotations

import functools
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import subprocess
import tempfile
from threading import Thread
from urllib.parse import urlsplit

from playwright.sync_api import sync_playwright


ROOT = Path(__file__).resolve().parents[1]
ARTIFACTS = ROOT / "browser-artifacts"


class Handler(SimpleHTTPRequestHandler):
    def log_message(self, format, *args):
        # Only failures need HTTP diagnostics; playwright records local responses.
        if len(args) > 1 and str(args[1]).startswith(("4", "5")):
            super().log_message(format, *args)


def assert_heading(page, title: str) -> None:
    # Docsify may render more than one heading with its navigation sidebar.
    page.locator("#main").get_by_role("heading", name=title, exact=True).first.wait_for(
        state="visible", timeout=25000
    )
    assert "Loading documentation..." not in page.locator("#main").inner_text()


def exercise(page, base_url: str, local_failures: list[str]) -> None:
    page.goto(base_url, wait_until="domcontentloaded")
    assert_heading(page, "BrowserDocs")
    assert "docs/index.md" in page.evaluate(
        "() => performance.getEntriesByType('resource').map(x => x.name).join('\\n')"
    ), "Docsify did not fetch docs/index.md"

    # This link must be resolved from the *Markdown page*, not from index.html.
    page.locator("#main").get_by_role("link", name="project overview").first.click()
    assert_heading(page, "Project overview")
    assert "project/overview" in page.url.lower(), f"Unexpected project URL: {page.url}"

    # Docsify hash paths must survive an actual refresh on an ordinary file server.
    project_url = page.url
    page.reload(wait_until="domcontentloaded")
    assert_heading(page, "Project overview")
    assert page.url == project_url, "Reload lost the deep link"

    # Direct navigation must render nested pages without a server-side SPA rewrite.
    page.goto(base_url + "#/templates/index", wait_until="domcontentloaded")
    assert_heading(page, "Choose a documentation template")
    page.locator("#main").get_by_role("link", name="Technical feature").first.click()
    assert_heading(page, "Technical feature specification template")
    assert "templates/technical-feature" in page.url.lower(), page.url

    # Test a deep link to a heading on a nested page, followed by refresh.
    anchor_url = base_url + "#/templates/index?id=keep-one-canonical-source"
    page.goto(anchor_url, wait_until="domcontentloaded")
    page.locator("#main").get_by_role(
        "heading", name="Keep one canonical source", exact=True
    ).wait_for(state="visible", timeout=25000)
    page.reload(wait_until="domcontentloaded")
    assert_heading(page, "Choose a documentation template")
    assert "id=keep-one-canonical-source" in page.url, page.url

    # Optional module is created by this test's selected generator configuration.
    page.goto(base_url + "#/architecture/api", wait_until="domcontentloaded")
    page.locator("#main h1").first.wait_for(state="visible", timeout=25000)
    assert "api" in page.locator("#main").inner_text().lower()

    assert not local_failures, "Missing/failed local Markdown requests:\\n" + "\\n".join(local_failures)


def main() -> int:
    ARTIFACTS.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="docsify-real-browser-") as td:
        output = Path(td) / "generated"
        command = [
            "dotnet", "run", "--configuration", "Release",
            "--project", str(ROOT / "src/DocsBoilerplate.Cli"),
            "--", "init",
            "--name", "BrowserDocs", "--description", "Browser navigation verification",
            "--output", str(output), "--profiles", "dotnet,blazor",
            "--modules", "api,testing", "--site", "docsify", "--non-interactive",
        ]
        subprocess.run(command, cwd=ROOT, check=True)
        subprocess.run(
            ["dotnet", "run", "--no-build", "--configuration", "Release",
             "--project", str(ROOT / "src/DocsBoilerplate.Cli"), "--",
             "validate", "--output", str(output)],
            cwd=ROOT, check=True,
        )

        manifest = json.loads((output / "docs.config.json").read_text(encoding="utf-8"))
        assert "index.html" in manifest["GeneratedFiles"]
        assert not any(path.endswith((".js", ".css")) for path in manifest["GeneratedFiles"])

        handler = functools.partial(Handler, directory=str(output))
        server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
        thread = Thread(target=server.serve_forever, daemon=True)
        thread.start()
        base_url = f"http://127.0.0.1:{server.server_port}/"
        failures: list[str] = []
        diagnostics: list[str] = []
        try:
            with sync_playwright() as playwright:
                browser = playwright.chromium.launch(headless=True)
                context = browser.new_context(viewport={"width": 1280, "height": 800})
                context.tracing.start(screenshots=True, snapshots=True, sources=True)
                page = context.new_page()
                page.on("pageerror", lambda error: diagnostics.append("JS: " + str(error)))
                page.on(
                    "requestfailed",
                    lambda request: diagnostics.append(
                        f"REQUEST: {request.url} {request.failure}"
                    ),
                )
                page.on(
                    "response",
                    lambda response: failures.append(
                        f"{response.status} {response.url}"
                    ) if urlsplit(response.url).hostname == "127.0.0.1"
                    and response.status >= 400 else None,
                )
                try:
                    exercise(page, base_url, failures)
                    assert not any("JS:" in item for item in diagnostics), (
                        "Uncaught browser JavaScript errors:\\n" + "\\n".join(diagnostics)
                    )
                    print("Docsify Chromium browser: homepage, navigation, refresh, nested routes, anchors and optional modules passed.")
                except Exception:
                    page.screenshot(path=str(ARTIFACTS / "docsify-failure.png"), full_page=True)
                    (ARTIFACTS / "browser-errors.txt").write_text(
                        "\\n".join(diagnostics + failures) + "\\n", encoding="utf-8"
                    )
                    raise
                finally:
                    context.tracing.stop(path=str(ARTIFACTS / "docsify-trace.zip"))
                    browser.close()
        finally:
            server.shutdown()
            thread.join(timeout=5)
            server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
