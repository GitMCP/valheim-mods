using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Managers;
using Jotunn.Utils;

namespace ContentTemplate
{
    /// <summary>
    /// Starting point for mods that add new items, pieces, or recipes. Content mods have
    /// to be installed on the server and on every client, which is what the
    /// <see cref="NetworkCompatibilityAttribute"/> below enforces: a client whose version
    /// does not match the server's is refused with a clear message instead of desyncing.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class ContentTemplatePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.example.contenttemplate";
        public const string PluginName = "ContentTemplate";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> EnableExampleContent;

        private void Awake()
        {
            Log = Logger;
            BindConfig();

            // Cloning a vanilla prefab is only possible once the game has loaded its own,
            // which happens long after plugin Awake.
            PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        }

        private void BindConfig()
        {
            // IsAdminOnly makes the server's value authoritative and syncs it to clients,
            // so a client cannot enable content the server does not have.
            EnableExampleContent = Config.Bind(
                "General",
                "EnableExampleContent",
                true,
                new ConfigDescription(
                    "Register the example item and building piece.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void RegisterContent()
        {
            // Jotunn raises this every time vanilla prefabs are (re)loaded, and registering
            // the same prefab twice is an error.
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;

            if (!EnableExampleContent.Value)
            {
                Log.LogInfo("Example content disabled by config.");
                return;
            }

            Localizations.Register();
            ExampleAssets.Load();
            ExampleItems.Register();
            ExamplePieces.Register();

            Log.LogInfo($"{PluginName} {PluginVersion} registered its content.");
        }
    }
}
