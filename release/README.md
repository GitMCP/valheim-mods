# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Njord, Warehouse Keeper 1.0.0

Built from commit `931377b` on
[`cursor/storage-hub-favourites-020c`](https://github.com/GitMCP/valheim-mods/pull/14),
which the assembly carries in its own version string (`1.0.0+931377b…`).

Thunderstore package name: **NjordWarehouseKeeper**. Plugin dll:
`NjordWarehouseKeeper.dll`. Tag: `njordwarehousekeeper-v1.0.0`.
Source lives in `src/NjordWarehouseKeeper`. GUID: `com.gitmcp.njord`.

Njord is a warehouse keeper you place with the hammer for 200 gold coins. He
uses the player mesh in a leather tunic and pants, stays where you put him, and
lists items across nearby chests. He has no storage of his own. He greets,
remarks, and nods goodbye the way a vendor does. Look at his torso or head to
talk to him.

| File | What it is for |
| --- | --- |
| `NjordWarehouseKeeper-1.0.0/NjordWarehouseKeeper-1.0.0.zip` | The Thunderstore package: upload it, or import it into r2modman or Gale |
| `NjordWarehouseKeeper-1.0.0/NjordWarehouseKeeper.dll` | The plugin on its own, for installing by hand |

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
server and every player have to run the same 1.0.x build.

### Checksums

    SHA-256  NjordWarehouseKeeper.dll         6c453296d933eca673059504320466c684cf03f079f4b260f35356cc60028bf3
    SHA-256  NjordWarehouseKeeper-1.0.0.zip   19def9bfd72c179b7b19a9eb354dc69955b52f9ea83dab85b854720668e71349
