using NjordWarehouseKeeper.UI;
using HarmonyLib;
using UnityEngine;

namespace NjordWarehouseKeeper.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class InventoryGuiPatch
    {
        private static bool _openingHub;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(InventoryGui.Show))]
        private static void ShowHubPrefix(InventoryGui __instance, Container container)
        {
            _openingHub = NjordWarehouseKeeperMarker.IsHub(container);
            if (_openingHub)
            {
                NjordWarehouseKeeperRecipes.HideVanillaCrafting();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Show))]
        private static void ShowHub(InventoryGui __instance, Container container)
        {
            _openingHub = false;
            if (NjordWarehouseKeeperMarker.IsHub(container))
            {
                NjordWarehouseKeeperRecipes.HideVanillaCrafting();
                NjordWarehouseKeeperPanel.Open(container);
            }
            else
            {
                NjordWarehouseKeeperPanel.Close();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetupCrafting")]
        private static bool SkipSetupCrafting(InventoryGui __instance)
        {
            if (!HubIsOpen(__instance))
            {
                return true;
            }

            NjordWarehouseKeeperRecipes.HideVanillaCrafting();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateCraftingPanel")]
        private static bool SkipCraftingPanel(InventoryGui __instance)
        {
            if (!HubIsOpen(__instance))
            {
                return true;
            }

            NjordWarehouseKeeperRecipes.HideVanillaCrafting();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateRecipe")]
        private static bool SkipRecipe(InventoryGui __instance)
        {
            return !HubIsOpen(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("UpdateRepair")]
        private static bool SkipRepair(InventoryGui __instance)
        {
            return !HubIsOpen(__instance);
        }

        private static bool HubIsOpen(InventoryGui gui)
        {
            return _openingHub
                || (gui != null && NjordWarehouseKeeperMarker.IsHub(gui.m_currentContainer));
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void HideHub()
        {
            _openingHub = false;
            NjordWarehouseKeeperPanel.Close();
        }

        /// <summary>
        /// Vanilla turns the container panel back on every frame, requires ZDO
        /// ownership to keep it open, and cancels a drag that did not come from
        /// the pack. Njord's dummy grid stays hidden, several people can talk
        /// to him at once (so this client may not own him), and click-to-drag
        /// starts from a nearby chest rather than the pack.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("UpdateContainer")]
        private static bool UpdateHubContainer(InventoryGui __instance, Player player)
        {
            var hub = __instance.m_currentContainer;
            if (!NjordWarehouseKeeperMarker.IsHub(hub))
            {
                return true;
            }

            if (__instance.m_container != null)
            {
                __instance.m_container.gameObject.SetActive(false);
            }

            if (player == null
                || Vector3.Distance(hub.transform.position, player.transform.position) > __instance.m_autoCloseDistance)
            {
                NjordWarehouseKeeperPanel.Close();
                __instance.CloseContainer();
                return false;
            }

            if (__instance.m_firstContainerUpdate)
            {
                __instance.m_firstContainerUpdate = false;
                __instance.m_containerHoldTime = 0f;
                __instance.m_containerHoldState = 0;
            }

            if (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse"))
            {
                __instance.m_containerHoldTime += Time.deltaTime;
                if (__instance.m_containerHoldTime > __instance.m_containerHoldPlaceStackDelay
                    && __instance.m_containerHoldState == 0)
                {
                    hub.StackAll();
                    __instance.m_containerHoldState = 1;
                }
                else if (__instance.m_containerHoldTime
                    > __instance.m_containerHoldPlaceStackDelay + __instance.m_containerHoldExitDelay
                    && __instance.m_containerHoldState == 1)
                {
                    __instance.Hide();
                }
            }
            else if (__instance.m_containerHoldState >= 0)
            {
                __instance.m_containerHoldState = -1;
                __instance.m_waitForContainerStack = false;
            }

            NjordWarehouseKeeperPanel.Tick();
            return false;
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
        private static void KeepOpenWhileSearching(InventoryGui __instance)
        {
            if (HubIsOpen(__instance))
            {
                NjordWarehouseKeeperRecipes.HideVanillaCrafting();
            }

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
