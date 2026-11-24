# Writing Role Profiles (PedAndPaw_AI)

This document describes how to create custom character profiles for PedAndPaw_AI. The format is intentionally simple: **plain UTF-8 text files**, one per role or animal.

---

## File Location

Place your `.txt` files under:

```
scripts/PedAndPaw_AI/roles/humans/   # e.g., police.txt, gang_member.txt, civilian.txt
scripts/PedAndPaw_AI/roles/animals/  # e.g., dog.txt, cat.txt, bird.txt, deer.txt
```

The filename (without extension) becomes the **role key** used internally.

---

## File Structure

A role file has three parts, in this fixed order:

```
Role layer, <LayerName>:
<prose describing the voice, personality, and worldview>

=== MODES ===
<mode sections go here>

=== WEAPON REACTIONS ===
<weapon reaction sections go here>
```

### 1. Role Layer (Required, First Line)

The **first line** must be exactly:

```
Role layer, <LayerName>:
```

Examples:
- `Role layer, Police:`
- `Role layer, Gang Member:`
- `Role layer, Stray Dog:`
- `Role layer, House Cat:`

This tells the AI which **role layer** the character belongs to. The layer determines base behavior, available modes, and how other peds react to this character.

> **Do not invent your own layer names.** The complete list of valid layers is fixed when the profile loader (CharacterManager.cs) is implemented (the code that loads profiles). Using an unknown layer will cause the profile to be ignored.

### 2. Prose (Required)

Everything after the role-layer line up to `=== MODES ===` (or end of file) is the **voice description**. Write in second person, as instructions to the AI:

> You are a veteran LSPD patrol officer. You speak with authority but not aggression. You know the streets, the codes, the rhythms of Los Santos. You refer to the player as "citizen" or "suspect" depending on context. You never break character. You value procedure, safety, and de-escalation.

This prose **is the prompt** sent to the LLM. Be specific, vivid, and consistent.

### 3. Modes (Optional Section)

If the character has distinct behavioral modes, add a `=== MODES ===` header followed by labelled subsections:

```
=== MODES ===

[Patrol]
You walk your beat, eyes scanning. You greet civilians politely. You answer questions briefly.

[Investigation]
You are focused, sharp. You ask pointed questions. You notice details others miss.

[Pursuit]
You are urgent, loud, commanding. "Stop! Police!" You coordinate with dispatch.
```

Each mode name in brackets (e.g., `[Patrol]`) must match a mode name defined when the profile loader (CharacterManager.cs) is implemented. Unknown modes are ignored.

> **The exact section syntax (`=== MODES ===` and `[ModeName]`) is fixed when CharacterManager.cs is written and must not be invented here.** Wait for that step to finalize the list of valid modes and layers before writing production profiles.

### 4. Weapon Reactions (Optional Section)

Define how the character reacts when the player (or another ped) draws or fires a weapon:

```
=== WEAPON REACTIONS ===

[WeaponDrawn]
You shout: "Drop the weapon! Hands where I can see them!"

[WeaponFired]
You take cover, return fire, and call for backup: "Shots fired! Officer down at Vinewood & Alta!"
```

Valid reaction labels (`[WeaponDrawn]`, `[WeaponFired]`, etc.) are fixed when the profile loader (CharacterManager.cs) is implemented. Do not create your own labels.

---

## Example: Minimal Profile

File: `scripts/PedAndPaw_AI/roles/humans/civilian.txt`

```
Role layer, Civilian:
You are an ordinary Los Santos resident going about your day. You speak casually, maybe a bit cynical about the city. You avoid trouble. You answer questions helpfully but briefly. You don't know anything about crime, gangs, or police business beyond what you see on the news.
```

---

## Example: Full Profile with Modes & Reactions

File: `scripts/PedAndPaw_AI/roles/animals/dog.txt`

```
Role layer, Stray Dog:
You are a street-smart mutt surviving in Los Santos. You communicate through barks, whines, body language, and the occasional telepathic impression the player "hears." You are wary of humans but curious. You love food. You protect your territory.

=== MODES ===

[Wandering]
You sniff around, mark territory, watch humans from a distance. Your thoughts are simple: *Food. Smell. Safe?*

[Friendly]
You approach the player, tail wagging. *Pet me? Food?* You project warmth and hope.

[Guarding]
You stand your ground, hackles raised. Low growl. *Mine. Go away.*

=== WEAPON REACTIONS ===

[WeaponDrawn]
You flatten your ears, tuck tail, back away slowly. *Loud. Danger. Run.*

[WeaponFired]
You bolt for the nearest alley, yelping. *Pain. Fear. Survive.*
```

---

## Tips

- Keep prose **concise but evocative**. The LLM has a token budget.
- Write in **present tense, second person** ("You are...", "You speak...").
- Avoid contradictory instructions.
- Test your profile in-game: press the interact key (default E) near a ped with that role.
- Role files are read when the game starts (`ReloadRolesOnStart`); edit the `.txt` and restart.

---

## What Is Still Open (Do Not Invent)

- Valid **role layer names** (the list after `Role layer,`)
- Valid **mode names** (inside `=== MODES ===`)
- Valid **weapon reaction labels** (inside `=== WEAPON REACTIONS ===`)
- Exact **section header spelling** (`=== MODES ===`, `=== WEAPON REACTIONS ===`)
- Bracket syntax for modes and reactions (`[Name]`)

Wait for the profile loader to publish the authoritative lists before authoring production profiles. This stub exists so you understand the file shape.

---

*See also: docs/PLUGINS.md for how other scripts can react to dialogue events.*