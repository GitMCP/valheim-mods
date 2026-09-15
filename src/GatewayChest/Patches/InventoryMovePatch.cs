using GatewayChest.Storage;
using HarmonyLib;

namespace GatewayChest.Patches
{
    /// <summary>
    /// Shift-click and drag-onto-container both end in <see cref="Inventory.MoveItemToThis"/>
    /// with the hub inventory as the destination. Catch that and send the stack into the
    /// network instead of filling the hub's own few slots. If nowhere else will take it,
    /// the original move still runs so the item is not lost.
    /// </summary>
    [HarmonyPatch(typeof(Inventory))]
    internal static class InventoryMovePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData))]
        private static bool RouteWhole(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item)
        {
            return !Handled(__instance, fromInventory, item);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
        private static bool RoutePlaced(Inventory __instance, Inventory fromInventory, ItemDrop.ItemData item)
        {
            return !Handled(__instance, fromInventory, item);
        }

        private static bool Handled(Inventory destination, Inventory from, ItemDrop.ItemData item)
        {
            var hub = GatewayChestHub.OpenHub;
            if (hub == null || destination == null || destination != hub.GetInventory())
            {
                return false;
            }

            return StorageNetwork.Route(from, item, hub, allowHub: false);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.StackAll))]
    internal static class ContainerStackPatch
    {
        private static bool Prefix(Container __instance)
        {
            if (!GatewayChestHub.IsHub(__instance) || Player.m_localPlayer == null)
            {
                return true;
            }

            StorageNetwork.DepositAll(Player.m_localPlayer, __instance);
            return false;
        }
    }
}
