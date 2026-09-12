using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Stops the bicycle damaging anything that is not alive.
    ///
    /// A lox has two ways of breaking things and the mod wants neither. It carries
    /// <c>lox_bite</c> and <c>lox_stomp</c> as weapons, and the stomp does 100 chop and
    /// 100 pickaxe damage, which is enough to fell trees, break ore and flatten a
    /// building; a tamed lox swinging at a wolf next to your house takes the house with
    /// it. It also carries an <see cref="Aoe"/> called <c>RunHitDamager</c> that hits
    /// whatever it runs into, which is what makes riding one through a fence expensive.
    ///
    /// A bicycle has no reason to bite or stomp, so the weapons go. Running into
    /// something is a real bicycle behaviour, so that stays, restricted to things that
    /// can actually be hurt.
    /// </summary>
    internal static class BicicretaHarmless
    {
        internal static void Apply(GameObject prefab)
        {
            var disarmed = Disarm(prefab);
            var restricted = 0;

            foreach (var aoe in prefab.GetComponentsInChildren<Aoe>(includeInactive: true))
            {
                // Aoe reads both of these once, in Awake, to build the layer mask it
                // searches. Cleared here, an instance never even looks at the layers
                // that structures, ore, trees, carts and ships live on.
                aoe.m_hitProps = false;
                aoe.m_hitTerrain = false;

                // The reach is a lox's: 4 m, which on something 1.1 m long would hurt
                // whatever the rider merely went past.
                aoe.m_radius = Mathf.Min(aoe.m_radius, BicicretaGeometry.HitRadius);
                restricted++;
            }

            BicicretaPlugin.Log.LogInfo(
                $"Dropped {disarmed} lox weapon(s) and held {restricted} collision damager(s) to living targets.");
        }

        /// <summary>
        /// Takes the lox's attacks away. The AI copes with an unarmed creature: it
        /// checks for a weapon before every attack and does nothing without one.
        /// </summary>
        private static int Disarm(GameObject prefab)
        {
            var humanoid = prefab.GetComponent<Humanoid>();
            if (humanoid == null)
            {
                BicicretaPlugin.Log.LogWarning("No Humanoid on the bicycle; it may still be armed.");
                return 0;
            }

            var disarmed = Count(humanoid.m_defaultItems) + Count(humanoid.m_randomWeapon);
            humanoid.m_defaultItems = new GameObject[0];
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_unarmedWeapon = null;
            return disarmed;
        }

        private static int Count(GameObject[] items) => items == null ? 0 : items.Length;
    }
}
