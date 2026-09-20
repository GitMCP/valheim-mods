# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Njord, Warehouse Keeper 1.3.0

Built from commit `ab9208e` on
[`cursor/njord-dress-poses-020c`](https://github.com/GitMCP/valheim-mods/pull/22),
which the assembly carries in its own version string (`1.3.0+ab9208e…`).

Thunderstore package name: **NjordWarehouseKeeper**. Plugin dll:
`NjordWarehouseKeeper.dll`. Source lives in `src/NjordWarehouseKeeper`. GUID:
`com.gitmcp.njord`.

Dress Njord from the item list (visual only; items stay in the chests). Press
**R** while talking to him or hovering him to cycle poses: weapons sheathed,
weapons drawn, sitting, and flexing. His head turns toward the nearest player.
From time to time he fidgets in place without walking off the spot.

| File | What it is for |
| --- | --- |
| `NjordWarehouseKeeper-1.3.0/NjordWarehouseKeeper-1.3.0.zip` | The Thunderstore package: upload it, or import it into r2modman or Gale |
| `NjordWarehouseKeeper-1.3.0/NjordWarehouseKeeper.dll` | The plugin on its own, for installing by hand |

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
server and every player have to run the same 1.3.x build.

### Checksums

    SHA-256  NjordWarehouseKeeper.dll         3ef3e4ad7d3bf04c247f53e9042fdd6eeaa3f8bb5e37506a3050a41f48b7a55a
    SHA-256  NjordWarehouseKeeper-1.3.0.zip   9d51ed2f520c006583671dc5a61f4dc58cec931c1273cd5ad78791bfa510cca4
