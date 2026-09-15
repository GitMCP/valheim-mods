using NjordWarehouseKeeper.UI;
using HarmonyLib;
using UnityEngine;

namespace NjordWarehouseKeeper.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Show))]
        private static void ShowHub(Container container)
        {
            if (NjordWarehouseKeeperMarker.IsHub(container))
            {
                NjordWarehouseKeeperPanel.Open(container);
            }
            else
            {
                NjordWarehouseKeeperPanel.Close();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void HideHub()
        {
            NjordWarehouseKeeperPanel.Close();
        }

        /// <summary>
        /// Vanilla turns the container panel back on every frame while Njord is in use.
        /// Hide that dummy grid — the panel sits in the gap between backpack and craft —
        /// and keep him marked in-use so walking away still closes it.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("UpdateContainer")]
        private static void HideVanillaContainer(InventoryGui __instance)
        {
            if (NjordWarehouseKeeperMarker.OpenHub == null || __instance.m_currentContainer != NjordWarehouseKeeperMarker.OpenHub)
            {
                return;
            }

            if (__instance.m_container != null)
            {
                __instance.m_container.gameObject.SetActive(false);
            }

            NjordWarehouseKeeperPanel.Tick();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnDropOutside")]
        private static bool DepositDragOnPanel()
        {
            return !NjordWarehouseKeeperPanel.TryDepositDrag();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnSplitOk")]
        private static bool HubSplitOk()
        {
            return !NjordWarehouseKeeperPanel.HandleSplitOk();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSplitCancel")]
        private static void HubSplitCancel()
        {
            NjordWarehouseKeeperPanel.ClearSplit();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void KeepOpenWhileSearching()
        {
            if (!NjordWarehouseKeeperPanel.SearchHasFocus())
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
            if (NjordWarehouseKeeperPanel.SearchHasFocus())
            {
                __result = true;
            }
        }
    }
}
