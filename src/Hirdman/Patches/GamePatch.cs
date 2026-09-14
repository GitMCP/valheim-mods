using HarmonyLib;

namespace Hirdman.Patches
{
    /// <summary>
    /// Opens the mod's two message channels when a world does.
    ///
    /// <see cref="ZRoutedRpc"/> is created with the connection rather than with the
    /// process, so registering at plugin load would be registering against nothing. The
    /// start of a game is the first moment there is a routing layer to register with,
    /// and the last moment before anything could need it.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    internal static class GameStartPatch
    {
        private static void Postfix()
        {
            HirdmanCalls.Register();
        }
    }

    /// <summary>
    /// Leaving a world throws the routing layer away, so the next one has to be told
    /// about the mod again. Anything the mod cached out of that world goes with it.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.OnDestroy))]
    internal static class GameStopPatch
    {
        private static void Postfix()
        {
            HirdmanCalls.Forget();
            Work.HirdmanFarm.Forget();
            HirdmanChatWindow.Close();
        }
    }
}
