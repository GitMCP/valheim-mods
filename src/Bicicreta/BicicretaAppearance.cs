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

        /// <summary>And this wide.</summary>
        private const float DonorFrameWidth = 1.546f;

        /// <summary>
        /// Every wooden building piece is the same unit cube under a different scale, so
        /// the pole and the beam a handlebar would be built from in game are the same
        /// mesh, and any box of wood can be had by asking for one of them at a size.
        /// </summary>
        private const string WoodSource = "wood_pole";

        private const string WoodPath = "New";

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

            var frontHub = Hub(visual.transform, "FrontWheel", BicicretaGeometry.FrontWheel);
            var rearHub = Hub(visual.transform, "RearWheel", BicicretaGeometry.RearWheel);

            var parts = 0;
            parts += Fit(CartSource, WheelPath, frontHub, "FrontWheel", Vector3.zero, wheelScale);
            parts += Fit(CartSource, WheelPath, rearHub, "RearWheel", Vector3.zero, wheelScale);

            var frameLengthways = BicicretaGeometry.FrameLength / DonorFrameLength;
            parts += Fit(CartSource, FramePath, visual.transform, "Frame",
                new Vector3(0f, BicicretaGeometry.FrameHeight, BicicretaGeometry.FrameMiddle),
                new Vector3(
                    BicicretaGeometry.FrameWidth / DonorFrameWidth,
                    frameLengthways,
                    frameLengthways));

            // Placed by its own seat pan rather than by the middle of its mesh, so that
            // the pan is what lands under the rider.
            parts += Fit(ChairSource, ChairPath, visual.transform, "Seat",
                new Vector3(0f, BicicretaGeometry.SeatPanHeight, BicicretaGeometry.SeatPanOffset),
                ChairScale, ChairPan);

            parts += Handlebar(visual.transform);

            var wheels = prefab.AddComponent<BicicretaWheels>();
            wheels.FrontHub = frontHub;
            wheels.RearHub = rearHub;

            BicicretaPlugin.Log.LogInfo(
                $"Hid {hidden} lox renderer(s) and built a bicycle from {parts} borrowed part(s).");
        }

        /// <summary>
        /// An empty at a wheel's axle, for the wheel itself to hang inside. Turning a
        /// wheel means turning one of these rather than the mesh: the borrowed meshes sit
        /// off to one side of their own pivots, so a wheel rotated about its own origin
        /// would swing around the bicycle instead of spinning where it stands.
        /// </summary>
        private static Transform Hub(Transform parent, string name, float offset)
        {
            var hub = new GameObject(name + "Hub");
            hub.transform.SetParent(parent, worldPositionStays: false);
            hub.transform.localPosition =
                new Vector3(0f, BicicretaGeometry.WheelRadius, offset);
            hub.transform.localRotation = Quaternion.identity;
            return hub.transform;
        }

        /// <summary>
        /// Builds the handlebar: a post standing on the front of the frame, a short neck
        /// reaching back from the top of it, and the bar itself across the rider's hands.
        ///
        /// A bare T would not do. The rider's hands come to rest above their knees, well
        /// behind the front of the frame, so a post under the bar would either rise out
        /// of the middle of the frame or leave the bar out of reach; the neck lets the
        /// post stand at the front and the bar sit where it is held.
        ///
        /// The neck is thinner than the two it joins so that it ends inside them. Boxes
        /// that share a face fight over which of them is drawn there.
        /// </summary>
        private static int Handlebar(Transform parent)
        {
            var thick = BicicretaGeometry.BarThickness;
            var neck = thick * 0.7f;

            // Down to the middle of the frame rather than the top of it, so that the
            // post is planted in the body instead of balanced on it.
            var stem = BicicretaGeometry.HandlebarHeight - BicicretaGeometry.FrameHeight;

            var parts = Fit(WoodSource, WoodPath, parent, "HandlebarStem",
                new Vector3(
                    0f,
                    BicicretaGeometry.HandlebarHeight - stem / 2f,
                    BicicretaGeometry.StemOffset),
                new Vector3(thick, stem, thick));

            parts += Fit(WoodSource, WoodPath, parent, "HandlebarNeck",
                new Vector3(
                    0f,
                    BicicretaGeometry.HandlebarHeight,
                    (BicicretaGeometry.StemOffset + BicicretaGeometry.HandlebarOffset) / 2f),
                new Vector3(
                    neck,
                    neck,
                    BicicretaGeometry.StemOffset - BicicretaGeometry.HandlebarOffset));

            parts += Fit(WoodSource, WoodPath, parent, "HandlebarBar",
                new Vector3(
                    0f, BicicretaGeometry.HandlebarHeight, BicicretaGeometry.HandlebarOffset),
                new Vector3(BicicretaGeometry.HandlebarWidth, thick, thick));

            return parts;
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

            // These parts are scenery. Anything the bicycle is meant to collide with is
            // the one capsule BicicretaBody leaves it, so a donor that brings a collider
            // of its own along would only be in the way.
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
