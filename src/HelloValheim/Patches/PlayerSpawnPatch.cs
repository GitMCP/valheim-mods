using HarmonyLib;

namespace HelloValheim.Patches
{
    /// <summary>
    /// Proof that the toolchain is wired up end to end: the patch target is matched by
    /// explicit signature, and the body reads <c>m_baseValue</c> (the player's comfort
    /// level), a private game field that is only reachable because the game assemblies
    /// are publicized at build time.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned), new[] { typeof(bool) })]
    internal static class PlayerSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!HelloValheimPlugin.GreetOnSpawn.Value || __instance != Player.m_localPlayer)
            {
                return;
            }

            HelloValheimPlugin.Log.LogInfo(
                $"Hello, {__instance.GetPlayerName()}! Comfort here is {__instance.m_baseValue}.");
        }
    }
}
