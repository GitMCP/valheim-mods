using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Bicycle
{
    /// <summary>
    /// How a player gets a bicycle: a hammer piece, so placement, the build preview, and
    /// the resource cost all come from the game. The piece is only a way to put a bicycle
    /// somewhere, so it hands over to the mount and removes itself.
    /// </summary>
    internal static class BicycleStand
    {
        internal const string PrefabName = "bicycle_stand";

        /// <summary>What it costs to build, and so what half of it is worth breaking one for.</summary>
        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("Wood", 10, 0, true),
            new RequirementConfig("Bronze", 4, 0, true),
            new RequirementConfig("LeatherScraps", 4, 0, true),
        };

        /// <summary>
        /// The cart is the closest the game has to a bicycle, so its build preview is
        /// also the most honest about what is being placed.
        /// </summary>
        private const string CloneSource = "Cart";

        internal static void Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, CloneSource);
            if (prefab == null)
            {
                BicyclePlugin.Log.LogError($"Could not clone '{CloneSource}' for the build piece.");
                return;
            }

            // Without this the placed piece spends its one frame of life behaving like a
            // cart, being pushed around by whatever it was placed against.
            var vagon = prefab.GetComponent<Vagon>();
            if (vagon != null)
            {
                Object.Destroy(vagon);
            }

            prefab.AddComponent<BicycleStandSpawner>();

            var config = new PieceConfig
            {
                Name = $"${PrefabName}_name",
                Description = $"${PrefabName}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Workbench,
                Requirements = Resources,
            };

            if (BicycleAssets.Icon != null)
            {
                config.Icon = BicycleAssets.Icon;
            }

            var piece = new CustomPiece(prefab, fixReference: false, config);
            if (!PieceManager.Instance.AddPiece(piece))
            {
                BicyclePlugin.Log.LogError($"Failed to register piece '{PrefabName}'.");
            }
        }
    }

    /// <summary>
    /// Replaces the placed piece with a rideable bicycle.
    /// </summary>
    internal class BicycleStandSpawner : MonoBehaviour
    {
        private void Awake()
        {
            var nview = GetComponent<ZNetView>();

            // Placement previews have no ZDO, and only one peer should spawn the bicycle.
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            // Spawning from inside Awake would run while the piece is still being placed,
            // before its final position is set.
            Invoke(nameof(SpawnBicycle), 0f);
        }

        private void SpawnBicycle()
        {
            var nview = GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var prefab = ZNetScene.instance?.GetPrefab(BicycleMount.PrefabName);
            if (prefab == null)
            {
                // Leaving the piece standing is better than silently eating the materials.
                BicyclePlugin.Log.LogError(
                    $"'{BicycleMount.PrefabName}' is not registered; leaving the piece in place.");
                return;
            }

            Instantiate(prefab, transform.position, transform.rotation);
            nview.Destroy();
        }
    }
}
