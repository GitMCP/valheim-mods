using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Works out whether a thing in the world is the thing that was asked for.
    ///
    /// "Gather raspberries" has to find <c>RaspberryBush</c>, whose dropped item is
    /// <c>Raspberry</c>, whose display name is "Raspberries". None of those three
    /// spellings is the one a player types, and which of them is closest changes from
    /// plant to plant, so matching is done against all of them at once and loosely. The
    /// alternative - a hand-written table of every pickable, ore and animal in the game -
    /// would be a hundred lines that go stale on the next update, and would still miss
    /// whatever another mod adds.
    /// </summary>
    internal static class HirdmanCatalog
    {
        /// <summary>
        /// Keyed by prefab name rather than by instance, because every raspberry bush in
        /// the world answers to exactly the same words. A world holds thousands of
        /// instances and a few hundred kinds, and only the kinds are worth remembering.
        /// </summary>
        private static readonly Dictionary<string, string[]> Memo = new Dictionary<string, string[]>();

        /// <summary>
        /// Trims a word down to what it has in common with the other spellings of
        /// itself: case, spaces and the English plural. This is not linguistics, it is
        /// the shortest thing that makes "Raspberries", "raspberry" and "RaspberryBush"
        /// meet in the middle.
        /// </summary>
        internal static string Normalise(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return string.Empty;
            }

            var lower = word.Trim().ToLowerInvariant().Replace(" ", string.Empty);

            if (lower.EndsWith("ies", StringComparison.Ordinal) && lower.Length > 4)
            {
                return lower.Substring(0, lower.Length - 3) + "y";
            }

            if (lower.EndsWith("es", StringComparison.Ordinal) && lower.Length > 3)
            {
                return lower.Substring(0, lower.Length - 2);
            }

            if (lower.EndsWith("s", StringComparison.Ordinal) && lower.Length > 2)
            {
                return lower.Substring(0, lower.Length - 1);
            }

            return lower;
        }

        /// <summary>
        /// An instantiated prefab is named "RaspberryBush(Clone)", and what is wanted is
        /// "RaspberryBush".
        /// </summary>
        internal static string PrefabName(GameObject go)
        {
            if (go == null)
            {
                return string.Empty;
            }

            var name = go.name;
            var clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
            return clone < 0 ? name : name.Substring(0, clone);
        }

        /// <summary>
        /// Does this thing answer to what was asked for? An empty subject matches
        /// everything, which is how "gather" and "gather mushrooms" end up being the
        /// same code path.
        ///
        /// A subject is matched word by word rather than whole, because what survives
        /// being pulled out of a sentence is rarely one word: "hunt boars in the woods"
        /// leaves "boars woods", and a boar should answer to that. One word hitting is
        /// enough, which is loose, but the cost of being loose is picking the wrong
        /// mushroom and the cost of being strict is a retainer that ignores you.
        /// </summary>
        internal static bool Answers(string subject, GameObject thing)
        {
            if (string.IsNullOrEmpty(subject))
            {
                return true;
            }

            var tokens = Describe(thing);
            var any = false;

            foreach (var word in subject.Split(' '))
            {
                var wanted = Normalise(word);
                if (wanted.Length < 3)
                {
                    continue;
                }

                any = true;
                foreach (var token in tokens)
                {
                    if (token.Contains(wanted) || wanted.Contains(token))
                    {
                        return true;
                    }
                }
            }

            // Nothing in the subject was a word worth matching on, so it was never
            // really a subject.
            return !any;
        }

        /// <summary>
        /// Every name a thing goes by: what its prefab is called, what it drops, and what
        /// that is called on screen in the player's own language.
        /// </summary>
        internal static string[] Describe(GameObject thing)
        {
            if (thing == null)
            {
                return new string[0];
            }

            var kind = PrefabName(thing);

            string[] cached;
            if (Memo.TryGetValue(kind, out cached))
            {
                return cached;
            }

            var tokens = new List<string>();
            Add(tokens, kind);

            var pickable = thing.GetComponent<Pickable>();
            if (pickable != null && pickable.m_itemPrefab != null)
            {
                Add(tokens, pickable.m_itemPrefab.name);
                Add(tokens, Display(pickable.m_itemPrefab));
            }

            var drop = thing.GetComponent<ItemDrop>();
            if (drop != null && drop.m_itemData != null && drop.m_itemData.m_shared != null)
            {
                Add(tokens, Localise(drop.m_itemData.m_shared.m_name));
            }

            var character = thing.GetComponent<Character>();
            if (character != null)
            {
                Add(tokens, Localise(character.m_name));
            }

            foreach (var item in Yields(thing))
            {
                Add(tokens, item);
            }

            cached = tokens.ToArray();
            Memo[kind] = cached;
            return cached;
        }

        /// <summary>What breaking or killing a thing gives you, by item prefab name.</summary>
        internal static IEnumerable<string> Yields(GameObject thing)
        {
            var five = thing.GetComponent<MineRock5>();
            if (five != null)
            {
                foreach (var name in FromTable(five.m_dropItems))
                {
                    yield return name;
                }
            }

            var rock = thing.GetComponent<MineRock>();
            if (rock != null)
            {
                foreach (var name in FromTable(rock.m_dropItems))
                {
                    yield return name;
                }
            }

            var drops = thing.GetComponent<CharacterDrop>();
            if (drops != null && drops.m_drops != null)
            {
                foreach (var entry in drops.m_drops)
                {
                    if (entry != null && entry.m_prefab != null)
                    {
                        yield return entry.m_prefab.name;
                    }
                }
            }
        }

        private static IEnumerable<string> FromTable(DropTable table)
        {
            if (table == null || table.m_drops == null)
            {
                yield break;
            }

            foreach (var entry in table.m_drops)
            {
                if (entry.m_item != null)
                {
                    yield return entry.m_item.name;
                    yield return Display(entry.m_item);
                }
            }
        }

        private static string Display(GameObject itemPrefab)
        {
            var drop = itemPrefab == null ? null : itemPrefab.GetComponent<ItemDrop>();
            return drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null
                ? null
                : Localise(drop.m_itemData.m_shared.m_name);
        }

        private static string Localise(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            return Localization.instance == null ? token : Localization.instance.Localize(token);
        }

        private static void Add(List<string> tokens, string word)
        {
            var normalised = Normalise(word);
            if (normalised.Length > 1 && !tokens.Contains(normalised))
            {
                tokens.Add(normalised);
            }
        }
    }
}
