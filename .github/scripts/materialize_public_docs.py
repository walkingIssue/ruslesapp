from __future__ import annotations

import argparse
import json
import re
from datetime import date
from pathlib import Path


def yaml_quote(value: str) -> str:
    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def coerce_slug(value: str, fallback: str) -> str:
    slug = value.strip().lower()
    slug = re.sub(r"[^a-z0-9]+", "-", slug)
    slug = re.sub(r"-{2,}", "-", slug).strip("-")
    return slug or fallback


def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def build_changelog_doc(result: dict) -> tuple[str, str] | None:
    changelog = result.get("changelog", {})
    if not changelog.get("should_create"):
        return None

    title = str(changelog.get("title") or "Replace with changelog title").strip()
    slug = coerce_slug(str(changelog.get("slug") or title), "generated-changelog-entry")
    summary = str(changelog.get("summary") or "Replace with a short summary.").strip()
    body = str(changelog.get("body_markdown") or "").strip()

    lines = [
        "---",
        f"title: {yaml_quote(title)}",
        f'date: "{date.today().isoformat()}"',
        f'slug: "{slug}"',
        f"summary: {yaml_quote(summary)}",
        "draft: true",
        "---",
        "",
    ]

    if body:
        lines.append(body)
        lines.append("")

    return slug, "\n".join(lines)


def build_devlog_doc(result: dict) -> tuple[str, str]:
    scaffold = result.get("devlog_scaffold", {})
    title = str(scaffold.get("suggested_title") or "Replace with devlog title").strip()
    slug = coerce_slug(title, "generated-devlog-entry")
    header_suggestions = [str(item).strip() for item in scaffold.get("header_suggestions", []) if str(item).strip()]
    reflection_prompts = [str(item).strip() for item in scaffold.get("reflection_prompts", []) if str(item).strip()]
    caution_notes = str(scaffold.get("caution_notes") or "").strip()

    lines = [
        "---",
        f"title: {yaml_quote(title)}",
        f'date: "{date.today().isoformat()}"',
        f'slug: "{slug}"',
        'summary: "Replace with a short summary."',
        "draft: true",
        "---",
        "",
        "> This draft was generated from docs-closeout. Rewrite it in your own voice before publishing.",
        "",
    ]

    if header_suggestions:
        for header in header_suggestions:
            lines.extend(
                [
                    f"## {header}",
                    "",
                    "Write this section in your own words.",
                    "",
                ]
            )
    else:
        lines.extend(
            [
                "## What changed",
                "",
                "Write this section in your own words.",
                "",
            ]
        )

    if reflection_prompts:
        lines.extend(["## Reflection prompts", ""])
        lines.extend([f"- {prompt}" for prompt in reflection_prompts])
        lines.append("")

    if caution_notes:
        lines.extend(["## Notes to review", "", caution_notes, ""])

    return slug, "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--artifact-dir", required=True)
    parser.add_argument("--repo-root", required=True)
    args = parser.parse_args()

    artifact_dir = Path(args.artifact_dir).resolve()
    repo_root = Path(args.repo_root).resolve()

    result_path = artifact_dir / "result.json"
    result = json.loads(result_path.read_text(encoding="utf-8"))

    changelog_doc = build_changelog_doc(result)
    if changelog_doc is not None:
        changelog_slug, changelog_text = changelog_doc
        write_text(repo_root / "docs" / "changelog" / f"{changelog_slug}.md", changelog_text)

    devlog_slug, devlog_text = build_devlog_doc(result)
    write_text(repo_root / "docs" / "devlog" / f"{devlog_slug}.md", devlog_text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
