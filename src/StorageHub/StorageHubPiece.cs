using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace StorageHub
{
    /// <summary>
    /// Hammer piece: Njord, a warehouse keeper. The piece still uses a vanilla
    /// container so opening him brings up the inventory GUI, but his own slots
    /// are never listed or filled — only the chests around him are.
    /// </summary>
    internal static class StorageHubPiece
    {
        internal const string PrefabName = "storage_hub";
        internal const string NpcName = "Njord";

        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("Coins", 200, 0, true),
        };

        private static readonly string[] CloneSources =
        {
            "piece_chest_wood",
            "piece_chest",
            "piece_chest_blackmetal",
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
                StorageHubPlugin.Log.LogError("Could not clone a vanilla piece for Njord.");
                return;
            }

            prefab.AddComponent<StorageHubMarker>();
            if (prefab.GetComponent<NjordTalk>() == null)
            {
                prefab.AddComponent<NjordTalk>();
            }

            var container = prefab.GetComponent<Container>();
            if (container != null)
            {
                container.m_name = "$storagehub_npc";
                container.m_privacy = Container.PrivacySetting.Public;
                // Dummy grid so InventoryGui still thinks a container is open.
                // The scan never lists these slots, and deposits never land here.
                container.m_width = 1;
                container.m_height = 1;
            }

            var pieceComp = prefab.GetComponent<Piece>();
            if (pieceComp != null)
            {
                pieceComp.m_name = $"${PrefabName}_name";
                pieceComp.m_description = $"${PrefabName}_description";
            }

            FitCollider(prefab);
            NjordLook.Attach(prefab);
            NjordLook.HideHostVisuals(prefab);

            var config = new PieceConfig
            {
                Name = $"${PrefabName}_name",
                Description = $"${PrefabName}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.None,
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

            StorageHubPlugin.Log.LogInfo($"Njord cloned from '{source}'.");
        }

        private static void FitCollider(GameObject prefab)
        {
            var box = prefab.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = new Vector3(0f, 0.9f, 0f);
                box.size = new Vector3(0.7f, 1.8f, 0.7f);
            }
        }
    }

    /// <summary>
    /// Marker on Njord. The scan skips other keepers so two in one room do not
    /// nest each other's networks.
    /// </summary>
    internal class StorageHubMarker : MonoBehaviour
    {
        internal static Container OpenHub { get; set; }

        private void Awake()
        {
            NjordLook.HideHostVisuals(gameObject);
            var body = transform.Find("NjordBody");
            if (body == null)
            {
                NjordLook.Attach(gameObject);
            }
        }

        internal static bool IsHub(Container container)
        {
            return container != null && container.GetComponent<StorageHubMarker>() != null;
        }
    }
}
