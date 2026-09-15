using HarmonyLib;
using UnityEngine;

namespace TogetherWeRow.Patches
{
    [HarmonyPatch(typeof(Ship))]
    internal static class ShipPatch
    {
        /// <summary>
        /// The oars are local scenery, so they are hung on the instance rather than
        /// written into the prefab. Every peer does this for itself.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void HangOars(Ship __instance)
        {
            if (__instance.GetComponent<TogetherWeRowRig>() == null)
            {
                __instance.gameObject.AddComponent<TogetherWeRowRig>();
            }
        }

        /// <summary>
        /// Extra hands add the same impulse the captain's paddle already uses, at the
        /// same point on the hull, in the direction the helm has asked for. One helper
        /// at Speed 1 is a second copy of that paddle, so the hull goes twice as fast
        /// from rowing. The same copies are added when the sail is up: the cloth keeps
        /// doing what it does, and the oars still push.
        ///
        /// It runs only on the owner because that is who integrates the rigidbody;
        /// everyone else is already watching that result.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Ship.CustomFixedUpdate))]
        private static void Pull(Ship __instance, float fixedDeltaTime)
        {
            if (!__instance.IsOwner())
            {
                return;
            }

            if (__instance.m_shipControlls == null || !__instance.m_shipControlls.HaveValidUser())
            {
                return;
            }

            var speed = __instance.GetSpeedSetting();
            if (speed == Ship.Speed.Stop)
            {
                return;
            }

            var rowers = TogetherWeRowCrew.Count(__instance);
            if (rowers == 0)
            {
                return;
            }

            var amount = rowers * TogetherWeRowPlugin.Speed.Value;
            if (amount <= 0f)
            {
                return;
            }

            var sign = speed == Ship.Speed.Back ? -1f : 1f;
            var pull = __instance.transform.forward
                       * (sign * __instance.m_backwardForce * amount * (1f - Mathf.Abs(__instance.GetRudderValue())));
            var at = __instance.transform.position + __instance.transform.forward * __instance.m_stearForceOffset;
            __instance.m_body.AddForceAtPosition(
                pull * (__instance.m_body.mass * fixedDeltaTime),
                at,
                ForceMode.Impulse);
        }
    }
}
