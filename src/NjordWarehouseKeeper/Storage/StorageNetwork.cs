using System.Collections.Generic;
using NjordWarehouseKeeper.Client;
using UnityEngine;

namespace NjordWarehouseKeeper.Storage
{
    /// <summary>
    /// Moves items between the player and the scanned chests.
    ///
    /// Valheim only writes a container ZDO from its owner (<see cref="Container"/>
    /// saves in OnContainerChanged). Opening a chest transfers ownership with an RPC;
    /// Take All then calls <see cref="ZNetView.ClaimOwnership"/> on the client before
    /// moving stacks. The hub does the same claim immediately before
    /// <see cref="Inventory.MoveItemToThis"/>, so the change is saved on this peer and
    /// replicated. Chests already open by someone else are left out of the scan.
    /// </summary>
    internal static class StorageNetwork
    {
        internal static List<IndexedStack> ListItems(Container hub)
        {
            var listed = new List<IndexedStack>();
            var network = StorageScanner.Scan(hub);
            foreach (var chest in network.Chests)
            {
                AddFrom(listed, hub, chest);
            }

            return listed;
        }

        internal static StorageScanner.Network Snapshot(Container hub)
        {
            return StorageScanner.Scan(hub);
        }

        /// <summary>
        /// Sends one stack into an existing pile, then into the first empty slot.
        /// The hub itself is only used if nowhere else will take it.
        /// Returns true when the stack left <paramref name="from"/> entirely.
        /// </summary>
        internal static bool Route(Inventory from, ItemDrop.ItemData item, Container hub, bool allowHub)
        {
            if (from == null || item == null || hub == null)
            {
                return false;
            }

            return RouteAmount(from, item, item.m_stack, hub, allowHub) && !from.ContainsItem(item);
        }

        internal static bool RouteAmount(
            Inventory from,
            ItemDrop.ItemData item,
            int amount,
            Container hub,
            bool allowHub)
        {
            if (from == null || item == null || hub == null || amount <= 0)
            {
                return false;
            }

            amount = Mathf.Min(amount, item.m_stack);
            if (amount <= 0 || !from.ContainsItem(item))
            {
                return false;
            }

            var network = StorageScanner.Scan(hub);
            var left = amount;
            left -= PutIntoStacks(from, item, left, network.Chests);
            if (left > 0 && from.ContainsItem(item))
            {
                left -= PutIntoEmpty(from, item, left, network.Chests);
            }

            if (left > 0 && allowHub && from.ContainsItem(item))
            {
                var hubOnly = new List<Container> { hub };
                left -= PutIntoStacks(from, item, left, hubOnly);
                if (left > 0 && from.ContainsItem(item))
                {
                    left -= PutIntoEmpty(from, item, left, hubOnly);
                }
            }

            return left < amount;
        }

        internal static int DepositAll(Player player, Container hub)
        {
            return DepositAll(player, hub, notify: true);
        }

        internal static int DepositAll(Player player, Container hub, bool notify)
        {
            if (player == null || hub == null)
            {
                return 0;
            }

            var inventory = player.GetInventory();
            if (inventory == null)
            {
                return 0;
            }

            var moving = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            var routed = 0;
            foreach (var item in moving)
            {
                if (item == null || item.m_equipped)
                {
                    continue;
                }

                if (!NjordWarehouseKeeperPlugin.DepositHotbar.Value && item.m_gridPos.y == 0)
                {
                    continue;
                }

                if (ClientPreferences.DepositSkipFavourites.Value &&
                    ClientPreferences.IsFavourite(ItemKey.Of(item)))
                {
                    continue;
                }

                if (Route(inventory, item, hub, allowHub: false))
                {
                    routed++;
                }
            }

            if (notify && routed == 0)
            {
                player.Message(MessageHud.MessageType.Center, "$njord_nospace");
            }

            return routed;
        }

        internal static bool Withdraw(Player player, IndexedStack group, int amount)
        {
            return Withdraw(player, group, amount, notify: true);
        }

