using NjordWarehouseKeeper.UI;
using HarmonyLib;

namespace NjordWarehouseKeeper.Patches
{
    /// <summary>
    /// After vanilla paints pack slots, overlay favourite stars. Clicks on a
    /// star must not pick up or drag the item underneath.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid))]
    internal static class InventoryGridFavouritePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("UpdateGui")]
        private static void AfterUpdateGui(InventoryGrid __instance)
        {
            InventoryFavouriteStars.Sync(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnLeftDown")]
        private static bool SkipLeftDown()
        {
            return !InventoryFavouriteStars.PointerOverStar();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnRightDown")]
        private static bool SkipRightDown()
        {
            return !InventoryFavouriteStars.PointerOverStar();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnLeftClick")]
        private static bool SkipLeftClick()
        {
            return !InventoryFavouriteStars.PointerOverStar();
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnBeginDrag")]
        private static bool SkipBeginDrag()
        {
            return !InventoryFavouriteStars.PointerOverStar();
        }
    }
}
