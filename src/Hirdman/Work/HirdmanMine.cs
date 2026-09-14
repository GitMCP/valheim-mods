using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Breaking rock.
    ///
    /// Mining is chopping with a different tool and one extra care. Ore deposits made of
    /// several pieces - which is most of them - decide which piece a blow landed on from
    /// the collider named in the hit, and ignore a blow that names none, so the search
    /// has to carry the collider it found all the way through to the swing rather than
    /// just the object.
    ///
    /// The other care is what not to hit. Half the destructible things in the world are
    /// somebody's house, and the rule that separates them is simple: anything with a
    /// <see cref="Piece"/> on it was built by a player, and a retainer does not swing a
    /// pickaxe at those.
    /// </summary>
    internal class HirdmanMine : HirdmanWork
    {
        private const float Reach = 2.6f;
        private const float SwingInterval = 1.6f;
        private const float SearchInterval = 2f;

        private Component _rock;
        private Collider _face;
        private float _searchedAt;
        private float _swungAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            if (_rock == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                Find(body, order);
                Scoop(body, HirdmanPlugin.HaulRadius, null);
            }

            if (_rock == null)
            {
                return Hold(body, order.Anchor, dt);
            }

            if (!body.Approach(dt, _rock.transform.position, Reach))
            {
                return true;
            }

            if (Time.time - _swungAt > SwingInterval)
            {
                _swungAt = Time.time;

                var pick = body.Wield(HirdmanRetainer.Pickaxe);
                if (pick == null)
                {
                    HirdmanPlugin.Log.LogWarning("A retainer has no pickaxe and cannot mine.");
                    _rock = null;
                    return true;
                }

                if (!body.Strike(_rock, pick, _face))
                {
                    _rock = null;
                }
            }

            return true;
        }

        private void Find(HirdmanBody body, HirdmanOrder order)
        {
            _rock = null;
            _face = null;
            var shortest = float.MaxValue;

            foreach (var collider in Physics.OverlapSphere(order.Anchor, HirdmanPlugin.WorkRadius.Value))
            {
                var rock = Deposit(collider);
                if (rock == null || !HirdmanCatalog.Answers(order.Subject, rock.gameObject))
                {
                    continue;
                }

                var distance = Vector3.Distance(body.Position, rock.transform.position);
                if (distance >= shortest)
                {
                    continue;
                }

                shortest = distance;
                _rock = rock;
                _face = collider;
            }
        }

        /// <summary>
        /// Is this a thing to mine? Rock that breaks into pieces, rock that breaks all at
        /// once, and the thin shells the game puts around silver and gold to hide them
        /// until the first blow - but never anything a player put there.
        /// </summary>
        private static Component Deposit(Collider collider)
        {
            var owner = collider.transform.root;
            if (owner.GetComponentInChildren<Piece>() != null)
            {
                return null;
            }

            Component rock = collider.GetComponentInParent<MineRock5>();
            if (rock != null)
            {
                return rock;
            }

            rock = collider.GetComponentInParent<MineRock>();
            if (rock != null)
            {
                return rock;
            }

            var breakable = collider.GetComponentInParent<Destructible>();
            return breakable != null && breakable.m_minToolTier > 0 ? breakable : null;
        }
    }
}
