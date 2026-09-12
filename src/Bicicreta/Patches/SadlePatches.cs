using HarmonyLib;

namespace Bicicreta.Patches
{
    /// <summary>
    /// The saddle can be aimed at directly, not only through the body, so it has to read
    /// and behave like the rest of the bicycle rather than like lox tack.
    /// </summary>
    [HarmonyPatch(typeof(Sadle))]
    internal static class SadlePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Sadle.Interact))]
        private static bool NeverRemoveSaddle(Sadle __instance, bool alt, ref bool __result)
        {
            if (!alt || __instance.GetComponentInParent<BicicretaTag>() == null)
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Sadle.GetHoverText))]
        private static bool HoverText(Sadle __instance, ref string __result)
        {
            if (__instance.GetComponentInParent<BicicretaTag>() == null)
            {
                return true;
            }

            __result = BicicretaHover.Text();
            return false;
        }
    }
}
