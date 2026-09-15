using HarmonyLib;

namespace TogetherWeRow.Patches
{
    [HarmonyPatch(typeof(Chair), nameof(Chair.GetHoverText))]
    internal static class ChairPatch
    {
        private static void Postfix(Chair __instance, ref string __result)
        {
            if (!__instance.m_inShip || string.IsNullOrEmpty(__result))
            {
                return;
            }

            var ship = __instance.GetComponentInParent<Ship>();
            if (ship == null || !TogetherWeRowCrew.CanRow(ship, __instance.m_attachPoint))
            {
                return;
            }

            // Too-far already decided; leave it. The success line is rebuilt so the
            // prompt can say Row without depending on which language $piece_use became.
            if (__result.IndexOf("888888", System.StringComparison.Ordinal) >= 0)
            {
                return;
            }

            __result = Localization.instance.Localize(
                __instance.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $togetherwerow_use");
        }
    }
}
