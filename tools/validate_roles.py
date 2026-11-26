#!/usr/bin/env python3
"""
validate_roles.py - role-file validator, grammar v1.1

Usage:
    python3 tools/validate_roles.py <path> [<path> ...]
    python3 tools/validate_roles.py --self-test

Grammar (identical to docs/PROFILES.md and src/Core/RoleParser.cs):

1. UTF-8, no BOM, at most 64 KB.
2. Line 1 must be exactly:  Role layer, <Display Name>:
   name 1-40 characters, no colon, nothing before it.
3. Free prose below line 1, at least one non-empty line.
4. '#' comment lines are ignored from line 2 onward.
5. Sections in this order: === MODES ===, === WEAPON REACTIONS ===, and the
   optional === TRAITS ===.
6. MODES: the five labels Friendly, Grumpy, Scared, Aggressive, Philosophical
   are required; Professional is optional. Nothing else allowed.
7. WEAPON REACTIONS: WeaponDrawn, WeaponAimed, WeaponFired, all required.
8. TRAITS (optional) may hold [CORE], [QUIRK] and [NAMES] blocks, one item per
   line. Block contents are lists, not prose.
9. Placeholders: {NAME} {SPECIES} {TRAIT_1} {TRAIT_2} {TRAIT_3} {QUIRK}
   {TIME_OF_DAY} {LIGHT} {WEATHER} {AREA}. An unknown placeholder is an error.

Warnings (the file still loads): fewer than 10 core traits, 8 quirks or
10 names, an adjective where a behaviour belongs, and forbidden-topic words.

Exit codes: 0 all files valid, 1 any file invalid, 2 usage error.
"""
import os
import re
import sys
import tempfile

MAX_BYTES = 64 * 1024
MODES_REQUIRED = ["Friendly", "Grumpy", "Scared", "Aggressive", "Philosophical"]
MODES_OPTIONAL = ["Professional"]
REACTIONS_REQUIRED = ["WeaponDrawn", "WeaponAimed", "WeaponFired"]
TRAIT_BLOCKS = ["CORE", "QUIRK", "NAMES"]
PLACEHOLDERS = ["NAME", "SPECIES", "TRAIT_1", "TRAIT_2", "TRAIT_3",
                "QUIRK", "TIME_OF_DAY", "LIGHT", "WEATHER", "AREA"]
RECOMMENDED = {"CORE": 10, "QUIRK": 8, "NAMES": 10}
ADJECTIVES = ["curious", "lazy", "happy", "sad", "angry", "friendly", "kind",
              "smart", "grumpy", "playful", "shy", "brave", "timid", "proud", "warm"]
FORBIDDEN = ["explicit sex", "sexual act", "rape", "porn", "nigger", "faggot",
             "drug deal", "cocaine", "heroin", "meth", "murder", "torture"]

MODES_HEADER = "=== MODES ==="
REACTIONS_HEADER = "=== WEAPON REACTIONS ==="
TRAITS_HEADER = "=== TRAITS ==="


def parse_blocks(lines, start, end, path, errors):
    """Parse [Label] blocks with prose bodies."""
    blocks = {}
    label = None
    body = []
    label_line = 0

    def flush():
        nonlocal label, body
        if label is None:
            return
        text = "\n".join(body).strip()
        if not text:
            errors.append(f"{path}:{label_line}: label '{label}' has empty body")
        else:
            blocks[label] = text
        label, body = None, []

    for index in range(start, end):
        line = lines[index]
        stripped = line.strip()
        if stripped.startswith("#"):
            continue
        if stripped.startswith("[") and stripped.find("]") > 1:
            flush()
            close = stripped.index("]")
            label = stripped[1:close].strip()
            label_line = index + 1
            rest = stripped[close + 1:].strip()
            if rest:
                body.append(rest)
            continue
        if label is not None and stripped:
            body.append(stripped)
    flush()
    return blocks


