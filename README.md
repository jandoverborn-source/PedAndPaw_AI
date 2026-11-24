# PedAndPaw_AI

PedAndPaw_AI is a ScriptHookVDotNet Enhanced mod for Grand Theft Auto V Enhanced that turns the streets of Los Santos into a place for conversation. Talk to pedestrians, police, and paramedics, or chat with the rats, dogs, cats, crows, and even the sharks. Every NPC has its own mood, memory, and personality, and the replies change with the time, weather, and what you're driving or carrying. Borrow a car if they trust you, or go fishing for a sarcastic seagull. There's no violence here: animals can't be hurt, and firearms only work at shooting ranges. Nudity is off by default and can be adjusted in the settings.

---

## 1. Features

- Natural-language dialogue with any pedestrian or animal in GTA V
- Role-based personality system (police, gang members, civilians, pets, wildlife)
- Configurable AI provider: Google Gemini, local LLMs (Ollama, LM Studio), or any OpenAI-compatible API
- Family-friendly preset for peaceful, non-violent play
- Extensible plugin architecture for other SHVDNE scripts
- Per-character profiles stored as plain-text `.txt` files

---

## 2. Requirements

| Requirement | Version / Notes |
|-------------|-----------------|
| GTA V | Enhanced Edition (Steam/Epic/Rockstar) |
| ScriptHookVDotNet Enhanced (SHVDNE) | Latest release |
| .NET Framework | 4.8 (Developer Pack or Targeting Pack) |
| AI Provider | Google Gemini API key **or** local LLM (Ollama/LM Studio) **or** any OpenAI-compatible endpoint |

---

## 3. Installation (Step-by-Step)

1. Download the latest release ZIP from the **Releases** page.
2. Extract the archive.
3. Copy the `scripts/` folder into your GTA V root directory (where `GTA5.exe` lives).
   - The structure must be: `Grand Theft Auto V/scripts/PedAndPaw_AI/`
4. Install ScriptHookVDotNet Enhanced if you haven't already (follow its installer instructions).
5. Launch GTA V. The mod loads automatically with SHVDNE.
6. Press the interact key (default: **E**) to talk to the nearest ped or animal, or **T** to send a text message to nearby civilians, police and emergency services.

---

## 4. Configuration

All settings live in `scripts/PedAndPaw_AI.ini`. The file documents every key; the table below covers the most important ones.

| Key | Section | Default | Description |
|-----|---------|---------|-------------|
| `InteractKey` / `TextMessageKey` | `[Keys]` | `E` / `T` | Talk to or dismiss the nearest ped; send a text message to nearby civilians, police and emergency services. |
| `Provider` | `[Ai]` | `gemini` | One of `gemini`, `ollama`, `openrouter`, `groq`, `custom`. |
| `BaseUrl` | `[Ai]` | `https://generativelanguage.googleapis.com/v1beta/openai/` | OpenAI-compatible chat completions endpoint. |
| `Model` | `[Ai]` | `gemini-3-flash-preview` | Model name for the selected provider. |
| `ApiKeyEnvVar` | `[Ai]` | `PEDANDPAW_AI_KEY` | Name of the environment variable that holds the key. The key itself is never stored in the ini. |
| `Temperature` | `[Ai]` | `0.8` | Sampling temperature, 0.0–1.5. |
| `MaxTokensPerReply` | `[Limits]` | `120` | Upper bound on reply length in tokens. |
| `MaxReplyCharacters` | `[Limits]` | `240` | How many characters of a reply are shown in-game. |
| `NudityPercent` | `[Content]` | `0` | 0 = family-friendly; 100 = the widest the content setting allows. |
| `EnableWeaponReactions` | `[Weapons]` | `true` | Whether weapon state and reactions are part of the dialogue. |
| `NpcSelfAwareness` | `[Behavior]` | `true` | Whether a character may admit, when asked, that it is an NPC. |
| `LogLevel` | `[General]` | `Info` | `None`, `Error`, `Info` or `Debug`. |

> **Security note:** never put a real API key in the ini. Set the environment variable named by `ApiKeyEnvVar` (default `PEDANDPAW_AI_KEY`) instead; the mod reads it at runtime.

---

## 5. Default AI Provider: Google Gemini

PedAndPaw_AI ships with **Google Gemini** as the default provider because it offers a generous free tier. To use it:

