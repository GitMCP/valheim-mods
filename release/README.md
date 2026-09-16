# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Njord, Warehouse Keeper 1.1.0

Built from commit `1bafab5` on
[`cursor/njord-click-drag-020c`](https://github.com/GitMCP/valheim-mods/pull/15),
which the assembly carries in its own version string (`1.1.0+1bafab5…`).

Thunderstore package name: **NjordWarehouseKeeper**. Plugin dll:
`NjordWarehouseKeeper.dll`. Source lives in `src/NjordWarehouseKeeper`. GUID:
`com.gitmcp.njord`.

Click a listed item to pick up one stack and drag it, the same as a chest slot.
Ctrl-click takes as much of that item as the pack will hold. Several people can
talk to Njord at the same time. On Recipes, hover a craft for have / need of
each ingredient; a greyed-out row still takes whatever the stores have.

| File | What it is for |
| --- | --- |
| `NjordWarehouseKeeper-1.1.0/NjordWarehouseKeeper-1.1.0.zip` | The Thunderstore package: upload it, or import it into r2modman or Gale |
| `NjordWarehouseKeeper-1.1.0/NjordWarehouseKeeper.dll` | The plugin on its own, for installing by hand |

### Installing the zip

Through a mod manager, use *Import local mod* and pick the zip. It carries its own
dependency list, so BepInEx and Jötunn come along with it.

### Installing the DLL

Drop `NjordWarehouseKeeper.dll` into `BepInEx/plugins`. Doing it this way installs no
dependencies, so
[BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and
[Jötunn 2.30.0](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) have to be
there already or the plugin will not load.

### Everyone needs it

The mod is declared `EveryoneMustHaveMod` with minor-version strictness, so the
server and every player have to run the same 1.1.x build.

### Checksums

    SHA-256  NjordWarehouseKeeper.dll         2f2de396c973db15be123c102c8681fe94ca654a336a17b42ac6d67366a2ba58
    SHA-256  NjordWarehouseKeeper-1.1.0.zip   1e29cb4942254e1080ae4dc563947207b6f748756618c74a5edc5bcf7b05c0aa
