# Prebuilt Bicicreta

A compiled build, put here only so it can be downloaded without installing a .NET SDK.
This branch exists for that download and is not meant to be merged; the source of truth
is `cursor/valheim-mod-scaffold-020c`, where `artifacts/` stays git-ignored.

| File | Use |
| --- | --- |
| `Bicicreta.dll` | Manual install: drop it in `BepInEx/plugins`. |
| `Bicicreta-0.2.0.zip` | Thunderstore layout, for importing into a mod manager. |

Built in Release configuration from commit `8bdf4ea` of
`cursor/valheim-mod-scaffold-020c`. These exact bytes were then loaded into the Valheim
dedicated server under BepInEx, which registered the creature and the hammer piece and
applied all six patches with no exceptions.

The commit is recorded inside the assembly too: the .NET SDK embeds the source revision
in `AssemblyInformationalVersion`, so a build can always be traced back to its source,
and rebuilding at a different commit changes the file even when no code changed.

## What changed in 0.2.0

Fixes for everything reported against 0.1.0: it can be ridden by aiming anywhere on it
and pressing use, it is drawn as a bicycle rather than sitting invisible underground, it
cannot be petted or renamed, and it is silent. It is also bicycle-sized now, so the rider
sits on the frame instead of at lox height and there is no invisible animal to walk into.

## Before it will load

[Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.0 and
[BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
have to be installed. Installing Jötunn with a mod manager pulls BepInEx in with it.
Without Jötunn the plugin is skipped, and BepInEx logs the missing dependency.

Because it adds content, it has to be installed on the server and on every client.
Replacing an older build means deleting the old `Bicicreta.dll` first if a mod manager is
not doing it for you.

## Checking what you downloaded

```
SHA-256  Bicicreta.dll         ced5c41b969a455b7b208bd29d67380e3e731de7c736379f0e05b957b827a9d3
SHA-256  Bicicreta-0.2.0.zip   4a5bf0f597e2e9cd07569c0f155884d5d34aa6000104b081125b39a6d5758dc4
```

See the repo README for how to build one of these yourself, and for the in-game steps to
build and ride the bicycle.
