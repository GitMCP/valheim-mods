using HarmonyLib;

namespace Bicicreta.Patches
{
    /// <summary>
    /// Turns the tamed-animal interaction into a vehicle one.
    ///
    /// <see cref="Tameable.Interact"/> has no riding branch at all: it pets, or it gives
    /// an order, or it renames. Riding is a separate <see cref="Sadle"/> interactable
    /// sitting on a child object, which the player has to aim at directly. On a lox that
    /// child is a saddle you can see; on a bicycle it is nothing, so riding was
    /// effectively unreachable and every click petted the bicycle instead.
    ///
    /// So the whole bicycle answers with the saddle's interaction, and petting, ordering,
    /// and renaming are taken away.
    /// </summary>
    [HarmonyPatch(typeof(Tameable))]
    internal static class TameablePatches
    {
        /// <summary>
        /// <see cref="Tameable"/> keeps "is a saddle fitted" in the ZDO, and its Awake
        /// hides the saddle object unless the flag is already set. A component of ours
        /// cannot reliably win that race, so set the flag and show the saddle here,
        /// straight after the game has made its decision. This runs on every peer, and
        /// only an owner may write to a ZDO, so the local activation is done either way.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void KeepSaddleFitted(Tameable __instance)
        {
            if (__instance.GetComponent<BicicretaTag>() == null)
            {
                return;
            }

            var nview = __instance.m_nview;
            if (nview != null && nview.IsValid() && nview.IsOwner())
            {
                nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
            }

            if (__instance.m_saddle != null)
            {
                __instance.m_saddle.gameObject.SetActive(true);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Tameable.Interact))]
        private static bool RideInsteadOfPetting(
            Tameable __instance, Humanoid user, bool hold, bool alt, ref bool __result)
        {
            var tag = __instance.GetComponent<BicicretaTag>();
            if (tag == null)
            {
                return true;
            }

            __result = false;

            // 'alt' renames an animal and removes a saddle. Neither belongs on a bicycle,
            // and removing the saddle would strand it unrideable with no way to refit one.
            if (hold || alt)
            {
                return false;
            }

            var saddle = tag.Saddle;
            if (saddle == null)
            {
                return false;
            }

            __result = saddle.Interact(user, repeat: false, alt: false);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Tameable.GetHoverText))]
        private static bool HoverText(Tameable __instance, ref string __result)
        {
            if (__instance.GetComponent<BicicretaTag>() == null)
            {
                return true;
            }

            __result = BicicretaHover.Text();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Tameable.GetHoverName))]
        private static bool HoverName(Tameable __instance, ref string __result)
        {
            if (__instance.GetComponent<BicicretaTag>() == null)
            {
                return true;
            }

            __result = BicicretaHover.Name();
            return false;
        }
    }
}
