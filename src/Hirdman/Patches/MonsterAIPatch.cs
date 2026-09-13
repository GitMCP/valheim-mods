using HarmonyLib;

namespace Hirdman.Patches
{
    /// <summary>
    /// Lets a retainer's brain take a frame from the game's AI.
    ///
    /// <see cref="MonsterAI.UpdateAI"/> is where a creature decides what to do, and it
    /// decides every frame, so a job cannot simply be layered on top: vanilla would spend
    /// the same frame walking the retainer somewhere else. Rather than reimplement a
    /// creature, the brain is asked first and only claims the frames it actually needs -
    /// which is why fighting, fleeing and following still behave exactly as the game
    /// intends.
    /// </summary>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAIPatch
    {
        private static bool Prefix(MonsterAI __instance, float dt, ref bool __result)
        {
            var brain = __instance.GetComponent<HirdmanBrain>();
            if (brain == null || !brain.Think(dt))
            {
                return true;
            }

            // True tells the caller the AI has handled its own movement this frame.
            __result = true;
            return false;
        }
    }
}
