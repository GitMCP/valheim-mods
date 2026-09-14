using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Killing something and bringing back what it drops.
    ///
    /// Almost none of this is fighting. A tamed creature already knows how to pick a
    /// quarry, close on it, circle, flinch and swing, and it does all of that better than
    /// anything written here would; what it will not do is go and start something. So
    /// this job chooses the quarry, points the game's own AI at it, and then gets out of
    /// the way by handing back every frame - which is also how the retainer keeps its
    /// vanilla combat behaviour instead of a worse copy of it.
    ///
    /// The one thing it does watch is whether the blows are landing. A retainer holding
    /// a tool its rig has no swing for would otherwise stand and mime at a boar forever,
    /// so if the quarry's health has not moved in a while the job hits it directly, the
    /// same way it would a tree. In the normal case that never fires.
    /// </summary>
    internal class HirdmanHunt : HirdmanWork
    {
        private const float Reach = 2.6f;
        private const float SearchInterval = 2f;

        /// <summary>How long to let the game's own swings do nothing before helping.</summary>
        private const float PatienceSeconds = 6f;

        private const float AssistInterval = 1.5f;

        private Character _quarry;
        private float _searchedAt;
        private float _quarryHealth;
        private float _hurtAt;
        private float _assistedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            if (_quarry != null && _quarry.IsDead())
            {
                _quarry = null;
            }

            if (_quarry == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _quarry = Find(body, order);

                if (_quarry != null)
                {
                    _quarryHealth = _quarry.GetHealth();
                    _hurtAt = Time.time;
                    Point(body, _quarry);
                }
            }

            // Anything that has been killed is lying about waiting to be carried home.
            Scoop(body, HirdmanPlugin.HaulRadius, null);

            if (_quarry == null)
            {
                return Hold(body, order.Anchor, dt);
            }

            // The game may have dropped the quarry for one of its own reasons - out of
            // range, out of sight - so it is pointed at it again rather than assumed.
            if (body.Ai.GetTargetCreature() != _quarry)
            {
                Point(body, _quarry);
            }

            Assist(body);

            // Handing the frame back is what makes this a fight rather than a walk.
            return false;
        }

        /// <summary>
        /// Points the game's own AI at one particular creature.
        ///
        /// Its <see cref="MonsterAI.SetTarget"/> cannot be used for this. That method
        /// only fills an empty slot, and the AI refills the slot itself every couple of
        /// seconds with whatever enemy is nearest - so an order to hunt the boar by the
        /// treeline turns silently into a fight with the greyling underfoot, and every
        /// attempt to correct it is ignored for as long as the greyling lives. Naming
        /// the quarry outright is the only way to be heard, and it has to be done again
        /// after each of those sweeps, which is what the caller above does.
        /// </summary>
        private static void Point(HirdmanBody body, Character quarry)
        {
            var ai = body.Ai;
            ai.m_targetCreature = quarry;
            ai.m_lastKnownTargetPos = quarry.transform.position;
            ai.m_beenAtLastPos = false;
            ai.SetAlerted(true);
        }

        private Character Find(HirdmanBody body, HirdmanOrder order)
        {
            Character closest = null;
            var shortest = HirdmanPlugin.WorkRadius.Value;

            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.IsDead() || character.IsPlayer())
                {
                    continue;
                }

                // Never another retainer, never anybody's pet, and only things the game
                // itself agrees are fair game - which for a tamed creature is everything
                // wild, and nothing belonging to a player.
                if (character.GetComponent<HirdmanTag>() != null || character.IsTamed())
                {
                    continue;
                }

                if (!BaseAI.IsEnemy(body.Humanoid, character))
                {
                    continue;
                }

                if (!HirdmanCatalog.Answers(order.Subject, character.gameObject))
                {
                    continue;
                }

                // Measured from the work site, so "hunt here" does not turn into a chase
                // across the map one boar at a time.
                if (Vector3.Distance(order.Anchor, character.transform.position) > shortest)
                {
                    continue;
                }

                var distance = Vector3.Distance(body.Position, character.transform.position);
                if (closest == null || distance < Vector3.Distance(body.Position, closest.transform.position))
                {
                    closest = character;
                }
            }

            return closest;
        }

        /// <summary>
        /// Lands a blow by hand, but only once it is clear the ordinary ones are not.
        /// </summary>
        private void Assist(HirdmanBody body)
        {
            var health = _quarry.GetHealth();
            if (health < _quarryHealth - 0.01f)
            {
                _quarryHealth = health;
                _hurtAt = Time.time;
                return;
            }

            if (Time.time - _hurtAt < PatienceSeconds ||
                Time.time - _assistedAt < AssistInterval ||
                !body.Near(_quarry.transform.position, Reach))
            {
                return;
            }

            _assistedAt = Time.time;

            var weapon = body.Humanoid.GetCurrentWeapon();
            if (weapon == null)
            {
                return;
            }

            var hit = new HitData
            {
                m_point = _quarry.GetCenterPoint(),
                m_dir = (_quarry.transform.position - body.Position).normalized,
                m_damage = weapon.GetDamage(),
                m_toolTier = (short)weapon.m_shared.m_toolTier,
                m_itemWorldLevel = (byte)weapon.m_worldLevel,
            };
            hit.SetAttacker(body.Humanoid);

            _quarry.Damage(hit);
        }
    }
}
