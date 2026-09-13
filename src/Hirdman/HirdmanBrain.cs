using System.Collections.Generic;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Carries out a retainer's standing order.
    ///
    /// This runs only on the peer that owns the retainer, and only for the parts of
    /// behaviour the game does not already do better. Following is left entirely to
    /// <see cref="MonsterAI"/>, which has a tamed wolf's worth of experience at walking
    /// behind someone; a fight is left to it as well, the moment it has a target. What is
    /// written here is the work - walking to a chosen tree, swinging at it, and carrying
    /// the wood home - because the game has no idea of a creature with a job.
    /// </summary>
    internal class HirdmanBrain : MonoBehaviour
    {
        /// <summary>How near an anchor counts as standing on it.</summary>
        private const float ArriveDistance = 1.5f;

        /// <summary>How near a tree has to be before an axe will reach it.</summary>
        private const float ChopReach = 2.4f;

        private const float SwingInterval = 1.5f;
        private const float SearchInterval = 2f;
        private const float OrderPollInterval = 0.5f;

        /// <summary>How far from its anchor a retainer will go looking for trees.</summary>
        private const float WorkRadius = 24f;

        /// <summary>How far it will step aside to pick up what it felled.</summary>
        private const float HaulRadius = 6f;

        /// <summary>
        /// What counts as timber. A retainer sent to chop wood should come back with wood
        /// and not with everything anyone ever dropped in the clearing.
        /// </summary>
        private static readonly HashSet<string> Timber = new HashSet<string>
        {
            "Wood", "RoundLog", "FineWood", "ElderBark", "YggdrasilWood",
        };

        private ZNetView _nview;
        private Humanoid _humanoid;
        private MonsterAI _ai;

        private HirdmanOrder _order;
        private Component _tree;
        private float _polledAt;
        private float _searchedAt;
        private float _swungAt;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _humanoid = GetComponent<Humanoid>();
            _ai = GetComponent<MonsterAI>();

            if (_nview != null && _nview.IsValid())
            {
                _order = HirdmanOrder.Read(_nview.GetZDO());
            }
        }

        /// <summary>
        /// Gives a retainer an order, from any peer.
        ///
        /// Only the peer that owns a creature may write its ZDO, and the player giving
        /// the order is often not that peer, so ownership is taken first. That is the
        /// same move the game makes when you open someone else's chest, and it is why an
        /// order needs no RPC of its own.
        /// </summary>
        internal static bool Give(GameObject retainer, HirdmanOrder order)
        {
            var nview = retainer?.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            order.Write(nview.GetZDO());
            return true;
        }

        /// <summary>
        /// Decides whether the game's own AI should run this frame.
        /// </summary>
        /// <returns>
        /// True when this brain has taken the frame, and vanilla should be skipped.
        /// </returns>
        internal bool Think(float dt)
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner() || _ai == null)
            {
                return false;
            }

            PollOrder();

            // A fight outranks any order. Handing the frame back is also how a retainer
            // gets the game's own flinching, circling and weapon choice for free.
            if (_ai.GetTargetCreature() != null)
            {
                return false;
            }

            switch (_order.Job)
            {
                case HirdmanJob.Follow:
                    return false;
                case HirdmanJob.ChopWood:
                    return Work(dt);
                default:
                    return Hold(dt);
            }
        }

        /// <summary>
        /// Rereads the order, rather than being told when it changes. A retainer can
        /// change hands between peers mid-task, and the new owner has to pick up an
        /// order it never saw given.
        /// </summary>
        private void PollOrder()
        {
            if (Time.time - _polledAt < OrderPollInterval)
            {
                return;
            }

            _polledAt = Time.time;

            var next = HirdmanOrder.Read(_nview.GetZDO());
            if (next.Job == _order.Job && next.Anchor == _order.Anchor && next.Master == _order.Master)
            {
                return;
            }

            _order = next;
            _tree = null;

            // Vanilla following is driven by this one field, so the order is expressed by
            // setting it and then staying out of the way.
            _ai.SetFollowTarget(_order.Job == HirdmanJob.Follow ? Master() : null);
        }

        private GameObject Master()
        {
            return _order.Master == ZDOID.None
                ? null
                : ZNetScene.instance?.FindInstance(_order.Master);
        }

        /// <summary>Stand on the anchor. Guarding is this plus the game's own alertness.</summary>
        private bool Hold(float dt)
        {
            if (Vector3.Distance(transform.position, _order.Anchor) > ArriveDistance)
            {
                _ai.MoveTo(dt, _order.Anchor, ArriveDistance, false);
            }
            else
            {
                _ai.StopMoving();
            }

            return true;
        }

        private bool Work(float dt)
        {
            if (_tree == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _tree = FindTree();
                Haul();
            }

            // A clearing with nothing left standing is not a failure; it is a retainer
            // waiting where it was put, which is what it would be doing anyway.
            if (_tree == null)
            {
                return Hold(dt);
            }

            var trunk = _tree.transform.position;
            if (Vector3.Distance(transform.position, trunk) > ChopReach)
            {
                _ai.MoveTo(dt, trunk, ChopReach * 0.75f, false);
                return true;
            }

            _ai.StopMoving();
            _ai.LookAt(trunk);

            if (Time.time - _swungAt > SwingInterval)
            {
                _swungAt = Time.time;
                Swing(trunk);
            }

            return true;
        }

        /// <summary>
        /// Fells a tree the way a player does: with the damage of the axe actually in its
        /// hands, so the game's own damage rules, tool tiers and drop tables decide what
        /// happens. A stone axe cannot bring down a birch here either.
        /// </summary>
        private void Swing(Vector3 trunk)
        {
            var destructible = _tree as IDestructible;
            if (destructible == null)
            {
                _tree = null;
                return;
            }

            // For the animation. It is allowed to fail: the swing is what the tree
            // responds to, and a retainer chopping stiffly is better than one that stops.
            _humanoid.StartAttack(null, false);

            var hit = new HitData
            {
                m_point = trunk + Vector3.up,
                m_dir = (trunk - transform.position).normalized,
            };
            hit.SetAttacker(_humanoid);

            var axe = _humanoid.GetCurrentWeapon();
            if (axe == null)
            {
                HirdmanPlugin.Log.LogWarning("A retainer has no axe in hand and cannot chop.");
                _tree = null;
                return;
            }

            hit.m_damage = axe.GetDamage();
            hit.m_toolTier = (short)axe.m_shared.m_toolTier;
            destructible.Damage(hit);
        }

        private Component FindTree()
        {
            Component closest = null;
            var shortest = float.MaxValue;

            foreach (var collider in Physics.OverlapSphere(_order.Anchor, WorkRadius))
            {
                Component tree = collider.GetComponentInParent<TreeBase>();
                if (tree == null)
                {
                    // A felled trunk is still wood, and still needs cutting up.
                    tree = collider.GetComponentInParent<TreeLog>();
                }

                if (tree == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(transform.position, tree.transform.position);
                if (distance < shortest)
                {
                    shortest = distance;
                    closest = tree;
                }
            }

            return closest;
        }

        private void Haul()
        {
            foreach (var collider in Physics.OverlapSphere(transform.position, HaulRadius))
            {
                var drop = collider.GetComponentInParent<ItemDrop>();
                if (drop == null || !drop.CanPickup(false))
                {
                    continue;
                }

                if (!Timber.Contains(PrefabName(drop.gameObject)))
                {
                    continue;
                }

                _humanoid.Pickup(drop.gameObject, autoequip: false, autoPickupDelay: false);
            }
        }

        /// <summary>
        /// An instantiated prefab is named "Wood(Clone)", and what is wanted is "Wood".
        /// </summary>
        private static string PrefabName(GameObject go)
        {
            var name = go.name;
            var clone = name.IndexOf("(Clone)", System.StringComparison.Ordinal);
            return clone < 0 ? name : name.Substring(0, clone);
        }
    }
}
