#!/usr/bin/env python3
"""
validate_roles.py — Locked role-file grammar validator

Usage:
    python3 tools/validate_roles.py <path> [<path> ...]
    python3 tools/validate_roles.py --self-test

The grammar enforced (identical to docs/PROFILES.md):

1. File is UTF-8, no BOM, max 8 KB (8192 bytes).
2. Line 1 must be exactly:  Role layer, <Display Name>:
   where <Display Name> is 1–40 characters and contains no colon.
   This is the FIRST physical line — no '#' comments or other content may
   precede it.  A comment on line 1 or any content before the role-layer
   line is a parse error.
3. After line 1, free prose (at least one non-empty line) until the first
   section header.  Non-empty means after stripping spaces and tabs.
4. Lines whose first non-space character is '#' are comments and are ignored
   everywhere FROM LINE 2 onward.  They do NOT affect the parsing of line 1
   or the section headers.
5. Section headers exactly "=== MODES ===" then "=== WEAPON REACTIONS ===", both
   required, in that order.  These headers are also scanned ignoring comment
   lines (so a comment between line 1 and the headers is OK).
6. Inside a section, "[Label]" starts a block; body text runs to the next
   label, the next section header or end of file.  A label with an empty body
   is an error.
7. MODES must contain exactly the five labels:
       Friendly, Grumpy, Scared, Aggressive, Philosophical
   all required, nothing else.
8. WEAPON REACTIONS must contain exactly the three labels:
       WeaponDrawn, WeaponAimed, WeaponFired
   all required, nothing else.

Any deviation is a parse error.

Exit codes:
    0 — every file parses
    1 — at least one file fails
    2 — usage error

CLI notes:
    * Paths may be .txt files or directories (recursively walked for *.txt).
    * Errors are reported as "path:line: message", one per line, then a final
      summary line.
    * On a successful run, a short summary per file is printed: path, display
      name, number of modes, number of reactions.
    * --self-test runs embedded fixtures (one valid file + at least eight
      distinct invalid cases, one per rule) and exits 0 only if every case
      behaves as expected; it prints one line per case.
    * In the self-test, "content before line 1" is an invalid case (comments
      or other text on line 1 is a parse error).

Constraints: English comments and messages, no API keys, no network, no
dependencies outside the Python standard library, do not touch any other file
(docs, role .txt files, src/, .github/). No git commands.
"""

import argparse
import sys
import os
import tempfile

# ---------------------------------------------------------------------------
# Helper: walk .txt paths
# ---------------------------------------------------------------------------

def resolve_paths(args):
    """Return sorted list of .txt file paths from the given arguments."""
    paths = []
    for arg in args:
        if os.path.isdir(arg):
            for root, _dirs, files in os.walk(arg):
                for f in files:
                    if f.endswith(".txt"):
                        paths.append(os.path.join(root, f))
        elif os.path.isfile(arg) and arg.endswith(".txt"):
            paths.append(arg)
    # Deduplicate while preserving order
    seen = set()
    uniq = []
    for p in paths:
        if p not in seen:
            seen.add(p)
            uniq.append(p)
    return uniq


# ---------------------------------------------------------------------------
# Grammar validation
# ---------------------------------------------------------------------------

MODES_EXPECTED = {"Friendly", "Grumpy", "Scared", "Aggressive", "Philosophical"}
REACTIONS_EXPECTED = {"WeaponDrawn", "WeaponAimed", "WeaponFired"}

MAX_BYTES = 8 * 1024  # 8 KB


def strip_leading_ws(line):
    """Return line with leading spaces/tabs removed."""
    return line.lstrip(' \t')


def is_comment_line(line):
    """True if the first non-space character is '#'."""
    stripped = strip_leading_ws(line)
    return stripped.startswith('#') if stripped else False


