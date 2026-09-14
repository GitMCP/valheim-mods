using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// The bell that calls everyone home.
    ///
    /// Once you can have several retainers out on several jobs, the expensive thing is no
    /// longer giving an order but collecting everybody afterwards - walking the whole
    /// steading to find the one still standing in a wood two hills away. The bell is one
    /// press for all of them, and it doubles as the answer to "where is home", which the
    /// rest of the mod needs anyway.
    /// </summary>
    internal static class HirdmanBell
    {
        internal const string PrefabName = "hirdman_bell";

        private const string CloneSource = "wood_pole2";

        /// <summary>
        /// A cauldron turned upside down is a bell. Valheim has no bell, and this is the
        /// only mesh in the game with the right silhouette - a round shoulder, a flared
        /// mouth - which is a better answer than a model nobody can see until it ships.
        /// </summary>
        private const string BellSource = "piece_cauldron";

        private const string BellPath = "new/cauldron (1)";

        /// <summary>The bell mesh is this tall, measured from its own pivot.</summary>
        private const float BellMeshHeight = 0.67f;

        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("Wood", 10, 0, true),
            new RequirementConfig("Bronze", 2, 0, true),
        };

        internal static void Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, CloneSource);
            if (prefab == null)
            {
                HirdmanPlugin.Log.LogError($"Could not clone '{CloneSource}' for the bell.");
                return;
            }

            Build(prefab);
            prefab.AddComponent<HirdmanBellRope>();

            var config = new PieceConfig
            {
                Name = $"${PrefabName}_name",
                Description = $"${PrefabName}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Forge,
                Requirements = Resources,
            };

            if (HirdmanAssets.Icon != null)
            {
                config.Icon = HirdmanAssets.Icon;
            }

            if (!PieceManager.Instance.AddPiece(new CustomPiece(prefab, fixReference: false, config)))
            {
                HirdmanPlugin.Log.LogError($"Failed to register piece '{PrefabName}'.");
            }
        }

        private static void Build(GameObject prefab)
        {
            var visual = HirdmanShapes.Clear(prefab, "BellVisual");

            var parts = HirdmanShapes.Wood(visual, "Post",
                new Vector3(0f, 1.1f, 0f), new Vector3(0.18f, 2.2f, 0.18f));

            // Reaches forward out of the top of the post, so the bell hangs clear of it
            // rather than against it.
            parts += HirdmanShapes.Wood(visual, "Arm",
                new Vector3(0f, 2.18f, 0.3f), new Vector3(0.14f, 0.14f, 0.75f));

            // Turned over, and hung by what is now its crown, so that its mouth opens
            // downwards and its top meets the underside of the arm.
            parts += HirdmanShapes.Fit(BellSource, BellPath, visual, "Bell",
                new Vector3(0f, 2.11f, 0.5f),
                Vector3.one * 0.8f,
                Quaternion.Euler(180f, 0f, 0f),
                new Vector3(0f, BellMeshHeight, 0f));

            HirdmanPlugin.Log.LogInfo($"Built the bell from {parts} borrowed part(s).");
        }
    }

    /// <summary>
    /// Ringing it. The press happens on one machine and the retainers are spread across
    /// several, so what actually travels is a call rather than an order.
    /// </summary>
    internal class HirdmanBellRope : MonoBehaviour, Hoverable, Interactable
    {
        private static readonly Vector3 MouthOffset = new Vector3(0f, 1.8f, 0f);

        private float _rungAt;

        public string GetHoverName()
        {
            return Localization.instance.Localize($"${HirdmanBell.PrefabName}_name");
        }

        public string GetHoverText()
        {
            return Localization.instance.Localize(
                $"{GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] Ring for everyone");
        }

        /// <summary>
        /// The piece's origin is the foot of the post, so without this the prompt would
        /// be drawn in the grass. It belongs up by the bell, which is what you aimed at.
        /// </summary>
        public float GetHoverOffset()
        {
            return MouthOffset.y;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            var player = user as Player;
            if (hold || player == null)
            {
                return false;
            }

            // A bell that can be rung sixty times a second is a bell that floods the
            // network with the same order.
            if (Time.time - _rungAt < 1f)
            {
                return true;
            }

            _rungAt = Time.time;

            var called = HirdmanCalls.Recall(player.GetPlayerID(), transform.position);
            player.Message(
                MessageHud.MessageType.Center,
                called == 0 ? "The bell rings out." : $"The bell rings out. {called} answer.");

            // The sound is the feedback that matters, and it belongs to whoever is stood
            // near enough to hear a bell.
            if (Chat.instance != null)
            {
                Chat.instance.SetNpcText(gameObject, MouthOffset, 24f, 3f, string.Empty, "*clang*", false);
            }

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }
    }
}
