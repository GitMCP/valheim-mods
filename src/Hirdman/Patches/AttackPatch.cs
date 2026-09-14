using HarmonyLib;

namespace Hirdman.Patches
{
    /// <summary>
    /// A retainer is a player rig, so a pickaxe swing uses the player attack, and that
    /// attack hits terrain. Left alone they mine a hole under their own feet, step down
    /// into it, and keep digging. They are not here to landscape.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
    internal static class AttackPatch
    {
        private static void Prefix(Attack __instance, Humanoid character)
        {
            if (HirdmanTag.On(character))
            {
                __instance.m_hitTerrain = false;
            }
        }
    }
}
