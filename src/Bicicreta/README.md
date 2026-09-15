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

## How the bicycle works

Valheim has no land vehicle to build on. The only vehicle in the game is the ship, and
riding is implemented entirely for tamed creatures by `Sadle`, which steers a `Character`
through its `MonsterAI`. So the bicycle is a creature that happens to be a bicycle:
cloned from the lox, born tamed and permanently saddled, with its wandering, aggression,
appetite, and meat drops taken away. Mounting, steering, stamina, dismounting, and the
handover of control between players in multiplayer then all come from the game rather
than from this mod.

A hammer piece, cloned from the cart, is how a player gets one: it gives the build
preview and resource cost for free, then replaces itself with the mount.

Four things about a lox do have to be argued with, because a bicycle is not an animal:

- **Riding is not where you would look for it.** `Tameable.Interact` pets, orders, and
  renames, and has no riding branch at all. Riding is a second interactable, `Sadle`,
  on a child object that the player has to aim at directly — fine when that child is a
  visible saddle, useless on a bicycle. `Tameable` also hides that child unless its ZDO
  already says a saddle is fitted. So the mod keeps the saddle fitted and forwards the
  whole bicycle's interaction to it, which also removes petting and renaming.
- **A lox is enormous**: 7.6 m long, and it seats its rider 3.4 m up on a bone inside
  its armature. A bicycle-sized model on an untouched lox leaves the rider floating in
  the air and the player colliding with an animal that is not drawn. `BicicretaGeometry`
  holds the dimensions that the model, the collider, and the seat all work from.
- **A lox is loud.** Its noises are separate effect prefabs spawned from `EffectList`
  fields, not components, so they are dropped by discarding every effect that carries an
  `AudioSource` and keeping the silent ones.
- **A lox breaks things.** It carries a `RunHitDamager`, an `Aoe` that hits whatever it
  runs into, and its `lox_stomp` does 100 chop and 100 pickaxe damage — enough to fell
  trees, break ore and flatten a building. The weapons go, since a bicycle has no reason
  to bite or stomp. Running into things stays, because that is a bicycle behaviour, but
  the damager is told not to hit props or terrain and its 4 m reach is cut to the length
  of the bicycle.

It has no bicycle model yet. That needs a Unity AssetBundle, so for now the lox's
renderers are switched off and a bicycle is assembled out of meshes the game already
ships: two of the cart's wheels, the cart's body narrowed and shrunk between them for a
frame, a wooden chair for the seat, and three boxes of wood for a handlebar. The skeleton
and animator are left untouched, because those are what drives movement.

Fitting the rider needs measurements that are not in the game's data. `Player.AttachStart`
puts the rider's root exactly on the attach point, and a character's root is at their
feet, so the riding pose leaves their weight well behind it: build the seat at the attach
point and the rider sits in front of it. The same goes for the hands, which the handlebar
has to reach to look held. `BicicretaGeometry` records both offsets, measured off
screenshots against the bicycle's own known dimensions, and derives the attach point from
where the seat is rather than the other way round.

The wheels roll as the bicycle travels. That needs no networking: `Character.GetVelocity`
reads the rigidbody on the peer that owns the bicycle and the velocity that owner
publishes to its ZDO on every other peer, so each client works the rotation out for
itself from something the game already sends. The borrowed meshes sit well off to one
side of their own pivots, though — the wheel's is 0.61 m out — so each one hangs inside
an empty at its axle and that is what turns; rotating the mesh itself would swing it
around the bicycle instead of spinning it where it stands.

The handlebar is not the plain T it looks like it should be. The rider's hands come to
rest above their knees, well behind the front of the frame, so a post directly under the
bar would have to rise out of the middle of the frame; instead the post stands on the
front of the frame's body and a short neck carries the bar back to the hands. Every
wooden building piece in the game turns out to be the same unit cube under a different
scale, which is why a pole and a beam are the same mesh here and any box of wood can be
had by asking for one of them at a size.
