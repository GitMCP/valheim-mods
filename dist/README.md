# Prebuilt Bicicreta

A compiled build, put here only so it can be downloaded without installing a .NET SDK.
This branch exists for that download and is not meant to be merged; the source of truth
is `cursor/valheim-mod-scaffold-020c`, where `artifacts/` stays git-ignored.

| File | Use |
| --- | --- |
| `Bicicreta.dll` | Manual install: drop it in `BepInEx/plugins`. |
| `Bicicreta-0.4.0.zip` | Thunderstore layout, for importing into a mod manager. |

Built in Release configuration from commit `bcfca2d` of
`cursor/valheim-mod-scaffold-020c`. These exact bytes were then loaded into the Valheim
dedicated server under BepInEx, which registered the creature and the hammer piece and
applied all six patches with no exceptions.

The commit is recorded inside the assembly too: the .NET SDK embeds the source revision
in `AssemblyInformationalVersion`, so a build can always be traced back to its source,
and rebuilding at a different commit changes the file even when no code changed.

## What changed in 0.4.0

It no longer breaks things. A lox carries `lox_bite` and `lox_stomp`, and the stomp does
100 chop and 100 pickaxe damage, enough to fell trees, break ore and flatten a building;
it also carries a `RunHitDamager` that hits whatever it runs into. The weapons are gone,
since a bicycle has no reason to bite or stomp. Running into things stays, restricted to
living targets, with the reach cut from a lox's 4 m to the length of the bicycle.

0.3.0 gave the bicycle a seat: the vanilla wooden chair, borrowed the same way as the
wheels and frame and placed exactly where the rider sits. Before that the rider sat on
the frame's bare top edge.

It also renamed the config key `UseCartModel` to `UseStandInModel`, since the stand-in
model is no longer just the cart. An existing config file keeps the old key, which is
ignored; the new one is written with its default on first load.

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
SHA-256  Bicicreta.dll         7a246693b5c45d084bc9c1d8869aacae26e468b967c3ac75d21ad859cb954a1d
SHA-256  Bicicreta-0.4.0.zip   9fa774d1727d2bad4f0b0e37e4d7c9fbf80aa20de3330126e648a60705112471
```

See the repo README for how to build one of these yourself, and for the in-game steps to
build and ride the bicycle.
