# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Njord, Warehouse Keeper 1.1.0

Built from commit `42201f4` on
[`cursor/njord-click-drag-020c`](https://github.com/GitMCP/valheim-mods/pull/15),
which the assembly carries in its own version string (`1.1.0+42201f4…`).

Thunderstore package name: **NjordWarehouseKeeper**. Plugin dll:
`NjordWarehouseKeeper.dll`. Source lives in `src/NjordWarehouseKeeper`. GUID:
`com.gitmcp.njord`.

Click a listed item to pick up one stack and drag it, the same as a chest slot.
Ctrl-click takes as much of that item as the pack will hold. Several people can
talk to Njord at the same time; he is not locked to one player.

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

    SHA-256  NjordWarehouseKeeper.dll         4aca1040ee40a1e8753640f5629ee771359f8926949ee63a5c94c47e77fe8f2d
    SHA-256  NjordWarehouseKeeper-1.1.0.zip   05a69f13d0db729e9f481b2b8f51685c6f6a967d6f79f87a2282810da414c0f2
