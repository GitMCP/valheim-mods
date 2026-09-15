using UnityEngine;

namespace Rows
{
    /// <summary>
    /// Who is actually rowing: anyone attached to this ship who is not at the helm.
    ///
    /// The captain already contributes Slow, Back, Half and Full through
    /// <see cref="ShipControlls"/>. Counting them again would double the paddle.
    /// A passenger chair is just an attach point, so the test is "attached to this
    /// hull, and not to the helm's own point".
    /// </summary>
    internal static class RowsCrew
    {
        internal static int Count(Ship ship)
        {
            if (ship == null)
            {
                return 0;
            }

            var helm = ship.m_shipControlls != null ? ship.m_shipControlls.m_attachPoint : null;
            var rowers = 0;
            foreach (var player in Player.GetAllPlayers())
            {
                if (player == null || !player.IsAttachedToShip())
                {
                    continue;
                }

                var point = player.GetAttachPoint();
                if (point == null || point == helm)
                {
                    continue;
                }

                if (point.GetComponentInParent<Ship>() == ship)
                {
                    rowers++;
                }
            }

            return rowers;
        }

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
    }
}
