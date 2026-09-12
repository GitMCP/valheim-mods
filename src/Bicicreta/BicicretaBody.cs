using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Brings the lox's physical size and its rider down to bicycle scale.
    ///
    /// A lox measures roughly 5.5 m by 4.6 m by 7.6 m and seats its rider 3.4 m up. Left
    /// alone, that gives a bicycle you cannot walk near because you collide with an
    /// animal that is not drawn, and a rider hovering two metres above the saddle.
    /// </summary>
    internal static class BicicretaBody
    {
        internal static void Apply(GameObject prefab)
        {
            Resize(prefab);
            LowerRider(prefab);
        }

        private static void Resize(GameObject prefab)
        {
            var capsule = prefab.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                BicicretaPlugin.Log.LogWarning("No CapsuleCollider on the bicycle; it will keep the lox's bulk.");
                return;
            }

            capsule.height = BicicretaGeometry.BodyHeight;
            capsule.radius = BicicretaGeometry.BodyRadius;
            capsule.center = new Vector3(0f, BicicretaGeometry.BodyHeight / 2f, 0f);

            // The lox carries extra capsules for precise hits on its body and head. They
            // are sized and placed for an animal, so they would stick out of a bicycle;
            // the body collider above is enough to stand in the way and to be hit.
            foreach (var extra in prefab.GetComponentsInChildren<CapsuleCollider>(includeInactive: true))
            {
                if (extra != capsule)
                {
                    extra.gameObject.SetActive(false);
                }
            }

            // Eye height decides where the creature looks from, and a bicycle's rider
            // should not be sighting along a lox's brow.
            var eyes = prefab.transform.Find("EyePos");
            if (eyes != null)
            {
                eyes.localPosition = new Vector3(0f, BicicretaGeometry.SeatPanHeight, 0f);
            }
        }

        /// <summary>
        /// Moves the seat onto the frame.
        ///
        /// The attach point normally hangs off a bone inside the lox's armature, so the
        /// rider is carried along by the walk animation. Re-parenting it to the root both
        /// puts the rider on the bicycle and stops them bobbing like a passenger on an
        /// animal, which is closer to how a bicycle should feel anyway.
        /// </summary>
        private static void LowerRider(GameObject prefab)
        {
            var saddle = prefab.GetComponentInChildren<Sadle>(includeInactive: true);
            if (saddle == null || saddle.m_attachPoint == null)
            {
                BicicretaPlugin.Log.LogWarning("No saddle attach point; the rider will sit at lox height.");
                return;
            }

            // This is where the rider's root goes, not where they appear to sit: the
            // pose carries them back and down from it onto the seat.
            var seat = new Vector3(
                0f, BicicretaGeometry.AttachHeight, BicicretaGeometry.AttachOffset);

            saddle.m_attachPoint.SetParent(prefab.transform, worldPositionStays: false);
            saddle.m_attachPoint.localPosition = seat;
            saddle.m_attachPoint.localRotation = Quaternion.identity;

            // The saddle object is parked up where a lox's back would be, which would
            // leave something aimable hovering above the bicycle. Its own collider is not
            // needed, because the body is what the player aims at now.
            saddle.transform.localPosition = seat;
            foreach (var collider in saddle.GetComponents<Collider>())
            {
                collider.enabled = false;
            }
        }
    }
}
