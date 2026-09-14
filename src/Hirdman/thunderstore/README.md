# Hirdman

Hire NPC retainers and order them about in plain language.

A *hirdman* was a member of a chieftain's retinue. Build an outpost, pay in coins, and
somebody signs on and comes to stand by it. Tell them what you want in your own words and
they will work out which of twelve jobs you meant.

They work under the same rules you play by. They spawn with empty pockets, so an axe
for chopping and a pickaxe for mining have to come from you; they will ask if you send
them to work without one. Hold **Alt + Use** on one of yours to open their pack like a
chest and put the tools in by hand, or drop them at their feet. A stone axe will not
bring down a birch for them either, and they will walk past it to a tree they can
actually cut. Armour they are given is worn and counted, and they look like people
because they use the player model and the player's own animations.

## Installation

Drop `Hirdman.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Because it
adds content, it has to be installed on the **server and on every client**.

## Hiring

| Piece | Cost to build | Where |
| --- | --- | --- |
| Hirdman outpost | 20 Wood, 10 Stone, 8 Leather scraps | Hammer, Misc, near a workbench |
| Muster bell | 10 Wood, 2 Bronze | Hammer, Misc, near a forge |

Press **Use** on the outpost to hire. It costs **100 coins** a head and you may keep
**four** at once; both are server settings. Everyone you hire gets a name, and only you
can call them.

Press **Use** on the bell to call your whole household back to it. Wherever the bell
stands is where home is, so moving the bell moves everybody's idea of home.

## Giving orders

Press **G** near a retainer to open a window and talk to them. Say what you want:

```
follow me
hold this ground
go and fell some trees
scout ahead of me
pick raspberries
dig for copper
tend the field
get something on the fire
hunt boar
put wood in the chest
get iron from the chest
tidy the chests
wait here
you're dismissed
```

Orders about a particular thing can name it — "gather raspberries", "mine copper", "hunt
boar". The name is matched against what the thing is called in the game files, what it
drops, and what that is called on your screen, so whichever you happen to say works. Say
nothing in particular and they take whatever the job applies to.

| Order | What they do |
| --- | --- |
| Wait | Drift around home, stand by the fire, get in your way. The default. |
| Follow | Walk with you. |
| Guard | Hold the ground and fight whatever comes. |
| Chop wood | Fell a tree, split the logs, pick up what falls, then the next tree. |
| Explore | Range around you and uncover ground on **your** map. |
| Gather | Pick berries, mushrooms and herbs. |
| Mine | Break ore deposits and carry the metal. |
| Farm | Sow seeds from your chests, and lift what is ripe. |
| Cook | Put raw food on the fires and take it off before it burns. |
| Hunt | Kill things and collect what they drop. |
| Haul | Pick up what is on the floor, and sort the chests. Name a thing to put it in or take it out: "put wood in the chest", "get iron from the chest". |
| Dismissed | Drop what they are carrying and leave your service. |

Press **Use** on a retainer to tell them to follow or wait — even if they are in the
middle of a job. Hold **Alt + Use** on one of yours to look in their pack. **Crouch +
Use** dismisses them: they drop their things and go.

Work happens where the retainer is standing when you tell it, so "chop wood" and "chop
wood here" are the same order. They range about 24 m from that spot, which is a server
setting.

Hauling sorts by one rule: a thing belongs wherever most of that thing already is. You
never have to declare that the third chest is the wood chest — it becomes the wood chest
as soon as it holds the most wood, and then stays it.

Orders can also be typed in the console (F5) as `hird <order>`, which is the same
vocabulary and useful for testing.

### What exploring can and cannot do

Valheim only simulates the world near a player. A creature in a zone nobody is standing
in does not exist, so no mod can send a companion off to survey a continent. What a scout
does instead is circle out to the edge of what the game keeps alive and back, so walking
anywhere with one in tow reveals a band of map several times wider than walking alone.
What it finds goes on your map only, because exploration is saved with your character.

## Using a local language model

Optional, and off by default. Common phrasings are recognised instantly with no model
installed at all; with a model on, anything the keywords could not place is handed to one
running on your own machine, which picks one of the same twelve orders. Nothing is sent
over the internet, and nothing is sent to the server: your machine turns the sentence into
an order, and only the order travels.

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

Choosing between twelve orders is a small job, so a small model does it well: Qwen3 4B at
Q4 is about 2.5 GB and answers in a second or two. Valheim wants your graphics card too,
though, so if the game starts stuttering, either set `KeepAlive` to `0` so the model is
unloaded after each order, or run a smaller model on the processor instead and leave the
card to the game. Only the player typing needs a model; friends without one can still
give keyword orders.

## Server settings

These are rules of the world rather than preferences, so the server's copy wins and is
pushed to everyone.

| Setting | Default | Description |
| --- | --- | --- |
| `Household / Price` | `100` | Coins to hire one. |
| `Household / Limit` | `4` | How many one player may keep at once. |
| `Household / WorkRadius` | `24` | How far from the work site they range. |
| `Household / ScoutRange` | `48` | How far a scout circles from you. |
| `Household / ScoutSight` | `80` | How much map a scout uncovers around itself. |

`Talking / Key` (default `G`) opens the chat window and is yours alone.

## Status

Working, and rough in the places you would expect of companions built on an animal's AI:
they path like tamed creatures, they will not open a door, and they are best given work
inside a fenced yard. What they do once ordered is the finished part.