def parse_traits(lines, start, path, errors, warnings):
    """Parse [CORE], [QUIRK], [NAMES] list blocks."""
    traits = {}
    block = None
    items = []
    block_line = 0

    def flush():
        nonlocal block, items
        if block is None:
            return
        upper = block.upper()
        if upper not in TRAIT_BLOCKS:
            errors.append(f"{path}:{block_line}: unknown traits block '[{block}]'; use [CORE], [QUIRK] or [NAMES]")
        else:
            cleaned = [item.strip() for item in items if item.strip()]
            traits[upper] = cleaned
            if cleaned and len(cleaned) < RECOMMENDED[upper]:
                warnings.append(f"{path}:{block_line}: only {len(cleaned)} {upper.lower()} items; {RECOMMENDED[upper]} recommended")
            if upper == "CORE":
                for item in cleaned:
                    lower = item.lower()
                    if any(lower == word or lower.startswith(word + " ") for word in ADJECTIVES):
                        warnings.append(f"{path}:{block_line}: core trait '{item}' is an adjective; describe a behaviour instead")
        block, items = None, []

    for index in range(start, len(lines)):
        stripped = lines[index].strip()
        if stripped.startswith("#"):
            continue
        if stripped.startswith("[") and stripped.find("]") > 1:
            flush()
            close = stripped.index("]")
            block = stripped[1:close].strip()
            block_line = index + 1
            continue
        if block is not None and stripped:
            items.append(stripped)
    flush()
    return traits


def parse_file(path):
    errors, warnings = [], []

    try:
        raw = open(path, "rb").read()
    except OSError as exc:
        return False, [f"{path}:0: cannot read file: {exc}"], [], {}

    if raw[:3] == b"\xef\xbb\xbf":
        return False, [f"{path}:1: UTF-8 BOM present"], [], {}
    if len(raw) > MAX_BYTES:
        return False, [f"{path}:0: file exceeds 64 KB limit ({len(raw)} bytes)"], [], {}

    try:
        text = raw.decode("utf-8")
    except UnicodeDecodeError:
        return False, [f"{path}:1: not valid UTF-8"], [], {}

    lines = text.replace("\r\n", "\n").replace("\r", "\n").split("\n")

    match = re.match(r"^Role layer, ([^:]{1,40}):$", lines[0].strip())
    if not match:
        return False, [f"{path}:1: line 1 must be 'Role layer, <Display Name>:' (name 1-40 chars, no colon)"], [], {}
    name = match.group(1).strip()

    indexes = {}
    order = []
    for index in range(1, len(lines)):
        stripped = lines[index].strip()
        if not stripped or stripped.startswith("#") or not stripped.startswith("==="):
            continue
        if stripped not in (MODES_HEADER, REACTIONS_HEADER, TRAITS_HEADER):
            errors.append(f"{path}:{index + 1}: unknown section header '{stripped}'")
            continue
        if stripped in indexes:
            errors.append(f"{path}:{index + 1}: duplicate section header '{stripped}'")
            continue
        indexes[stripped] = index
        order.append(stripped)

    expected_order = [MODES_HEADER, REACTIONS_HEADER, TRAITS_HEADER]
    for position, header in enumerate(order):
        if expected_order.index(header) != position:
            errors.append(f"{path}:{indexes[header] + 1}: sections must appear in the order MODES, WEAPON REACTIONS, TRAITS")

    if MODES_HEADER not in indexes:
        errors.append(f"{path}:1: missing section header '{MODES_HEADER}'")
    if REACTIONS_HEADER not in indexes:
        errors.append(f"{path}:1: missing section header '{REACTIONS_HEADER}'")

    if MODES_HEADER in indexes:
        prose = [ln.strip() for ln in lines[1:indexes[MODES_HEADER]]
                 if ln.strip() and not ln.strip().startswith("#")]
        if not prose:
            errors.append(f"{path}:2: missing prose before the first section header")

    modes = {}
    if MODES_HEADER in indexes:
        end = indexes.get(REACTIONS_HEADER, indexes.get(TRAITS_HEADER, len(lines)))
        modes = parse_blocks(lines, indexes[MODES_HEADER] + 1, end, path, errors)
        for label in modes:
            if label not in MODES_REQUIRED + MODES_OPTIONAL:
                errors.append(f"{path}:unknown: unknown mode label '{label}'")
        for label in MODES_REQUIRED:
            if label not in modes:
                errors.append(f"{path}:unknown: missing mode label '{label}'")

    reactions = {}
    if REACTIONS_HEADER in indexes:
        end = indexes.get(TRAITS_HEADER, len(lines))
        reactions = parse_blocks(lines, indexes[REACTIONS_HEADER] + 1, end, path, errors)
        for label in reactions:
            if label not in REACTIONS_REQUIRED:
                errors.append(f"{path}:unknown: unknown reaction label '{label}'")
        for label in REACTIONS_REQUIRED:
            if label not in reactions:
                errors.append(f"{path}:unknown: missing reaction label '{label}'")

    traits = {}
    if TRAITS_HEADER in indexes:
        traits = parse_traits(lines, indexes[TRAITS_HEADER] + 1, path, errors, warnings)

    # placeholders: known ones only
    texts = [ln for ln in lines[1:indexes.get(MODES_HEADER, len(lines))]]
    texts += list(modes.values()) + list(reactions.values())
    for text in texts:
        for found in re.findall(r"\{([A-Za-z0-9_]+)\}", text or ""):
            if found not in PLACEHOLDERS:
                errors.append(f"{path}:unknown placeholder '{{{found}}}'; known placeholders are " +
                              ", ".join("{" + p + "}" for p in PLACEHOLDERS))

    # 13+ policy: warn, do not fail, because role files often forbid these words
    lowered = " ".join(texts + [i for items in traits.values() for i in items]).lower()
    for word in FORBIDDEN:
        if re.search(r"(?<![a-z])" + re.escape(word) + r"(?![a-z])", lowered):
            warnings.append(f"{path}:content: '{word}' appears in the role file; the 13+ policy should keep this out of character text")

    ok = not errors
    summary = {"display_name": name, "modes": len(modes), "reactions": len(reactions),
               "core": len(traits.get("CORE", [])), "quirks": len(traits.get("QUIRK", [])),
               "names": len(traits.get("NAMES", []))} if ok else {}
    return ok, errors, warnings, summary


