using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using NjordWarehouseKeeper.Client;
using NjordWarehouseKeeper.UI;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// Adds a buildable warehouse keeper, Njord, who is not storage of his own so much
    /// as a window onto every container around him. Talking to him scans nearby chests,
    /// lists their contents together, and routes deposits into an existing stack or the
    /// first empty slot.
    ///
    /// Items never leave the chest they already sit in until someone takes or moves
    /// them. Njord does not clone stacks into a fake inventory: withdraw and deposit
    /// call the same <see cref="Inventory.MoveItemToThis"/> the vanilla GUI uses, after
    /// claiming ZDO ownership the same way Take All does, so multiplayer does not
    /// duplicate or drop items. Several people can talk to him at once; the
    /// exclusive lock that vanilla chests use is skipped for Njord himself.
    ///
    /// Because it adds a piece, it must be installed on the server and on every client.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(Client.EpicLootCompat.PluginId, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class NjordWarehouseKeeperPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gitmcp.njord";
        public const string PluginName = "Njord, Warehouse Keeper";
        public const string PluginVersion = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<bool> RequireLineOfSight;
        internal static ConfigEntry<bool> DepositHotbar;
        internal static ConfigEntry<bool> Reorganize;
        internal static ConfigEntry<float> ReorganizeInterval;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            // Register before Valheim builds its localization table. Jötunn copies
            // custom translations into that table during its localization load.
            Localizations.Register();
            BindConfig();
            ClientPreferences.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(NjordWarehouseKeeperPlugin).Assembly);

            PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        }

        private void Update()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            if (NjordWarehouseKeeperPrefs.TickCapture())
            {
                return;
            }

            HubHotkey.Tick();
        }

        private void BindConfig()
        {
            Radius = Config.Bind(
                "Njord",
                "Radius",
                15f,
                new ConfigDescription(
                    "How far from Njord, in metres, a container is still part of the network.",
                    new AcceptableValueRange<float>(4f, 40f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            RequireLineOfSight = Config.Bind(
                "Njord",
                "RequireLineOfSight",
                false,
                new ConfigDescription(
                    "Only include chests Njord can see. Off by default so a chest in the " +
                    "next room still counts; turn it on if a busy hall is pulling in too much.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DepositHotbar = Config.Bind(
                "Njord",
                "DepositHotbar",
                false,
                new ConfigDescription(
                    "When depositing everything, also send the hotbar (the first inventory row). " +
                    "Off by default so tools stay on the belt.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            Reorganize = Config.Bind(
                "Njord",
                "Reorganize",
                true,
                new ConfigDescription(
                    "From time to time Njord merges leftover piles of the same item so they " +
                    "use as few chest slots as possible. Chests someone has open are left alone.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ReorganizeInterval = Config.Bind(
                "Njord",
                "ReorganizeInterval",
                60f,
                new ConfigDescription(
                    "Seconds between each tidy pass. Only used when Reorganize is on.",
                    new AcceptableValueRange<float>(15f, 1800f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void RegisterContent()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;

            NjordWarehouseKeeperAssets.Load();
            NjordWarehouseKeeperPiece.Register();

            var patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{PluginName} {PluginVersion} registered its content, {patched} method(s) patched.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
