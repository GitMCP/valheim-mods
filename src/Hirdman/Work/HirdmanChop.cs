using System.Collections.Generic;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Felling trees and carrying the wood.
    ///
    /// A tree is not done when it falls. The trunk becomes a log, the log splits, and
    /// the wood sits on the ground; walking off to the next standing trunk in the
    /// middle of that is how a clearing fills with logs nobody will ever pick up.
    /// This job stays with one tree until the logs and the drops are gone, then
    /// looks for another.
    /// </summary>
    internal class HirdmanChop : HirdmanWork
    {
        /// <summary>How near a tree has to be before an axe will reach it.</summary>
        private const float Reach = 2.4f;

        private const float SwingInterval = 1.5f;
        private const float SearchInterval = 0.4f;

        /// <summary>
        /// How far a trunk can fall or a log can roll and still belong to the tree we
        /// are finishing. Wider than a beech log is long, short of the next stand of
        /// trees in an ordinary wood.
        /// </summary>
        private const float Clearing = 16f;

        /// <summary>
        /// After a trunk or a log dies, wait this long for the next piece to exist
        /// before deciding the tree is finished. Instantiates happen in the same blow,
        /// but the collider is not always there the same frame.
        /// </summary>
        private const float FallWait = 0.8f;

        /// <summary>
        /// What counts as timber. A retainer sent to chop wood should come back with
        /// wood, and with the resin and cones that fall with it, and not with everything
        /// anyone ever dropped in the clearing.
        /// </summary>
        private static readonly HashSet<string> Timber = new HashSet<string>
        {
            "Wood", "RoundLog", "FineWood", "ElderBark", "YggdrasilWood", "Frostwood", "Blackwood",
            "Resin", "Feathers", "Acorn", "BeechSeeds", "BirchSeeds", "PineCone", "FirCone", "AncientSeed",
        };

        private Component _tree;
        private ItemDrop _drop;
        private Vector3 _stump;
        private bool _clearing;
        private bool _had;
        private float _until;
        private float _searchedAt;
        private float _swungAt;
        private float _dropSince;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            if (body.Need(Skills.SkillType.Axes, "I have no axe. Give me one and I'll chop.") == null)
            {
                Forget();
                return Hold(body, order.Anchor, dt);
            }

            if (_tree == null && _had)
            {
                _had = false;
                _until = Time.time + FallWait;
                _drop = null;
            }

            if (_tree == null && _clearing && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _tree = LogAt(body, _stump, Clearing) ?? StubAt(body, _stump);
            }

            if (_tree != null)
            {
                Remember(_tree);
                return Swing(body, dt);
            }

            if (_clearing)
            {
                if (Gather(body, _stump, Clearing, dt, null))
                {
                    return true;
                }

                if (Time.time < _until)
                {
                    body.Approach(dt, _stump, Reach);
                    return true;
                }

                _clearing = false;
                _drop = null;
                _searchedAt = 0f;
            }

            if (Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _tree = Closest<TreeLog>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                    log => Splittable(body, log));
                if (_tree == null)
                {
                    _tree = Closest<TreeBase>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                        tree => Chopable(body, tree));
                }

                if (_tree != null)
                {
                    Remember(_tree);
                    return Swing(body, dt);
                }

                if (Gather(body, body.Position, HirdmanPlugin.HaulRadius, dt, IsTimber))
                {
                    return true;
                }

                body.Ask("Nothing here I can cut with this axe.");
            }

            if (_drop != null)
            {
                return Gather(body, body.Position, HirdmanPlugin.HaulRadius, dt, IsTimber);
            }

            return Hold(body, order.Anchor, dt);
        }

        private bool Swing(HirdmanBody body, float dt)
        {
            if (_tree == null)
            {
                return true;
            }

            var stand = Stand(_tree, body.Position);
            if (!body.Approach(dt, stand, Reach))
            {
                return true;
            }

            Scoop(body, Reach * 2f, IsTimber);

            var axe = body.Wield(Skills.SkillType.Axes);
            if (axe == null)
            {
                Forget();
                return true;
            }

            if (Time.time - _swungAt > SwingInterval)
            {
                _swungAt = Time.time;
                if (!body.Strike(_tree, axe))
                {
                    _tree = null;
                }
            }

            return true;
        }

        private bool Gather(HirdmanBody body, Vector3 centre, float radius, float dt,
            System.Func<GameObject, bool> wanted)
        {
            if (_drop == null)
            {
                _drop = Closest<ItemDrop>(centre, centre, radius, drop => DropWanted(drop, wanted));
                if (_drop != null)
                {
                    _dropSince = Time.time;
                }
            }

            if (_drop == null)
            {
                return Scoop(body, HirdmanPlugin.HaulRadius, wanted ?? IsTimber) > 0;
            }

            if (Time.time - _dropSince > 4f)
            {
                _drop = null;
                return false;
            }

            if (!body.Approach(dt, _drop.transform.position, Reach))
            {
                return true;
            }

            var drop = _drop;
            if (!body.Take(drop))
            {
                if (drop != null && drop.CanPickup(false))
                {
                    _drop = null;
                }

                return true;
            }

            _drop = null;
            Scoop(body, Reach * 2f, wanted ?? IsTimber);
            return true;
        }

        private void Remember(Component tree)
        {
            var first = !_clearing;
            _had = true;
            _clearing = true;
            if (tree is TreeBase || first)
            {
                _stump = tree.transform.position;
            }
        }

        private void Forget()
        {
            _tree = null;
            _drop = null;
            _clearing = false;
            _had = false;
            _stump = Vector3.zero;
        }

        private static Component LogAt(HirdmanBody body, Vector3 stump, float radius)
        {
            return Closest<TreeLog>(stump, stump, radius, log => Splittable(body, log));
        }

        private static Component StubAt(HirdmanBody body, Vector3 stump)
        {
            return Closest<Destructible>(stump, stump, 3f, stub => IsStub(body, stub));
        }

        private static bool Chopable(HirdmanBody body, TreeBase tree)
        {
            return body.CanBreak(tree.m_minToolTier, Skills.SkillType.Axes);
        }

        private static bool Splittable(HirdmanBody body, TreeLog log)
        {
            return body.CanBreak(log.m_minToolTier, Skills.SkillType.Axes);
        }

        private static bool IsStub(HirdmanBody body, Destructible stub)
        {
            if (stub == null || stub.GetComponent<Piece>() != null ||
                stub.GetComponent<TreeBase>() != null || stub.GetComponent<TreeLog>() != null ||
                stub.GetComponent<Pickable>() != null)
            {
                return false;
            }

            var name = HirdmanCatalog.PrefabName(stub.gameObject);
            if (name.IndexOf("stub", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("stump", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return body.CanBreak(stub.m_minToolTier, Skills.SkillType.Axes);
        }

        private static bool DropWanted(ItemDrop drop, System.Func<GameObject, bool> wanted)
        {
            return drop != null && (wanted == null || wanted(drop.gameObject));
        }

        private static bool IsTimber(GameObject drop)
        {
            return Timber.Contains(HirdmanCatalog.PrefabName(drop));
        }

        /// <summary>
        /// A point on the thing's surface nearest the retainer, so a fallen log is
        /// walked to at the near end rather than at an origin buried in the middle.
        /// </summary>
        private static Vector3 Stand(Component thing, Vector3 from)
        {
            var face = thing.GetComponentInChildren<Collider>();
            if (face == null || face.GetComponent<Heightmap>() != null)
            {
                return thing.transform.position;
            }

            return face.ClosestPoint(from);
        }
    }
}
