using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace GatewayChest
{
    /// <summary>
    /// Hammer piece: a chest cloned from vanilla so placement, wear, and the Container
    /// ZDO all come from the game. A marker component is what makes opening it a hub
    /// rather than a second pile of slots.
    /// </summary>
    internal static class GatewayChestPiece
    {
        internal const string PrefabName = "gateway_chest";

        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("FineWood", 8, 0, true),
            new RequirementConfig("Iron", 4, 0, true),
            new RequirementConfig("SurtlingCore", 2, 0, true),
        };

        private static readonly string[] CloneSources =
        {
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
                GatewayChestPlugin.Log.LogError("Could not clone a vanilla chest for the Gateway Chest.");
                return;
            }

            prefab.AddComponent<GatewayChestHub>();

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

            if (GatewayChestAssets.Icon != null)
            {
                config.Icon = GatewayChestAssets.Icon;
            }

            var piece = new CustomPiece(prefab, fixReference: false, config);
            if (!PieceManager.Instance.AddPiece(piece))
            {
                GatewayChestPlugin.Log.LogError($"Failed to register piece '{PrefabName}'.");
                return;
            }

            GatewayChestPlugin.Log.LogInfo($"Gateway Chest cloned from '{source}'.");
        }
    }

    /// <summary>
    /// Marker on the hub prefab. The scan skips other hubs so two gateways in one room
    /// do not swallow each other's networks.
    /// </summary>
    internal class GatewayChestHub : MonoBehaviour
    {
        internal static Container OpenHub { get; set; }

        internal static bool IsHub(Container container)
        {
            return container != null && container.GetComponent<GatewayChestHub>() != null;
        }
    }
}