def resolve_paths(args):
    found = []
    for arg in args:
        if os.path.isdir(arg):
            for root, _dirs, files in os.walk(arg):
                found += [os.path.join(root, f) for f in files if f.endswith(".txt")]
        elif os.path.isfile(arg) and arg.endswith(".txt"):
            found.append(arg)
    seen, unique = set(), []
    for path in found:
        if path not in seen:
            seen.add(path)
            unique.append(path)
    return sorted(unique)


VALID_FILE = """Role layer, Test Ped:
A short line about this character.
=== MODES ===
[Friendly]
A friendly line.
[Grumpy]
A grumpy line.
[Scared]
A scared line.
[Aggressive]
An aggressive line.
[Philosophical]
A thoughtful line.
[Professional]
A calm, factual line.
=== WEAPON REACTIONS ===
[WeaponDrawn]
A drawn-weapon line.
[WeaponAimed]
An aimed line.
[WeaponFired]
A fired line.
=== TRAITS ===
[CORE]
chases butterflies
sleeps in boxes
begs for food
[QUIRK]
believes the player is furniture
[NAMES]
Silvia
Mrr
"""

INVALID_CASES = [
    ("bom.txt", "\ufeff" + VALID_FILE, "UTF-8 BOM present"),
    ("toobig.txt", "Role layer, Test:\n" + "x" * (65 * 1024) + "\n=== MODES ===\n", "file exceeds 64 KB"),
    ("wrong_line1.txt", "BadLine\nprose\n=== MODES ===\n", "line 1 format"),
    ("missing_prose.txt", "Role layer, Test:\n=== MODES ===\n[Friendly]\ntext\n", "missing prose"),
    ("wrong_order.txt", "Role layer, Test:\nprose\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nt\n=== MODES ===\n[Friendly]\nt\n", "sections in wrong order"),
    ("unknown_mode.txt", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly]\nt\n[Grumpy]\nt\n[Scared]\nt\n[Aggressive]\nt\n[Philosophical]\nt\n[Unknown]\nt\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nt\n[WeaponAimed]\nt\n[WeaponFired]\nt\n", "unknown mode label"),
    ("missing_reaction.txt", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly]\nt\n[Grumpy]\nt\n[Scared]\nt\n[Aggressive]\nt\n[Philosophical]\nt\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nt\n", "missing reaction label"),
    ("empty_body.txt", "Role layer, Test:\nprose\n=== MODES ===\n[Friendly]\n[Grumpy]\nt\n[Scared]\nt\n[Aggressive]\nt\n[Philosophical]\nt\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nt\n[WeaponAimed]\nt\n[WeaponFired]\nt\n", "label with empty body"),
    ("unknown_trait_block.txt", VALID_FILE.replace("[QUIRK]", "[WHATEVER]"), "unknown traits block"),
    ("unknown_placeholder.txt", VALID_FILE.replace("A friendly line.", "Hello {PLAYER_X}."), "unknown placeholder"),
    ("traits_first.txt", "Role layer, Test:\nprose\n=== TRAITS ===\n[CORE]\nx\n=== MODES ===\n[Friendly]\nt\n[Grumpy]\nt\n[Scared]\nt\n[Aggressive]\nt\n[Philosophical]\nt\n=== WEAPON REACTIONS ===\n[WeaponDrawn]\nt\n[WeaponAimed]\nt\n[WeaponFired]\nt\n", "traits before modes"),
]


