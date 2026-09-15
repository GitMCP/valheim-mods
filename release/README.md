# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Storage Hub 1.1.0

Built from commit `e4aa23c` on
[`cursor/storage-hub-favourites-020c`](https://github.com/GitMCP/valheim-mods/pull/14),
which the assembly carries in its own version string (`1.1.0+e4aa23c…`).

A black metal chest that lists and moves items across nearby containers. Star
items as favourites, bind a Deposit+Resupply hotkey, withdraw known-recipe
ingredients, and Resupply the pack to the counts set in Preferences. The hub
panel uses Items and Recipes tabs, and Deposit / Resupply / Quick Stack sit
above the filters.

| File | What it is for |
| --- | --- |
| `StorageHub-1.1.0/StorageHub-1.1.0.zip` | The Thunderstore package: upload it, or import it into r2modman or Gale |
| `StorageHub-1.1.0/StorageHub.dll` | The plugin on its own, for installing by hand |

### Installing the zip

Through a mod manager, use *Import local mod* and pick the zip. It carries its own
dependency list, so BepInEx and Jötunn come along with it.

### Installing the DLL

Drop `StorageHub.dll` into `BepInEx/plugins`. Doing it this way installs no
dependencies, so
[BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and
[Jötunn 2.30.0](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) have to be
there already or the plugin will not load.

### Everyone needs it

Storage Hub is declared `EveryoneMustHaveMod` with minor-version strictness, so the
server and every player have to run the same 1.1.x build.
