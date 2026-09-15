using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;

namespace TogetherWeRow
{
    /// <summary>
    /// Puts an oar on every passenger seat of a boat, and lets whoever sits there
    /// push with the captain.
    ///
    /// Valheim ships already paddle. Slow and Back are the person at the helm
    /// sculling with the rudder; Half and Full are the sail. The chairs on the deck
    /// are ordinary furniture that happen to be on a boat, and sitting in one does
    /// nothing to the hull. This mod does not add a new control: it counts who is
    /// already sat down, other than the helmsman and anyone on the mast, and adds
    /// the same kind of force the paddle already uses, once per occupied gunwale
    /// seat. One helper is a second copy of that paddle, including while the sail
    /// is up.
    ///
    /// The oars are scenery borrowed from wooden building pieces. They are not
    /// networked objects. Every client builds the same ones locally, and the extra
    /// force is applied only by the peer that owns the ship, which is the one
    /// already integrating its rigidbody.
    ///
    /// Because the speed of a shared ship is a fact of the world, the server and
    /// every client have to agree that the oars exist, which is what
    /// <see cref="NetworkCompatibilityAttribute"/> enforces.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class TogetherWeRowPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.gitmcp.togetherwerow";
        public const string PluginName = "Together We Row";
        public const string PluginVersion = MyPluginInfo.PLUGIN_VERSION;

        internal static ManualLogSource Log;

        internal static ConfigEntry<float> Speed;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Speed = Config.Bind(
                "TogetherWeRow",
                "Speed",
                1f,
                new ConfigDescription(
                    "How much one helper adds, as a multiple of the helm's paddle. 1 means " +
                    "the helm plus one rower goes twice as fast from paddling, and that " +
                    "same extra still applies with the sail up.",
                    new AcceptableValueRange<float>(0f, 4f),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            Localizations.Register();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(TogetherWeRowPlugin).Assembly);

            var patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{PluginName} {PluginVersion} loaded, {patched} method(s) patched.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
