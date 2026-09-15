# Together We Row

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

Drop `TogetherWeRow.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Configuration

`BepInEx/config/com.gitmcp.togetherwerow.cfg` is written on first launch. The force is a
rule of the world, so the server's copy wins.

| Setting | Default | Description |
| --- | --- | --- |
| `TogetherWeRow / Speed` | `1` | How much one helper adds, as a multiple of the helm's paddle. `1` doubles the paddle with a second rower, and still applies with the sail up. |

## How the oars work

A Valheim ship already paddles. Slow and Back are the person at the helm sculling with
the rudder; Half and Full are the sail. The chairs on the deck are ordinary furniture
that happen to be on a boat, and sitting in one does nothing to the hull.

This mod does not add a new control. It counts who is already sat down, other than the
helmsman and anyone on the mast, and adds the same kind of force the paddle already uses,
once per occupied gunwale seat, on the peer that owns the ship. One helper is a second
copy of that paddle, including while the sail is up. That peer is the one already
integrating the rigidbody, so the extra push does not need a second network path.

The oars themselves are scenery. A real oar mesh would need an AssetBundle; until then a
shaft and a blade are two boxes of the wooden pole every client already has loaded. They
are not networked objects. Every peer hangs the same ones locally on each passenger
chair, and they stroke only while that seat is taken and the helm has the ship under way.
