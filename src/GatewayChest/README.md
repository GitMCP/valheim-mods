# Gateway Chest

A chest that is a window onto every container around it. Build one, open it, and
the items in nearby chests appear together. Take a stack from the list and it
comes out of the chest it actually sits in. Deposit, and it goes to an existing
pile of the same thing, or into the first empty slot the network has.

The hub keeps a few slots of its own only as overflow: if nowhere else will take
what you send, it lands in the Gateway Chest rather than vanishing.

Because it adds a piece, it must be installed on the **server and on every client**.
A client whose version does not match the server's is refused.

## Installation

Drop `GatewayChest.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Building one

| Cost | Where |
| --- | --- |
| 8 Fine wood, 4 Iron, 2 Surtling cores | Hammer, Furniture, near a workbench |

## Using it

Walk up and press use. Your inventory stays on the left. The right-hand panel is
the network: search, category tabs, sort by name, quantity or category, and a
count of used slots across every connected chest.

- **Click a row** to pull that stack into your inventory.
- **Shift-click** an item in your inventory, or press **Deposit**, to send it
  into the network. The hotbar is left alone unless you turn that on in config.
- Hold use on the chest, the same as a vanilla chest, to stack everything that
  will fit.

Chests behind a ward you cannot pass, private chests that are not yours, chests
someone else already has open, other Gateway Chests, and incinerators are left
out of the scan.

## Configuration

`BepInEx/config/com.gitmcp.gatewaychest.cfg` is written on first launch. The
server's values are authoritative and are synced to clients.

| Setting | Default | Description |
| --- | --- | --- |
| `GatewayChest / Radius` | `15` | How far, in metres, a container is still part of the network. |
| `GatewayChest / RequireLineOfSight` | `false` | Only include chests the hub can see. Off so a chest in the next room still counts. |
| `GatewayChest / DepositHotbar` | `false` | Also deposit the first inventory row. |

## How the hub works

Valheim chests already sync through a ZDO: the owner writes the inventory blob,
everyone else loads it. Opening a chest is an RPC that hands ownership to the
player, which is why two people cannot rummage the same box at once.

The Gateway Chest does not copy those stacks into a fake inventory. It reads the
chests that are already loaded around it, lists the live `ItemData`, and when you
take or deposit it calls `Inventory.MoveItemToThis` after `ZNetView.ClaimOwnership`,
the same claim Take All uses. The item changes chest only once, on the peer that
now owns that ZDO.

The vanilla container grid is hidden while the hub is open so you are not looking
at the hub's own overflow slots and thinking that is the whole network. Walking
away still closes it, because the game still thinks that Gateway Chest is the
open container.