def parse_file(filepath):
    """
    Validate a single role .txt file against the locked grammar.

    Returns (ok, errors, summary) where:
      ok            — True if no parse errors
      errors        — list of "path:line: message" strings
      summary dict  — {display_name, modes, reactions} on success, else {}
    """
    errors = []

    # ----- read file -----
    try:
        with open(filepath, "rb") as f:
            raw = f.read()
    except OSError as e:
        errors.append(f"{filepath}:0: cannot read file: {e}")
        return False, errors, {}

    # BOM check — return early if BOM present
    if raw[:3] == b'\xef\xbb\xbf':
        errors.append(f"{filepath}:0: UTF-8 BOM present")
        return False, errors, {}
    if raw[:2] == b'\xff\xfe' or raw[:2] == b'\xfe\xff':
        errors.append(f"{filepath}:0: BOM or non-UTF-16 encoding detected")
        return False, errors, {}

    # Decode UTF-8
    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        errors.append(f"{filepath}:0: invalid UTF-8")
        return False, errors, {}

    # Size check
    if len(raw) > MAX_BYTES:
        errors.append(f"{filepath}:0: file exceeds 8 KB limit ({len(raw)} bytes)")

    lines = text.splitlines()

    # ----- Line 1 must be exactly "Role layer, <Display Name>:" -----
    # No comment-skipping: line 1 is the first physical line.
    if not lines:
        errors.append(f"{filepath}:1: file is empty; expected 'Role layer, <Name>:'")
    else:
        raw_line = lines[0]
        stripped = strip_leading_ws(raw_line)
        if not stripped.startswith("Role layer,"):
            errors.append(f"{filepath}:1: line 1 must start with 'Role layer,': got '{stripped[:40]}'")
        else:
            # Extract display name between "Role layer," and ":"
            body = stripped[len("Role layer,"):].strip()
            colon_idx = body.find(':')
            if colon_idx < 0:
                errors.append(f"{filepath}:1: expected format 'Role layer, <Display Name>:'")
            else:
                display_name = body[:colon_idx]
                if len(display_name) < 1 or len(display_name) > 40:
                    errors.append(f"{filepath}:1: display name must be 1-40 characters (got {len(display_name)})")
                elif ':' in display_name:
                    errors.append(f"{filepath}:1: display name must not contain a colon")

    # ----- find section headers: === MODES === then === WEAPON REACTIONS === -----
    # Scan from line index 1 (second physical line), skipping comment lines.
    start_scan = 1
    modes_found = False
    reactions_found = False
    modes_section_idx = None
    reactions_section_idx = None
    modes_body_start = None
    reactions_body_start = None

    scan_idx = start_scan
    while scan_idx < len(lines):
        line = lines[scan_idx]
        if is_comment_line(line):
            scan_idx += 1
            continue
        stripped = strip_leading_ws(line)
        if not modes_found:
            if stripped == "=== MODES ===":
                modes_found = True
                modes_section_idx = scan_idx
                modes_body_start = scan_idx + 1
            else:
                scan_idx += 1
                continue
        elif not reactions_found:
            if stripped == "=== WEAPON REACTIONS ===":
                reactions_found = True
                reactions_section_idx = scan_idx
                reactions_body_start = scan_idx + 1
            else:
                scan_idx += 1
                continue
        else:
            break
        scan_idx += 1

    if not modes_found:
        errors.append(f"{filepath}:1: missing section header '=== MODES ==='")
    if not reactions_found:
        errors.append(f"{filepath}:1: missing section header '=== WEAPON REACTIONS ==='")

    # ----- prose is required between line 1 and the first section header -----
    if modes_section_idx is not None:
        prose_lines = [
            ln for ln in lines[1:modes_section_idx]
            if not is_comment_line(ln) and strip_leading_ws(ln).strip()
        ]
        if not prose_lines:
            errors.append(
                f"{filepath}:2: missing prose before the first section header"
            )

    # Line anchors for label-set errors, so every message carries a line number
    modes_anchor = (modes_section_idx + 1) if modes_section_idx is not None else 1
    reactions_anchor = (
        (reactions_section_idx + 1) if reactions_section_idx is not None else 1
    )

    # ----- validate MODES body -----
    modes_found_labels = set()

    if modes_section_idx is not None:
        if reactions_section_idx is not None:
            modes_lines = lines[modes_body_start:reactions_section_idx]
        else:
            modes_lines = lines[modes_body_start:]
    else:
        modes_lines = []

    # Parse labels and bodies in MODES
    # A line matching [Label] starts a new label; text after ] on the same line
    # is body text. Subsequent lines are body text until the next [Label] or section end.
    current_label = None
    current_body_parts = []
    current_label_line = None  # 1-based line number in the file

    start_idx = modes_body_start if modes_body_start is not None else 0
    for li, line in enumerate(modes_lines, start=start_idx):
        stripped = strip_leading_ws(line)

        # Check if this line starts a new label [Label]
        if stripped.startswith('[') and ']' in stripped:
            # Flush previous label
            if current_label is not None:
                label_text = current_label.strip()
                body_text = '\n'.join(current_body_parts).strip()
                if label_text in MODES_EXPECTED:
                    modes_found_labels.add(label_text)
                else:
                    errors.append(f"{filepath}:{current_label_line}: unknown mode label '{label_text}'")
                if not body_text:
                    errors.append(f"{filepath}:{current_label_line}: label '{label_text}' has empty body")
            # Extract label name and optional body text
            # Find the closing ]
            close_idx = stripped.index(']')
            label_name = stripped[1:close_idx]
            # Text after ] may be body text on the same line
            rest_after = stripped[close_idx + 1:].strip()
            current_label = label_name
            current_label_line = li + 1  # 1-based line number in original file
            current_body_parts = []
            if rest_after:
                current_body_parts.append(rest_after)
        else:
            # Not a label line — it's body text (if we are in a label block)
            if current_label is not None:
                current_body_parts.append(line)

    # Flush the last label
    if current_label is not None:
        label_text = current_label.strip()
        body_text = '\n'.join(current_body_parts).strip()
        if label_text in MODES_EXPECTED:
            modes_found_labels.add(label_text)
        else:
            errors.append(f"{filepath}:{current_label_line}: unknown mode label '{label_text}'")
        if not body_text:
            errors.append(f"{filepath}:{current_label_line}: label '{label_text}' has empty body")

    # Check modes: exactly the five expected, no extra, no missing
    modes_extra = modes_found_labels - MODES_EXPECTED
    modes_missing = MODES_EXPECTED - modes_found_labels
    if modes_extra:
        for lbl in sorted(modes_extra):
            errors.append(f"{filepath}:{modes_anchor}: extra mode label '{lbl}'")
    if modes_missing:
        for lbl in sorted(modes_missing):
            errors.append(f"{filepath}:{modes_anchor}: missing mode label '{lbl}'")
    if not modes_found_labels:
        # Only report "no labels" if the section header was actually found
        if modes_section_idx is not None:
            errors.append(f"{filepath}:1: no mode labels found in '=== MODES ===' section")

    # ----- validate WEAPON REACTIONS body -----
    reactions_found_labels = set()

    if reactions_section_idx is not None:
        reactions_lines = lines[reactions_body_start:] if reactions_body_start is not None else []
    else:
        reactions_lines = []

    current_label = None
    current_body_parts = []

    for li, line in enumerate(reactions_lines, start=(reactions_body_start if reactions_body_start is not None else 0)):
        stripped = strip_leading_ws(line)

        if stripped.startswith('[') and ']' in stripped:
            # Flush previous label
            if current_label is not None:
                label_text = current_label.strip()
                body_text = '\n'.join(current_body_parts).strip()
                if label_text in REACTIONS_EXPECTED:
                    reactions_found_labels.add(label_text)
                else:
                    errors.append(f"{filepath}:{current_label_line}: unknown reaction label '{label_text}'")
                if not body_text:
                    errors.append(f"{filepath}:{current_label_line}: label '{label_text}' has empty body")
            # Extract label name and optional body text
            close_idx = stripped.index(']')
            label_name = stripped[1:close_idx]
            rest_after = stripped[close_idx + 1:].strip()
            current_label = label_name
            current_label_line = li + 1  # 1-based
            current_body_parts = []
            if rest_after:
                current_body_parts.append(rest_after)
        else:
            if current_label is not None:
                current_body_parts.append(line)

    # Flush last reaction label
    if current_label is not None:
        label_text = current_label.strip()
        body_text = '\n'.join(current_body_parts).strip()
        if label_text in REACTIONS_EXPECTED:
            reactions_found_labels.add(label_text)
        else:
            errors.append(f"{filepath}:{current_label_line}: unknown reaction label '{label_text}'")
        if not body_text:
            errors.append(f"{filepath}:{current_label_line}: label '{label_text}' has empty body")

    # Check reactions: exactly the three expected
    reactions_extra = reactions_found_labels - REACTIONS_EXPECTED
    reactions_missing = REACTIONS_EXPECTED - reactions_found_labels
    if reactions_extra:
        for lbl in sorted(reactions_extra):
            errors.append(f"{filepath}:{reactions_anchor}: extra reaction label '{lbl}'")
    if reactions_missing:
        for lbl in sorted(reactions_missing):
            errors.append(f"{filepath}:{reactions_anchor}: missing reaction label '{lbl}'")
    if not reactions_found_labels:
        if reactions_section_idx is not None:
            errors.append(f"{filepath}:1: no reaction labels found in '=== WEAPON REACTIONS ===' section")

    # ----- summary on success -----
    ok = len(errors) == 0
    summary = {}
    if ok and len(lines) > 0:
        raw_line = lines[0]
        stripped = strip_leading_ws(raw_line)
        body = stripped[len("Role layer,"):].strip()
        colon_idx = body.find(':')
        display_name = body[:colon_idx] if colon_idx >= 0 else body
        mode_count = len(modes_found_labels) if modes_section_idx is not None else 0
        reaction_count = len(reactions_found_labels) if reactions_section_idx is not None else 0
        summary = {
            "display_name": display_name,
            "modes": mode_count,
            "reactions": reaction_count,
        }

    return ok, errors, summary


