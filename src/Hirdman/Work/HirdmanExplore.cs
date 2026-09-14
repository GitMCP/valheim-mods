using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Ranging about and putting what it sees on its employer's map.
    ///
    /// The honest version of this job is smaller than it sounds, and the reason is worth
    /// writing down. Valheim only simulates the world near a player: a creature in a zone
    /// nobody is standing in is not slow, it does not exist, so a scout sent over the
    /// horizon stops thinking the moment it crosses out of everyone's active area and
    /// wakes up unchanged whenever somebody wanders back. No mod can send a companion
    /// off to survey a continent, because there is nothing running out there to survey
    /// it with.
    ///
    /// What is worth doing instead is ranging around whoever hired it - circling out to
    /// the edge of what the engine keeps alive and back - so that walking anywhere with
    /// a scout in tow reveals a band of map several times wider than walking alone. That
    /// is a real difference on a long trip, and it is the whole of what this does.
    ///
    /// Which map the ground lands on is the other half. Exploration is per character and
    /// saved with it, so the reveal has to happen on one particular player's client and
    /// nowhere else - and the peer walking the scout is frequently not that client. So
    /// the scout reports where it is standing and <see cref="HirdmanCalls"/> carries it.
    /// </summary>
    internal class HirdmanExplore : HirdmanWork
    {
        /// <summary>How often to tell the employer's map where the scout is standing.</summary>
        private const float ReportInterval = 0.7f;

        /// <summary>Give up on a leg of the circuit that is taking too long.</summary>
        private const float LegTimeout = 20f;

        private Vector3 _leg;
        private bool _walking;
        private float _leftAt;
        private float _reportedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            Report(body);

            var centre = Centre(body, order);
            var range = HirdmanPlugin.ScoutRange.Value;

            // Whoever it is scouting for has moved on and left it behind.
            if (!body.Near(centre, range * 1.8f))
            {
                return Hold(body, centre, dt);
            }

            var stuck = Time.time - _leftAt > LegTimeout;
            if (_walking && !stuck && !body.Approach(dt, _leg, HirdmanBody.ArriveDistance * 2f))
            {
                return true;
            }

            // Out to the edge of what is loaded, rather than anywhere nearby: the point
            // of a scout is the ground its employer is not walking over.
            _leg = Somewhere(centre, range * 0.6f, range);
            _walking = true;
            _leftAt = Time.time;
            return true;
        }

        /// <summary>
        /// Who to range around. The employer if they are here, because a scout is worth
        /// most on the move; the work site if they are not, so that a scout left behind
        /// patrols instead of walking to the horizon.
        /// </summary>
        private static Vector3 Centre(HirdmanBody body, HirdmanOrder order)
        {
            if (order.Master != ZDOID.None && ZNetScene.instance != null)
            {
                var master = ZNetScene.instance.FindInstance(order.Master);
                if (master != null)
                {
                    return master.transform.position;
                }
            }

            return order.Anchor;
        }

        private void Report(HirdmanBody body)
        {
            if (Time.time - _reportedAt < ReportInterval)
            {
                return;
            }

            _reportedAt = Time.time;

            var zdo = body.Zdo;
            if (zdo == null)
            {
                return;
            }

            var owner = HirdmanContract.Read(zdo).Owner;
            if (owner != 0L)
            {
                HirdmanCalls.Scouted(owner, body.Position);
            }
        }
    }
}
