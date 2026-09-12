# ContentTemplate

Starting point for mods that add new content. It registers one example item and one
example building piece so the whole registration path can be verified before there is
real content to add.

Because it adds content, it must be installed on the server **and** on every client. A
client whose version does not match the server's is refused with a clear message.

## Installation

Install with a mod manager, or drop `ContentTemplate.dll` into `BepInEx/plugins`.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Content

| Name | How to get it |
| --- | --- |
| Example Blade | Forge, level 2 — 8 Bronze, 4 Wood |
| Example Lantern | Hammer, Furniture — 3 Wood, 2 Resin |

## Configuration

`BepInEx/config/com.example.contenttemplate.cfg` is written on first launch. The
server's values are authoritative and are synced to clients.

| Setting | Default | Description |
| --- | --- | --- |
| `General / EnableExampleContent` | `true` | Register the example item and building piece. |
