using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
    /// tamed and permanently saddled, with its wandering, aggression, bulk, and voice
    /// removed, and with petting replaced by mounting. Riding, stamina, and the
    /// multiplayer handover of control then all come from the game.
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

        // Generated from the project file's Version, so the plugin, the assembly, and
        // the Thunderstore manifest cannot disagree about which build this is.
        public const string PluginVersion = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> RideSpeed;
        internal static ConfigEntry<float> StaminaDrain;
        internal static ConfigEntry<bool> UseStandInModel;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            BindConfig();

            // Riding, and not petting, is a change to how the game treats a tamed
            // creature, so it needs patches rather than prefab edits.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(BicicretaPlugin).Assembly);

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

            UseStandInModel = Config.Bind(
                "Bicicreta",
                "UseStandInModel",
                true,
                new ConfigDescription(
                    "Hide the lox and build a bicycle out of borrowed vanilla parts. Turn " +
                    "this off to see the unmodified clone, which is useful when diagnosing " +
                    "the model.",
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

            // A patch whose target signature no longer matches the current game build
            // silently contributes nothing, so report the count rather than assuming.
            var patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{PluginName} {PluginVersion} registered its content, {patched} method(s) patched.");
        }

        private void RegisterDrops()
        {
            ItemManager.OnItemsRegistered -= RegisterDrops;
            BicicretaMount.RegisterDrops();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
