using StorageHub.Storage;
using HarmonyLib;

namespace StorageHub.Patches
{
    /// <summary>
    /// Shift-click and drag-onto-container both end in <see cref="Inventory.MoveItemToThis"/>
    /// with Njord's dummy inventory as the destination. Catch that and send the stack
    /// into nearby chests instead. Njord never keeps the item himself.
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
            var hub = StorageHubMarker.OpenHub;
            if (hub == null || destination == null || destination != hub.GetInventory())
            {
                return false;
            }

            StorageNetwork.Route(from, item, hub, allowHub: false);
            return true;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.StackAll))]
    internal static class ContainerStackPatch
    {
        private static bool Prefix(Container __instance)
        {
            if (!StorageHubMarker.IsHub(__instance) || Player.m_localPlayer == null)
            {
                return true;
            }

            StorageNetwork.DepositAll(Player.m_localPlayer, __instance);
            return false;
        }
    }
}
