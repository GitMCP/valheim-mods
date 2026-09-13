# Hirdman

Recruit NPC retainers and order them about in plain language.

A *hirdman* was a member of a chieftain's retinue. Plant a muster post, and someone comes
to stand by it. Press Use to send them with you or leave them where they are; say more
than that, and they will work out what you meant.

They fell trees with a real axe from their own inventory and carry the wood, under the
same rules you play by: a stone axe will not bring down a birch for them either. They
fight what attacks them, they will not swing at your buildings, and they stay where you
left them.

## Installation

Drop `Hirdman.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Because it
adds content, it has to be installed on the **server and on every client**.

## Recruiting one

| Cost | Where |
| --- | --- |
| 10 Wood, 5 Leather scraps, 50 Coins | Hammer, Misc, near a workbench |

## Giving orders

Press **Use** on a retainer to toggle between following you and waiting where it stands.

For anything else, type an order in the console (F5):

```
hird go and gather some wood
hird defend the camp
hird stay here
hird follow me
```

There are four things a retainer can be doing — waiting, following, guarding, and
chopping wood — and an order is a sentence that picks one. Common phrasings are
recognised instantly with no model installed at all.

## Using a local language model

Optional, and off by default. With it on, anything the keywords could not place is handed
to a language model running on your own machine, which chooses one of the same four
orders. Nothing is sent over the internet, and nothing is sent to the server: your
machine turns the sentence into an order, and only the order travels.

Install [Ollama](https://ollama.com/) and pull a small model:

```
ollama pull qwen3:4b
```

Then set `Model / Enabled` to `true` in `BepInEx/config/com.gitmcp.hirdman.cfg`.

| Setting | Default | Description |
| --- | --- | --- |
| `Model / Enabled` | `false` | Ask a model about orders keywords could not place. |
| `Model / Endpoint` | `http://127.0.0.1:11434/api/chat` | Where the model is listening. An OpenAI-shaped server is also understood. |
| `Model / Name` | `qwen3:4b` | Which model to ask. |
| `Model / TimeoutSeconds` | `8` | How long to wait before giving up on it. |
| `Model / KeepAlive` | `5m` | How long the model stays in memory between orders. |

Choosing between four orders is a small job, so a small model does it well: Qwen3 4B at
Q4 is about 2.5 GB and answers in a second or two. Valheim wants your graphics card too,
though, so if the game starts stuttering, either set `KeepAlive` to `0` so the model is
unloaded after each order, or run a smaller model on the processor instead and leave the
card to the game. Only the player typing needs a model; friends without one can still
give keyword orders.

## Status

Early. One retainer, four orders, and no window of its own yet — orders are typed in the
console. What a retainer *does* once ordered is the part that is finished.
