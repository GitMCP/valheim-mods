using Jotunn.Managers;
using UnityEngine;

namespace Bicycle
{
    /// <summary>
    /// Stands in for a bicycle model until there is a real one.
    ///
    /// A proper bicycle needs a mesh, which means a Unity AssetBundle. Until that exists,
    /// the lox is hidden and the cart's wheels are shown in its place: it is the only
    /// wheeled thing the game ships, and it at least reads as a vehicle rather than an
    /// animal. The lox's skeleton and animator are left completely alone, because they
    /// are what <see cref="Character"/> movement drives; only the renderers are switched
    /// off. Once <see cref="BicycleAssets.Bundle"/> carries a real model this goes away.
    /// </summary>
    internal static class BicycleAppearance
    {
        private const string ModelSource = "Cart";
        private const float ModelScale = 0.55f;

        internal static void Apply(GameObject prefab)
        {
            if (!BicyclePlugin.UseCartModel.Value)
            {
                BicyclePlugin.Log.LogInfo("UseCartModel is off; leaving the clone's own model in place.");
                return;
            }

            var donor = PrefabManager.Instance.GetPrefab(ModelSource);
            if (donor == null)
            {
                BicyclePlugin.Log.LogWarning(
                    $"No '{ModelSource}' prefab to borrow a model from; the bicycle will look like a lox.");
                return;
            }

            var donorVisual = FindVisual(donor);
            if (donorVisual == null)
            {
                BicyclePlugin.Log.LogWarning(
                    $"Found no renderers under '{ModelSource}'; the bicycle will look like a lox.");
                return;
            }

            var hidden = 0;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = false;
                hidden++;
            }

            var visual = Object.Instantiate(donorVisual, prefab.transform);
            visual.name = "BicycleVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * ModelScale;

            // The donor's physics would fight the mount's own collider and rigidbody.
            StripPhysics(visual);

            BicyclePlugin.Log.LogInfo(
                $"Hid {hidden} renderer(s) and fitted the '{ModelSource}' model.");
        }

        /// <summary>
        /// Finds the shallowest child that contains the renderers, rather than relying on
        /// a child name that a game update could rename.
        /// </summary>
        private static GameObject FindVisual(GameObject donor)
        {
            foreach (Transform child in donor.transform)
            {
                if (child.GetComponentInChildren<Renderer>(includeInactive: true) != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static void StripPhysics(GameObject visual)
        {
            foreach (var collider in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.Destroy(collider);
            }

            foreach (var body in visual.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.Destroy(body);
            }
        }
    }
}
