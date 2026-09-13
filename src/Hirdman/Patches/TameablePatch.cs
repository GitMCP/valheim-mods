using HarmonyLib;

namespace Hirdman.Patches
{
    /// <summary>
    /// Replaces petting with an order.
    ///
    /// <see cref="Tameable.Interact"/> is what pressing Use on a tamed creature runs, and
    /// all it knows how to do is pet, rename and issue the game's own follow-or-wait
    /// command. A retainer is not a pet, so the whole method is taken over: Use toggles
    /// between walking with you and waiting where it stands, which is the order players
    /// give most and the one that should not need typing.
    /// </summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    internal static class TameablePatch
    {
        private static bool Prefix(Tameable __instance, Humanoid user, bool alt, ref bool __result)
        {
            if (__instance.GetComponent<HirdmanTag>() == null)
            {
                return true;
            }

            // Interact is answered on the peer that pressed the key, so this is where the
            // player giving the order actually is.
            var player = user as Player;
            var retainer = __instance.gameObject;

            var order = HirdmanOrder.Read(__instance.m_nview.GetZDO());
            var following = order.Job == HirdmanJob.Follow;

            order.Job = following ? HirdmanJob.Idle : HirdmanJob.Follow;
            order.Anchor = retainer.transform.position;
            if (!following && player != null)
            {
                order.Master = HirdmanOrder.Identify(player);
            }

            if (HirdmanBrain.Give(retainer, order))
            {
                HirdmanSpeech.Say(retainer, order.Acknowledgement());
            }

            // Handled, so nothing gets petted or renamed.
            __result = true;
            return false;
        }
    }
}
