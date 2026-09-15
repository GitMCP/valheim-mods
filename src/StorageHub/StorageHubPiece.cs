using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace StorageHub
{
    /// <summary>
    /// Hammer piece: a black metal chest cloned from vanilla so placement, wear, and
    /// the Container ZDO all come from the game. A marker component is what makes
    /// opening it a hub rather than a second pile of slots.
    /// </summary>
    internal static class StorageHubPiece
    {
        internal const string PrefabName = "storage_hub";

        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("Wood", 10, 0, true),
            new RequirementConfig("Tar", 2, 0, true),
            new RequirementConfig("BlackMetal", 6, 0, true),
        };

        private static readonly string[] CloneSources =
        {
            "piece_chest_blackmetal",
            "piece_chest",
            "piece_chest_wood",
        };

        internal static void Register()
        {
            GameObject prefab = null;
            string source = null;
            foreach (var candidate in CloneSources)
            {
                prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, candidate);
                if (prefab != null)
                {
                    source = candidate;
                    break;
                }
            }

            if (prefab == null)
            {
                StorageHubPlugin.Log.LogError("Could not clone a vanilla chest for the Storage Hub.");
                return;
            }

            prefab.AddComponent<StorageHubMarker>();

            var container = prefab.GetComponent<Container>();
            if (container != null)
            {
                container.m_name = $"${PrefabName}_name";
                container.m_privacy = Container.PrivacySetting.Public;
            }

            var config = new PieceConfig
            {
                Name = $"${PrefabName}_name",
                Description = $"${PrefabName}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.Workbench,
                Requirements = Resources,
            };

            if (StorageHubAssets.Icon != null)
            {
                config.Icon = StorageHubAssets.Icon;
            }

            var piece = new CustomPiece(prefab, fixReference: false, config);
            if (!PieceManager.Instance.AddPiece(piece))
            {
                StorageHubPlugin.Log.LogError($"Failed to register piece '{PrefabName}'.");
                return;
            }

            StorageHubPlugin.Log.LogInfo($"Storage Hub cloned from '{source}'.");
        }
    }

    /// <summary>
    /// Marker on the hub prefab. The scan skips other hubs so two hubs in one room
    /// do not swallow each other's networks.
    /// </summary>
    internal class StorageHubMarker : MonoBehaviour
    {
        internal static Container OpenHub { get; set; }

        internal static bool IsHub(Container container)
        {
            return container != null && container.GetComponent<StorageHubMarker>() != null;
        }
    }
}
