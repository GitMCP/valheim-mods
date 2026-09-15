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

            NjordLook.Attach(prefab);
            NjordLook.HideHostVisuals(prefab);
            FitCollider(prefab);

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

        /// <summary>
        /// The clone keeps the wood chest's low colliders, so a look-at aimed
        /// at Njord's chest or head misses them. Drop those and put a standing
        /// capsule on the same object as the Container, which is what the
        /// hover ray uses.
        /// </summary>
        internal static void FitCollider(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            var existing = prefab.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                {
                    Object.DestroyImmediate(existing[i], true);
                }
            }

            var capsule = prefab.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.isTrigger = false;
            capsule.enabled = true;
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

            StorageHubPiece.FitCollider(gameObject);
            var wear = GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.SetupColliders();
            }
        }

        internal static bool IsHub(Container container)
        {
            return container != null && container.GetComponent<StorageHubMarker>() != null;
        }
    }
}
