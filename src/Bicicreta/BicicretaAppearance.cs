using Jotunn.Managers;
using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Stands in for a bicycle model until there is a real one.
    ///
    /// A proper bicycle needs a mesh, which means a Unity AssetBundle. Until that exists
    /// the lox is hidden and a bicycle is assembled out of the only wheeled thing the
    /// game ships: two of the cart's wheels, with the cart's body shrunk down between
    /// them for a frame. The lox's skeleton and animator are left completely alone,
    /// because they are what <see cref="Character"/> movement drives; only its renderers
    /// are switched off. Once <see cref="BicicretaAssets.Bundle"/> carries a real model
    /// this goes away.
    /// </summary>
    internal static class BicicretaAppearance
    {
        private const string ModelSource = "Cart";
        private const string WheelPath = "Wheel1/default";
        private const string FramePath = "Vagon/new/default";
        private const string VisualName = "BicicretaVisual";

        /// <summary>The cart wheel mesh is 1 m across, so this is its radius.</summary>
        private const float DonorWheelRadius = 0.5f;

        /// <summary>The cart body mesh is this long, nose to tail.</summary>
        private const float DonorFrameLength = 3.27f;

        internal static void Apply(GameObject prefab)
        {
            if (!BicicretaPlugin.UseCartModel.Value)
            {
                BicicretaPlugin.Log.LogInfo("UseCartModel is off; leaving the clone's own model in place.");
                return;
            }

            var donor = PrefabManager.Instance.GetPrefab(ModelSource);
            if (donor == null)
            {
                BicicretaPlugin.Log.LogWarning(
                    $"No '{ModelSource}' prefab to borrow a model from; the bicycle will look like a lox.");
                return;
            }

            var hidden = 0;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = false;
                hidden++;
            }

            var visual = new GameObject(VisualName);
            visual.transform.SetParent(prefab.transform, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            var wheelScale = BicicretaGeometry.WheelRadius / DonorWheelRadius;
            var half = BicicretaGeometry.Wheelbase / 2f;

            var parts = 0;
            parts += Fit(donor, WheelPath, visual.transform, "FrontWheel",
                new Vector3(0f, BicicretaGeometry.WheelRadius, half), wheelScale) ? 1 : 0;
            parts += Fit(donor, WheelPath, visual.transform, "RearWheel",
                new Vector3(0f, BicicretaGeometry.WheelRadius, -half), wheelScale) ? 1 : 0;
            parts += Fit(donor, FramePath, visual.transform, "Frame",
                new Vector3(0f, BicicretaGeometry.FrameHeight, 0f),
                BicicretaGeometry.Wheelbase / DonorFrameLength) ? 1 : 0;

            BicicretaPlugin.Log.LogInfo(
                $"Hid {hidden} lox renderer(s) and built a bicycle from {parts} '{ModelSource}' part(s).");
        }

        /// <summary>
        /// Copies one mesh out of the donor and centres it on <paramref name="center"/>.
        ///
        /// Only leaf objects holding nothing but a mesh are borrowed. An earlier version
        /// took a whole wheel, which dragged along the cart's rigidbody and the joint
        /// holding it to the axle; the joint then refused to let the rigidbody be
        /// removed. These meshes are also modelled off to one side of their own pivot, so
        /// placing one means correcting for where its geometry actually sits.
        /// </summary>
        private static bool Fit(
            GameObject donor, string path, Transform parent, string name, Vector3 center, float scale)
        {
            var source = donor.transform.Find(path);
            var mesh = source == null ? null : source.GetComponent<MeshFilter>();
            if (mesh == null || mesh.sharedMesh == null)
            {
                BicicretaPlugin.Log.LogWarning($"'{ModelSource}/{path}' is gone; the bicycle is missing its {name}.");
                return false;
            }

            var part = Object.Instantiate(source.gameObject, parent);
            part.name = name;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one * scale;
            part.transform.localPosition = center - mesh.sharedMesh.bounds.center * scale;

            foreach (var renderer in part.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = true;
            }

            return true;
        }
    }
}
