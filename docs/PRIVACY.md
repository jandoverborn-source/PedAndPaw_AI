# Privacy, children and cloud providers

This page exists because the honest answer to *"does what my child types get sent to a
company?"* depends on one setting: **local or cloud**.

## With a local model (recommended)

```ini
[Ai]
Provider = ollama
BaseUrl = http://localhost:11434/v1
```

The mod talks only to `127.0.0.1`. Ollama runs on the same machine. **Nothing the player
types leaves the computer.** No account, no key, no cost. This is the configuration to
recommend to a parent.

The mod itself has no telemetry. Its only network activity is the configured provider
endpoint, and its only logging is a text file next to the ini (`PedAndPaw_AI.log`).

## With a cloud provider (Gemini or any OpenAI-compatible service)

The text **is** sent to that company. That is what a cloud model is. What can be said
precisely:

| | Free / unpaid tier | Paid tier |
|---|---|---|
| Are prompts and replies sent to the provider? | Yes | Yes |
| May the provider use them to improve its products? | **Yes, per Google's Gemini API terms** | No, per the same terms |
| Does a human ever read them? | Possible on the unpaid tier | Not for product improvement |

Source: [Gemini API Additional Terms of Service](https://ai.google.dev/gemini-api/terms).
Read them yourself before deciding; they change.

What the mod sends per message: the role prose of that character, the situation
(time, weather, street, vehicle, wanted level, weapon state) and the player's own text.
It does not send your name, your account, or anything about your machine.

## Age

- The mod is written for **13+**, with the content rules described in the README.
- A **provider account is not the same thing as the mod.** Google's AI Studio and the
  Gemini API have their own minimum age, and Google accounts have their own rules for
  minors. Those are the adult's responsibility, not the child's.
- Practical rule: **the API key belongs to an adult.** A child playing GTA should be on a
  local model, or on an adult's paid key with the adult aware of what that means.
- Do not build a workflow that requires a minor to create an account with a cloud AI
  provider. Ours does not: the mod works fully offline.

## What the mod never does

- No telemetry, no analytics, no crash reporting to us.
- No key or token written into any file we ship; the key comes from the environment or
  from `PedAndPaw_AI.key`, which is git-ignored and stays on that machine.
- No conversation history written to disk. Memory is per session, in memory only.

## Known limits, stated plainly

- We cannot promise a cloud provider behaves well. We can only tell you what its terms
  say and give you a local option.
- A small local model is weaker than a cloud model. That is the trade: quality for
  privacy and cost.
