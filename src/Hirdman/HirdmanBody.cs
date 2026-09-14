using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Everything a job is allowed to do to the world, in one place.
    ///
    /// The jobs below this are meant to read like instructions to a person - walk there,
    /// swing at that, put this away - and none of them should have to know that walking
    /// is <see cref="BaseAI.MoveTo"/> with a stopping distance, or that swinging is a
    /// hand-built <see cref="HitData"/> carrying the numbers off whatever is in the
    /// retainer's hand. Keeping that in one place is also the only way the seven jobs
    /// stay comparable in length; the interesting part of mining is choosing the rock,
    /// not hitting it.
    /// </summary>
    internal class HirdmanBody
    {
        /// <summary>How near a point counts as standing on it.</summary>
        internal const float ArriveDistance = 1.5f;

        private readonly Humanoid _humanoid;
        private readonly MonsterAI _ai;
        private readonly ZNetView _nview;

        internal HirdmanBody(GameObject retainer)
        {
            Go = retainer;
            _humanoid = retainer.GetComponent<Humanoid>();
            _ai = retainer.GetComponent<MonsterAI>();
            _nview = retainer.GetComponent<ZNetView>();
        }

        internal GameObject Go { get; }

        internal Vector3 Position => Go.transform.position;

        internal Humanoid Humanoid => _humanoid;

        internal MonsterAI Ai => _ai;

        internal ZDO Zdo => _nview != null && _nview.IsValid() ? _nview.GetZDO() : null;

        internal Inventory Inventory => _humanoid == null ? null : _humanoid.GetInventory();

        internal bool Near(Vector3 point, float distance)
        {
            return Vector3.Distance(Position, point) <= distance;
        }

        /// <summary>Walk towards a point, and say whether you have got there.</summary>
        internal bool Approach(float dt, Vector3 point, float stopWithin)
        {
            if (Near(point, stopWithin))
            {
                _ai.StopMoving();
                _ai.LookAt(point);
                return true;
            }

            _ai.MoveTo(dt, point, stopWithin * 0.75f, false);
            return false;
        }

        internal void Stop()
        {
            _ai.StopMoving();
        }

        internal void Say(string line)
        {
            HirdmanSpeech.Say(Go, line);
        }

        /// <summary>
        /// Puts the right tool in the retainer's hand, if it has one. Switching costs
        /// nothing when the tool is already held, which is the usual case, so jobs call
        /// this every time rather than remembering.
        /// </summary>
        internal ItemDrop.ItemData Wield(string prefabName)
        {
            var inventory = Inventory;
            if (inventory == null)
            {
                return null;
            }

            var tool = inventory.GetItem(prefabName, -1, true);
            if (tool == null)
            {
                return null;
            }

            if (_humanoid.GetCurrentWeapon() != tool)
            {
                _humanoid.EquipItem(tool, true);
            }

            return tool;
        }

        /// <summary>
        /// Hits something the way a player does: with the damage of the tool actually in
        /// the retainer's hands, so the game's own damage rules, tool tiers and drop
        /// tables decide what happens. A stone axe cannot fell a birch here either, and
        /// a bronze pickaxe cannot touch silver.
        /// </summary>
        internal bool Strike(Component target, ItemDrop.ItemData tool, Collider where = null)
        {
            var destructible = target as IDestructible;
            if (destructible == null || tool == null)
            {
                return false;
            }

            var point = where != null
                ? where.ClosestPoint(Position + Vector3.up)
                : target.transform.position + Vector3.up;

            // For the animation. It is allowed to fail: the blow is what the world
            // responds to, and a retainer swinging stiffly is better than one that stops.
            _humanoid.StartAttack(null, false);

            var hit = new HitData
            {
                m_point = point,
                m_dir = (point - Position).normalized,
                m_damage = tool.GetDamage(),
                m_toolTier = (short)tool.m_shared.m_toolTier,

                // Rocks made of several pieces decide which piece was struck from the
                // collider named here, and quietly ignore a blow that names none.
                m_hitCollider = where,
            };
            hit.SetAttacker(_humanoid);

            destructible.Damage(hit);
            return true;
        }

        /// <summary>
        /// Picks something up off the ground, if there is room for it.
        /// </summary>
        internal bool Take(ItemDrop drop)
        {
            if (drop == null || !drop.CanPickup(false))
            {
                return false;
            }

            var inventory = Inventory;
            if (inventory == null || !inventory.CanAddItem(drop.m_itemData, drop.m_itemData.m_stack))
            {
                return false;
            }

            return _humanoid.Pickup(drop.gameObject, false, false);
        }

        /// <summary>
        /// How full the retainer is, as a fraction. Jobs use it to decide when to stop
        /// working and go and empty their arms.
        /// </summary>
        internal float Burden()
        {
            var inventory = Inventory;
            if (inventory == null)
            {
                return 0f;
            }

            var slots = inventory.GetWidth() * inventory.GetHeight();
            return slots <= 0 ? 0f : (float)inventory.NrOfItems() / slots;
        }
    }
}
