# Granskningar / reviews

Den här mappen finns för **spårbarhet**: varje extern eller intern granskning av koden
blir en egen Markdown-fil, så att det går att se i efterhand vad som påpekades, vad som
åtgärdades och vad som medvetet inte åtgärdades — och varför.

## Namnkonvention

```
ÅÅÅÅ-MM-DD-<område>-<granskare>.md
```

Exempel: `2026-11-25-modconfig-claude-haiku.md`.

## Vad en granskningsfil innehåller

1. **Vem och när** — granskare, datum, underlag (vilka filer som lästes).
2. **Fyndtabell** — allvarlighet, plats, problem, åtgärd, **status**.
3. **Inte åtgärdat** — och skälet, så att beslutet syns och inte ser ut som en glömska.
4. **Uppföljning** — vad granskningen inte täckte och vad som behöver granskas härnäst.

## Statusar

| Status | Betyder |
|---|---|
| `fixed` | Rätta i kod, med test eller verifiering |
| `not-applicable` | Påpekandet stämmer inte för den här kodbasen |
| `deferred` | Riktigt, men medvetet uppskjutet; skälet står i filen |
| `open` | Riktigt och ännu inte åtgärdat |

Regeln är enkel: **inget fynd får försvinna tyst.** Antingen åtgärdas det, eller så står
det kvar som `deferred`/`open` med en anledning.
