# Bicicreta

Adds a bicycle you can build and ride.

Build one with the hammer (Misc category, near a workbench), and it appears ready to
ride. Aim anywhere on it and press use to mount. Steering, stamina, and dismounting work
exactly as they do for a saddled lox, because underneath that is what the game is being
asked to do.

It cannot be petted, ordered about, or renamed, it is silent, and it is the size of a
bicycle rather than of the animal it is built from.

It will not damage your buildings, trees, ore, carts or ships, whether you ride it into
them or park it next to a fight. It has no attacks at all; the only harm it can do is to
run an enemy over.

Because it adds content, it must be installed on the **server and on every client**. A
client whose version does not match the server's is refused with a clear message.

## Installation

Drop `Bicicreta.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Building one

| Cost | Where |
| --- | --- |
| 10 Wood, 4 Bronze, 4 Leather scraps | Hammer, Misc, near a workbench |

Breaking a bicycle returns half its materials.

## Configuration

`BepInEx/config/com.gitmcp.bicicreta.cfg` is written on first launch. The server's values
are authoritative and are synced to clients.

| Setting | Default | Description |
| --- | --- | --- |
| `Bicicreta / RideSpeed` | `1.6` | Speed multiplier relative to a saddled lox. |
| `Bicicreta / StaminaDrain` | `0.5` | Stamina cost multiplier relative to a saddled lox. |
| `Bicicreta / UseStandInModel` | `true` | Show the borrowed bicycle instead of the lox. |

## Known limitation

It does not yet have a bicycle model. A real one needs a Unity AssetBundle, so for now
the lox is hidden and a bicycle is assembled out of parts the game already ships: two of
the cart's wheels, the cart's body narrowed down for a frame, a wooden chair for the seat,
and three boxes of wood for a handlebar. That makes for a long bicycle, because the wheels
have to stand clear of both the frame and the chair. The wheels do roll as you ride, but
the bicycle still moves with the lox's walking animation underneath, which is what
carries it along.
