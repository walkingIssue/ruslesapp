from __future__ import annotations

import argparse
import sys
import subprocess
import os
import re
import textwrap
from datetime import date
from pathlib import Path
from typing import Literal

from openai import OpenAI
from pydantic import BaseModel, Field


MAX_DIFF_CHARS = int(os.environ.get("DOCS_CLOSEOUT_MAX_DIFF_CHARS", "120000"))


class DocUpdate(BaseModel):
    target_file: str
    action: Literal["none", "update", "create"]
    reason: str
    notes: str


class InventoryChange(BaseModel):
    data_area: str
    change: str
    risk: Literal["low", "medium", "high"]
    policy_note: str


class ChangelogDraft(BaseModel):
    should_create: bool
    title: str
    slug: str
    summary: str
    tags: list[str] = Field(default_factory=list)
    version: str | None = None
    body_markdown: str


class DevlogScaffold(BaseModel):
    suggested_title: str
    header_suggestions: list[str] = Field(default_factory=list)
    reflection_prompts: list[str] = Field(default_factory=list)
    caution_notes: str


class DocsCloseoutResult(BaseModel):
    session_summary: str
    human_actions: list[str] = Field(default_factory=list)
    doc_updates: list[DocUpdate] = Field(default_factory=list)
    changelog: ChangelogDraft
    privacy_needs_review: bool
    privacy_summary: str
    privacy_proposed_log_entry: str
    privacy_inventory_changes: list[InventoryChange] = Field(default_factory=list)
    devlog_scaffold: DevlogScaffold


def run_git(repo_root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def read_text_if_exists(path: Path) -> str:
    if not path.exists():
        return f"[missing] {path.name}"
    return path.read_text(encoding="utf-8")


def truncate(text: str, limit: int) -> str:
    if len(text) <= limit:
        return text
    return f"{text[:limit]}\n\n[TRUNCATED AFTER {limit} CHARACTERS]"


def yaml_quote(value: str) -> str:
    escaped = value.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escaped}"'


def coerce_slug(value: str, fallback: str = "replace-with-slug") -> str:
    slug = value.strip().lower()
    slug = re.sub(r"[^a-z0-9]+", "-", slug)
    slug = re.sub(r"-{2,}", "-", slug).strip("-")
    return slug or fallback


def render_changelog(changelog: ChangelogDraft) -> str:
    if not changelog.should_create:
        return "# Changelog Draft\n\nNo changelog entry was recommended for this diff.\n"

    frontmatter = [
        "---",
        f"title: {yaml_quote(changelog.title.strip() or 'Replace with changelog title')}",
        f'date: "{date.today().isoformat()}"',
        f'slug: "{coerce_slug(changelog.slug, "replace-with-changelog-slug")}"',
        f"summary: {yaml_quote(changelog.summary.strip() or 'Replace with a short summary.')}",
        "draft: true",
        "---",
        "",
    ]

    return "# Changelog Draft\n\n" + "\n".join(frontmatter) + changelog.body_markdown.strip() + "\n"


def render_docs_review(result: DocsCloseoutResult) -> str:
    lines = ["# Internal Docs Review", ""]
    if not result.doc_updates:
        lines.extend(["No internal documentation updates were recommended.", ""])
        return "\n".join(lines)

    for update in result.doc_updates:
        lines.extend(
            [
                f"## {update.target_file}",
                f"- Action: `{update.action}`",
                f"- Reason: {update.reason}",
                f"- Notes: {update.notes}",
                "",
            ]
        )

    return "\n".join(lines)


def render_privacy_review(result: DocsCloseoutResult) -> str:
    lines = [
        "# Privacy And Data Inventory Review",
        "",
        f"Needs review: `{str(result.privacy_needs_review).lower()}`",
        "",
        result.privacy_summary.strip(),
        "",
        "## Proposed Session Log Entry",
        "",
        result.privacy_proposed_log_entry.strip() or "No data-inventory change proposed.",
        "",
    ]

    if result.privacy_inventory_changes:
        lines.extend(["## Suggested Inventory Changes", ""])
        for item in result.privacy_inventory_changes:
            lines.extend(
                [
                    f"### {item.data_area}",
                    f"- Change: {item.change}",
                    f"- Risk: `{item.risk}`",
                    f"- Policy note: {item.policy_note}",
                    "",
                ]
            )

    return "\n".join(lines)


def render_devlog_scaffold(result: DocsCloseoutResult) -> str:
    scaffold = result.devlog_scaffold
    suggested_title = scaffold.suggested_title.strip() or "Replace with devlog title"
    suggested_slug = coerce_slug(suggested_title, "replace-with-devlog-slug")
    lines = [
        "# Devlog Scaffold",
        "",
        "Final devlog prose should be written by a human.",
        "",
        "## Required Frontmatter Starter",
        "",
        "```yaml",
        "---",
        f"title: {yaml_quote(suggested_title)}",
        f'date: "{date.today().isoformat()}"',
        f'slug: "{suggested_slug}"',
        'summary: "Replace with a short summary."',
        "draft: true",
        "---",
        "```",
        "",
        "Optional fields are intentionally omitted so a human can add them only if needed.",
        "",
        f"Suggested title: {suggested_title}",
        "",
        "## Header Suggestions",
        "",
    ]

    lines.extend([f"- {item}" for item in scaffold.header_suggestions] or ["- No specific header suggestions"])
    lines.extend(["", "## Reflection Prompts", ""])
    lines.extend([f"- {item}" for item in scaffold.reflection_prompts] or ["- No reflection prompts suggested"])
    lines.extend(["", "## Caution Notes", "", scaffold.caution_notes.strip(), ""])
    return "\n".join(lines)


