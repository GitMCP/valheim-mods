using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// What a retainer does when nobody has told it to do anything.
    ///
    /// This is the job with no output, and it is the one that decides whether the mod
    /// works. A companion standing at parade rest on the exact spot you left it reads as
    /// a spawned prefab no matter how good the rest of its behaviour is; the same
    /// companion drifting over to the fire, standing there a while, wandering off to look
    /// at the chests and muttering about the weather reads as somebody waiting for you.
    /// Nothing here is useful and all of it is the point.
    ///
    /// It is also written to be cheap, because it is what nearly every retainer is doing
    /// nearly all of the time: one short walk every ten seconds or so, and a search for
    /// somewhere worth standing only when it is time to choose.
    /// </summary>
    internal class HirdmanIdle : HirdmanWork
    {
        /// <summary>How far from home a retainer will drift while waiting.</summary>
        private const float Range = 9f;

        private const float RestMin = 6f;
        private const float RestMax = 18f;

        /// <summary>How often it is worth looking for somewhere better to stand.</summary>
        private const float HauntChance = 0.45f;

        private const float HauntRadius = 12f;

        private static readonly string[] Mutterings =
        {
            "Quiet today.",
            "There's work needs doing, if you've a mind.",
            "Cold enough to freeze the sea.",
            "I could eat.",
            "Long way from anywhere, this.",
            "Wind's turning.",
            "Say the word and I'll go.",
        };

        private Vector3 _spot;
        private bool _walking;
        private float _restUntil;
        private float _mutteredAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            Mutter(body);

            // Wandered too far - carried off by a fight, or the bell moved home. Getting
            // back matters more than looking busy.
            if (!body.Near(order.Anchor, Range * 2f))
            {
                return Hold(body, order.Anchor, dt);
            }

            if (_walking)
            {
                if (!body.Approach(dt, _spot, HirdmanBody.ArriveDistance))
                {
                    return true;
                }

                _walking = false;
                _restUntil = Time.time + Random.Range(RestMin, RestMax);
            }

            if (Time.time < _restUntil)
            {
                body.Stop();
                return true;
            }

            _spot = Choose(body, order.Anchor);
            _walking = true;
            return true;
        }

        /// <summary>
        /// Somewhere to stand. Usually anywhere, but often enough near something a person
        /// would actually gravitate to - a fire, a bench, the stores - that the milling
        /// about looks like it has reasons behind it.
        /// </summary>
        private static Vector3 Choose(HirdmanBody body, Vector3 anchor)
        {
            if (Random.value < HauntChance)
            {
                var haunt = Haunt(anchor);
                if (haunt.HasValue)
                {
                    return haunt.Value;
                }
            }

            return Somewhere(anchor, 2f, Range);
        }

        private static Vector3? Haunt(Vector3 anchor)
        {
            var found = 0;
            Vector3 chosen = Vector3.zero;

            foreach (var collider in Physics.OverlapSphere(anchor, HauntRadius))
            {
                var thing = collider.transform.root;
                if (thing.GetComponentInChildren<Fireplace>() == null &&
                    thing.GetComponentInChildren<CraftingStation>() == null &&
                    thing.GetComponentInChildren<Container>() == null)
                {
                    continue;
                }

                // Reservoir sampling: one pass, no list, and every candidate equally
                // likely without knowing up front how many there are.
                found++;
                if (Random.Range(0, found) == 0)
                {
                    chosen = thing.position;
                }
            }

            return found == 0 ? (Vector3?)null : Somewhere(chosen, 1.2f, 2.5f);
        }

        private void Mutter(HirdmanBody body)
        {
            if (Time.time - _mutteredAt < Random.Range(60f, 150f))
            {
                return;
            }

            _mutteredAt = Time.time;

            // Only worth saying if somebody is close enough to hear it, and speech is
            // drawn on the listener's machine rather than sent anywhere.
            if (Player.m_localPlayer != null &&
                Vector3.Distance(Player.m_localPlayer.transform.position, body.Position) < 12f)
            {
                body.Say(Mutterings[Random.Range(0, Mutterings.Length)]);
            }
        }
    }

    /// <summary>
    /// Holding ground. Nothing more than standing on the spot, because the part that
    /// matters - noticing something hostile and going for it - is what
    /// <see cref="MonsterAI"/> already does with every frame this job hands back.
    /// </summary>
    internal class HirdmanGuard : HirdmanWork
    {
        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            return Hold(body, order.Anchor, dt);
        }
    }
}
