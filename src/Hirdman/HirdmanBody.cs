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
    /// retainer's hand.
    /// </summary>
    internal class HirdmanBody
    {
        /// <summary>How near a point counts as standing on it.</summary>
        internal const float ArriveDistance = 1.5f;

        private const float AskInterval = 12f;

        private static int _itemMask;

        private readonly Humanoid _humanoid;
        private readonly MonsterAI _ai;
        private readonly ZNetView _nview;
        private float _askedAt = float.MinValue;

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
        /// Says something, but not every frame. A retainer without an axe would otherwise
        /// ask for one sixty times a second.
        /// </summary>
        internal void Ask(string line)
        {
            if (Time.time - _askedAt < AskInterval)
            {
                return;
            }

            _askedAt = Time.time;
            Say(line);
        }

        /// <summary>
        /// Puts the best tool of a kind into the retainer's hands, or says they have none.
        /// </summary>
        internal ItemDrop.ItemData Need(Skills.SkillType skill, string missing)
        {
            var tool = Wield(skill);
            if (tool != null)
            {
                return tool;
            }

            Ask(missing);
            return null;
        }

        /// <summary>
        /// Puts the best tool of a kind in the retainer's hand, if it has one. Switching
        /// costs nothing when the tool is already held, which is the usual case, so jobs
        /// call this every time rather than remembering.
        /// </summary>
        internal ItemDrop.ItemData Wield(Skills.SkillType skill)
        {
            var tool = Best(skill);
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
        /// Puts the hardest-hitting weapon in hand for a fight. A pickaxe will do in a
        /// pinch, but a sword is what a fight is for, so tools lose to anything that
        /// is actually a weapon.
        /// </summary>
        internal ItemDrop.ItemData WieldCombat()
        {
            ItemDrop.ItemData best = null;
            var inventory = Inventory;
            if (inventory == null)
            {
                return null;
            }

            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || !item.IsWeapon() || !item.HavePrimaryAttack())
                {
                    continue;
                }

                if (BetterWeapon(item, best))
                {
                    best = item;
                }
            }

            if (best == null)
            {
                return _humanoid.GetCurrentWeapon();
            }

            Stamp(best);
            if (_humanoid.GetCurrentWeapon() != best)
            {
                _humanoid.EquipItem(best, true);
            }

            return best;
        }

        /// <summary>
        /// Puts on the best armour in the bag. Visuals come from <see cref="VisEquipment"/>
        /// the moment a piece is equipped; the numbers come from <see cref="Player"/>,
        /// which is why a retainer is a player rig rather than a dvergr wearing a hat.
        /// </summary>
        internal void Wear()
        {
            var inventory = Inventory;
            if (inventory == null || _humanoid == null)
            {
                return;
            }

            EquipBest(inventory, ItemDrop.ItemData.ItemType.Helmet);
            EquipBest(inventory, ItemDrop.ItemData.ItemType.Chest);
            EquipBest(inventory, ItemDrop.ItemData.ItemType.Legs);
            EquipBest(inventory, ItemDrop.ItemData.ItemType.Shoulder);
        }

        internal int BestTier(Skills.SkillType skill)
        {
            var tool = Best(skill);
            return tool == null ? -1 : tool.m_shared.m_toolTier;
        }

        internal bool CanBreak(int minTier, Skills.SkillType skill)
        {
            var tool = Best(skill);
            return tool != null && tool.m_shared.m_toolTier >= minTier;
        }

        /// <summary>
        /// Finds something in a bag by the name of the thing it was made from.
        ///
        /// The game's own <see cref="Inventory.GetItem"/> cannot be used for this. It
        /// hides anything made in an earlier world than the current one, which is a rule
        /// about how strong a player's gear is allowed to be, and applies it to
        /// everything - so a retainer carrying wood picked up before the last boss died
        /// will look in its own arms and honestly report that it has none.
        /// </summary>
        internal static ItemDrop.ItemData Find(Inventory inventory, string prefabName)
        {
            if (inventory == null)
            {
                return null;
            }

            foreach (var item in inventory.GetAllItems())
            {
                if (item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName)
                {
                    return item;
                }
            }

            return null;
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

            _humanoid.StartAttack(null, false);

            var hit = new HitData
            {
                m_point = point,
                m_dir = (point - Position).normalized,
                m_damage = tool.GetDamage(),
                m_toolTier = (short)tool.m_shared.m_toolTier,
                m_itemWorldLevel = (byte)tool.m_worldLevel,
                m_hitCollider = where,
            };
            hit.SetAttacker(_humanoid);

            destructible.Damage(hit);
            return true;
        }

        /// <summary>
        /// Picks something up off the ground, the way a player does: own it, then take
        /// it. Fresh loot belongs to whoever owned the thing that dropped it, and
        /// <see cref="ItemDrop.CanPickup"/> is only "do I own this ZDO". Asking first
        /// and taking on a later frame is what <see cref="Player.AutoPickup"/> does;
        /// calling <see cref="ItemDrop.Pickup(Humanoid)"/> is not, because its waiting
        /// path casts the taker to a <see cref="Player"/> and throws for anyone else.
        /// A retainer is a player rig, but it is still not <see cref="Player.m_localPlayer"/>.
        /// </summary>
        internal bool Take(ItemDrop drop)
        {
            if (drop == null)
            {
                return false;
            }

            drop.Load();

            if (!drop.CanPickup(false))
            {
                drop.RequestOwn();
                return false;
            }

            var inventory = Inventory;
            if (inventory == null || !inventory.CanAddItem(drop.m_itemData, drop.m_itemData.m_stack))
            {
                return false;
            }

            if (!_humanoid.Pickup(drop.gameObject, false, false))
            {
                return false;
            }

            Wear();
            return true;
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

        internal static int ItemMask()
        {
            if (_itemMask == 0)
            {
                _itemMask = LayerMask.GetMask("item");
            }

            return _itemMask;
        }

        private ItemDrop.ItemData Best(Skills.SkillType skill)
        {
            var inventory = Inventory;
            if (inventory == null)
            {
                return null;
            }

            ItemDrop.ItemData best = null;
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_shared.m_skillType != skill)
                {
                    continue;
                }

                if (best == null ||
                    item.m_shared.m_toolTier > best.m_shared.m_toolTier ||
                    (item.m_shared.m_toolTier == best.m_shared.m_toolTier &&
                     item.GetDamage().GetTotalDamage() > best.GetDamage().GetTotalDamage()))
                {
                    best = item;
                }
            }

            Stamp(best);
            return best;
        }

        private void EquipBest(Inventory inventory, ItemDrop.ItemData.ItemType slot)
        {
            ItemDrop.ItemData best = null;
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_shared.m_itemType != slot)
                {
                    continue;
                }

                if (best == null || item.GetArmor() > best.GetArmor())
                {
                    best = item;
                }
            }

            if (best == null)
            {
                return;
            }

            Stamp(best);
            if (!best.m_equipped)
            {
                _humanoid.EquipItem(best, true);
            }
        }

        private static bool BetterWeapon(ItemDrop.ItemData candidate, ItemDrop.ItemData current)
        {
            if (current == null)
            {
                return true;
            }

            var candidateTool = candidate.m_shared.m_skillType == Skills.SkillType.Pickaxes ||
                                candidate.m_shared.m_skillType == Skills.SkillType.Axes;
            var currentTool = current.m_shared.m_skillType == Skills.SkillType.Pickaxes ||
                              current.m_shared.m_skillType == Skills.SkillType.Axes;
            if (candidateTool != currentTool)
            {
                return !candidateTool;
            }

            return candidate.GetDamage().GetTotalDamage() > current.GetDamage().GetTotalDamage();
        }

        /// <summary>
        /// A retainer's kit used to be issued from the prefab, which the game hands over
        /// without stamping a world onto it. Gear the player gives them is usually
        /// current, but anything that is not is brought up so a blow is not turned away
        /// before its tier is even read.
        /// </summary>
        private static void Stamp(ItemDrop.ItemData item)
        {
            if (item != null && item.m_worldLevel < Game.m_worldLevel)
            {
                item.m_worldLevel = Game.m_worldLevel;
            }
        }
    }
}
