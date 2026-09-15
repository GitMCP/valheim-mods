using System.Collections.Generic;
using UnityEngine;

namespace GatewayChest.Storage
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

            AddFrom(listed, hub, hub);
            return listed;
        }

        internal static StorageScanner.Network Snapshot(Container hub)
        {
            var network = StorageScanner.Scan(hub);
            var own = hub == null ? null : hub.GetInventory();
            if (own != null)
            {
                network.UsedSlots += own.NrOfItems();
                network.TotalSlots += own.GetWidth() * own.GetHeight();
            }

            return network;
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

        internal static void DepositAll(Player player, Container hub)
        {
            if (player == null || hub == null)
            {
                return;
            }

            var inventory = player.GetInventory();
            if (inventory == null)
            {
                return;
            }

            var moving = new List<ItemDrop.ItemData>(inventory.GetAllItems());
            var routed = 0;
            foreach (var item in moving)
            {
                if (item == null || item.m_equipped)
                {
                    continue;
                }

                if (!GatewayChestPlugin.DepositHotbar.Value && item.m_gridPos.y == 0)
                {
                    continue;
                }

                if (Route(inventory, item, hub, allowHub: true))
                {
                    routed++;
                }
            }

            if (routed == 0)
            {
                player.Message(MessageHud.MessageType.Center, "$gatewaychest_nospace");
            }
        }

        internal static bool Withdraw(Player player, IndexedStack group, int amount)
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
                player.Message(MessageHud.MessageType.Center, "$gatewaychest_playerfull");
                return false;
            }

            var left = take;
            for (var i = 0; i < group.Parts.Count && left > 0; i++)
            {
                left -= TakeFrom(player, group.Parts[i], left);
            }

            return left < take;
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
                        item.m_worldLevel);
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

        private static void EnsureOwner(Container container)
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
