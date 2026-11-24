# Role File Format — Authoritative Specification (PedAndPaw_AI v1)

This document is the **locked, authoritative grammar** for role‑files used by PedAndPaw_AI. It is read‑only and must not be modified until the grammar evolves to a new version.
All parser tools (tools/validate_roles.py) and the runtime (CharacterManager.cs) follow this specification exactly.

---

## 1. File Constraints

- **Encoding:** plain UTF‑8, no BOM.
- **Location:** `scripts/PedAndPaw_AI/roles/humans/` or `scripts/PedAndPaw_AI/roles/animals/`.
- **Extension:** `.txt`.
- **Maximum size:** 8 KB.
- **Name:** The file name without the `.txt` extension is the role key used internally.

---

## 2. First Line – Role Layer Declaration

The very first line **must** match exactly the pattern:
```
Role layer, <Display Name>:
```
where `<Display Name>`:
- has 1–40 characters,
- contains no `:` character.

The display name is the *role layer* string passed to the LLM.

> **Example**: `Role layer, Police:`

If this line is missing or malformed the file is rejected.

---

## 3. Free Prose (Voice)

All content from line 2 up to (but not including) the first section header is free prose.
- At least one non‑empty line must exist.
- This prose is the voice sent to the model; it defines personality, tone, and instructions to the AI.
- Write in present tense, second person (e.g., “You are …”).

---

## 4. Comments

Lines whose first non‑space character is `#` are comments and are ignored. Blank lines are ignored unless they form part of prose.

---

## 5. Section Headers (Exact)

Two section headers **must** appear, in this order, both required:
1. `=== MODES ===`
2. `=== WEAPON REACTIONS ===`

- Headers must match exactly, including spaces.
- No other headers are allowed.
- Wrong order or spelling causes a parse error.

---

## 6. Section Content – `[Label]` Blocks

Inside each section, blocks begin with a line of the form `[Label]` with no leading/trailing spaces inside the brackets.
The block body consists of all text up to the next `[Label]`, the next section header, or end of file.

---

## 7. MODES Section – Five Mandatory Labels

The `=== MODES ===` section **must** contain exactly these five labels, each exactly once:
- `Friendly`
- `Grumpy`
- `Scared`
- `Aggressive`
- `Philosophical`

Any missing, duplicate, or unknown label results in a parse error.
The body of each label describes how the character behaves in that mode.

---

## 8. WEAPON REACTIONS Section – Three Mandatory Labels

The `=== WEAPON REACTIONS ===` section **must** contain exactly these three labels, each exactly once:
- `WeaponDrawn`
- `WeaponAimed`
- `WeaponFired`

Any missing, duplicate, or unknown label results in a parse error.
The body of each label describes the character’s response when a weapon is involved.

---

## 9. Animals’ Special Rule

- Animals are *talkable*.
- They can never be shot or killed; weapon reactions must describe **fear and flight**, not damage.
- The three weapon‑reaction labels still apply, but the content must reflect non‑violent escape.

---

## 10. Clean‑Content Guarantee

- Role files must not contain explicit sexual content.
- The `NudityPercent` setting in `PedAndPaw_AI.ini` governs runtime AI output; the role file itself must remain clean.

---

## 11. Parse Errors

A parse error causes the file to be rejected by `CharacterManager.cs` with a clear log line. Parse errors include:
- Missing or malformed first line.
- Content before line 1.
- Missing or mis‑typed section header.
- Wrong header order.
- Missing, duplicate, or unknown labels.
- No prose before the first section header.
- File size > 8 KB.

---

## 12. Validation Command

Run the built‑in validator:
```
python3 tools/validate_roles.py scripts/PedAndPaw_AI/roles
```
It will report pass/fail for each `.txt` file and line numbers for errors.

---

## 13. CharacterManager.cs – Runtime Behavior

- Loads all files in `scripts/PedAndPaw_AI/roles/` recursively.
- Parses according to the grammar above.
- On parse error, logs e.g.:
```
[ERROR] PedAndPaw_AI: Role file 'scripts/PedAndPaw_AI/roles/animals/bad_dog.txt' has parse error: unknown label 'Wesrpe' in WEAPON REACTIONS section. Loading aborted.
```
- Successfully parsed files provide their prose, modes, and weapon reactions to the AI prompt.

---

## 14. Shipped Example Files

The repository ships three reference files, and they are the ones the validator runs against:

| File | Role layer | What it demonstrates |
|------|------------|----------------------|
| `scripts/PedAndPaw_AI/roles/humans/police.txt` | `Police` | A human NPC with lawful authority and de-escalation first |
| `scripts/PedAndPaw_AI/roles/animals/street_cat.txt` | `Street Cat` | An animal that begs for food, judges humans and stays aloof |
| `scripts/PedAndPaw_AI/roles/animals/mountain_lion.txt` | `Mountain Lion` | An animal that is enormous and behaves like a house cat |

Open those files in the repository instead of copying examples from this page. A snippet here would drift from the real file the moment either is edited, and the files, not this page, are what the mod loads.

The shape of a file, with the prose elided:

```
Role layer, <Display Name>:
<free prose: the voice and worldview>

=== MODES ===
[Friendly] ...
[Grumpy] ...
[Scared] ...
[Aggressive] ...
[Philosophical] ...

=== WEAPON REACTIONS ===
[WeaponDrawn] ...
[WeaponAimed] ...
[WeaponFired] ...
```

---

## 15. Version History

- **v1.0.0** (2025‑11‑24): Initial grammar – five MODE labels, three WEAPON REACTIONS labels, fixed header order, comment syntax, 8 KB limit, clean‑content rule, validation command, runtime behavior.

> *This document is the authoritative specification. Do not deviate without a version bump.*

---

*End of document.*