def render_session_summary(result: DocsCloseoutResult) -> str:
    lines = [
        "# Docs Closeout Summary",
        "",
        result.session_summary.strip(),
        "",
        "## Human Actions",
        "",
    ]

    lines.extend([f"- {item}" for item in result.human_actions] or ["- No manual follow-up was suggested"])
    lines.extend(
        [
            "",
            "## Generated Files",
            "",
            "- `documentation-review.md`",
            "- `changelog-draft.md`",
            "- `privacy-review.md`",
            "- `devlog-scaffold.md`",
            "- `result.json`",
            "",
        ]
    )
    return "\n".join(lines)


def build_prompt(repo_root: Path, base_ref: str, head_ref: str, pr_title: str, pr_body: str) -> str:
    changed_files = run_git(repo_root, "diff", "--name-only", base_ref, head_ref)
    diff_stat = run_git(repo_root, "diff", "--stat", base_ref, head_ref)
    diff_patch = run_git(repo_root, "diff", "--unified=2", base_ref, head_ref)

    docs_index = read_text_if_exists(repo_root / "docs" / "README.md")
    data_inventory = read_text_if_exists(repo_root / "docs" / "data-inventory-and-privacy-impact.md")
    architecture_overview = read_text_if_exists(repo_root / "docs" / "architecture-overview.md")

    return textwrap.dedent(
        f"""
        Review the repository diff and produce docs-closeout material.

        Repository root: {repo_root}
        Base ref: {base_ref}
        Head ref: {head_ref}

        PR title:
        {pr_title or "[none]"}

        PR body:
        {pr_body or "[none]"}

        Changed files:
        {changed_files or "[none]"}

        Diff stat:
        {diff_stat or "[none]"}

        Existing docs index:
        {docs_index}

        Existing data inventory:
        {data_inventory}

        Existing architecture overview:
        {architecture_overview}

        Diff excerpt:
        {truncate(diff_patch, MAX_DIFF_CHARS)}
        """
    ).strip()


def write_no_diff(out_dir: Path) -> None:
    (out_dir / "session-summary.md").write_text("# Docs Closeout Summary\n\nNo diff was found.\n", encoding="utf-8")
    (out_dir / "documentation-review.md").write_text("# Internal Docs Review\n\nNo diff was found.\n", encoding="utf-8")
    (out_dir / "privacy-review.md").write_text("# Privacy And Data Inventory Review\n\nNo diff was found.\n", encoding="utf-8")
    (out_dir / "devlog-scaffold.md").write_text("# Devlog Scaffold\n\nNo diff was found.\n", encoding="utf-8")
    (out_dir / "changelog-draft.md").write_text("# Changelog Draft\n\nNo diff was found.\n", encoding="utf-8")
    (out_dir / "result.json").write_text("{}", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--base-ref", required=True)
    parser.add_argument("--head-ref", required=True)
    parser.add_argument("--out-dir", required=True)
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    out_dir = Path(args.out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    changed_files = run_git(repo_root, "diff", "--name-only", args.base_ref, args.head_ref)
    if not changed_files.strip():
        write_no_diff(out_dir)
        return 0

    api_key = os.environ.get("OPENAI_API_KEY")
    if not api_key:
        raise RuntimeError("OPENAI_API_KEY is required")

    model = os.environ.get("OPENAI_MODEL", "gpt-5-mini")
    reasoning_effort = os.environ.get("OPENAI_REASONING_EFFORT", "low")
    pr_title = os.environ.get("PR_TITLE", "")
    pr_body = os.environ.get("PR_BODY", "")

    instructions_path = repo_root / ".github" / "prompts" / "docs-closeout-system.md"
    instructions = read_text_if_exists(instructions_path)
    user_prompt = build_prompt(repo_root, args.base_ref, args.head_ref, pr_title, pr_body)

    client = OpenAI(api_key=api_key)
    request_kwargs: dict[str, object] = {
        "model": model,
        "instructions": instructions,
        "input": [{"role": "user", "content": user_prompt}],
        "text_format": DocsCloseoutResult,
    }

    if reasoning_effort:
        request_kwargs["reasoning"] = {"effort": reasoning_effort}

    response = client.responses.parse(**request_kwargs)
    result = response.output_parsed
    if result is None:
        raise RuntimeError("Model did not return a structured result")

    (out_dir / "result.json").write_text(result.model_dump_json(indent=2), encoding="utf-8")
    (out_dir / "documentation-review.md").write_text(render_docs_review(result), encoding="utf-8")
    (out_dir / "privacy-review.md").write_text(render_privacy_review(result), encoding="utf-8")
    (out_dir / "devlog-scaffold.md").write_text(render_devlog_scaffold(result), encoding="utf-8")
    (out_dir / "changelog-draft.md").write_text(render_changelog(result.changelog), encoding="utf-8")
    (out_dir / "session-summary.md").write_text(render_session_summary(result), encoding="utf-8")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"docs_closeout.py failed: {exc}", file=sys.stderr)
        raise
