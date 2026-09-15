# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Rows 0.2.0

Built from commit `eea37bf` on
[`cursor/rows-mod-020c`](https://github.com/GitMCP/valheim-mods/pull/9),
which the assembly carries in its own version string (`0.2.0+eea37bf…`).

Passenger seats on boats get oars. Sit in one and you help the captain go faster.
One extra rower doubles the helm's paddle, including while the sail is up. Oars
dip into the water. The mast seat is left alone.

| File | What it is for |
| --- | --- |
| `Rows-0.2.0/Rows-0.2.0.zip` | The Thunderstore package: upload it, or import it into r2modman or Gale |
| `Rows-0.2.0/Rows.dll` | The plugin on its own, for installing by hand |

### Installing the zip

Through a mod manager, use *Import local mod* and pick the zip. It carries its own
dependency list, so BepInEx and Jötunn come along with it.

### Installing the DLL

Drop `Rows.dll` into `BepInEx/plugins`. Doing it this way installs no dependencies, so
[BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and
[Jötunn 2.30.0](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) have to be
there already or the plugin will not load.

### Everyone needs it

Rows is declared `EveryoneMustHaveMod` with minor-version strictness, so the server and
every player have to run the same 0.2.x build.
