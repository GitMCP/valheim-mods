using Jotunn.Managers;
using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Stands in for a bicycle model until there is a real one.
    ///
    /// A proper bicycle needs its own mesh, which means a Unity AssetBundle. Until that
    /// exists the lox is hidden and a bicycle is assembled out of parts the game already
    /// ships: the cart's wheels, its body shrunk down for a frame, and a wooden chair
    /// for the seat. Borrowing costs nothing to load, because every mesh, material, and
    /// shader involved is already in memory on every peer.
    ///
    /// The lox's skeleton and animator are left completely alone, because they are what
    /// <see cref="Character"/> movement drives; only its renderers are switched off. Once
    /// <see cref="BicicretaAssets.Bundle"/> carries a real model this goes away.
    /// </summary>
    internal static class BicicretaAppearance
    {
        private const string VisualName = "BicicretaVisual";

        private const string CartSource = "Cart";
        private const string WheelPath = "Wheel1/default";
        private const string FramePath = "Vagon/new/default";

        /// <summary>
        /// The wooden chair. Not <c>piece_chair</c>, which despite the name is the
        /// stool; the chair with a back is <c>piece_chair02</c>.
        /// </summary>
        private const string ChairSource = "piece_chair02";

        private const string ChairPath = "New/high";

        /// <summary>The cart wheel mesh is 1 m across, so this is its radius.</summary>
        private const float DonorWheelRadius = 0.5f;

        /// <summary>The cart body mesh is this long, nose to tail.</summary>
        private const float DonorFrameLength = 3.27f;

        /// <summary>
        /// The chair's own seat pan, in its mesh's coordinates. Found by counting the
        /// mesh's vertices in horizontal slices: they cluster into a base, a flat
        /// surface 0.50 up that runs from z -0.14 to 0.39, and a back that leans away
        /// to z -0.50 at full height. The middle one is what a sitter rests on, and
        /// they rest towards the back of it.
        /// </summary>
        private static readonly Vector3 ChairPan = new Vector3(0f, 0.5f, -0.05f);

        /// <summary>
        /// The chair is 1.20 m tall and 1.14 m deep, which is a great deal of furniture
        /// to carry on a bicycle, so it is taken in. Not evenly, though: it is only
        /// 0.51 m wide to begin with, and shrinking that to match would leave the rider
        /// overhanging both sides of their own seat.
        /// </summary>
        private static readonly Vector3 ChairScale = new Vector3(0.85f, 0.5f, 0.55f);

        internal static void Apply(GameObject prefab)
        {
            if (!BicicretaPlugin.UseStandInModel.Value)
            {
                BicicretaPlugin.Log.LogInfo("UseStandInModel is off; leaving the clone's own model in place.");
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

            var wheelScale = Vector3.one * (BicicretaGeometry.WheelRadius / DonorWheelRadius);

            var parts = 0;
            parts += Fit(CartSource, WheelPath, visual.transform, "FrontWheel",
                new Vector3(0f, BicicretaGeometry.WheelRadius, BicicretaGeometry.FrontWheel),
                wheelScale);
            parts += Fit(CartSource, WheelPath, visual.transform, "RearWheel",
                new Vector3(0f, BicicretaGeometry.WheelRadius, BicicretaGeometry.RearWheel),
                wheelScale);
            parts += Fit(CartSource, FramePath, visual.transform, "Frame",
                new Vector3(0f, BicicretaGeometry.FrameHeight, BicicretaGeometry.FrameMiddle),
                Vector3.one * (BicicretaGeometry.FrameLength / DonorFrameLength));

            // Placed by its own seat pan rather than by the middle of its mesh, so that
            // the pan is what lands under the rider.
            parts += Fit(ChairSource, ChairPath, visual.transform, "Seat",
                new Vector3(0f, BicicretaGeometry.SeatPanHeight, BicicretaGeometry.SeatPanOffset),
                ChairScale, ChairPan);

            BicicretaPlugin.Log.LogInfo(
                $"Hid {hidden} lox renderer(s) and built a bicycle from {parts} borrowed part(s).");
        }

        /// <summary>
        /// Copies one mesh out of a donor prefab and places it so that
        /// <paramref name="anchor"/> - a point in the mesh's own coordinates, or the
        /// middle of it when not given - lands on <paramref name="target"/>, relative to
        /// the bicycle's own origin.
        ///
        /// Only leaf objects holding nothing but a mesh are borrowed. An earlier version
        /// took a whole wheel, which dragged along the cart's rigidbody and the joint
        /// holding it to the axle; the joint then refused to let the rigidbody be
        /// removed. These meshes are also modelled off to one side of their own pivot, so
        /// placing one means correcting for where its geometry actually sits.
        /// </summary>
        internal static int Fit(
            string donorName, string path, Transform parent, string name, Vector3 target,
            Vector3 scale, Vector3? anchor = null)
        {
            var donor = PrefabManager.Instance.GetPrefab(donorName);
            var source = donor == null ? null : donor.transform.Find(path);
            var mesh = source == null ? null : source.GetComponent<MeshFilter>();
            if (mesh == null || mesh.sharedMesh == null)
            {
                BicicretaPlugin.Log.LogWarning(
                    $"'{donorName}/{path}' is gone; the bicycle is missing its {name}.");
                return 0;
            }

            var part = Object.Instantiate(source.gameObject, parent);
            part.name = name;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = scale;
            part.transform.localPosition =
                target - Vector3.Scale(anchor ?? mesh.sharedMesh.bounds.center, scale);

            // A LODGroup decides visibility from a size it recorded for the object it was
            // authored on, which no longer describes a part that has been rescaled and
            // reparented. Left in place it can switch the mesh off again.
            foreach (var lod in part.GetComponentsInChildren<LODGroup>(includeInactive: true))
            {
                Object.DestroyImmediate(lod);
            }

            foreach (var renderer in part.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = true;
            }

            return 1;
        }
    }
}
