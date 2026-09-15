using UnityEngine;

namespace Rows
{
    /// <summary>
    /// Who is actually rowing: anyone attached to this ship who can reach the water.
    ///
    /// The captain already contributes Slow, Back, Half and Full through
    /// <see cref="ShipControlls"/>. Counting them again would double the paddle.
    /// A seat on the mast is too far inboard and too high to dip an oar, so it is
    /// not a rowing bench either.
    /// </summary>
    internal static class RowsCrew
    {
        /// <summary>
        /// Seats closer to the mast than this, in the horizontal plane, sit on the
        /// spine of the ship rather than at the gunwale.
        /// </summary>
        private const float MastRadius = 0.9f;

        /// <summary>
        /// Ship-local X inside this is the centerline: far from the water even when
        /// the mast object itself is a little forward or aft of the chair.
        /// </summary>
        private const float Centerline = 0.5f;

        internal static int Count(Ship ship)
        {
            if (ship == null)
            {
                return 0;
            }

            var rowers = 0;
            foreach (var player in Player.GetAllPlayers())
            {
                if (player == null || !player.IsAttachedToShip())
                {
                    continue;
                }

                if (CanRow(ship, player.GetAttachPoint()))
                {
                    rowers++;
                }
            }

            return rowers + Dummies;
        }

        /// <summary>
        /// Temporary console pretence. No models, no seats taken, just extra
        /// copies of the paddle so a solo helm can feel the speed.
        /// </summary>
        internal static int Dummies;

        internal static bool Occupied(Transform attachPoint)
        {
            if (attachPoint == null)
            {
                return false;
            }

            foreach (var player in Player.GetAllPlayers())
            {
                if (player != null && player.GetAttachPoint() == attachPoint)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool CanRow(Ship ship, Transform attach)
        {
            if (ship == null || attach == null)
            {
                return false;
            }

            if (attach.GetComponentInParent<Ship>() != ship)
            {
                return false;
            }

            if (ship.m_shipControlls != null && attach == ship.m_shipControlls.m_attachPoint)
            {
                return false;
            }

            var local = ship.transform.InverseTransformPoint(attach.position);
            if (Mathf.Abs(local.x) < Centerline)
            {
                return false;
            }

            var mast = ship.m_mastObject;
            if (mast != null)
            {
                if (attach.IsChildOf(mast.transform))
                {
                    return false;
                }

                var offset = attach.position - mast.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < MastRadius * MastRadius)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