1. Get an API key from [Google AI Studio](https://aistudio.google.com/).
2. Set the environment variable named by `ApiKeyEnvVar`, which defaults to `PEDANDPAW_AI_KEY`.
3. (Optional) Change `Model` in the ini to another model your key can reach.

### Local LLM (Ollama / LM Studio)

Run a local model with zero cloud cost and full privacy:

```bash
# Ollama example
ollama run llama3.1:8b
# Then set Provider = ollama, BaseUrl = http://localhost:11434/v1, Model = llama3.1:8b
```

LM Studio works the same way — start the local server and point `BaseUrl` to `http://localhost:1234/v1`.

### Any OpenAI-Compatible API

Set `Provider = custom`, provide `BaseUrl` and the name of the environment variable holding the key, and choose any model the endpoint serves.

---

## 6. Cloud Providers — Cost Warning

Cloud APIs (Gemini, OpenAI, Anthropic, etc.) **charge per token**. Long conversations with many peds can accumulate costs quickly. Monitor your usage dashboard and consider:

- Setting a low `MaxTokensPerReply` (default 120)
- Using `Temperature = 0.5` for shorter replies
- Switching to a local LLM for unlimited free inference

---

## 7. Custom Characters

Create your own personalities by adding `.txt` files under:

```
scripts/PedAndPaw_AI/roles/humans/    # police, gang, civilian, etc.
scripts/PedAndPaw_AI/roles/animals/   # dog, cat, bird, deer, etc.
```

See **docs/PROFILES.md** for the exact file format, role layers, modes, and weapon-reaction sections.

---

## 8. Building from Source

The project consists of a Core library and the main SHVDNE script.

### Building the Project

```bash
dotnet build src/PedAndPaw_AI.csproj -c Release -p:SHVDNE_DIR=/path/to/shvdne
```

The output DLLs are placed in `scripts/`:
- `scripts/PedAndPaw_AI.dll` (main mod)
- `scripts/PedAndPaw_AI.Core.dll` (core library)

### Running Tests

The Core logic checks can be executed without launching the game:

```bash
dotnet run --project tests/CoreChecks
```

---

## 9. Plugins (For Modders)

Other SHVDNE scripts can subscribe to PedAndPaw_AI events. The real API surface is defined in `src/ModBridge.cs` and documented in `docs/PLUGINS.md`.

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnDialogueStarted` | `ModBridge.OnDialogueStarted += ...` <br> `DialogueStartedHandler(Ped ped, string roleLayer)` | Fired when the player opens dialogue with a ped. |
| `OnAiResponseReceived` | `ModBridge.OnAiResponseReceived += ...` <br> `AiResponseReceivedHandler(Ped ped, string response)` | Fired after the AI generates a reply. |
| `OnPedStateChanged` | `ModBridge.OnPedStateChanged += ...` <br> `PedStateChangedHandler(Ped ped, PedState oldState, PedState newState)` | Fired when a ped's internal dialogue state changes. |

The event bus handles subscriber exceptions internally; a throwing subscriber is logged as a warning and will not crash the mod. Your plugin must tolerate the bus being empty if loaded before PedAndPaw_AI. The `ApiVersion` constant (`"0.3.0"`) is bumped on any signature change.

---

## 10. Known Limitations

- **Response latency:** Cloud models can take several seconds to reply. Local models are faster but need decent hardware.
- **Quality varies:** Smaller local models (7B–8B) may hallucinate or break character. Larger models improve coherence.
- **No voice output:** Text-only for now; TTS integration is planned for a future step.
- **SHVDNE dependency:** This mod only works with ScriptHookVDotNet Enhanced, not the legacy SHVDN.

---

## 11. Family-Friendly Preset

For peaceful, child-safe gameplay, set in `PedAndPaw_AI.ini`:

```ini
NudityPercent = 0
```

This ensures:
- No sexual or suggestive content in AI replies
- Every character stays friendly and calm; weapon reactions stay verbal warnings
- All dialogue steers toward peaceful, cooperative interaction

> A character may admit it is an NPC when asked directly (`NpcSelfAwareness`).

---

## 12. License & Contributing

**License:** MIT — see [LICENSE](LICENSE).

**Pull Request Guidelines:**

- One logical change per PR.
- Run `dotnet build` and verify the DLL loads in-game before submitting.
- Follow the existing C# style (4-space indent, braces on new lines).
- Update documentation (README, PROFILES.md, PLUGINS.md) if your change affects user-facing behavior.
- No API keys, no binary blobs, no generated files in source control.

---

*PedAndPaw_AI — because Los Santos deserves a conversation.*
