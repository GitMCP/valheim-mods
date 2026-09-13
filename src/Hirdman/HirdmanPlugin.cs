using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Hirdman
{
    /// <summary>
    /// Adds retainers: NPC companions you recruit and then order about in your own words.
    ///
    /// The shape of the mod comes from one constraint. A frame is 16 ms and understanding
    /// a sentence is not, so language can never be in the loop that drives a character.
    /// What a retainer does is therefore ordinary hand-written behaviour over a closed
    /// set of orders (<see cref="HirdmanJob"/>), and understanding happens once, when
    /// someone speaks, in <see cref="HirdmanParser"/>. Everything else in the mod exists
    /// on one side of that seam or the other.
    ///
    /// Because it adds content, it must be installed on the server and on every client,
    /// which is what <see cref="NetworkCompatibilityAttribute"/> enforces. Understanding
    /// is not part of that contract: it produces a few numbers, on one machine, and only
    /// the numbers travel.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class HirdmanPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gitmcp.hirdman";
        public const string PluginName = "Hirdman";

        // Generated from the project file's Version, so the plugin, the assembly, and
        // the Thunderstore manifest cannot disagree about which build this is.
        public const string PluginVersion = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Taking frames from the game's AI, and replacing petting with orders, are
            // changes to how the game treats a tamed creature, so they need patches
            // rather than prefab edits.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(HirdmanPlugin).Assembly);

            // Cloning vanilla prefabs is only possible once the game has loaded its own,
            // which happens long after plugin Awake.
            PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
            ItemManager.OnItemsRegistered += RegisterKit;
        }

        private void RegisterContent()
        {
            // Jotunn raises this every time vanilla prefabs are (re)loaded, and
            // registering the same prefab twice is an error.
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;

            Localizations.Register();
            HirdmanAssets.Load();

            if (!HirdmanRetainer.Register())
            {
                Log.LogError("No retainer was registered; not adding the muster post.");
                return;
            }

            HirdmanMuster.Register();
            CommandManager.Instance.AddConsoleCommand(new HirdmanCommand());

            // A patch whose target signature no longer matches the current game build
            // silently contributes nothing, so report the count rather than assuming.
            var patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{PluginName} {PluginVersion} registered its content, {patched} method(s) patched.");
        }

        private void RegisterKit()
        {
            ItemManager.OnItemsRegistered -= RegisterKit;
            HirdmanRetainer.RegisterKit();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
