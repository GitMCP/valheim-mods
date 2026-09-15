# valheim-mods

Valheim mods built on [BepInEx](https://docs.bepinex.dev/) 5 and
[HarmonyX](https://github.com/BepInEx/HarmonyX), with a build that works on Linux,
macOS, and Windows and needs no Visual Studio and no local copy of the game.

| Project | What it is |
| --- | --- |
| [`src/Bicicreta`](src/Bicicreta/README.md) | A buildable, rideable bicycle. |
| [`src/TogetherWeRow`](src/TogetherWeRow/README.md) | Oars on boat seats so extra players help the ship go faster. |
| [`src/GatewayChest`](src/GatewayChest/README.md) | A chest that lists and moves items across nearby containers. |
| [`src/HelloValheim`](src/HelloValheim/README.md) | A minimal plugin kept as the template for mods that only patch existing behavior. |

Each mod's own README has the player-facing description and how that mod works. The rest
of this file is about the repo: building, packaging, and adding another mod.

All four are verified to load into a running game.

## Requirements

- .NET SDK 8 or newer (the mods themselves target .NET Framework 4.6.2, which the
  build pulls in as reference assemblies, so no Mono install is needed)
- `curl` and `tar`, for fetching the game reference assemblies

## Getting started

```bash
dotnet build               # builds every mod plus the API explorer
```

That is the whole setup if Valheim is installed in Steam's default location, which the
build finds on its own. On a machine without the game — CI, or a container like the one
this repo was written in — fetch the reference assemblies first:

```bash
tools/fetch-game-libs.sh   # populates lib/valheim (~33 MB of reference assemblies)
```

### Where the game assemblies come from

Mods have to compile against Valheim's own assemblies (`assembly_valheim.dll` and the
Unity modules). `tools/fetch-game-libs.sh` gets them from the **Valheim Dedicated
Server** (Steam app `896660`), which Steam serves to anonymous logins for free and
which ships the same managed assemblies as the client. That keeps the build
reproducible on a machine that has never had Valheim installed.

Those assemblies are Iron Gate's copyrighted binaries. They live in the git-ignored
`lib/valheim/` and are never committed or redistributed; only our compiled plugin dll is.

To build against a real game install instead, copy `Environment.props.example` to
`Environment.props` (git-ignored) and set `ValheimInstall`.

### Publicized assemblies

Valheim keeps most of its interesting state in private fields. The build runs
[BepInEx.AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) over
the game assemblies, so patches can read private members directly instead of going
through reflection. This only affects compile-time metadata; the game still runs its
own untouched assemblies, and the publicizer emits an `IgnoresAccessChecksTo` shim so
the runtime allows the access.

## Day-to-day commands

| Command | Effect |
| --- | --- |
| `dotnet build` | Build everything |
| `dotnet build src/HelloValheim -t:Deploy` | Copy the plugin into a local BepInEx install (needs `ModDeployPath` in `Environment.props`) |
| `dotnet build src/HelloValheim -t:Package` | Build `artifacts/HelloValheim-<version>.zip`, ready to upload to Thunderstore |

A mod's version is declared once, in its `.csproj`. `-t:Package` stamps it into the
Thunderstore manifest on the way into the archive, so the two cannot drift apart. The
archive gets the plugin, the mod folder's `README.md`, and the `icon.png` and
`manifest.json` from `thunderstore/`, plus a `CHANGELOG.md` if the mod keeps one there.

The archive is only needed to publish, which is optional — a plain `.dll` in
`BepInEx/plugins` is a complete install. Publishing does require a Thunderstore team,
which is what supplies the namespace half of a package identifier; a team can have one
member. Nothing in the repo depends on the team name, since Thunderstore takes it at
upload time, but it is the prefix others would use to depend on the mod:
`<Team>-Bicicreta-<version>`.

## Finding something to patch

Valheim ships no API documentation, so writing a patch starts with finding the exact
signature to match. `tools/ApiExplorer` reads the game assemblies as metadata (nothing
is executed) and prints real declarations, including private ones:

```bash
$ dotnet run --project tools/ApiExplorer -- types "^Player$"
Player  [assembly_valheim]

$ dotnet run --project tools/ApiExplorer -- members Player "^OnSpawned|baseValue"
// Player : Humanoid  [assembly_valheim]
private Int32 m_baseValue;
public Void OnSpawned(Boolean spawnValkyrie);
```

## Testing a mod

### Headless smoke test, no game needed

```bash
tools/run-test-server.sh
```

This installs BepInEx into the dedicated server, installs each mod's Thunderstore
dependencies (read from its manifest, so Jötunn and friends come along), deploys every
mod, and starts the server. It catches the failures a successful build cannot: BepInEx
refusing to load the plugin, a Harmony patch whose target signature no longer matches
the shipped game build, and a clone source or recipe ingredient that no longer exists.
Watch for each mod's own lines:

```
[Info   :HelloValheim] HelloValheim 0.1.0 loaded, 1 method(s) patched.
[Info   :Together We Row] Together We Row 1.0.0 loaded, 3 method(s) patched.
[Info   :Gateway Chest] Gateway Chest 0.1.0 registered its content, 6 method(s) patched.
[Info   :Bicicreta] Hid 3 lox renderer(s) and built a bicycle from 3 'Cart' part(s).
[Info   :Bicicreta] Silenced 13 lox sound source(s).
[Info   :Bicicreta] Bicicreta 0.1.0 registered its content, 6 method(s) patched.
[Info   :Bicicreta] Bicicreta refunds 3 material type(s) when broken.
[Info   :Jotunn.Managers.CreatureManager] Adding 1 custom creatures
[Info   :Jotunn.Managers.PieceManager] Adding 1 custom pieces to the PieceTables
```

A patch that no longer matches its target contributes nothing and logs no error, which
is why `HelloValheim` reports its patch count instead of just "loaded". Jötunn's counts
serve the same purpose for content, and the bicycle reports what it managed to borrow
from vanilla prefabs for the same reason.

This only exercises code that runs headlessly. Riding, and anything else touching the
local player, input, or UI, has to be tested in the real client.

### In the real client

**1. Install the loader and Jötunn.** The easy route is
[r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/): pick Valheim, make a
profile, install [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/),
and BepInEx comes along as a dependency. By hand instead, unzip
[BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
into the folder holding `valheim.exe` and unzip Jötunn into `BepInEx/plugins`. Use the
Thunderstore pack rather than a BepInEx release; only the pack is preconfigured for
Valheim.

**2. Build and deploy.**

```bash
dotnet build src/Bicicreta -t:Deploy
```

A Valheim install in Steam's default location is found automatically, so usually there
is nothing to configure and nothing to fetch. Otherwise, or to deploy into an r2modman
profile instead of the game folder, name the target explicitly:

```bash
dotnet build src/Bicicreta -t:Deploy -p:ModDeployPath="$HOME/.config/r2modmanPlus-local/Valheim/profiles/Default/BepInEx/plugins"
```

Deploy copies only the plugin assembly, on purpose. Never copy a whole `bin/Debug`
folder into `plugins`; BepInEx tries to load everything it finds there.

**3. Launch and confirm it loaded.** Start the game (through r2modman's *Start modded*,
if that is how it was installed) and read `BepInEx/LogOutput.log` for the same
registration lines the headless test prints. `devcommands` is the fastest way in, and it
works in singleplayer and in a world hosted from the client, but not on a dedicated
server.

**4. Get a bicycle.** Turn on the developer console in Settings → Gameplay, which since
patch 0.221.4 replaced the `-console` launch option, then press F5:

```
devcommands
spawn Hammer
debugmode
```

`debugmode` makes building free and drops the workbench requirement, which is enough to
see whether the piece and the mount work. Equip the hammer, find **Bicicreta** in the
**Misc** category, and place it. To check the recipe itself rather than just the mount,
skip `debugmode` and buy it for real — `spawn Wood 20`, `spawn Bronze 8`,
`spawn LeatherScraps 8`, then build a workbench and stand next to it.

**5. Ride it.** Walk up and press the use key. The hover text reads *Ride*, and from
there steering, stamina, and dismounting are the game's own saddled-lox controls.

One thing will look wrong and is known: the bicycle is a kit of borrowed vanilla meshes
rather than a real bicycle mesh, and being a lox underneath, it still moves with lox
walking animation.

## After a Valheim update

Game updates rename and re-sign members, which makes a patch silently stop applying.
Re-run `tools/fetch-game-libs.sh` to refresh `lib/valheim`, rebuild, and fix whatever no
longer compiles. `lib/valheim/SOURCE.txt` records which game build the current
references came from.

## Adding custom art

Content mods need art, and the two kinds cost very differently:

- **Icons, textures, and other images** load from a PNG at runtime. Drop the file in the
  mod's `Assets/` folder, where it is embedded into the dll automatically, and load it
  with `AssetUtils.LoadImage`. Nothing else to install. `src/Bicicreta` does this for its
  build-menu icon.
- **Meshes, materials, prefabs, and shaders** have to be built into a Unity AssetBundle,
  which means installing a Unity editor matching the game's engine, currently
  **Unity 6000.0.75f1**. Build the bundle, put it in `Assets/`, and `BicicretaAssets` picks
  it up; until then the mod borrows vanilla models.

Cloning is worth taking seriously rather than treating as a stopgap: a weapon cloned from
`SwordBronze` inherits its mesh, animations, and attack data, so a new item with its own
name, icon, recipe, and stats needs no Unity at all.

## Adding a mod

Copy the closer of the two templates, then in the new `.csproj` set `AssemblyName`,
`RootNamespace`, and `Version`; update the plugin GUID, name, and version constants in
the plugin class; write a `README.md` in the mod folder; update `thunderstore/manifest.json`;
and run `dotnet sln add`. Shared build logic lives in `src/Directory.Build.props` and
`src/Directory.Build.targets`, so a mod's own project file stays a few lines long.
