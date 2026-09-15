using StorageHub.UI;
using HarmonyLib;
using UnityEngine;

namespace StorageHub.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Show))]
        private static void ShowHub(Container container)
        {
            if (StorageHubMarker.IsHub(container))
            {
                StorageHubPanel.Open(container);
            }
            else
            {
                StorageHubPanel.Close();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void HideHub()
        {
            StorageHubPanel.Close();
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
            if (StorageHubMarker.OpenHub == null || __instance.m_currentContainer != StorageHubMarker.OpenHub)
            {
                return;
            }

            if (__instance.m_container != null)
            {
                __instance.m_container.gameObject.SetActive(false);
            }

            StorageHubPanel.Tick();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnDropOutside")]
        private static bool DepositDragOnPanel()
        {
            return !StorageHubPanel.TryDepositDrag();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnSplitOk")]
        private static bool HubSplitOk()
        {
            return !StorageHubPanel.HandleSplitOk();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnSplitCancel")]
        private static void HubSplitCancel()
        {
            StorageHubPanel.ClearSplit();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        private static void KeepOpenWhileSearching()
        {
            if (!StorageHubPanel.SearchHasFocus())
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
            if (StorageHubPanel.SearchHasFocus())
            {
                __result = true;
            }
        }
    }
}
