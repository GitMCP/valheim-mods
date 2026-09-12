# valheim-mods

Valheim mods built on [BepInEx](https://docs.bepinex.dev/) 5 and
[HarmonyX](https://github.com/BepInEx/HarmonyX), with a build that works on Linux,
macOS, and Windows and needs no Visual Studio and no local copy of the game.

Two templates, both verified to load into a running game:

| Project | Use it for |
| --- | --- |
| `src/HelloValheim` | Changing how existing things behave. Plain Harmony patching, no other dependency. |
| `src/ContentTemplate` | Adding items, building pieces, and recipes. Uses [Jötunn](https://valheim-modding.github.io/Jotunn/) and must be installed on the server and every client. |

## Requirements

- .NET SDK 8 or newer (the mods themselves target .NET Framework 4.6.2, which the
  build pulls in as reference assemblies, so no Mono install is needed)
- `curl` and `tar`, for fetching the game reference assemblies

## Getting started

```bash
tools/fetch-game-libs.sh   # populates lib/valheim (~33 MB of reference assemblies)
dotnet build               # builds every mod plus the API explorer
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
Thunderstore manifest on the way into the archive, so the two cannot drift apart.

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
[Info   :ContentTemplate] ContentTemplate 0.1.0 registered its content.
[Info   :Jotunn.Managers.ItemManager] Adding 1 custom items to the ObjectDB
[Info   :Jotunn.Managers.ItemManager] Adding 1 custom recipes to the ObjectDB
[Info   :Jotunn.Managers.PieceManager] Adding 1 custom pieces to the PieceTables
```

A patch that no longer matches its target contributes nothing and logs no error, which
is why `HelloValheim` reports its patch count instead of just "loaded". Jötunn's counts
serve the same purpose for content.

This only exercises code that runs headlessly. Anything touching the local player,
input, or UI still has to be tested in the real client.

### In the real client

1. Install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
   (use the Thunderstore pack, not a BepInEx release — it is preconfigured for Valheim).
2. Set `ModDeployPath` in `Environment.props` and run `dotnet build -t:Deploy`.
3. Launch the game and read `BepInEx/LogOutput.log`.

Deploy on purpose copies only the plugin assembly. Never copy a whole `bin/Debug`
folder into `plugins`; BepInEx tries to load everything it finds there.

## After a Valheim update

Game updates rename and re-sign members, which makes a patch silently stop applying.
Re-run `tools/fetch-game-libs.sh` to refresh `lib/valheim`, rebuild, and fix whatever no
longer compiles. `lib/valheim/SOURCE.txt` records which game build the current
references came from.

## Adding custom art

Content mods need art, and the two kinds cost very differently:

- **Icons, textures, and other images** load from a PNG at runtime. Drop the file in the
  mod's `Assets/` folder, where it is embedded into the dll automatically, and load it
  with `AssetUtils.LoadImage`. Nothing else to install. `ContentTemplate` does this for
  its item icon.
- **Meshes, materials, prefabs, and shaders** have to be built into a Unity AssetBundle,
  which means installing a Unity editor matching the game's engine, currently
  **Unity 6000.0.75f1**. Build the bundle, put it in `Assets/`, and `ExampleAssets` picks
  it up; until then the templates clone vanilla prefabs instead.

Cloning is worth taking seriously as a first step: a weapon cloned from `SwordBronze`
inherits its mesh, animations, and attack data, so a new item with its own name, icon,
recipe, and stats needs no Unity at all.

## Adding a mod

Copy the closer of the two templates, then in the new `.csproj` set `AssemblyName`,
`RootNamespace`, and `Version`; update the plugin GUID, name, and version constants in
the plugin class; update `thunderstore/manifest.json`; and run `dotnet sln add`. Shared
build logic lives in `src/Directory.Build.props` and `src/Directory.Build.targets`, so a
mod's own project file stays a few lines long.
