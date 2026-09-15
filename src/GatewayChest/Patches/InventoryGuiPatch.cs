using GatewayChest.UI;
using HarmonyLib;
using UnityEngine;

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
        /// Hide that grid — the hub panel sits in the gap between backpack and craft —
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

        [HarmonyPrefix]
        [HarmonyPatch("OnDropOutside")]
        private static bool DepositDragOnPanel()
        {
            return !GatewayPanel.TryDepositDrag();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnSplitOk")]
        private static bool HubSplitOk()
        {
            return !GatewayPanel.HandleSplitOk();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSplitCancel")]
        private static void HubSplitCancel()
        {
            GatewayPanel.ClearSplit();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void KeepOpenWhileSearching()
        {
            if (!GatewayPanel.SearchHasFocus())
            {
                return;
            }

            ZInput.ResetButtonStatus("Use");
            ZInput.ResetButtonStatus("Inventory");
            ZInput.ResetButtonStatus("JoyButtonB");
            ZInput.ResetButtonStatus("JoyButtonY");
        }
    }

    /// <summary>
    /// InventoryGui skips close-keys while chat is focused. Treat the hub search
    /// field the same so typing E does not close the chest.
    /// </summary>
    [HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
    internal static class ChatFocusPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (GatewayPanel.SearchHasFocus())
            {
                __result = true;
            }
        }
    }
}
