using Jotunn.Managers;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Builds the mod's two pieces out of meshes the game already has loaded.
    ///
    /// Neither an outpost nor a bell exists in Valheim, and a real model would mean
    /// shipping an AssetBundle. Borrowing costs nothing instead: every mesh, material and
    /// shader here is already in memory on every peer, so the pieces cannot fail to load
    /// and cannot look out of place.
    ///
    /// The one discovery that makes this easy is that every wooden building piece is the
    /// same unit cube under a different scale. Asking <c>wood_pole</c> for its mesh and
    /// scaling it is how you get a plank, a post or a crossarm of any size you like.
    /// </summary>
    internal static class HirdmanShapes
    {
        internal const string WoodSource = "wood_pole";

        /// <summary>The unit cube every wooden piece turns out to be.</summary>
        internal const string WoodPath = "New";

        /// <summary>
        /// Replaces a donor piece's own model with an empty to hang parts off, and hands
        /// back that empty. The donor keeps its collider, its
        /// <see cref="WearNTear"/> and its <see cref="Piece"/>, which is the whole reason
        /// for cloning one rather than building a prefab from nothing.
        /// </summary>
        internal static Transform Clear(GameObject prefab, string name)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = false;
            }

            var visual = new GameObject(name);
            visual.transform.SetParent(prefab.transform, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            return visual.transform;
        }

        /// <summary>
        /// A box of wood of exactly the given size, centred on <paramref name="target"/>.
        /// </summary>
        internal static int Wood(Transform parent, string name, Vector3 target, Vector3 size)
        {
            return Fit(WoodSource, WoodPath, parent, name, target, size);
        }

        /// <summary>
        /// Copies one mesh out of a donor prefab and places it so that
        /// <paramref name="anchor"/> - a point in the mesh's own coordinates, or the
        /// middle of it when not given - lands on <paramref name="target"/>.
        ///
        /// Only leaf objects holding nothing but a mesh are borrowed, because a whole
        /// sub-object drags its colliders, joints and scripts along with it. The parts
        /// are scenery: the piece's own collider is what the world interacts with.
        /// </summary>
        internal static int Fit(
            string donorName,
            string path,
            Transform parent,
            string name,
            Vector3 target,
            Vector3 scale,
            Quaternion? rotation = null,
            Vector3? anchor = null)
        {
            var donor = PrefabManager.Instance.GetPrefab(donorName);
            var source = donor == null ? null : donor.transform.Find(path);
            var mesh = source == null ? null : source.GetComponent<MeshFilter>();
            if (mesh == null || mesh.sharedMesh == null)
            {
                HirdmanPlugin.Log.LogWarning($"'{donorName}/{path}' is gone; {name} will be missing.");
                return 0;
            }

            var turn = rotation ?? Quaternion.identity;
            var part = Object.Instantiate(source.gameObject, parent);
            part.name = name;
            part.transform.localRotation = turn;
            part.transform.localScale = scale;

            // Where the mesh sits relative to its own pivot is the modeller's business
            // and is rarely the middle, so placing a part means correcting for it - and
            // the correction turns with the part.
            part.transform.localPosition =
                target - turn * Vector3.Scale(anchor ?? mesh.sharedMesh.bounds.center, scale);

            // A LODGroup decides visibility from a size it recorded for the object it was
            // authored on, which no longer describes a part that has been rescaled and
            // reparented. Left in place it can switch the mesh off again.
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

            return 1;
        }
    }
}
