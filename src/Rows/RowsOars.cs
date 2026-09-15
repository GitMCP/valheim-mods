using Jotunn.Managers;
using UnityEngine;

namespace Rows
{
    /// <summary>
    /// An oar assembled from the wooden pole the game already ships.
    ///
    /// A real oar mesh would need an AssetBundle. Until then a shaft and a blade
    /// are two boxes of wood, the same trick the bicycle uses for its handlebar.
    /// They are local scenery: no ZNetView, no collider, nothing the simulation
    /// has to agree about beyond the fact that a player is sitting.
    /// </summary>
    internal static class RowsOars
    {
        private const string WoodSource = "wood_pole";
        private const string WoodPath = "New";

        internal static Transform Build(Transform parent, float side)
        {
            var pivot = new GameObject("RowsOar").transform;
            pivot.SetParent(parent, worldPositionStays: false);
            pivot.localPosition = new Vector3(side * 0.55f, 0.35f, 0f);
            pivot.localRotation = Rest(side);

            // The pole is a unit cube. Stretching it makes a shaft; flattening a
            // second copy at the far end makes a blade.
            Fit("Shaft", pivot, new Vector3(side * 0.85f, 0f, 0f), new Vector3(1.7f, 0.07f, 0.07f));
            Fit("Blade", pivot, new Vector3(side * 1.75f, 0f, 0f), new Vector3(0.4f, 0.04f, 0.28f));
            return pivot;
        }

        internal static Quaternion Rest(float side)
        {
            return Quaternion.Euler(0f, 0f, side * 18f);
        }

        internal static Quaternion Stroke(float side, float time)
        {
            return Quaternion.Euler(0f, 0f, side * (18f + Mathf.Sin(time * 3.2f) * 26f));
        }

        private static void Fit(string name, Transform parent, Vector3 target, Vector3 scale)
        {
            var donor = PrefabManager.Instance.GetPrefab(WoodSource);
            var source = donor == null ? null : donor.transform.Find(WoodPath);
            var mesh = source == null ? null : source.GetComponent<MeshFilter>();
            if (mesh == null || mesh.sharedMesh == null)
            {
                RowsPlugin.Log.LogWarning($"'{WoodSource}/{WoodPath}' is gone; the oar is missing its {name}.");
                return;
            }

            var part = Object.Instantiate(source.gameObject, parent);
            part.name = name;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = scale;
            part.transform.localPosition = target - Vector3.Scale(mesh.sharedMesh.bounds.center, scale);

            foreach (var lod in part.GetComponentsInChildren<LODGroup>(includeInactive: true))
            {
                Object.DestroyImmediate(lod);
            }

            foreach (var collider in part.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (var renderer in part.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = true;
            }
        }
    }
}
