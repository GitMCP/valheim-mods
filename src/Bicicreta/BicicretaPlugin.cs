using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Bicicreta
{
    /// <summary>
    /// Adds a rideable bicycle.
    ///
    /// Valheim has no land vehicle to build on: the only vehicle in the game is the ship,
    /// and riding is implemented entirely for tamed creatures by <see cref="Sadle"/>,
    /// which drives a <see cref="Character"/> through its <see cref="MonsterAI"/>. So the
    /// bicycle is a creature that happens to be a bicycle - cloned from the lox, born
    /// tamed and permanently saddled, with its wandering and aggression removed. Riding,
    /// stamina, and the multiplayer handover of control then all come from the game.
    ///
    /// Because it adds content, it must be installed on the server and on every client,
    /// which is what <see cref="NetworkCompatibilityAttribute"/> enforces.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BicicretaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gitmcp.bicicreta";
        public const string PluginName = "Bicicreta";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> RideSpeed;
        internal static ConfigEntry<float> StaminaDrain;
        internal static ConfigEntry<bool> UseCartModel;

        private void Awake()
        {
            Log = Logger;
            BindConfig();

            // Cloning vanilla prefabs is only possible once the game has loaded its own,
            // which happens long after plugin Awake.
            PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
            ItemManager.OnItemsRegistered += RegisterDrops;
        }

        private void BindConfig()
        {
            // IsAdminOnly makes the server's value authoritative and syncs it to clients,
            // so riders cannot set their own speed on someone else's server.
            RideSpeed = Config.Bind(
                "Bicicreta",
                "RideSpeed",
                1.6f,
                new ConfigDescription(
                    "Multiplier on the bicycle's ride speed, relative to a saddled lox.",
                    new AcceptableValueRange<float>(0.5f, 4f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            StaminaDrain = Config.Bind(
                "Bicicreta",
                "StaminaDrain",
                0.5f,
                new ConfigDescription(
                    "Multiplier on the stamina pedalling costs, relative to a saddled lox.",
                    new AcceptableValueRange<float>(0f, 2f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            UseCartModel = Config.Bind(
                "Bicicreta",
                "UseCartModel",
                true,
                new ConfigDescription(
                    "Hide the lox and show the cart's wheels in its place. Turn this off to " +
                    "see the unmodified clone, which is useful when diagnosing the model.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void RegisterContent()
        {
            // Jotunn raises this every time vanilla prefabs are (re)loaded, and
            // registering the same prefab twice is an error.
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;

            Localizations.Register();
            BicicretaAssets.Load();

            if (!BicicretaMount.Register())
            {
                Log.LogError("Bicicreta mount was not registered; not adding the build piece.");
                return;
            }

            BicicretaStand.Register();

            Log.LogInfo($"{PluginName} {PluginVersion} registered its content.");
        }

        private void RegisterDrops()
        {
            ItemManager.OnItemsRegistered -= RegisterDrops;
            BicicretaMount.RegisterDrops();
        }
    }
}
