using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Picking berries, mushrooms, thistle and anything else that grows.
    ///
    /// The interesting part is not the picking, it is being able to say what to pick. A
    /// player who asks for raspberries wants raspberries and not the mushrooms next to
    /// them, and the words they use are not the words the prefabs use, which is what
    /// <see cref="HirdmanCatalog"/> exists to bridge. An order with nothing named picks
    /// whatever is growing, which is the right default for standing in a meadow.
    /// </summary>
    internal class HirdmanGather : HirdmanWork
    {
        private const float Reach = 2f;
        private const float SearchInterval = 1.5f;
        private const float PickInterval = 1f;

        private Pickable _crop;
        private float _searchedAt;
        private float _pickedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            if (_crop == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _crop = Closest<Pickable>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                    p => p.CanBePicked() && HirdmanCatalog.Answers(order.Subject, p.gameObject));
            }

            // Everything worth picking has been picked. Waiting is the right answer:
            // most of it grows back, and the retainer is standing where it was sent.
            if (_crop == null)
            {
                return Hold(body, order.Anchor, dt);
            }

            if (!body.Approach(dt, _crop.transform.position, Reach))
            {
                return true;
            }

            if (Time.time - _pickedAt > PickInterval)
            {
                _pickedAt = Time.time;
                Pick(body, _crop);
                _crop = null;
            }

            // Whatever was picked a moment ago is lying at the retainer's feet.
            Scoop(body, Reach * 2f, null);
            return true;
        }

        /// <summary>
        /// Picks one, the way the game does.
        ///
        /// Ownership is taken first so that the pick resolves on this machine. That is
        /// not tidiness: the game's own pick handler reaches for the local player to
        /// decide where to play its effect, so it has to run somewhere there is one. A
        /// retainer being walked by a dedicated server therefore leaves the berries
        /// alone rather than bringing the server down, and picks them the moment a
        /// player is near enough to take it over.
        /// </summary>
        private static void Pick(HirdmanBody body, Pickable crop)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var nview = crop.m_nview;
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            if (!nview.IsOwner())
            {
                return;
            }

            crop.Interact(body.Humanoid, false, false);
        }
    }
}
