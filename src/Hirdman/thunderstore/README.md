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
| Chop wood | Keep felling trees, take a full bag home, mend the axe, come back. |
| Explore | Range around you and uncover ground on **your** map. |
| Gather | Keep picking what grows, take it home when the bag is full. |
| Mine | Keep breaking ore, take the metal home, mend the pickaxe, come back. |
| Farm | Sow seeds from your chests, and lift what is ripe. |
| Cook | Put raw food on the fires and take it off before it burns. |
| Hunt | Kill things and collect what they drop. |
| Haul | Pick up what is on the floor, and sort the chests. Name a thing to put it in or take it out: "put wood in the chest", "get iron from the chest". |
| Dismissed | Drop what they are carrying and leave your service. |

Press **Use** on a retainer to tell them to follow or wait — even if they are in the
middle of a job. Hold **Alt + Use** on one of yours to look in their pack. **Crouch +
Use** dismisses them: they drop their things and go.

Work happens where the retainer is standing when you tell it, so "chop wood" and "chop
wood here" are the same order. They keep at it until you say otherwise: as the nearest
trees go they walk a little further (about as far as a scout ranges), and when the bag
is full they take the wood home to your chests, mend a worn axe at a bench, and come
back. Valheim only simulates the world near a player, so they cannot walk a continent
alone — but they will not stop after the first stand of trees either.

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

## Understanding leftover orders

Common phrasings are recognised instantly with no model involved. Anything the keywords
could not place is handed to a small language model that starts with Valheim, on the
processor, and picks one of the same twelve orders. Nothing is sent over the internet
after the first launch, and nothing is sent to the server: your machine turns the
sentence into an order, and only the order travels.

The first time you open the game with the mod, it fetches about 470 MB into
`BepInEx/config/Hirdman/ear/` (the runtime and the weights). That folder survives
mod updates, so later launches just start the ear. Dedicated servers skip it: they
have nobody speaking.

If you already generated a config from an older build, set `Model / Enabled` to `true`
and leave `Model / Source` as `Bundled`.

| Setting | Default | Description |
| --- | --- | --- |
| `Model / Enabled` | `true` | Ask a model about orders keywords could not place. |
| `Model / Source` | `Bundled` | `Bundled` starts the shipped model with the game. `External` talks to a server you run. |
| `Model / Endpoint` | `http://127.0.0.1:11434/api/chat` | Where an external model is listening. Ignored when Source is Bundled. |
| `Model / Name` | `qwen3:4b` | Which model to ask of an external server. Ignored when Source is Bundled. |
| `Model / TimeoutSeconds` | `15` | How long to wait before giving up on it. |
| `Model / KeepAlive` | `5m` | How long an external Ollama model stays in memory. Ignored when Source is Bundled. |

Choosing between twelve orders is a small job, so the bundled model is small on
purpose: Qwen2.5 0.5B at Q4, running on the CPU so Valheim keeps the graphics card.
Friends without the ear still give keyword orders. To use a model you already run
(Ollama or otherwise), set `Source` to `External` and fill in Endpoint and Name.

## Server settings

These are rules of the world rather than preferences, so the server's copy wins and is
pushed to everyone.

| Setting | Default | Description |
| --- | --- | --- |
| `Household / Price` | `100` | Coins to hire one. |
| `Household / Limit` | `4` | How many one player may keep at once. |
| `Household / WorkRadius` | `24` | How far they look from where they stand. |
| `Household / ScoutRange` | `48` | How far a scout circles, and how far a standing job will walk on from the original spot. |
| `Household / ScoutSight` | `80` | How much map a scout uncovers around itself. |

`Talking / Key` (default `G`) opens the chat window and is yours alone.

## Status

Working, and rough in the places you would expect of companions built on an animal's AI:
they path like tamed creatures, they will not open a door, and they are best given work
inside a fenced yard. What they do once ordered is the finished part.
