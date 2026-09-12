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
        public const string PluginGuid = "com.example.hellovalheim";
        public const string PluginName = "HelloValheim";
        public const string PluginVersion = "0.1.0";

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

            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
