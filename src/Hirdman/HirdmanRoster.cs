using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Finding a retainer to talk to.
    ///
    /// The game already keeps a list of every character loaded on this peer, so there is
    /// no register to maintain and nothing to go stale when one dies or unloads.
    /// </summary>
    internal static class HirdmanRoster
    {
        /// <summary>How far away a retainer can be and still hear an order.</summary>
        internal const float EarshotRadius = 20f;

        internal static GameObject Nearest(Vector3 from, float within)
        {
            GameObject closest = null;
            var shortest = within;

            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.GetComponent<HirdmanTag>() == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(from, character.transform.position);
                if (distance <= shortest)
                {
                    shortest = distance;
                    closest = character.gameObject;
                }
            }

            return closest;
        }
    }
}