        internal static bool Withdraw(Player player, IndexedStack group, int amount, bool notify)
        {
            if (player == null || group == null || amount <= 0)
            {
                return false;
            }

            var dest = player.GetInventory();
            var sample = group.FirstLive();
            if (dest == null || sample == null)
            {
                return false;
            }

            var take = HowManyFit(dest, sample, amount);
            if (take <= 0)
            {
                if (notify)
                {
                    player.Message(MessageHud.MessageType.Center, "$njord_playerfull");
                }

                return false;
            }

            var left = take;
            for (var i = 0; i < group.Parts.Count && left > 0; i++)
            {
                left -= TakeFrom(player, group.Parts[i], left);
            }

            return left < take;
        }

        internal static int Resupply(Player player, Container hub)
        {
            return Resupply(player, hub, notify: true);
        }

        internal static int Resupply(Player player, Container hub, bool notify)
        {
            if (player == null || hub == null)
            {
                return 0;
            }

            var keys = ClientPreferences.ResupplyKeys();
            if (keys.Count == 0)
            {
                if (notify)
                {
                    player.Message(MessageHud.MessageType.Center, "$njord_resupply_none");
                }

                return 0;
            }

            var listed = ListItems(hub);
            var dest = player.GetInventory();
            var moved = 0;
            var blocked = false;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                int want;
                if (!ClientPreferences.TryGetResupply(key, out want) || want <= 0)
                {
                    continue;
                }

                var need = want - ItemLookup.CountIn(dest, key);
                if (need <= 0)
                {
                    continue;
                }

                var group = FindGroup(listed, key);
                if (group == null || group.Quantity <= 0)
                {
                    continue;
                }

                var sample = group.FirstLive();
                if (sample != null && HowManyFit(dest, sample, 1) <= 0)
                {
                    blocked = true;
                    continue;
                }

                if (Withdraw(player, group, need, notify: false))
                {
                    moved++;
                }
            }

