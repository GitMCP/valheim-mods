# Storage Hub

A black metal chest that is a window onto every container around it. Build one,
open it, and the items in nearby chests appear together. Take a stack from the
list and it comes out of the chest it actually sits in. Deposit, and it goes to
an existing pile of the same thing, or into the first empty slot the network has.

The hub keeps a few slots of its own only as overflow: if nowhere else will take
what you send, it lands in the Storage Hub rather than vanishing.

Because it adds a piece, it must be installed on the **server and on every client**.
A client whose version does not match the server's is refused.

## Installation

Drop `StorageHub.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Building one

| Cost | Where |
| --- | --- |
| 10 Fine wood, 2 Iron, 2 Surtling cores | Hammer, Furniture, near a workbench |

The piece uses the vanilla black metal chest model.

## Using it

Walk up and press use. Your inventory stays on the left and crafting on the
right. The hub panel opens in the center, between them, with search, category
tabs, sort, and a count of used slots across every connected chest. Identical
items from different chests share one row.

- **Click a row** to pull that item into your inventory (as much as will fit).
- **Shift-click a row** to pick how many to take.
- **Click the star** on a row to mark that item as a favourite. The Favourites
  category lists only starred items. The star next to the sort buttons filters
  the current category the same way.
- **Click an item in your inventory**, then click the hub panel (or a row) to
  store it. Dropping on a row stores the dragged item; it does not withdraw.
- **Shift-click** an item in your inventory, or press **Deposit**, to send it
  into the network. The hotbar is left alone unless you turn that on in config.
- **Resupply** fills your pack from the hub up to the counts set in Preferences.
- **Recipe** lists crafts you know. Rows the hub cannot afford are greyed out.
  Click a row to pull that recipe's ingredients into your pack.
- **The cog** opens client Preferences: skip favourites on Deposit, bind a
  Deposit+Resupply hotkey, and choose which items Resupply should keep, with a
  quantity for each. The resupply list is titled Resupply.
- While in range of a hub (the same radius as the network), the hotkey deposits
  and then resupplies without opening the chest.
- Click the search box to type. It keeps focus, so E does not close the panel.
- Hold use on the chest, the same as a vanilla chest, to stack everything that
  will fit.

Chests behind a ward you cannot pass, private chests that are not yours, chests
someone else already has open, other Storage Hubs, and incinerators are left
out of the scan.

## Configuration

`BepInEx/config/com.gitmcp.storagehub.cfg` is written on first launch. The
server's values for radius, line of sight, and the hotbar are authoritative and
are synced to clients. Favourites, Resupply, and the deposit-skip toggle are
**client-only** and stay on that machine.

| Setting | Default | Description |
| --- | --- | --- |
| `StorageHub / Radius` | `15` | How far, in metres, a container is still part of the network. |
| `StorageHub / RequireLineOfSight` | `false` | Only include chests the hub can see. Off so a chest in the next room still counts. |
| `StorageHub / DepositHotbar` | `false` | Also deposit the first inventory row. |
| `Client / DepositSkipFavourites` | `false` | Deposit leaves starred items in the pack. Also set from the hub cog. |
| `Client / RestockHotkey` | *(none)* | In range of a hub, Deposit then Resupply. Bound from the hub cog. |
| `Client / Favourites` | *(empty)* | Starred item keys. Edited from the hub list. |
| `Client / Resupply` | *(empty)* | Item keys and counts for the Resupply button. Edited from the hub cog. |

## How the hub works

Valheim chests already sync through a ZDO: the owner writes the inventory blob,
everyone else loads it. Opening a chest is an RPC that hands ownership to the
player, which is why two people cannot rummage the same box at once.

The Storage Hub does not copy those stacks into a fake inventory. It reads the
chests that are already loaded around it, lists the live `ItemData`, and when you
take or deposit it calls `Inventory.MoveItemToThis` after `ZNetView.ClaimOwnership`,
the same claim Take All uses. The item changes chest only once, on the peer that
now owns that ZDO.

The vanilla container grid is hidden while the hub is open so you are not looking
at the hub's own overflow slots and thinking that is the whole network. Walking
away still closes it, because the game still thinks that Storage Hub is the
open container.
