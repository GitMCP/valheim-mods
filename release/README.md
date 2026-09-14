# Built packages

Prebuilt plugin binaries, so a build environment is not needed just to play with one.
Everything here is generated from the source in this repository by
`dotnet build src/<Mod> -c Release -t:Package`, and is committed only as a convenience.

Anything in here is a copy of something the build can produce again at any time. If the
two ever disagree, the source is right.

This branch keeps **only the current package**. Older zips are dropped when a new one
is built.

## Hirdman 0.3.4

Built from commit `37a5441` on
[`cursor/hirdman-retainer-fixes-020c`](https://github.com/GitMCP/valheim-mods/pull/8),
which the assembly carries in its own version string (`0.3.4+37a5441…`).

Hire NPC retainers and order them about in plain language. A standing job keeps going:
chop (or mine, or gather) until you say otherwise, take a full bag home to the chests,
mend a worn tool at a bench, and come back.

Hire fresh retainers; anyone hired under 0.2.0 is still the old dvergr prefab.

| File | What it is for |
| --- | --- |
| `Hirdman-0.3.4/Hirdman-0.3.4.zip` | The Thunderstore package: upload it, or import it into r2modman or Gale |
| `Hirdman-0.3.4/Hirdman.dll` | The plugin on its own, for installing by hand |

### Installing the zip

Through a mod manager, use *Import local mod* and pick the zip. It carries its own
dependency list, so BepInEx and Jötunn come along with it.

The same file is what gets uploaded to Thunderstore, which is why it holds `manifest.json`,
`icon.png`, `README.md` and `CHANGELOG.md` alongside the plugin rather than just the plugin.

### Installing the DLL

Drop `Hirdman.dll` into `BepInEx/plugins`. Doing it this way installs no dependencies, so
[BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and
[Jötunn 2.30.0](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) have to be
there already or the plugin will not load.

### Everyone needs it

Hirdman is declared `EveryoneMustHaveMod` with minor-version strictness, so the server and
every player have to run the same 0.3.x build. A client without it cannot connect, which
is deliberate: retainers are real creatures in the world, and a client that did not know
what they were would be sent things it could not build.
