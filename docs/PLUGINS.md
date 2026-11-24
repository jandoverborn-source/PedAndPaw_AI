# Plugin Architecture (PedAndPaw_AI)

This document describes how external ScriptHookVDotNet Enhanced (SHVDNE) scripts can integrate with PedAndPaw_AI by subscribing to its events.

---

## Overview

PedAndPaw_AI exposes a **static event bus** called `ModBridge` (namespace `PedAndPaw_AI`). Other SHVDNE scripts can register handlers for three core events. The API version is currently `0.3.0`.

---

## Events

### 1. OnDialogueStarted

Fired when the player opens a dialogue with a pedestrian or animal.

**Signature:** `void DialogueStartedHandler(Ped ped, string roleLayer)`

| Parameter | Type | Description |
|-----------|------|-------------|
| `ped` | `GTA.Ped` | The ped the player is talking to. |
| `roleLayer` | `string` | The role layer name from the ped's profile (e.g., `"Police"`, `"Stray Dog"`). |

---

### 2. OnAiResponseReceived

Fired after the AI provider returns a response for the current dialogue turn.

**Signature:** `void AiResponseReceivedHandler(Ped ped, string response)`

| Parameter | Type | Description |
|-----------|------|-------------|
| `ped` | `GTA.Ped` | The ped that generated the response. |
| `response` | `string` | The AI-generated reply text. |

---

### 3. OnPedStateChanged

Fired when a ped's internal dialogue state transitions.

**Signature:** `void PedStateChangedHandler(Ped ped, PedState oldState, PedState newState)`

| Parameter | Type | Description |
|-----------|------|-------------|
| `ped` | `GTA.Ped` | The ped whose state changed. |
| `oldState` | `PedState` | The state before the change. |
| `newState` | `PedState` | The current state. |

**`PedState` Values:**
- `Idle` (0) — not in dialogue
- `AwaitingPlayer` (1) — dialogue open, waiting for player input
- `QueryingAI` (2) — request sent to LLM, waiting for response
- `Speaking` (3) — AI response received, displaying to player
- `Fleeing` (4) — ped is fleeing (weapon reaction)
- `Aggressive` (5) — ped is attacking (weapon reaction)

---

## How to Subscribe

```csharp
using PedAndPaw_AI;
using GTA;

public class MyPlugin : Script
{
    public MyPlugin()
    {
        // Subscribe to events. PedAndPaw_AI handles subscriber exceptions internally.
        ModBridge.OnDialogueStarted += OnDialogueStarted;
        ModBridge.OnAiResponseReceived += OnAiResponseReceived;
        ModBridge.OnPedStateChanged += OnPedStateChanged;
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

---

## Integration Notes

- **API Version:** The `ModBridge.ApiVersion` constant (currently `"0.3.0"`) is bumped on any signature change.
- **Loading Order:** Your plugin must tolerate the event bus being empty or uninitialized if loaded before PedAndPaw_AI.
- **Safety:** PedAndPaw_AI wraps event invocations in a try-catch block; a throwing subscriber will be logged as a warning and will not crash the main mod.

---

## See Also

- **docs/PROFILES.md** — how role layers are defined.
- **README.md** — installation, configuration, and building from source.
