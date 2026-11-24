# Testing PedAndPaw_AI

Two kinds of test: the ones that run without the game, and the in-game smoke test.

## Without the game

```bash
# Build everything
dotnet build src/Core/PedAndPaw_AI.Core.csproj -c Release
dotnet build src/PedAndPaw_AI.csproj -c Release -p:SHVDNE_DIR=/path/to/shvdne

# Config, role parser, prompts and rate limits (23 checks)
dotnet run --project tests/CoreChecks -c Release

# The locked role-file grammar, including its own self-test
python3 tools/validate_roles.py scripts/PedAndPaw_AI/roles
python3 tools/validate_roles.py --self-test
```

`tests/CoreChecks` needs no game and no API key: it proves the ini defaults, that
the shipped ini parses without warnings, that every grammar violation is rejected
with a line number, what the generated prompts contain, and that the three limits
(cooldown, per-minute cap, daily budget) hold.

## Clean-install test

`tools/test_install.sh` moves the existing mods aside, installs ScriptHookVDotNet
from a pinned release and copies this repository's `scripts/` folder into the game.
Nothing is deleted, and `restore` puts everything back.

```bash
GAME_DIR="/path/to/Grand Theft Auto V Enhanced"

tools/test_install.sh status                    # what is there now
tools/test_install.sh backup --apply            # copy the current files aside
tools/test_install.sh clean --apply             # move non-core mods aside
tools/test_install.sh install-shvdne shvdne.zip --apply
tools/test_install.sh install-mod --apply       # build + copy scripts/
tools/test_install.sh restore <backup-dir> --apply
```

A clean install keeps only the ASI loader and ScriptHookV itself
(`dinput8.dll`, `ScriptHookV.dll`, `MinHook.x64.dll`).

## In-game smoke test

1. Start GTA V Enhanced, load a save, walk up to a pedestrian (within 4 metres).
2. Press **E**: a text box opens. Type a line and press Enter.
   - Expect a subtitle with the placeholder reply from `NullAiProvider`, and a
     ticker notification when `ShowNotifications` is on.
3. Press **T** near a human: same behaviour.
   Targeting an animal with **T** must answer "You cannot send a text message to an animal."
4. Press **Escape**: "Conversation ended."

Then read the logs in the game's `scripts/` folder:

| File | Look for |
|------|----------|
| `PedAndPaw_AI.log` | `PedAndPaw_AI 0.3.0 loaded. enabled=True, roles=3, provider=NullAiProvider, keys=E/T/Escape` |
| `PedAndPaw_AI.log` | `Dialogue started with ped <handle> as <Role> (<Mode>)` |
| `ScriptHookVDotNet.log` | the assembly `PedAndPaw_AI.dll` loading without errors |

## Known limits of this build

- Role choice is deterministic per ped handle, not per model: a police officer can
  receive the cat role until model-specific mapping lands (it needs verified model
  hashes).
- `Game.GetUserInput` blocks the script thread while the text box is open.
- The AI provider is a placeholder; no network call is made yet.

## SHVDNE gotchas found the hard way

- **Never pass `null` as the model filter.** `World.GetNearbyPeds(ped, radius, null)` and
  `World.GetAllPeds(null)` throw `ArgumentNullException: Parameter name: array`, because
  SHVDNE runs `Array.ConvertAll` on it without a null check. Pass an empty `Model[]` instead.
  This crashes inside `Script.DoTick()`, so it kills the tick until the next frame and floods
  the log; wrap key handlers in `try/catch` as a second line of defence.
- Three APIs SHVDNE marks obsolete are replaced here: `GTA.Chrono.GameClock` instead of
  `World.CurrentTimeOfDay`, `Player.Wanted.WantedLevel` instead of `Player.WantedLevel`,
  and `Notification.PostTicker` instead of `Notification.Show`.
- `Script.Tick` runs often: throttle periodic scans with `Game.FrameCount % N` and never
  `await` in it.
