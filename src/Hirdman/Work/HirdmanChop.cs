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
            if (_tree == null && Time.time - _searchedAt > SearchInterval)
            {
                _searchedAt = Time.time;
                _tree = Find(body, order.Anchor);
                Scoop(body, HirdmanPlugin.HaulRadius, IsTimber);
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

            if (Time.time - _swungAt > SwingInterval)
            {
                _swungAt = Time.time;

                var axe = body.Wield(HirdmanRetainer.Axe);
                if (axe == null)
                {
                    HirdmanPlugin.Log.LogWarning("A retainer has no axe in hand and cannot chop.");
                    _tree = null;
                    return true;
                }

                if (!body.Strike(_tree, axe))
                {
                    _tree = null;
                }
            }

            return true;
        }

        private static Component Find(HirdmanBody body, Vector3 anchor)
        {
            // A standing tree first, and a felled trunk if there is nothing left
            // standing, because a log still needs cutting up before it is wood.
            Component tree = Closest<TreeBase>(body, anchor, HirdmanPlugin.WorkRadius.Value, null);
            return tree != null ? tree : Closest<TreeLog>(body, anchor, HirdmanPlugin.WorkRadius.Value, null);
        }

        private static bool IsTimber(GameObject drop)
        {
            return Timber.Contains(HirdmanCatalog.PrefabName(drop));
        }
    }
}
