# valheim-mods

Valheim mods built on [BepInEx](https://docs.bepinex.dev/) 5 and
[HarmonyX](https://github.com/BepInEx/HarmonyX), with a build that works on Linux,
macOS, and Windows and needs no Visual Studio and no local copy of the game.

`src/HelloValheim` is a minimal working plugin kept as the template for new mods.

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

This installs BepInEx into the dedicated server, deploys every mod, and starts the
server. It catches the two failures a successful build cannot: BepInEx refusing to load
the plugin, and a Harmony patch whose target signature no longer matches the shipped
game build. Watch for the plugin's own line:

```
[Info   :   BepInEx] Loading [HelloValheim 0.1.0]
[Info   :HelloValheim] HelloValheim 0.1.0 loaded, 1 method(s) patched.
```

A patch that no longer matches its target contributes nothing and logs no error, which
is why the plugin reports its patch count instead of just "loaded".

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

## Adding a mod

Copy `src/HelloValheim`, then in the new `.csproj` set `AssemblyName`, `RootNamespace`,
and `Version`; update the plugin GUID, name, and version constants in the plugin class;
update `thunderstore/manifest.json`; and run `dotnet sln add`.

### Jötunn

Mods that add custom items, pieces, prefabs, or localisation usually want
[Jötunn](https://valheim-modding.github.io/Jotunn/), which wraps the registration
boilerplate. Add it with `dotnet add package JotunnLib`, and add
`ValheimModding-Jotunn-2.30.0` to the mod's Thunderstore dependencies. It is not used by
`HelloValheim`, which only needs plain Harmony patching.
