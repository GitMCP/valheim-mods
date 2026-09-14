using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Hirdman.Patches
{
    /// <summary>
    /// A retainer is a player rig, not a player.
    ///
    /// The look, the animator, the armour math and the inventory all live on
    /// <see cref="Player"/>, and there is no way to keep those without keeping the
    /// component, because <see cref="Player"/> <em>is</em> the <see cref="Humanoid"/>.
    /// What has to go is everything that component then does because it believes it is
    /// the person sitting at the keyboard: counting itself among the world's players,
    /// destroying itself for not being the local one, reading input, moving the zone
    /// origin, and writing picked-up berries into the real player's profile.
    /// </summary>
    internal static class PlayerPatch
    {
        [HarmonyPatch(typeof(Player), "Awake")]
        private static class AwakePatch
        {
            private static void Postfix(Player __instance)
            {
                if (!HirdmanTag.On(__instance))
                {
                    return;
                }

                // Player.Awake adds every instance to the world's player list, including
                // the prefab itself the moment it is cloned. Bosses, beds, workbenches
                // and the minimap all read that list, and a retainer is not a player.
                var players = Traverse.Create(typeof(Player)).Field("s_players").GetValue<List<Player>>();
                players?.Remove(__instance);

                // The same Awake hooks inventory changes to the local profile's pickup
                // stats. A retainer gathering wood is not the player picking it up.
                var inventory = __instance.GetInventory();
                var method = AccessTools.Method(typeof(Player), "OnInventoryChanged");
                if (inventory != null && method != null)
                {
                    inventory.m_onChanged = (Action)Delegate.Remove(
                        inventory.m_onChanged,
                        Delegate.CreateDelegate(typeof(Action), __instance, method));
                }

                // Player.Awake puts every instance into the bed-wakeup clip, and the
                // only thing that ever clears it is Player.FixedUpdate, which is skipped
                // below. Without this they spend the rest of their lives lying flat.
                Wake(__instance);
            }
        }

        /// <summary>
        /// Gets a retainer onto its feet. The player rig starts in the wakeup pose, and
        /// a borrowed dvergr AI can also put the animator to sleep; either looks like a
        /// person who cannot walk.
        /// </summary>
        internal static void Wake(Player player)
        {
            if (player == null)
            {
                return;
            }

            if (player.m_animator != null)
            {
                player.m_animator.SetBool("wakeup", false);
                player.m_animator.SetBool("sleeping", false);
            }

            Traverse.Create(player).Field("m_wakeupTimer").SetValue(-1f);

            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();
            zdo.Set(ZDOVars.s_wakeup, false);
            zdo.Set(ZDOVars.s_sleeping, false);
        }

        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        private static class FixedUpdatePatch
        {
            private static bool Prefix(Player __instance)
            {
                // The owner-and-not-local branch in here destroys the object. That is
                // how the game throws away a stale local player after loading, and it
                // would throw away every retainer the moment one was hired.
                return !HirdmanTag.On(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        private static class UpdatePatch
        {
            private static bool Prefix(Player __instance)
            {
                return !HirdmanTag.On(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), "LateUpdate")]
        private static class LateUpdatePatch
        {
            private static bool Prefix(Player __instance)
            {
                // Owner LateUpdate writes the zone origin to this body's position.
                // Doing that for a retainer would unload the world around the real
                // player the moment they hired someone and walked ten metres off.
                return !HirdmanTag.On(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveStamina))]
        private static class HaveStaminaPatch
        {
            private static bool Prefix(Player __instance, ref bool __result)
            {
                if (!HirdmanTag.On(__instance))
                {
                    return true;
                }

                // Player stamina only regenerates in Player.FixedUpdate, which is
                // skipped above. Without this a retainer would swing twice and stop.
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        private static class UseStaminaPatch
        {
            private static bool Prefix(Player __instance)
            {
                return !HirdmanTag.On(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HaveEitr))]
        private static class HaveEitrPatch
        {
            private static bool Prefix(Player __instance, ref bool __result)
            {
                if (!HirdmanTag.On(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitrPatch
        {
            private static bool Prefix(Player __instance)
            {
                return !HirdmanTag.On(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetHoverName))]
        private static class HoverNamePatch
        {
            private static bool Prefix(Player __instance, ref string __result)
            {
                if (!HirdmanTag.On(__instance))
                {
                    return true;
                }

                __result = HirdmanNames.Of(__instance.gameObject);
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetHoverText))]
        private static class HoverTextPatch
        {
            private static bool Prefix(Player __instance, ref string __result)
            {
                if (!HirdmanTag.On(__instance))
                {
                    return true;
                }

                var name = HirdmanNames.Of(__instance.gameObject);
                var order = HirdmanBrain.Orders(__instance.gameObject);
                var prompt = order.Job == HirdmanJob.Follow
                    ? "Wait here"
                    : "Follow me";

                var text = $"{name}\n[<color=yellow><b>$KEY_Use</b></color>] {prompt}";
                if (HirdmanContract.Of(__instance.gameObject).BelongsTo(Player.m_localPlayer))
                {
                    text += "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Look in their pack";
                    text += "\n[<color=yellow><b>$KEY_Crouch + $KEY_Use</b></color>] Dismiss";
                }

                __result = Localization.instance.Localize(text);
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        private static class ControllerPatch
        {
            private static bool Prefix(PlayerController __instance)
            {
                return !HirdmanTag.On(__instance);
            }
        }
    }
}
