# Rows

Passenger seats on boats get oars. Sit in one and you help the captain go faster.

The person at the helm still steers and still sets the sail. Everyone else on a
gunwale chair is a rower: each occupied seat adds a full copy of the helm's
paddle in the direction the helm has asked for, including while the sail is up.
One helper doubles the paddle. An empty seat does nothing. Nobody at the helm,
and the oars rest. The seat against the mast has no oar; it is too far from the
water.

The oars are wooden stand-ins until there is a real mesh. They hang in the
water and stroke while you sit and the ship is under way.

Because this changes how a shared ship moves, it has to be installed on the **server
and on every client**.

## Installation

Drop `Rows.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Configuration

`BepInEx/config/com.gitmcp.rows.cfg` is written on first launch. The force is a rule
of the world, so the server's copy wins.

| Setting | Default | Description |
| --- | --- | --- |
| `Rows / Speed` | `1` | How much one helper adds, as a multiple of the helm's paddle. `1` doubles the paddle with a second rower, and still applies with the sail up. |

## Testing

Temporary: open the console (F5) and type `rows 1` to pretend one extra person is
rowing, `rows 2` for two, `rows 0` to clear. There are no dummy models; the hull
just takes the extra paddle. This command will be removed later.
