# Changelog

## 1.2.2

Favourite stars on pack slots. Open inventory (Tab, or while talking to Njord)
and click the star on an occupied slot to mark that item the same way the list
star does. Deposit skip and the Favourites filter both see it. Chest slots are
left alone. The preferences toggle is now **Ignore favourite items on deposit**
and defaults to on.

If EpicLoot is installed, listed magic items use that mod's rarity colours on
the row (and unique names stay separate). Item tooltips now draw above the
list so the scroll mask no longer crops them.

Search on both panels clears when Njord closes, so the next talk starts with
every item listed. An X on the right of each search box clears the filter;
typed text stops short of that icon. Favourite stars on pack slots and listed
rows sit in the upper-left corner. The item list sits above the panel trim.
EpicLoot items keep the rarity card colour only; the slot frame sprite is not drawn.
A full store now says so when you drop or shift-click an item, not only when you press Deposit.
The item panel is taller so more rows fit, and the list is clipped inside the wood.

## 1.2.1

Deposit works again on Valheim 1.0.15. `FindFreeStackItem` no longer takes
whether the stack is cheated; the game now merges those piles and marks the
destination if a cheated item lands on it.

## 1.2.0

Talking to Njord no longer shows the vanilla crafting column. A wood panel sits in that slot instead, listing the same Craft-tab recipes each bench would, so Withdraw pulls those ingredients.

**Recipe panel (right)**
- Station dropdown in place of CRAFT / UPGRADE (All, Handcraft, Hammer, and every crafting bench)
- Known recipes listed like the Forge: compact rows, orange when selected, missing ingredients greyed and at the bottom
- Search box under the station dropdown filters by item name
- Each row is that item's craft recipe at the chosen station (upgrade-only recipes are skipped; hammer pieces stay under Hammer)
- Detail view with icon, name, tooltip, and ingredient slots
- **Withdraw** pulls those ingredients into the pack; greyed recipes still take whatever the stores have

**Items panel (center)**
- Items list only; the Items / Recipes tab buttons are gone
- Click-to-drag picks a **complete stack** when nearby chests have enough to fill one, instead of the leftover pile in the first chest scanned

**Auto-reorganize**
- On a timer, Njord merges leftover piles of the same item so they occupy as few chest slots as possible
- Full stacks and chests someone already has open are left alone
- Config: `Njord / Reorganize` (default on) and `Njord / ReorganizeInterval` (default 60s)
- Preferences has a **Reorganize** button that runs the same tidy immediately

Vanilla crafting is hidden only while talking to Njord. Closing the talk, walking away, or opening inventory with Tab restores the default craft panel for handcraft and every bench.

## 1.1.1

Deposit works again on Valheim 1.0.14. The game now refuses to merge cheated
stacks with regular ones, and `FindFreeStackItem` gained that extra argument.

## 1.1.0

Click a listed item to pick up one stack and drag it, the same as a chest slot.
Ctrl-click takes as much of that item as the pack will hold. Several people can
talk to Njord at the same time; he is not locked to one player. On the Recipes
tab, hover a craft to see each ingredient with have / need. Click a greyed-out
row to take whatever of those ingredients the stores have.

## 1.0.0

First release as **Njord, Warehouse Keeper**.

Njord is a warehouse keeper you place with the hammer for 200 gold coins. No
other materials, and no workbench. He uses the player mesh in a leather tunic
and pants, stays where you put him, and talks like a vendor: a greeting when
you walk up, a line now and then while you linger, a goodbye when you leave.
Look at his torso or head to talk to him.

He has no storage of his own. Nearby chests are the stores. Talk to him and
their items appear together. Take a stack from the list and it comes out of
the chest it actually sits in. Deposit, and it goes to an existing pile of the
same thing, or into the first empty slot the network has. If nowhere nearby
will take what you send, it stays in your pack.

Favourites, a client preferences tab, and Resupply. Star an item in the list
to pin it. The Favourites category and the star filter show only those items.
The cog opens client-only settings: skip favourites when depositing everything,
a Deposit+Resupply hotkey used while in range of Njord, and a Resupply list
of items and counts to keep in the pack. The panel uses Items and Recipes
tabs; Items keeps search, categories, and sort, Recipes lists known crafts
and withdraws their ingredients when nearby chests have enough. A station
dropdown filters by All, Handcraft, Hammer, or any crafting station. Deposit,
Resupply, and Quick Stack (deposit then resupply) sit above the filters. Rows
show the vanilla item tooltip after the cursor and the row have stayed still
for a moment, so scrolling the list does not flash tips.

The plugin dll is `NjordWarehouseKeeper.dll`. Thunderstore name
`NjordWarehouseKeeper`. GUID `com.gitmcp.njord`.