# ---------------------------------------------------------------------------
# Self-test fixtures
# ---------------------------------------------------------------------------

VALID_FILE = """Role layer, TestPed:

This is a test role file.
It has some free prose.

=== MODES ===
[Friendly] pedestrians enjoy the sunshine
[Grumpy] no one likes the rain
[Scared] cats run away
[Aggressive] dogs bark at strangers
[Philosophical] thoughts on existence

=== WEAPON REACTIONS ===
[WeaponDrawn] player pulls gun
[WeaponAimed] player aims down sights
[WeaponFired] bang bang
"""

INVALID_CASES = [
    # Rule 1: BOM present
    ("bom.txt", b'\xef\xbb\xbfRole layer, Test:\nLine 2\n', "UTF-8 BOM present"),
    # Rule 2: File over 8 KB
    ("toobig.txt", ("X" * (8193)) + "\nRole layer, Test:\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "file exceeds 8 KB limit"),
    # Rule 3: Line 1 missing/wrong format
    ("wrong_line1.txt", "BadLine\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "line 1 does not match 'Role layer, <Name>:'"),
    # Rule 4: Display name has colon
    ("colon_name.txt", "Role layer, Bad:Name:\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "display name contains a colon"),
    # Rule 5: Display name too short (0 chars)
    ("empty_name.txt", "Role layer, :\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "display name is empty"),
    # Rule 6: Missing MODES section
    ("missing_modes.txt", "Role layer, TestPed:\nprose\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "missing '=== MODES ===' section"),
    # Rule 7: Missing WEAPON REACTIONS section
    ("missing_reactions.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n", "missing '=== WEAPON REACTIONS ===' section"),
    # Rule 8: Wrong order (WEAPON REACTIONS before MODES)
    ("wrong_order.txt", "Role layer, TestPed:\nprose\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n=== MODES ===\n[Friendly]\ntext\n", "sections in wrong order"),
    # Rule 9: Unknown mode label
    ("unknown_mode.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[UnknownMode]\ntext\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "unknown mode label"),
    # Rule 10: Unknown reaction label
    ("unknown_reaction.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[UnknownReaction]\ntext\n", "unknown reaction label"),
    # Rule 11: Empty body for a label
    ("empty_body.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\n", "label with empty body"),
    # Rule 12: Duplicate mode label
    ("dup_mode.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n[Friendly]\ntext\n[Grumpy]\ntext\n[Scared]\ntext\n[Aggressive]\ntext\n[Philosophical]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "duplicate mode label"),
    # Rule 13: Extra mode label (more than 5)
    ("extra_mode.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n[Grumpy]\ntext\n[Scared]\ntext\n[Aggressive]\ntext\n[Philosophical]\ntext\n[Extra]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "extra mode label"),
    # Rule 14: Extra reaction label (more than 3)
    ("extra_reaction.txt", "Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n[WeaponAimed]\ntext\n[WeaponFired]\ntext\n[Extra]\ntext\n", "extra reaction label"),
    # Rule 15: Content before line 1 — a '#' comment on line 1
    ("comment_on_line1.txt", "# Role layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "comment on line 1 is a parse error; role-layer line must be first physical line"),
    # Rule 17: No prose between line 1 and the first section header
    ("noprose.txt", "Role layer, TestPed:\n=== MODES ===\n[Friendly]\ntext\n[Grumpy]\ntext\n[Scared]\ntext\n[Aggressive]\ntext\n[Philosophical]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n[WeaponAimed]\ntext\n[WeaponFired]\ntext\n", "no prose before the first section header"),
    # Rule 16: Content before line 1 — arbitrary text on line 1
    ("text_before_role.txt", "Some other text\nRole layer, TestPed:\nprose\n=== MODES ===\n[Friendly]\ntext\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\ntext\n", "arbitrary text on line 1 is a parse error"),
]

# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def self_test():
    """Run embedded fixtures and report per-case results."""
    cases = []

    # Valid file
    import os as _os
    try:
        with tempfile.NamedTemporaryFile(suffix=".txt", delete=False, mode='wb') as f:
            f.write(VALID_FILE.encode("utf-8"))
            vpath = f.name
        from validate_roles import parse_file as _pf
        ok_v, errs_v, summ_v = _pf(vpath)
        if ok_v and not errs_v and summ_v.get("display_name") == "TestPed" and summ_v.get("modes") == 5 and summ_v.get("reactions") == 3:
            cases.append(("valid", "PASS"))
        else:
            cases.append(("valid", f"FAIL: ok={ok_v} errs={errs_v} sum={summ_v}"))
    except Exception as e:
        cases.append(("valid", f"FAIL: exception {e}"))
    _os.unlink(vpath)

    # Invalid cases — each must produce an error (ok=False or non-empty errs)
    for fname, content, desc in INVALID_CASES:
        try:
            with tempfile.NamedTemporaryFile(suffix=".txt", delete=False, mode='wb') as f:
                if isinstance(content, str):
                    f.write(content.encode("utf-8"))
                else:
                    f.write(content)
                ipath = f.name
            from validate_roles import parse_file as _pf2
            ok_i, errs_i, summ_i = _pf2(ipath)
            has_error = (not ok_i) or len(errs_i) > 0
            if has_error:
                cases.append((fname, "PASS"))
            else:
                cases.append((fname, f"FAIL: expected error but ok={ok_i} errs={errs_i}"))
        except Exception as e:
            cases.append((fname, f"FAIL: exception {e}"))
        finally:
            try:
                _os.unlink(ipath)
            except Exception:
                pass

    # Print one line per case
    for name, result in cases:
        print(f"{name}: {result}")

    # Exit 0 only if all cases passed
    all_pass = all(r == "PASS" for _, r in cases)
    return 0 if all_pass else 1


def validate_paths(paths):
    """Validate a list of file paths and print results."""
    if not paths:
        print("Usage: python3 tools/validate_roles.py <path> [<path> ...]", file=sys.stderr)
        return 2

    exit_code = 0
    for fp in paths:
        ok, errors, summary = parse_file(fp)
        if errors:
            exit_code = 1
            for e in errors:
                print(e)
        if ok and summary:
            dsn = summary.get("display_name", "???")
            mc = summary.get("modes", 0)
            rc = summary.get("reactions", 0)
            print(f"{fp}: display_name={dsn}, modes={mc}, reactions={rc}")

    return exit_code


def main():
    parser = argparse.ArgumentParser(
        description="Validate PedAndPaw_AI role-file grammar"
    )
    parser.add_argument(
        "paths",
        nargs="*",
        metavar="PATH",
        help=" .txt file or directory to walk recursively for *.txt",
    )
    parser.add_argument(
        "--self-test",
        action="store_true",
        help="Run embedded fixtures and exit 0 only if all behave as expected",
    )
    args = parser.parse_args()

    if args.self_test:
        sys.exit(self_test())

    paths = resolve_paths(args.paths)
    sys.exit(validate_paths(paths))


if __name__ == "__main__":
    main()