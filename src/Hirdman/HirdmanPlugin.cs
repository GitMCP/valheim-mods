using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Adds retainers: NPC companions you hire and then order about in your own words.
    ///
    /// The shape of the mod comes from one constraint. A frame is 16 ms and understanding
    /// a sentence is not, so language can never be in the loop that drives a character.
    /// What a retainer does is therefore ordinary hand-written behaviour over a closed
    /// set of orders (<see cref="HirdmanJob"/>), and understanding happens once, when
    /// someone speaks, in <see cref="HirdmanInterpreter"/>. Everything else in the mod
    /// exists on one side of that seam or the other.
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

        /// <summary>How far a retainer will step aside to pick up what it just made.</summary>
        internal const float HaulRadius = 6f;

        internal static ManualLogSource Log;

        /// <summary>
        /// The plugin doubles as the mod's coroutine host, because asking a model a
        /// question is the one thing here that takes longer than a frame and a
        /// <see cref="BaseUnityPlugin"/> is already a <see cref="MonoBehaviour"/> that
        /// outlives everything else.
        /// </summary>
        internal static HirdmanPlugin Instance { get; private set; }

        internal static ConfigEntry<int> HirePrice;
        internal static ConfigEntry<int> HireLimit;
        internal static ConfigEntry<float> WorkRadius;
        internal static ConfigEntry<float> ScoutRange;
        internal static ConfigEntry<float> ScoutSight;

        internal static ConfigEntry<KeyboardShortcut> TalkKey;

        internal static ConfigEntry<bool> ModelEnabled;
        internal static ConfigEntry<string> ModelEndpoint;
        internal static ConfigEntry<string> ModelName;
        internal static ConfigEntry<int> ModelTimeout;
        internal static ConfigEntry<string> ModelKeepAlive;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;
            BindHousehold();
            BindTalking();
            BindModel();

            // Taking frames from the game's AI, replacing petting with orders, and
            // opening the routing layer for the bell are changes to how the game behaves
            // rather than to what it contains, so they need patches rather than prefabs.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(HirdmanPlugin).Assembly);

            // Cloning vanilla prefabs is only possible once the game has loaded its own,
            // which happens long after plugin Awake.
            PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        }

        /// <summary>
        /// What a household costs and how far it ranges. These are rules of the world
        /// rather than preferences, so the server's copy wins and is pushed to everyone:
        /// two players on one world disagreeing about the price of a retainer is not a
        /// difference of opinion, it is a duplication bug.
        /// </summary>
        private void BindHousehold()
        {
            HirePrice = Config.Bind(
                "Household",
                "Price",
                100,
                new ConfigDescription(
                    "Coins to hire one retainer at an outpost.",
                    new AcceptableValueRange<int>(0, 10000),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            HireLimit = Config.Bind(
                "Household",
                "Limit",
                4,
                new ConfigDescription(
                    "How many retainers one player may keep around them at once. Counted " +
                    "among those the game currently has loaded, which is the ones near you.",
                    new AcceptableValueRange<int>(1, 20),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            WorkRadius = Config.Bind(
                "Household",
                "WorkRadius",
                24f,
                new ConfigDescription(
                    "How far from the spot it was set to work a retainer will range for " +
                    "trees, ore, crops, chests and quarry.",
                    new AcceptableValueRange<float>(8f, 64f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ScoutRange = Config.Bind(
                "Household",
                "ScoutRange",
                48f,
                new ConfigDescription(
                    "How far a scout circles from whoever it is scouting for. Valheim only " +
                    "simulates the world near a player, so a scout sent further than this " +
                    "would stop existing rather than keep walking.",
                    new AcceptableValueRange<float>(16f, 96f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ScoutSight = Config.Bind(
                "Household",
                "ScoutSight",
                80f,
                new ConfigDescription(
                    "How much map a scout uncovers around itself, in metres.",
                    new AcceptableValueRange<float>(20f, 200f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        /// <summary>
        /// Which key opens the chat window. Nothing about it is the server's business.
        /// </summary>
        private void BindTalking()
        {
            TalkKey = Config.Bind(
                "Talking",
                "Key",
                new KeyboardShortcut(KeyCode.G),
                "Opens a window to talk to the nearest retainer.");
        }

        /// <summary>
        /// None of this is admin-only or synced either. A model describes the machine a
        /// player is sitting at: whether they have one, what it is called, and how long
        /// they are willing to wait for it. One player running a model and three others
        /// not is a normal way to play, and nothing about it needs the server's
        /// agreement, because what reaches the server is an order and not a sentence.
        /// </summary>
        private void BindModel()
        {
            ModelEnabled = Config.Bind(
                "Model",
                "Enabled",
                false,
                "Ask a local language model about orders that keywords could not place. " +
                "Off by default: without this the mod works entirely on keywords.");

            ModelEndpoint = Config.Bind(
                "Model",
                "Endpoint",
                "http://127.0.0.1:11434/api/chat",
                "Where the model is listening. The default is Ollama's. A server that " +
                "answers in the OpenAI shape is also understood.");

            ModelName = Config.Bind(
                "Model",
                "Name",
                "qwen3:4b",
                "Which model to ask. Choosing between a dozen orders is a small job, so a " +
                "small model does it well and answers quickly.");

            ModelTimeout = Config.Bind(
                "Model",
                "TimeoutSeconds",
                8,
                new ConfigDescription(
                    "How long to wait before giving up and admitting the order was not " +
                    "understood.",
                    new AcceptableValueRange<int>(1, 60)));

            ModelKeepAlive = Config.Bind(
                "Model",
                "KeepAlive",
                "5m",
                "How long the model should stay in memory between orders, in Ollama's " +
                "notation. Valheim wants the graphics card too, so '0' unloads it after " +
                "every order and '-1' keeps it resident.");
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
                Log.LogError("No retainer was registered; not adding the outpost or the bell.");
                return;
            }

            HirdmanOutpost.Register();
            HirdmanBell.Register();
            CommandManager.Instance.AddConsoleCommand(new HirdmanCommand());

            // A patch whose target signature no longer matches the current game build
            // silently contributes nothing, so report the count rather than assuming.
            var patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{PluginName} {PluginVersion} registered its content, {patched} method(s) patched.");
        }

        private void Update()
        {
            HirdmanChatWindow.Poll();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
