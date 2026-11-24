# Plugin Architecture (PedAndPaw_AI)

This document describes how external ScriptHookVDotNet Enhanced (SHVDNE) scripts can integrate with PedAndPaw_AI by subscribing to its events.

---

## Overview

PedAndPaw_AI exposes a **static event bus** called `PluginBus` (namespace `PedAndPaw_AI`). Other SHVDNE scripts can register handlers for three core events. The exact API signatures are **fixed when ModBridge.cs is implemented** — this stub documents the event *names*, *payloads*, and *semantics* so you can plan your integration.

> **Do not implement against this stub.** Wait for ModBridge.cs to publish the real event bus with verified delegate types.

---

## Events

### 1. OnDialogueStarted

Fired when the player opens a dialogue with a pedestrian or animal.

| Property | Type | Description |
|----------|------|-------------|
| `Ped` | `GTA.Ped` | The ped the player is talking to. |
| `RoleLayer` | `string` | The role layer name from the ped's profile (e.g., `"Police"`, `"Stray Dog"`). |

**Use case:** Log interactions, trigger custom UI, suppress other mod behavior during dialogue.

---

### 2. OnAiResponseReceived

Fired after the AI provider returns a response for the current dialogue turn.

| Property | Type | Description |
|----------|------|-------------|
| `Ped` | `GTA.Ped` | The ped that generated the response. |
| `Response` | `string` | The AI-generated reply text (before any post-processing). |

**Use case:** Log conversations, feed responses to a TTS mod, analyze sentiment, trigger animations.

---

### 3. OnPedStateChanged

Fired when a ped's internal dialogue state transitions.

| Property | Type | Description |
|----------|------|-------------|
| `Ped` | `GTA.Ped` | The ped whose state changed. |
| `OldState` | `PedState` | Previous state (enum, defined when ModBridge.cs is implemented). |
| `NewState` | `PedState` | New state (enum, defined when ModBridge.cs is implemented). |

**Typical `PedState` values (subject to step-2 finalization):**
- `Idle` — not in dialogue
- `AwaitingPlayer` — dialogue open, waiting for player input
- `QueryingAI` — request sent to LLM, waiting for response
- `Speaking` — AI response received, displaying to player
- `Fleeing` — ped is fleeing (weapon reaction)
- `Aggressive` — ped is attacking (weapon reaction)

**Use case:** Sync other mod state (e.g., disable wanted level during dialogue, trigger custom animations per state).

---

## How to Subscribe (API Preview)

```csharp
using PedAndPaw_AI;
using GTA;

public class MyPlugin : Script
{
    public MyPlugin()
    {
        // Subscribe once on script init
        PluginBus.OnDialogueStarted += OnDialogueStarted;
        PluginBus.OnAiResponseReceived += OnAiResponseReceived;
        PluginBus.OnPedStateChanged += OnPedStateChanged;
    }

    private void OnDialogueStarted(Ped ped, string roleLayer)
    {
        // Your logic here
    }

    private void OnAiResponseReceived(Ped ped, string response)
    {
        // Your logic here
    }

    private void OnPedStateChanged(Ped ped, PedState oldState, PedState newState)
    {
        // Your logic here
    }
}
```

> **The delegate types, event names, and `PedState` enum are finalized when ModBridge.cs is implemented.** The above is illustrative only.

---

## Loading Order

- PedAndPaw_AI must load **before** your plugin script.
- Make sure your plugin's `Script` constructor runs after the PedAndPaw_AI event bus is initialized. SHVDNE's script load order has not been verified for this project yet; check SHVDNE's own documentation before relying on it, and treat a missing bus as a normal case your plugin must tolerate.

---

## Versioning & Compatibility

- Event signatures are **semantic-versioned**. Breaking changes increment the major version.
- Check the release notes of the PedAndPaw_AI version you built against. This stub promises no runtime version property.

---

## See Also

- **docs/PROFILES.md** — how role layers (passed in `OnDialogueStarted`) are defined.
- **README.md** — installation, configuration, building from source.

---

*Plugin API finalized when ModBridge.cs is implemented. This stub exists for planning purposes only.*