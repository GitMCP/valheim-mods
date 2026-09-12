using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Keeps the bicycle rideable.
    ///
    /// <see cref="Tameable"/> stores whether a creature is wearing a saddle in its ZDO,
    /// because the saddle is normally an item a player crafts and fits. A bicycle is not
    /// ridden bareback, so the flag is set as soon as the owning peer has a ZDO to write
    /// it to, which also survives the world being saved and reloaded.
    /// </summary>
    internal class AlwaysSaddled : MonoBehaviour
    {
        private bool _applied;

        // This component is added to the prefab at runtime, so it cannot assume it runs
        // after ZNetView has produced a ZDO, and a peer does not necessarily own the
        // bicycle the moment it appears. Rather than depend on either, keep trying and
        // stop as soon as it takes.
        private void Awake() => TryApply();

        private void Update()
        {
            TryApply();

            if (_applied)
            {
                enabled = false;
            }
        }

        private void TryApply()
        {
            if (_applied)
            {
                return;
            }

            var nview = GetComponent<ZNetView>();

            // Placement previews have no ZDO at all, and only the owner may write to one.
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
            _applied = true;

            // The flag alone is what persists, but Tameable only acts on it when it next
            // looks, so fit the saddle now rather than leaving the bicycle briefly
            // unrideable right after it is built.
            var tameable = GetComponent<Tameable>();
            if (tameable != null && tameable.m_saddle != null)
            {
                tameable.SetSaddle(true);
            }
        }
    }
}
