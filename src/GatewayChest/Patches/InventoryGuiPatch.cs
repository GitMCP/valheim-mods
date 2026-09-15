using GatewayChest.UI;
using HarmonyLib;

namespace GatewayChest.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Show))]
        private static void ShowHub(Container container)
        {
            if (GatewayChestHub.IsHub(container))
            {
                GatewayPanel.Open(container);
            }
            else
            {
                GatewayPanel.Close();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void HideHub()
        {
            GatewayPanel.Close();
        }

        /// <summary>
        /// Vanilla turns the container panel back on every frame while the hub is owned.
        /// Hide that grid — the hub panel is parented next to it, in the same slot —
        /// and keep the hub marked in-use so walking away still closes it.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("UpdateContainer")]
        private static void HideVanillaContainer(InventoryGui __instance)
        {
            if (GatewayChestHub.OpenHub == null || __instance.m_currentContainer != GatewayChestHub.OpenHub)
            {
                return;
            }

            if (__instance.m_container != null)
            {
                __instance.m_container.gameObject.SetActive(false);
            }

            GatewayPanel.Tick();
        }
    }
}
