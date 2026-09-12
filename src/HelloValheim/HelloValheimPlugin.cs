using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace HelloValheim
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    [BepInProcess("valheim_server.exe")]
    [BepInProcess("valheim_server.x86_64")]
    public class HelloValheimPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gitmcp.hellovalheim";
        public const string PluginName = "HelloValheim";

        // Generated from the project file's Version, so the plugin, the assembly, and
        // the Thunderstore manifest cannot disagree about which build this is.
        public const string PluginVersion = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> GreetOnSpawn;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            GreetOnSpawn = Config.Bind(
                "General",
                "GreetOnSpawn",
                true,
                "Write a greeting to the BepInEx log whenever the local player spawns.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(HelloValheimPlugin).Assembly);

            // A patch whose target signature no longer matches the current game build
            // silently contributes nothing, so report the count rather than assuming.
            var patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded, {patched} method(s) patched.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
