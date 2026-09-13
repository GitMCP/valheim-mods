# Prebuilt Hirdman

A compiled build, put here only so it can be downloaded without installing a .NET SDK.
This branch exists for that download and is not meant to be merged; the source of truth
is `main`, where `artifacts/` stays git-ignored.

| File | Use |
| --- | --- |
| `Hirdman.dll` | Manual install: drop it in `BepInEx/plugins`. |
| `Hirdman-0.1.0.zip` | Thunderstore layout, for importing into a mod manager. |

Built in Release configuration from commit `57d0eb5` of
`cursor/hirdman-npc-mod-020c`. These exact bytes were then loaded into the Valheim
dedicated server under BepInEx, which registered the retainer and the muster post and
applied both patches with no exceptions.

The commit is recorded inside the assembly too: the .NET SDK embeds the source revision
in `AssemblyInformationalVersion`, so a build can always be traced back to its source,
and rebuilding at a different commit changes the file even when no code changed.

## What it does

Plant a **Muster post** (hammer, Misc, near a workbench; 10 Wood, 5 Leather scraps, 50
Coins) and a retainer comes to stand by it.

Press **Use** on it to toggle between following you and waiting where it stands. For
anything else, type an order in the console (F5):

```
hird go and gather some wood
hird defend the camp
hird stay here
hird follow me
```

There are four things a retainer can be doing — waiting, following, guarding and chopping
wood — and an order is a sentence that picks one. Common phrasings are recognised
instantly, with no model installed.

## Using a local model

Optional, off by default, and only needed by the player typing. Install
[Ollama](https://ollama.com/), run `ollama pull qwen3:4b`, then set `Model / Enabled` to
`true` in `BepInEx/config/com.gitmcp.hirdman.cfg`. Anything the keywords could not place
is then handed to the model, which picks one of the same four orders.

Nothing leaves your machine, and nothing extra reaches the server: your machine turns the
sentence into an order, and only the order travels. If the game starts stuttering, the
model and Valheim are fighting over the graphics card — set `Model / KeepAlive` to `0` so
it unloads after each order, or run a smaller model on the processor instead.

## Before it will load

[Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.0 and
[BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
have to be installed. Installing Jötunn with a mod manager pulls BepInEx in with it.

Because it adds content, it has to be installed on the server and on every client.

## Known limits of this build

It has no window of its own yet, which is why orders go in through the console. What a
retainer does once ordered is the finished part.

A retainer is a dvergr, because that is the only humanoid in the game that arrives with
equipment visuals, speech and a humanoid path agent already attached. It is 1.68 m and
looks like a dvergr, not like a viking.

## Checking what you downloaded

```
SHA-256  Hirdman.dll         cc65c907abb3c2ef782aa71dba174c941652eecd0a84343a5af19c02d776a280
SHA-256  Hirdman-0.1.0.zip   456dfb5c1e0ea4be7479ba010c3a2a3b6477abcee30de17ccdc40c8f24e0ffa9
```
