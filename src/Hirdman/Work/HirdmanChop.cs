using System.Collections.Generic;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Felling trees and carrying the wood.
    ///
    /// The oldest job in the mod and still the template for the rest: find the nearest
    /// thing of the right sort near the work site, walk to it, hit it with the right tool
    /// at a human rate, and pick up what falls out. Everything specific to trees is in
    /// the two searches; the rhythm is the same for rock and for anything else that has
    /// to be broken before it is worth anything.
    /// </summary>
    internal class HirdmanChop : HirdmanWork
    {
        /// <summary>How near a tree has to be before an axe will reach it.</summary>
        private const float Reach = 2.4f;

        private const float SwingInterval = 1.5f;
        private const float SearchInterval = 2f;

        /// <summary>
        /// What counts as timber. A retainer sent to chop wood should come back with wood
        /// and not with everything anyone ever dropped in the clearing.
        /// </summary>
        private static readonly HashSet<string> Timber = new HashSet<string>
        {
            "Wood", "RoundLog", "FineWood", "ElderBark", "YggdrasilWood", "Frostwood", "Blackwood",
        };

        private Component _tree;
        private float _searchedAt;
        private float _swungAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            if (body.Need(Skills.SkillType.Axes, "I have no axe. Give me one and I'll chop.") == null)
            {
                _tree = null;
                return Hold(body, order.Anchor, dt);
            }

            if (_tree == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _tree = Find(body, order.Anchor);
                Scoop(body, HirdmanPlugin.HaulRadius, IsTimber);
                if (_tree == null)
                {
                    body.Ask("Nothing here I can cut with this axe.");
                }
            }

            if (_tree == null)
            {
                return Hold(body, order.Anchor, dt);
            }

            var trunk = _tree.transform.position;
            if (!body.Approach(dt, trunk, Reach))
            {
                return true;
            }

            var axe = body.Wield(Skills.SkillType.Axes);
            if (axe == null)
            {
                _tree = null;
                return Hold(body, order.Anchor, dt);
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

        private static Component Find(HirdmanBody body, Vector3 anchor)
        {
            // A stone axe will not bring down a birch. The game says so with a floating
            // "Too hard" and no damage, and a retainer that does not read that will stand
            // at the nearest trunk forever. Skip anything the axe in the bag cannot cut,
            // and take a log if nothing is still standing.
            bool Chopable(TreeBase tree)
            {
                return body.CanBreak(tree.m_minToolTier, Skills.SkillType.Axes);
            }

            bool Splittable(TreeLog log)
            {
                return body.CanBreak(log.m_minToolTier, Skills.SkillType.Axes);
            }

            Component tree = Closest<TreeBase>(body, anchor, HirdmanPlugin.WorkRadius.Value, Chopable);
            return tree != null ? tree : Closest<TreeLog>(body, anchor, HirdmanPlugin.WorkRadius.Value, Splittable);
        }

        private static bool IsTimber(GameObject drop)
        {
            return Timber.Contains(HirdmanCatalog.PrefabName(drop));
        }
    }
}
