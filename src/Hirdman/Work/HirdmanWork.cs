using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// One thing a retainer knows how to do.
    ///
    /// A job owns its own memory - which tree it chose, how long since it last looked
    /// around - and that memory is thrown away when the order changes, which is why
    /// these are objects rather than a switch in the brain. It also means a new job is a
    /// new file and nothing else: the brain, the parser, the model prompt and the chat
    /// window all learn about it from <see cref="HirdmanJob"/>.
    /// </summary>
    internal abstract class HirdmanWork
    {
        /// <summary>
        /// Do a frame of it.
        /// </summary>
        /// <returns>
        /// True when the job has taken the frame and the game's own AI should be skipped.
        /// Handing a frame back is how a retainer keeps vanilla's flinching, circling and
        /// weapon choice for free, so jobs give back every frame they do not need.
        /// </returns>
        internal abstract bool Run(HirdmanBody body, HirdmanOrder order, float dt);

        /// <summary>
        /// How far a standing job will walk to keep working. The work radius is one
        /// search from where they stand; this is the leash from the original spot, so
        /// a forest does not end after the first twenty-four metres.
        /// </summary>
        internal static float Roam
        {
            get
            {
                return Mathf.Max(HirdmanPlugin.WorkRadius.Value, HirdmanPlugin.ScoutRange.Value);
            }
        }

        internal static HirdmanWork For(HirdmanJob job)
        {
            switch (job)
            {
                case HirdmanJob.ChopWood:
                    return new HirdmanChop();
                case HirdmanJob.Explore:
                    return new HirdmanExplore();
                case HirdmanJob.Gather:
                    return new HirdmanGather();
                case HirdmanJob.Mine:
                    return new HirdmanMine();
                case HirdmanJob.Farm:
                    return new HirdmanFarm();
                case HirdmanJob.Cook:
                    return new HirdmanCook();
                case HirdmanJob.Hunt:
                    return new HirdmanHunt();
                case HirdmanJob.Haul:
                    return new HirdmanHaul();
                case HirdmanJob.Guard:
                    return new HirdmanGuard();
                default:
                    return new HirdmanIdle();
            }
        }

        /// <summary>
        /// Mill about the work site rather than standing on the exact spot. Used when a
        /// job has run out of things to do. Guarding still stands; that is the point of
        /// guarding.
        /// </summary>
        protected static bool Hold(HirdmanBody body, Vector3 anchor, float dt)
        {
            var phase = Mathf.Floor(Time.time / 4f);
            var angle = phase * 2.399f;
            var reach = 2.5f + (phase % 4f);
            var point = anchor + new Vector3(Mathf.Cos(angle) * reach, 0f, Mathf.Sin(angle) * reach);
            if (ZoneSystem.instance != null)
            {
                float height;
                if (ZoneSystem.instance.GetGroundHeight(point, out height))
                {
                    point.y = height;
                }
            }

            body.Approach(dt, point, HirdmanBody.ArriveDistance);
            return true;
        }

        /// <summary>
        /// The nearest thing of a kind worth walking to.
        ///
        /// Every job starts with a search like this, and they all want the same thing:
        /// look at what is around the work site, keep the ones this job cares about, and
        /// go to whichever is closest to where the retainer is standing rather than to
        /// the site. A collider search is the right tool because the world is only
        /// loaded near a player anyway, so the sphere is never large.
        /// </summary>
        protected static T Closest<T>(HirdmanBody body, Vector3 centre, float radius, System.Func<T, bool> wanted)
            where T : Component
        {
            return Closest(body.Position, centre, radius, wanted);
        }

        /// <summary>
        /// The same search, measured from an arbitrary point. Finishing a tree wants the
        /// log that belongs to that stump, not whichever log happens to be nearest the
        /// retainer's feet.
        /// </summary>
        protected static T Closest<T>(Vector3 from, Vector3 centre, float radius, System.Func<T, bool> wanted)
            where T : Component
        {
            T closest = null;
            var shortest = float.MaxValue;

            foreach (var collider in Physics.OverlapSphere(centre, radius))
            {
                var candidate = collider.GetComponentInParent<T>();
                if (candidate == null || (wanted != null && !wanted(candidate)))
                {
                    continue;
                }

                var distance = Vector3.Distance(from, candidate.transform.position);
                if (distance < shortest)
                {
                    shortest = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        /// <summary>
        /// Sweeps up what is lying on the ground nearby. Every job that breaks something
        /// open ends with this, because a retainer that fells a wood and leaves the logs
        /// where they fell has not really done the job.
        /// </summary>
        protected static int Scoop(HirdmanBody body, float radius, System.Func<GameObject, bool> wanted)
        {
            var taken = 0;

            foreach (var collider in Physics.OverlapSphere(body.Position, radius, HirdmanBody.ItemMask()))
            {
                var drop = HirdmanBody.DropOn(collider);
                if (drop == null || (wanted != null && !wanted(drop.gameObject)))
                {
                    continue;
                }

                if (body.Take(drop))
                {
                    taken++;
                }
            }

            return taken;
        }

        /// <summary>
        /// A point to walk to, somewhere in a ring around a centre, on ground that
        /// exists. Used by everything that mills about rather than going anywhere.
        /// </summary>
        protected static Vector3 Somewhere(Vector3 centre, float inner, float outer)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var reach = Random.Range(inner, outer);
            var point = centre + new Vector3(Mathf.Cos(angle) * reach, 0f, Mathf.Sin(angle) * reach);

            if (ZoneSystem.instance != null)
            {
                float height;
                if (ZoneSystem.instance.GetGroundHeight(point, out height))
                {
                    point.y = height;
                }
            }

            return point;
        }
    }
}
