# Hirdman

Recruit NPC retainers and order them about in plain language.

A *hirdman* was a member of a chieftain's retinue. Build a muster post, recruit one, and
tell it what to do by talking to it — "go chop some wood", "stay here and guard the
camp", "follow me". It walks off and does the work with a real axe from its own
inventory, under the same rules you play by.

## Installation

Drop `Hirdman.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Because it
adds content, it has to be installed on the **server and on every client**.

## Status

Early. This build has one retainer, three orders, and a keyword parser. The natural
language half is deliberately last: orders are a small fixed set, so a language model
only ever has to choose between them, and the mod works without one.
