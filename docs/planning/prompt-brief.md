# Project brief (original prompt)

> Working document, originally Swedish. Converted to Markdown so it lives with the project.

You are a senior C\# and game developer specialized in GTA V Enhanced modding with ScriptHookVDotNet Enhanced (SHVDNE, .NET Framework 4.8).

Build an open-source mod called "PedAndPaw\_AI" (MIT license, for GitHub) that lets the player talk by text with NPCs and animals in GTA V Enhanced. The focus is on NPCs, real-time AI conversations with memory, personality, mood and attitude systems, and reactions to the player's actions. Animals can talk by text but can never be shot or killed.

## Core features

- E: interact with the nearest NPC (talk) or dismiss them.

- T: send a text message instead of speaking to nearby civilians, police and emergency services NPCs (fire and rescue, paramedics).

- Real-time conversations with per-NPC short-term memory, sized to keep prompts small.

- Context-aware replies using: time of day, weather, area, the player's vehicle, wanted level, weapon drawn, aiming, recent gunshots.

- Role awareness: each NPC knows its role (civilian, police, rescue, shopkeeper, animal) and acts accordingly.

- Attitude system: the NPC's attitude toward the player changes with the player's actions (drawing a weapon lowers trust and raises fear; friendly conversation raises trust).

- Modes per character (e.g. Friendly, Grumpy, Scared, Aggressive, Philosophical), assigned randomly per individual or switched dynamically by events.

- Vehicle borrowing: the player can ask an NPC to lend a vehicle; attitude decides whether they agree.

- Weapons may only be fired at designated places: shooting ranges and the player's own property outside built-up areas. Elsewhere, firing is blocked and nearby NPCs react.

- Content slider: a configurable nudity percentage from 0 to 100, default 0, that the AI must respect in all dialogue.

- NPC self-awareness: when the player asks, a character may admit in character that it is a computer-controlled character (NpcSelfAwareness in the ini).

- A documented family-friendly preset (NudityPercent = 0, weapon reactions kept to warnings) for parents who let children play. A peaceful mode of play is the main goal, not combat.

## AI provider

- Default: Google Gemini API key, using its OpenAI-compatible chat completions endpoint.

- The provider must be replaceable through PedAndPaw\_AI.ini with any OpenAI-compatible endpoint, including local Ollama (http://localhost:11434/v1, no key required), OpenRouter and Groq.

- Keep token use low: short replies, configurable max tokens, a cooldown between NPC messages, and a maximum number of AI calls per minute. NPCs must not talk excessively.

- All HTTP calls run asynchronously (async/await or Task.Run) and must never block the Tick loop.

## Animals (all can talk by text, each with its own personality and view of the world)

- Rat: curious and playful.

- Seven dog breeds, each a distinct character: Husky, Poodle, Pug, Retriever, Rottweiler, German Shepherd, Westie. All track scents, chase their ball and beg for treats.

- Cat: rubs against everyone's legs and wants treats and food.

- Crow: remembers the player and wants to stay close.

- Seagull: the sky's rats. Can be scared away.

- Mountain lion: behaves like an ordinary house cat.

- Fish, sharks, dolphin and whale: always have something they want to ask, such as "what is your name?" or "what time is it?"

## Architecture

- Main.cs: Tick loop, configurable keys from the ini file, finds the nearest Ped in range, opens Game.GetUserInput, shows replies as subtitles or notifications.

- ContextScanner.cs: detects human vs animal (Ped.IsHuman, model hash), weapon state, aiming, wanted level, gunshots, weather, time, area.

- CharacterManager.cs: loads role and mode files, switches modes dynamically.

- IAiProvider.cs and OpenAiCompatibleClient.cs: provider interface and HttpClient implementation.

- IAudioHook.cs and ModBridge.cs: interfaces and events (OnDialogueStarted, OnAiResponseReceived, OnPedStateChanged) so speech-to-text, text-to-speech and other SHVDNE scripts can be added as plugins later.

## Editable role files

- Each role and animal is a plain text file in scripts/PedAndPaw\_AI/roles/. The user writes the personality in their own words, and the AI speaks exactly in that voice.

- Modes and weapon reactions are defined inside each file.

- Example role file, `scripts/PedAndPaw_AI/roles/humans/Police.txt`:

  ```
  Role layer, Police:
  On-duty officer. Professional, alert, authority without cartoon toughness.
  Keep public order in mind. Short, clear speech.
  ```

  The first line names the role layer; the text that follows is the voice the AI must speak in. Modes and weapon reactions are appended as their own labelled sections in the same file. docs/PROFILES.md defines the section names.

- Code lives in src/ and is compiled to scripts/PedAndPaw_AI.dll. GitHub users copy the released DLL into their scripts folder and never need Visual Studio; developers run dotnet build. All code comments in English.

## Delivery in small steps

  1. A clean folder and file structure for a GitHub repository, with a README outline, MIT LICENSE, .gitignore, docs stubs, a CI workflow that builds the DLL, and a fully commented PedAndPaw\_AI.ini covering all settings above.

2. Example role files: two animals (street cat and mountain lion) and one human NPC (police officer), each with several modes and weapon reactions.

3. Complete, commented code for Main.cs and ContextScanner.cs, so we can verify that it compiles cleanly in SHVDNE before writing OpenAiCompatibleClient.cs.
