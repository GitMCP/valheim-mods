# Prebuilt Bicicreta

A compiled build, put here only so it can be downloaded without installing a .NET SDK.
This branch exists for that download and is not meant to be merged; the source of truth
is `cursor/valheim-mod-scaffold-020c`, where `artifacts/` stays git-ignored.

| File | Use |
| --- | --- |
| `Bicicreta.dll` | Manual install: drop it in `BepInEx/plugins`. |
| `Bicicreta-0.3.0.zip` | Thunderstore layout, for importing into a mod manager. |

Built in Release configuration from commit `f3294fa` of
`cursor/valheim-mod-scaffold-020c`. These exact bytes were then loaded into the Valheim
dedicated server under BepInEx, which registered the creature and the hammer piece and
applied all six patches with no exceptions.

The commit is recorded inside the assembly too: the .NET SDK embeds the source revision
in `AssemblyInformationalVersion`, so a build can always be traced back to its source,
and rebuilding at a different commit changes the file even when no code changed.

## What changed in 0.3.0

The bicycle has a seat: the vanilla wooden chair, borrowed the same way as the wheels
and frame and placed exactly where the rider sits. Before this the rider sat on the
frame's bare top edge.

The config key `UseCartModel` is now `UseStandInModel`, since the stand-in model is no
longer just the cart. An existing config file keeps the old key, which is ignored; the
new one is written with its default on first load.

0.2.0 fixed everything reported against 0.1.0: it can be ridden by aiming anywhere on
it and pressing use, it is drawn as a bicycle rather than sitting invisible underground,
it cannot be petted or renamed, it is silent, and it is bicycle-sized, so the rider sits
on the frame rather than at lox height.

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
SHA-256  Bicicreta.dll         2c46cbe3efc5b5cda06d0a12ce103b5e95626b1924fef45eecb33bf624fc798c
SHA-256  Bicicreta-0.3.0.zip   99d401dba49c7ac081b2638713f8a10ea2713f8ce06206a2b5da3e1c0105da7b
```

See the repo README for how to build one of these yourself, and for the in-game steps to
build and ride the bicycle.
