# Prebuilt Bicicreta

A compiled build, put here only so it can be downloaded without installing a .NET SDK.
This branch exists for that download and is not meant to be merged; the source of truth
is `cursor/valheim-mod-scaffold-020c`, where `artifacts/` stays git-ignored.

| File | Use |
| --- | --- |
| `Bicicreta.dll` | Manual install: drop it in `BepInEx/plugins`. |
| `Bicicreta-0.1.0.zip` | Thunderstore layout, for importing into a mod manager. |

Built from this branch's source in Release configuration, and verified to load into the
Valheim dedicated server under BepInEx with its creature and hammer piece registered.

## Before it will load

[Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.0 and
[BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
have to be installed. Installing Jötunn with a mod manager pulls BepInEx in with it.
Without Jötunn the plugin is skipped, and BepInEx logs the missing dependency.

Because it adds content, it has to be installed on the server and on every client.

## Checking what you downloaded

```
SHA-256  Bicicreta.dll         f637fbebdce207b323a9b6a84a53cab0bc598548e87d162c2554059262efb840
SHA-256  Bicicreta-0.1.0.zip   bd5d20816a9b43f31c4fa098f812ddc1ce034810bb5abef29433fc70f1efea5c
```

See the repo README for how to build one of these yourself, and for the in-game steps to
build and ride the bicycle.
