# Njord, Warehouse Keeper

Njord is a warehouse keeper you place with the hammer. Talk to him, and the
items in nearby chests appear together. Take a stack from the list and it comes
out of the chest it actually sits in. Deposit, and it goes to an existing pile
of the same thing, or into the first empty slot the network has.

He keeps no goods of his own. If nowhere nearby will take what you send, it
stays in your pack.

Because he is a buildable piece, the mod must be installed on the **server and
on every client**. A client whose version does not match the server's is refused.

## Installation

Drop `NjordWarehouseKeeper.dll` into `BepInEx/plugins`, or install the zip with a
mod manager. Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Source

The source is on [GitHub](https://github.com/GitMCP/valheim-mods). The mod is
open: if you want to fix something, change how it works, or help maintain it,
open a pull request or an issue. You do not need to ask first.

## Building one

| Cost | Where |
| --- | --- |
| 200 gold coins | Hammer, Furniture |

No workbench, and no other materials. Place him like any furniture. He stands
where you put him, in a leather tunic and pants, and will remark on the stores
now and then the way a vendor does.

## Using it

Walk up and press use. Your inventory stays on the left. Vanilla crafting on the
right is hidden; Njord's recipe panel opens in that slot, and his item list
opens in the center. The cog still opens Preferences. Identical items from
different chests share one row. Hover a row the same way you hover a pack
slot: keep the cursor still for a moment and the vanilla item tooltip appears.

- **Click a row** to pick up one stack and drag it, the same as a chest slot.
  If the stores have enough to fill a complete stack, that is what you pick up,
  not a leftover pile from the first chest that happens to hold some.
- **Ctrl-click a row** to take as much of that item as your pack will hold.
- **Shift-click a row** to pick how many to take.
- **Click the star** on a row to mark that item as a favourite. The Favourites
  category lists only starred items. The star next to the sort buttons filters
  the current category the same way.
- **Click an item in your inventory**, then click the panel (or a row) to
  store it. Dropping on a row stores the dragged item; it does not withdraw.
- **Shift-click** an item in your inventory, or press **Deposit**, to send it
  into the network. The hotbar is left alone unless you turn that on in config.
- **Resupply** fills your pack from nearby chests up to the counts set in Preferences.
- **Quick Stack** deposits and then resupplies in one click.
- **Recipes** sit in the right-hand panel, in place of vanilla crafting. A
  dropdown at the top picks a station (All, Handcraft, Hammer, and every
  crafting bench in the game). Known crafts are listed like the vanilla recipe
  list: compact rows, orange when selected, greyed and at the bottom when the
  nearby chests cannot fully afford them. The right side shows the item, its
  tooltip, and the ingredient slots. **Withdraw** pulls those ingredients into
  your pack; a greyed recipe still takes whatever is there. Hover an ingredient
  to see have / need.
- **The cog** opens client Preferences: skip favourites on Deposit, bind a
  Deposit+Resupply hotkey, and choose which items Resupply should keep, with a
  quantity for each. The resupply list is titled Resupply.
- While in range of Njord (the same radius as the network), the hotkey deposits
  and then resupplies without talking to him.
- Click the search box to type. It keeps focus, so E does not close the panel.
- Hold use, the same as a vanilla chest, to stack everything that will fit.
- **From time to time** Njord merges leftover piles of the same item in nearby
  chests so they occupy as few slots as they can. Full stacks stay put. Chests
  someone already has open are left alone. The interval is in config.

Chests behind a ward you cannot pass, private chests that are not yours, chests
someone else already has open, other keepers, and incinerators are left
out of the scan.

## Configuration

`BepInEx/config/com.gitmcp.njord.cfg` is written on first launch. The
server's values for radius, line of sight, the hotbar, and reorganize are
authoritative and are synced to clients. Favourites, Resupply, and the
deposit-skip toggle are **client-only** and stay on that machine.

| Setting | Default | Description |
| --- | --- | --- |
| `Njord / Radius` | `15` | How far, in metres, a container is still part of Njord's network. |
| `Njord / RequireLineOfSight` | `false` | Only include chests he can see. Off so a chest in the next room still counts. |
| `Njord / DepositHotbar` | `false` | Also deposit the first inventory row. |
| `Njord / Reorganize` | `true` | Merge leftover piles of the same item so they use as few chest slots as possible. |
| `Njord / ReorganizeInterval` | `60` | Seconds between tidy passes (15–1800). |
| `Client / DepositSkipFavourites` | `false` | Deposit leaves starred items in the pack. Also set from the cog. |
| `Client / RestockHotkey` | *(none)* | In range of Njord, Deposit then Resupply. Bound from the cog. |
| `Client / Favourites` | *(empty)* | Starred item keys. Edited from the item list. |
| `Client / Resupply` | *(empty)* | Item keys and counts for the Resupply button. Edited from the cog. |

## How it works

Valheim chests already sync through a ZDO: the owner writes the inventory blob,
everyone else loads it. Opening a chest is an RPC that hands ownership to the
player, which is why two people cannot rummage the same box at once.

Njord does not copy those stacks into a fake inventory. He reads the chests that
are already loaded around him, lists the live `ItemData`, and when you take or
deposit he calls `Inventory.MoveItemToThis` after `ZNetView.ClaimOwnership`,
the same claim Take All uses. The item changes chest only once, on the peer that
now owns that ZDO. Talking to him does not lock him to one player, and does not
steal his ZDO: several people can have the panel open at once. Nearby chests
that someone already has open in the vanilla GUI are still left out of the scan.

When nobody is rummaging a chest, Njord periodically merges leftover piles of
the same item so they take as few slots as they can. Only the peer that owns
his ZDO does that pass, so two clients do not tidy the same hall at once.

Walking away still closes the panel, because the game still treats talking to
Njord as having a container open.
