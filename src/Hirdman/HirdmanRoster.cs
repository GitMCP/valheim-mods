using System.Collections.Generic;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Finding retainers.
    ///
    /// The game already keeps a list of every character loaded on this peer, so there is
    /// no register to maintain and nothing to go stale when one dies or unloads. The
    /// price of that is the word "loaded": Valheim only instantiates what is near a
    /// player, so these queries see the retainers in the neighbourhood and not the ones
    /// asleep in a saved zone on the far side of the world. Every caller here wants the
    /// near ones anyway - you cannot talk to, count out or watch walk in somebody the
    /// engine is not simulating.
    /// </summary>
    internal static class HirdmanRoster
    {
        /// <summary>How far away a retainer can be and still hear an order.</summary>
        internal const float EarshotRadius = 20f;

        internal static IEnumerable<GameObject> Loaded()
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (character != null && character.GetComponent<HirdmanTag>() != null)
                {
                    yield return character.gameObject;
                }
            }
        }

        /// <summary>Everyone in a player's service that this machine can see.</summary>
        internal static IEnumerable<GameObject> Of(Player player)
        {
            if (player == null)
            {
                yield break;
            }

            foreach (var retainer in Loaded())
            {
                if (HirdmanContract.Of(retainer).BelongsTo(player))
                {
                    yield return retainer;
                }
            }
        }

        internal static int CountFor(Player player)
        {
            var count = 0;
            foreach (var unused in Of(player))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// The retainer a player is talking to: the nearest one that is theirs. Falling
        /// back to anyone's is deliberate - somebody's first order is often given before
        /// they realise a retainer has an owner, and refusing on a technicality is worse
        /// than answering.
        /// </summary>
        internal static GameObject Listening(Player player, float within)
        {
            if (player == null)
            {
                return null;
            }

            var mine = Nearest(player.transform.position, within, player);
            return mine != null ? mine : Nearest(player.transform.position, within, null);
        }

        internal static GameObject Nearest(Vector3 from, float within, Player owner = null)
        {
            GameObject closest = null;
            var shortest = within;

            foreach (var retainer in Loaded())
            {
                if (owner != null && !HirdmanContract.Of(retainer).BelongsTo(owner))
                {
                    continue;
                }

                var distance = Vector3.Distance(from, retainer.transform.position);
                if (distance <= shortest)
                {
                    shortest = distance;
                    closest = retainer;
                }
            }

            return closest;
        }
    }
}
