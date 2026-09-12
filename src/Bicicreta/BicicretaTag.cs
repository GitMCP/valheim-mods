using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Marks an instance as a bicycle, so the patches in <c>Patches/</c> can tell one
    /// apart from a real tamed animal without matching on prefab names.
    /// </summary>
    internal class BicicretaTag : MonoBehaviour
    {
        private Sadle _saddle;

        /// <summary>
        /// The saddle that does the actual riding. Looked up lazily because component
        /// Awake order is not defined, and it lives on a child that
        /// <see cref="Tameable"/> deactivates until it believes a saddle is fitted.
        /// </summary>
        internal Sadle Saddle
        {
            get
            {
                if (_saddle == null)
                {
                    _saddle = GetComponentInChildren<Sadle>(includeInactive: true);
                }

                return _saddle;
            }
        }
    }
}
