# Bicycle

Adds a bicycle you can build and ride.

Build one with the hammer (Misc category, near a workbench), and it appears ready to
ride. Mounting, steering, stamina, and dismounting work exactly as they do for a saddled
lox, because underneath that is what the game is being asked to do.

Because it adds content, it must be installed on the **server and on every client**. A
client whose version does not match the server's is refused with a clear message.

## Installation

Install with a mod manager, or drop `Bicycle.dll` into `BepInEx/plugins`.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Building one

| Cost | Where |
| --- | --- |
| 10 Wood, 4 Bronze, 4 Leather scraps | Hammer, Misc, near a workbench |

Breaking a bicycle returns half its materials.

## Configuration

`BepInEx/config/com.example.bicycle.cfg` is written on first launch. The server's values
are authoritative and are synced to clients.

| Setting | Default | Description |
| --- | --- | --- |
| `Bicycle / RideSpeed` | `1.6` | Speed multiplier relative to a saddled lox. |
| `Bicycle / StaminaDrain` | `0.5` | Stamina cost multiplier relative to a saddled lox. |
| `Bicycle / UseCartModel` | `true` | Show the cart's wheels instead of the lox. |

## Known limitation

It does not yet have a bicycle model. A real one needs a Unity AssetBundle, so for now
the lox is hidden and the cart's wheels are shown in its place.
