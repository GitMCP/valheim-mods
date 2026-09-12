# HelloValheim

A minimal BepInEx plugin, used as the starting point for this repo's mods. It logs a
greeting when the local player spawns, which is enough to confirm that BepInEx loaded
the plugin and that Harmony patching is working.

## Installation

Install with a mod manager, or drop `HelloValheim.dll` into `BepInEx/plugins`.

## Configuration

`BepInEx/config/com.gitmcp.hellovalheim.cfg` is written on first launch.

| Setting | Default | Description |
| --- | --- | --- |
| `General / GreetOnSpawn` | `true` | Log a greeting whenever the local player spawns. |