            if (notify && moved == 0)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    blocked ? "$njord_playerfull" : "$njord_resupply_none");
            }

            return moved;
        }

        internal static void Restock(Player player, Container hub)
        {
            if (player == null || hub == null)
            {
                return;
            }

            var deposited = DepositAll(player, hub, notify: false);
            var resupplied = Resupply(player, hub, notify: false);
            if (deposited > 0 || resupplied > 0)
            {
                player.Message(MessageHud.MessageType.Center, "$njord_restocked");
                return;
            }

            player.Message(MessageHud.MessageType.Center, "$njord_restock_none");
        }

        internal static int CountBySharedName(List<IndexedStack> listed, string sharedName)
        {
            var n = 0;
            if (listed == null || string.IsNullOrEmpty(sharedName))
            {
                return 0;
            }

            for (var i = 0; i < listed.Count; i++)
            {
                if (listed[i].SharedName == sharedName)
                {
                    n += listed[i].Quantity;
                }
            }

            return n;
        }

        internal static bool CanAffordRecipe(List<IndexedStack> listed, Recipe recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            return CanAffordNeeds(listed, RequirementNeeds(recipe.m_resources), recipe.m_requireOnlyOneIngredient);
        }

        internal static bool CanAffordPiece(List<IndexedStack> listed, Piece piece)
        {
            if (piece == null)
            {
                return false;
            }

            return CanAffordNeeds(listed, RequirementNeeds(piece.m_resources), onlyOne: false);
        }

        internal static bool WithdrawRecipe(Player player, Container hub, Recipe recipe)
        {
            if (player == null || hub == null || recipe == null)
            {
                return false;
            }

            return WithdrawNeeds(
                player,
                hub,
                RequirementNeeds(recipe.m_resources),
                recipe.m_requireOnlyOneIngredient);
        }

        internal static bool WithdrawPiece(Player player, Container hub, Piece piece)
        {
            if (player == null || hub == null || piece == null)
            {
                return false;
            }

            return WithdrawNeeds(player, hub, RequirementNeeds(piece.m_resources), onlyOne: false);
        }

        internal static List<IngredientLine> RecipeIngredients(Recipe recipe, List<IndexedStack> listed)
        {
            return IngredientLines(recipe == null ? null : recipe.m_resources, listed);
        }

        internal static List<IngredientLine> PieceIngredients(Piece piece, List<IndexedStack> listed)
        {
            return IngredientLines(piece == null ? null : piece.m_resources, listed);
        }

        internal readonly struct IngredientLine
        {
            internal IngredientLine(string displayName, int need, int have, Sprite icon)
            {
                DisplayName = displayName;
                Need = need;
                Have = have;
                Icon = icon;
            }

            internal readonly string DisplayName;
            internal readonly int Need;
            internal readonly int Have;
            internal readonly Sprite Icon;
        }

        private static bool CanAffordNeeds(List<IndexedStack> listed, List<Need> needs, bool onlyOne)
        {
            if (needs.Count == 0)
            {
                return false;
            }

            if (onlyOne)
            {
                for (var i = 0; i < needs.Count; i++)
                {
                    if (CountBySharedName(listed, needs[i].SharedName) >= needs[i].Amount)
                    {
                        return true;
                    }
                }

                return false;
            }

            for (var i = 0; i < needs.Count; i++)
            {
                if (CountBySharedName(listed, needs[i].SharedName) < needs[i].Amount)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool WithdrawNeeds(Player player, Container hub, List<Need> needs, bool onlyOne)
        {
            if (player == null || hub == null || needs == null || needs.Count == 0)
            {
                return false;
            }

            var listed = ListItems(hub);
            var complete = CanAffordNeeds(listed, needs, onlyOne);
            if (onlyOne)
            {
                Need pick = null;
                Need fallback = null;
                var fallbackHave = -1;
                for (var i = 0; i < needs.Count; i++)
                {
                    var have = CountBySharedName(listed, needs[i].SharedName);
                    if (have >= needs[i].Amount)
                    {
                        pick = needs[i];
                        break;
                    }

                    if (have > fallbackHave)
                    {
                        fallback = needs[i];
                        fallbackHave = have;
                    }
                }

                if (pick != null)
                {
                    needs = new List<Need> { pick };
                }
                else if (fallback != null && fallbackHave > 0)
                {
                    needs = new List<Need> { fallback };
                }
                else
                {
                    player.Message(MessageHud.MessageType.Center, "$njord_recipe_missing");
                    return false;
                }
            }

            var anyInStores = false;
            for (var i = 0; i < needs.Count; i++)
            {
                if (CountBySharedName(listed, needs[i].SharedName) > 0)
                {
                    anyInStores = true;
                    break;
                }
            }

            if (!anyInStores)
            {
                player.Message(MessageHud.MessageType.Center, "$njord_recipe_missing");
                return false;
            }

            var taken = 0;
            for (var i = 0; i < needs.Count; i++)
            {
                taken += WithdrawShared(player, hub, needs[i].SharedName, needs[i].Amount);
            }

            if (taken <= 0)
            {
                player.Message(MessageHud.MessageType.Center, "$njord_playerfull");
                return false;
            }

            player.Message(
                MessageHud.MessageType.Center,
                complete ? "$njord_recipe_ok" : "$njord_recipe_partial");
            return true;
        }

        private static List<IngredientLine> IngredientLines(Piece.Requirement[] resources, List<IndexedStack> listed)
        {
            var needs = RequirementNeeds(resources);
            var lines = new List<IngredientLine>(needs.Count);
            for (var i = 0; i < needs.Count; i++)
            {
                lines.Add(new IngredientLine(
                    needs[i].DisplayName,
                    needs[i].Amount,
                    CountBySharedName(listed, needs[i].SharedName),
                    needs[i].Icon));
            }

            return lines;
        }

        internal static Container FindHubInRange(Player player)
        {
            if (player == null)
            {
                return null;
            }

            if (NjordWarehouseKeeperMarker.OpenHub != null)
            {
                return NjordWarehouseKeeperMarker.OpenHub;
            }

            var origin = player.transform.position;
            var radius = NjordWarehouseKeeperPlugin.Radius.Value;
            var playerId = Game.instance != null && Game.instance.GetPlayerProfile() != null
                ? Game.instance.GetPlayerProfile().GetPlayerID()
                : 0L;
            Container best = null;
            var bestDist = radius;
            var markers = UnityEngine.Object.FindObjectsByType<NjordWarehouseKeeperMarker>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var i = 0; i < markers.Length; i++)
            {
                var hub = markers[i] == null ? null : markers[i].GetComponent<Container>();
                if (hub == null || hub.m_nview == null || !hub.m_nview.IsValid())
                {
                    continue;
                }

                if (hub.m_checkGuardStone && !PrivateArea.CheckAccess(hub.transform.position, 0f, flash: false))
                {
                    continue;
                }

                if (!hub.CheckAccess(playerId))
                {
                    continue;
                }

                var dist = Vector3.Distance(origin, hub.transform.position);
                if (dist <= bestDist)
                {
                    best = hub;
                    bestDist = dist;
                }
            }

            return best;
        }

        private static IndexedStack FindGroup(List<IndexedStack> listed, string key)
        {
            for (var i = 0; i < listed.Count; i++)
            {
                if (listed[i].Key() == key)
                {
                    return listed[i];
                }
            }

            return null;
        }

        private static List<Need> RequirementNeeds(Piece.Requirement[] resources)
        {
            var needs = new List<Need>();
            if (resources == null)
            {
                return needs;
            }

            for (var i = 0; i < resources.Length; i++)
            {
                var req = resources[i];
                if (req == null || req.m_resItem == null || req.m_resItem.m_itemData == null)
                {
                    continue;
                }

                var amount = req.GetAmount(1);
                if (amount <= 0)
                {
                    continue;
                }

                var data = req.m_resItem.m_itemData;
                var shared = data.m_shared.m_name;
                needs.Add(new Need
                {
                    SharedName = shared,
                    DisplayName = Localization.instance.Localize(shared),
                    Amount = amount,
                    Icon = data.GetIcon(),
                });
            }

            return needs;
        }

        private static int WithdrawShared(Player player, Container hub, string sharedName, int amount)
        {
            if (amount <= 0 || player == null)
            {
                return 0;
            }

            var dest = player.GetInventory();
            if (dest == null)
            {
                return 0;
            }

            var listed = ListItems(hub);
            var left = amount;
            var taken = 0;
            var have = CountPlayerName(dest, sharedName);
            for (var i = 0; i < listed.Count && left > 0; i++)
            {
                var group = listed[i];
                if (group.SharedName != sharedName || group.Quantity <= 0)
                {
                    continue;
                }

                if (!Withdraw(player, group, left, notify: false))
                {
                    continue;
                }

                var now = CountPlayerName(dest, sharedName);
                var got = Mathf.Max(0, now - have);
                if (got <= 0)
                {
                    break;
                }

                taken += got;
                left -= got;
                have = now;
            }

            return taken;
        }

        private static int CountPlayerName(Inventory inventory, string sharedName)
        {
            if (inventory == null || string.IsNullOrEmpty(sharedName))
            {
                return 0;
            }

            var n = 0;
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared != null && item.m_shared.m_name == sharedName)
                {
                    n += item.m_stack;
                }
            }

            return n;
        }

        private sealed class Need
        {
            internal string SharedName;
            internal string DisplayName;
            internal int Amount;
            internal Sprite Icon;
        }

        private static int HowManyFit(Inventory dest, ItemDrop.ItemData sample, int want)
        {
            if (want <= 0 || !dest.CanAddItem(sample, 1))
            {
                return 0;
            }

            if (dest.CanAddItem(sample, want))
            {
                return want;
            }

            var lo = 1;
            var hi = want;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (dest.CanAddItem(sample, mid))
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return lo;
        }

        private static int TakeFrom(Player player, StackPart part, int take)
        {
            var item = part.Live();
            var source = part.Source == null ? null : part.Source.GetInventory();
            var dest = player.GetInventory();
            if (item == null || source == null || dest == null || take <= 0)
            {
                return 0;
            }

            take = Mathf.Min(take, item.m_stack);
            EnsureOwner(part.Source);
            var before = item.m_stack;
            if (take >= item.m_stack)
            {
                dest.MoveItemToThis(source, item);
            }
            else
            {
                var clone = item.Clone();
                clone.m_stack = take;
                if (!dest.AddItem(clone))
                {
                    return 0;
                }

                source.RemoveItem(item, take);
            }

            var after = source.ContainsItem(item) ? item.m_stack : 0;
            return Mathf.Max(0, before - after);
        }

        private static void AddFrom(List<IndexedStack> listed, Container hub, Container chest)
        {
            var inventory = chest == null ? null : chest.GetInventory();
            if (inventory == null)
            {
                return;
            }

            var distance = hub == null ? 0f : Vector3.Distance(hub.transform.position, chest.transform.position);
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null)
                {
                    continue;
                }

                var part = new StackPart
                {
                    Source = chest,
                    Pos = item.m_gridPos,
                    SharedName = item.m_shared.m_name,
                };

                IndexedStack group = null;
                for (var i = 0; i < listed.Count; i++)
                {
                    if (listed[i].SameAs(item))
                    {
                        group = listed[i];
                        break;
                    }
                }

                if (group == null)
                {
                    group = new IndexedStack
                    {
                        SharedName = item.m_shared.m_name,
                        DisplayName = Localization.instance.Localize(item.m_shared.m_name),
                        Quality = item.m_quality,
                        Variant = item.m_variant,
                        WorldLevel = item.m_worldLevel,
                        Category = ItemCategories.Of(item),
                        Icon = item.GetIcon(),
                        Distance = distance,
                    };
                    listed.Add(group);
                }

                group.Parts.Add(part);
                group.Quantity += item.m_stack;
                if (distance < group.Distance)
                {
                    group.Distance = distance;
                }
            }
        }

        private static int PutIntoStacks(
            Inventory from,
            ItemDrop.ItemData item,
            int amount,
            List<Container> chests)
        {
            var moved = 0;
            foreach (var chest in chests)
            {
                if (amount <= 0 || !from.ContainsItem(item) || item.m_stack <= 0)
                {
                    break;
                }

                var inventory = chest.GetInventory();
                if (inventory == null)
                {
                    continue;
                }

                while (amount > 0 && from.ContainsItem(item) && item.m_stack > 0)
                {
                    var existing = inventory.FindFreeStackItem(
                        item.m_shared.m_name,
                        item.m_quality,
                        item.m_worldLevel,
                        item.m_cheated);
                    if (existing == null)
                    {
                        break;
                    }

                    var space = existing.m_shared.m_maxStackSize - existing.m_stack;
                    var n = Mathf.Min(amount, Mathf.Min(space, item.m_stack));
                    if (n <= 0)
                    {
                        break;
                    }

                    var got = MoveAmount(chest, from, item, n, existing.m_gridPos);
                    if (got <= 0)
                    {
                        break;
                    }

                    moved += got;
                    amount -= got;
                }
            }

            return moved;
        }

        private static int PutIntoEmpty(
            Inventory from,
            ItemDrop.ItemData item,
            int amount,
            List<Container> chests)
        {
            var moved = 0;
            foreach (var chest in chests)
            {
                if (amount <= 0 || !from.ContainsItem(item) || item.m_stack <= 0)
                {
                    break;
                }

                var inventory = chest.GetInventory();
                if (inventory == null)
                {
                    continue;
                }

                while (amount > 0 && from.ContainsItem(item) && item.m_stack > 0 && inventory.HaveEmptySlot())
                {
                    var slot = inventory.FindEmptySlot(false);
                    if (slot.x < 0)
                    {
                        break;
                    }

                    var n = Mathf.Min(amount, Mathf.Min(item.m_stack, item.m_shared.m_maxStackSize));
                    if (n <= 0)
                    {
                        break;
                    }

                    var got = MoveAmount(chest, from, item, n, slot);
                    if (got <= 0)
                    {
                        break;
                    }

                    moved += got;
                    amount -= got;
                }
            }

            return moved;
        }

        private static int MoveAmount(
            Container chest,
            Inventory from,
            ItemDrop.ItemData item,
            int amount,
            Vector2i pos)
        {
            if (chest == null || from == null || item == null || amount <= 0)
            {
                return 0;
            }

            var before = item.m_stack;
            EnsureOwner(chest);
            chest.GetInventory().MoveItemToThis(from, item, amount, pos.x, pos.y);
            var after = from.ContainsItem(item) ? item.m_stack : 0;
            return Mathf.Max(0, before - after);
        }

        internal static void EnsureOwner(Container container)
        {
            var view = container == null ? null : container.m_nview;
            if (view == null || !view.IsValid())
            {
                return;
            }

            if (!view.IsOwner())
            {
                view.ClaimOwnership();
            }
        }
    }
}