def self_test():
    cases = []
    with tempfile.TemporaryDirectory() as folder:
        path = os.path.join(folder, "valid.txt")
        open(path, "w", encoding="utf-8").write(VALID_FILE)
        ok, errors, warnings, summary = parse_file(path)
        cases.append(("valid", "PASS" if ok and summary.get("modes") == 6 else f"FAIL: {errors}"))

        for name, content, _why in INVALID_CASES:
            path = os.path.join(folder, name)
            with open(path, "w", encoding="utf-8") as handle:
                handle.write(content)
            ok, errors, _warnings, _summary = parse_file(path)
            cases.append((name, "PASS" if not ok and errors else "FAIL: expected an error"))

        thin = VALID_FILE.replace("[CORE]\nchases butterflies\nsleeps in boxes\nbegs for food\n", "[CORE]\ncurious\n")
        path = os.path.join(folder, "thin.txt")
        open(path, "w", encoding="utf-8").write(thin)
        ok, _errors, warnings, _summary = parse_file(path)
        adjective = any("adjective" in w for w in warnings)
        cases.append(("thin_and_adjective_warns", "PASS" if ok and adjective else "FAIL: expected a warning"))

    for name, result in cases:
        print(f"{name}: {result}")
    return 0 if all(result == "PASS" for _n, result in cases) else 1


def main():
    args = sys.argv[1:]
    if not args:
        print("Usage: python3 tools/validate_roles.py <path> [<path> ...]  |  --self-test", file=sys.stderr)
        return 2
    if args[0] == "--self-test":
        return self_test()

    exit_code = 0
    for path in resolve_paths(args):
        ok, errors, warnings, summary = parse_file(path)
        for warning in warnings:
            print("warning: " + warning)
        if not ok:
            exit_code = 1
            for error in errors:
                print(error)
            continue
        print(f"{path}: display_name={summary['display_name']}, modes={summary['modes']}, "
              f"reactions={summary['reactions']}, core={summary['core']}, "
              f"quirks={summary['quirks']}, names={summary['names']}")
    return exit_code


if __name__ == "__main__":
    sys.exit(main())
