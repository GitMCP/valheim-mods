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
            if (body.Need(Skills.SkillType.Pickaxes, "I have no pickaxe. I'll need one before I can mine.") == null)
            {
                _rock = null;
                return Hold(body, order.Anchor, dt);
            }

            if (_rock == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                Find(body, order);
                Scoop(body, HirdmanPlugin.HaulRadius, null);
                if (_rock == null)
                {
                    body.Ask("Nothing here I can break with this pickaxe.");
                }
            }

            if (_rock == null)
            {
                return Hold(body, order.Anchor, dt);
            }

            var stand = StandBy(_rock, body.Position);
            if (body.Position.y < stand.y - 1.2f)
            {
                // In a hole under the deposit. Climb out; do not swing down.
                body.Approach(dt, stand, HirdmanBody.ArriveDistance);
                return true;
            }

            if (!body.Approach(dt, stand, Reach))
            {
                return true;
            }

            if (Time.time - _swungAt > SwingInterval)
            {
                _swungAt = Time.time;

                var pick = body.Wield(Skills.SkillType.Pickaxes);
                if (pick == null || !body.Strike(_rock, pick, _face))
                {
                    _rock = null;
                }
            }

            return true;
        }

        /// <summary>
        /// A point on the ground beside the rock, not its centre, which for a boulder is
        /// often inside the mesh and for a deposit on a slope is above the dirt. Standing
        /// under it is how a retainer starts mining the floor.
        /// </summary>
        private static Vector3 StandBy(Component rock, Vector3 from)
        {
            var pos = rock.transform.position;
            var dir = from - pos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = Vector3.forward;
            }

            dir.Normalize();
            var stand = pos + dir * 1.8f;
            if (ZoneSystem.instance != null)
            {
                float height;
                if (ZoneSystem.instance.GetGroundHeight(stand, out height))
                {
                    stand.y = height;
                }
            }

            return stand;
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

                if (!body.CanBreak(MinTier(rock), Skills.SkillType.Pickaxes))
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
                _face = Terrain(collider) ? null : collider;
            }
        }

        /// <summary>
        /// Is this a thing to mine? Rock that breaks into pieces, rock that breaks all at
        /// once, and the thin shell the game puts over a deposit to hide it until the
        /// first blow - but never anything a player put there.
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
            return breakable != null && Shell(breakable) ? breakable : null;
        }

        private static bool Terrain(Collider collider)
        {
            return collider == null || collider.GetComponent<Heightmap>() != null ||
                   collider.gameObject.layer == LayerMask.NameToLayer("terrain");
        }

        /// <summary>
        /// Is this breakable thing a deposit rather than scenery?
        ///
        /// Most ore is not what it appears to be. A copper boulder is a shell with one
        /// point of health and no ore in it at all; the first blow destroys it and leaves
        /// behind the rock that can actually be mined. Judging those shells by the tool
        /// they demand does not work, because copper, tin and the muddy scrap piles
        /// demand nothing - so the obvious rule quietly skips exactly the ores a player
        /// is most likely to ask for, and a retainer told to mine copper stands in a
        /// field of it reporting that there is none.
        ///
        /// What a deposit does have is something to leave behind: either loot of its own,
        /// or the mineable rock underneath. Trees and berry bushes are somebody else's
        /// work, so they are turned away even when they would otherwise qualify.
        /// </summary>
        private static bool Shell(Destructible breakable)
        {
            if (breakable.GetDestructibleType() == DestructibleType.Tree ||
                breakable.GetComponent<Pickable>() != null)
            {
                return false;
            }

            if (breakable.m_minToolTier > 0 || breakable.GetComponent<DropOnDestroyed>() != null)
            {
                return true;
            }

            var leaves = breakable.m_spawnWhenDestroyed;
            return leaves != null &&
                   (leaves.GetComponent<MineRock5>() != null || leaves.GetComponent<MineRock>() != null);
        }

        private static int MinTier(Component rock)
        {
            switch (rock)
            {
                case MineRock5 broken:
                    return broken.m_minToolTier;
                case MineRock legacy:
                    return legacy.m_minToolTier;
                case Destructible shell:
                    return shell.m_minToolTier;
                default:
                    return 0;
            }
        }
    }
}
