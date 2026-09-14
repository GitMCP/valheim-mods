using HarmonyLib;

namespace Hirdman.Patches
{
    /// <summary>
    /// Binds the inventory window's chest panel to a retainer's pack.
    ///
    /// Vanilla will only show a <see cref="Container"/> there, and a retainer is not one.
    /// While a pack is open the container update is replaced, take-all and stack-all are
    /// aimed at the bag, and closing the window writes it down.
    /// </summary>
    internal static class InventoryGuiPatch
    {
        [HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
        private static class UpdateContainerPatch
        {
            private static bool Prefix(InventoryGui __instance, Player player)
            {
                return HirdmanPack.Draw(__instance, player);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        private static class ShowPatch
        {
            private static void Prefix(Container container)
            {
                // Opening a real chest while a pack is bound would keep drawing the pack
                // over it. The pack yields.
                if (container != null)
                {
                    HirdmanPack.Close();
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static class HidePatch
        {
            private static void Prefix()
            {
                HirdmanPack.Close();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
        private static class TakeAllPatch
        {
            private static bool Prefix(InventoryGui __instance)
            {
                var from = HirdmanPack.Inventory;
                var player = Player.m_localPlayer;
                if (from == null || player == null)
                {
                    return true;
                }

                player.GetInventory().MoveAll(from);
                HirdmanPack.Holder?.GetComponent<HirdmanBrain>()?.Stow();
                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnStackAll")]
        private static class StackAllPatch
        {
            private static bool Prefix()
            {
                var into = HirdmanPack.Inventory;
                var player = Player.m_localPlayer;
                if (into == null || player == null)
                {
                    return true;
                }

                into.StackAll(player.GetInventory());
                HirdmanPack.Holder?.GetComponent<HirdmanBrain>()?.Stow();
                return false;
            }
        }
    }
}
