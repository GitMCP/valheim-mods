# Changelog

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
`NjordWarehouseKeeper`. GUID `com.gitmcp.storagehub`.
