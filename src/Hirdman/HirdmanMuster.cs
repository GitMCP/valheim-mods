using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// How a player gets a retainer: a hammer piece, so the build preview, the placement
    /// rules and the resource cost all come from the game. The post is only a way to put
    /// someone somewhere, so it hands over and removes itself.
    /// </summary>
    internal static class HirdmanMuster
    {
        internal const string PrefabName = "hirdman_muster";

        /// <summary>What recruiting one costs.</summary>
        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("Wood", 10, 0, true),
            new RequirementConfig("LeatherScraps", 5, 0, true),
            new RequirementConfig("Coins", 50, 0, true),
        };

        /// <summary>
        /// A plain wooden post, which is honest about what is being placed and, unlike
        /// most pieces, is a shape the game will let anything borrow.
        /// </summary>
        private const string CloneSource = "wood_pole";

        internal static void Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, CloneSource);
            if (prefab == null)
            {
                HirdmanPlugin.Log.LogError($"Could not clone '{CloneSource}' for the muster post.");
                return;
            }

            prefab.AddComponent<HirdmanMusterSpawner>();

            var config = new PieceConfig
            {
                Name = $"${PrefabName}_name",
                Description = $"${PrefabName}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Workbench,
                Requirements = Resources,
            };

            if (HirdmanAssets.Icon != null)
            {
                config.Icon = HirdmanAssets.Icon;
            }

            var piece = new CustomPiece(prefab, fixReference: false, config);
            if (!PieceManager.Instance.AddPiece(piece))
            {
                HirdmanPlugin.Log.LogError($"Failed to register piece '{PrefabName}'.");
            }
        }
    }

    /// <summary>
    /// Replaces the placed post with a retainer, standing where the post stood and
    /// waiting there until it is told otherwise.
    /// </summary>
    internal class HirdmanMusterSpawner : MonoBehaviour
    {
        private void Awake()
        {
            var nview = GetComponent<ZNetView>();

            // A build preview has no ZDO, and only one peer should do the recruiting.
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            // Spawning from inside Awake would run while the post is still being placed,
            // before its final position is known.
            Invoke(nameof(Recruit), 0f);
        }

        private void Recruit()
        {
            var nview = GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var prefab = ZNetScene.instance?.GetPrefab(HirdmanRetainer.PrefabName);
            if (prefab == null)
            {
                // Leaving the post standing is better than silently eating the materials.
                HirdmanPlugin.Log.LogError(
                    $"'{HirdmanRetainer.PrefabName}' is not registered; leaving the post in place.");
                return;
            }

            var retainer = Instantiate(prefab, transform.position, transform.rotation);

            // Told to wait here, so that a fresh recruit stands where it was raised
            // instead of inheriting whatever an empty ZDO happens to mean.
            HirdmanBrain.Give(retainer, new HirdmanOrder
            {
                Job = HirdmanJob.Idle,
                Anchor = transform.position,
                Master = ZDOID.None,
            });

            nview.Destroy();
        }
    }
}
