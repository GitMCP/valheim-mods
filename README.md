# valheim-mods

Valheim mods built on [BepInEx](https://docs.bepinex.dev/) 5 and
[HarmonyX](https://github.com/BepInEx/HarmonyX), with a build that works on Linux,
macOS, and Windows and needs no Visual Studio and no local copy of the game.

| Project | What it is |
| --- | --- |
| `src/Bicicreta` | Adds a buildable, rideable bicycle. |
| `src/Rows` | Puts oars on boat seats so extra players help the ship go faster. |
| `src/HelloValheim` | A minimal plugin kept as the template for mods that only patch existing behavior. |

All three are verified to load into a running game.

## How the bicycle works

Valheim has no land vehicle to build on. The only vehicle in the game is the ship, and
riding is implemented entirely for tamed creatures by `Sadle`, which steers a `Character`
through its `MonsterAI`. So the bicycle is a creature that happens to be a bicycle:
cloned from the lox, born tamed and permanently saddled, with its wandering, aggression,
appetite, and meat drops taken away. Mounting, steering, stamina, dismounting, and the
handover of control between players in multiplayer then all come from the game rather
than from this mod.

A hammer piece, cloned from the cart, is how a player gets one: it gives the build
preview and resource cost for free, then replaces itself with the mount.

Four things about a lox do have to be argued with, because a bicycle is not an animal:

- **Riding is not where you would look for it.** `Tameable.Interact` pets, orders, and
  renames, and has no riding branch at all. Riding is a second interactable, `Sadle`,
  on a child object that the player has to aim at directly — fine when that child is a
  visible saddle, useless on a bicycle. `Tameable` also hides that child unless its ZDO
  already says a saddle is fitted. So the mod keeps the saddle fitted and forwards the
  whole bicycle's interaction to it, which also removes petting and renaming.
- **A lox is enormous**: 7.6 m long, and it seats its rider 3.4 m up on a bone inside
  its armature. A bicycle-sized model on an untouched lox leaves the rider floating in
  the air and the player colliding with an animal that is not drawn. `BicicretaGeometry`
  holds the dimensions that the model, the collider, and the seat all work from.
- **A lox is loud.** Its noises are separate effect prefabs spawned from `EffectList`
  fields, not components, so they are dropped by discarding every effect that carries an
  `AudioSource` and keeping the silent ones.
- **A lox breaks things.** It carries a `RunHitDamager`, an `Aoe` that hits whatever it
  runs into, and its `lox_stomp` does 100 chop and 100 pickaxe damage — enough to fell
  trees, break ore and flatten a building. The weapons go, since a bicycle has no reason
  to bite or stomp. Running into things stays, because that is a bicycle behaviour, but
  the damager is told not to hit props or terrain and its 4 m reach is cut to the length
  of the bicycle.

It has no bicycle model yet. That needs a Unity AssetBundle, so for now the lox's
renderers are switched off and a bicycle is assembled out of meshes the game already
ships: two of the cart's wheels, the cart's body narrowed and shrunk between them for a
frame, a wooden chair for the seat, and three boxes of wood for a handlebar. The skeleton
and animator are left untouched, because those are what drives movement.

Fitting the rider needs measurements that are not in the game's data. `Player.AttachStart`
puts the rider's root exactly on the attach point, and a character's root is at their
feet, so the riding pose leaves their weight well behind it: build the seat at the attach
point and the rider sits in front of it. The same goes for the hands, which the handlebar
has to reach to look held. `BicicretaGeometry` records both offsets, measured off
screenshots against the bicycle's own known dimensions, and derives the attach point from
where the seat is rather than the other way round.

The wheels roll as the bicycle travels. That needs no networking: `Character.GetVelocity`
reads the rigidbody on the peer that owns the bicycle and the velocity that owner
publishes to its ZDO on every other peer, so each client works the rotation out for
itself from something the game already sends. The borrowed meshes sit well off to one
side of their own pivots, though — the wheel's is 0.61 m out — so each one hangs inside
an empty at its axle and that is what turns; rotating the mesh itself would swing it
around the bicycle instead of spinning it where it stands.

The handlebar is not the plain T it looks like it should be. The rider's hands come to
rest above their knees, well behind the front of the frame, so a post directly under the
bar would have to rise out of the middle of the frame; instead the post stands on the
front of the frame's body and a short neck carries the bar back to the hands. Every
wooden building piece in the game turns out to be the same unit cube under a different
scale, which is why a pole and a beam are the same mesh here and any box of wood can be
had by asking for one of them at a size.

## How the oars work

A Valheim ship already paddles. Slow and Back are the person at the helm sculling with
the rudder; Half and Full are the sail. The chairs on the deck are ordinary furniture
that happen to be on a boat, and sitting in one does nothing to the hull.

`src/Rows` does not add a new control. It counts who is already sat down, other than the
helmsman and anyone on the mast, and adds the same kind of force the paddle already uses,
once per occupied gunwale seat, on the peer that owns the ship. One helper is a second
copy of that paddle, including while the sail is up. That peer is the one already
integrating the rigidbody, so the extra push does not need a second network path.

The oars themselves are scenery. A real oar mesh would need an AssetBundle; until then a
shaft and a blade are two boxes of the wooden pole every client already has loaded. They
are not networked objects. Every peer hangs the same ones locally on each passenger
chair, and they stroke only while that seat is taken and the helm has the ship under way.

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
archive gets the plugin, the manifest, and the `icon.png` and `README.md` beside it in
`thunderstore/`, plus a `CHANGELOG.md` if the mod keeps one.

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
the plugin class; update `thunderstore/manifest.json`; and run `dotnet sln add`. Shared
build logic lives in `src/Directory.Build.props` and `src/Directory.Build.targets`, so a
mod's own project file stays a few lines long.
