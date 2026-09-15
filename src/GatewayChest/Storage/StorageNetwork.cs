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

            var network = StorageScanner.Scan(hub);
            if (TryFillStacks(from, item, network.Chests) && !from.ContainsItem(item))
            {
                return true;
            }

            if (TryEmptySlots(from, item, network.Chests) && !from.ContainsItem(item))
            {
                return true;
            }

            if (allowHub && from.ContainsItem(item))
            {
                TryMove(hub, from, item);
            }

            return !from.ContainsItem(item);
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

        internal static bool Withdraw(Player player, IndexedStack stack)
        {
            if (player == null || stack == null)
            {
                return false;
            }

            var item = stack.Live();
            var source = stack.Source == null ? null : stack.Source.GetInventory();
            var dest = player.GetInventory();
            if (item == null || source == null || dest == null)
            {
                return false;
            }

            if (!dest.CanAddItem(item, 1))
            {
                player.Message(MessageHud.MessageType.Center, "$gatewaychest_playerfull");
                return false;
            }

            EnsureOwner(stack.Source);
            dest.MoveItemToThis(source, item);
            return true;
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

                listed.Add(new IndexedStack
                {
                    Source = chest,
                    Pos = item.m_gridPos,
                    SharedName = item.m_shared.m_name,
                    DisplayName = Localization.instance.Localize(item.m_shared.m_name),
                    Quantity = item.m_stack,
                    Category = ItemCategories.Of(item),
                    Icon = item.GetIcon(),
                    Distance = distance,
                });
            }
        }

        private static bool TryFillStacks(Inventory from, ItemDrop.ItemData item, List<Container> chests)
        {
            var moved = false;
            foreach (var chest in chests)
            {
                if (!from.ContainsItem(item))
                {
                    return true;
                }

                var inventory = chest.GetInventory();
                if (inventory == null)
                {
                    continue;
                }

                if (inventory.FindFreeStackItem(item.m_shared.m_name, item.m_quality, item.m_worldLevel) == null)
                {
                    continue;
                }

                moved |= TryMove(chest, from, item);
            }

            return moved;
        }

        private static bool TryEmptySlots(Inventory from, ItemDrop.ItemData item, List<Container> chests)
        {
            var moved = false;
            foreach (var chest in chests)
            {
                if (!from.ContainsItem(item))
                {
                    return true;
                }

                var inventory = chest.GetInventory();
                if (inventory == null || !inventory.HaveEmptySlot())
                {
                    continue;
                }

                moved |= TryMove(chest, from, item);
            }

            return moved;
        }

        private static bool TryMove(Container chest, Inventory from, ItemDrop.ItemData item)
        {
            var before = item.m_stack;
            EnsureOwner(chest);
            chest.GetInventory().MoveItemToThis(from, item);
            return !from.ContainsItem(item) || item.m_stack < before;
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
