using System;
using System.Collections.Generic;

namespace NjordWarehouseKeeper.Client
{
    /// <summary>
    /// Stable id for a grouped hub row: shared name plus quality, variant, and
    /// world level, so a crude iron sword is not the same favourite as a fully
    /// upgraded one. Weapons, armour, and other unique (non-stacking) items
    /// also carry a per-instance token in <see cref="ItemDrop.ItemData.m_customData"/>
    /// so starring one copy does not star every other copy of the same piece.
    /// </summary>
    internal static class ItemKey
    {
        private const char Sep = '|';
        private const char InstanceSep = '#';
        internal const string FavouriteIdKey = "njord.fav";

        internal static string Of(string sharedName, int quality, int variant, int worldLevel)
        {
            return (sharedName ?? "") + Sep + quality + Sep + variant + Sep + worldLevel;
        }

        internal static string Of(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return "";
            }

            return Of(item.m_shared.m_name, item.m_quality, item.m_variant, item.m_worldLevel);
        }

        internal static bool IsUnique(ItemDrop.ItemData item)
        {
            return item?.m_shared != null && item.m_shared.m_maxStackSize <= 1;
        }

        /// <summary>
        /// Favourite lookup key. Stackable items use the type key. Unique items
        /// use type plus the stamped instance id when one exists, otherwise the
        /// type key so older configs still match.
        /// </summary>
        internal static string FavouriteOf(ItemDrop.ItemData item)
        {
            var type = Of(item);
            if (string.IsNullOrEmpty(type) || !IsUnique(item))
            {
                return type;
            }

            var id = ReadId(item);
            return string.IsNullOrEmpty(id) ? type : type + InstanceSep + id;
        }

        /// <summary>
        /// Same as <see cref="FavouriteOf"/>, but stamps a new instance id on a
        /// unique item that does not have one yet.
        /// </summary>
        internal static string EnsureFavourite(ItemDrop.ItemData item)
        {
            var type = Of(item);
            if (string.IsNullOrEmpty(type) || !IsUnique(item))
            {
                return type;
            }

            var id = ReadId(item);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                WriteId(item, id);
            }

            return type + InstanceSep + id;
        }

        internal static string TypeOf(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            var hash = key.LastIndexOf(InstanceSep);
            var lastPipe = key.LastIndexOf(Sep);
            if (hash < 0 || hash < lastPipe)
            {
                return key;
            }

            return key.Substring(0, hash);
        }

        internal static bool TryParse(
            string key,
            out string sharedName,
            out int quality,
            out int variant,
            out int worldLevel)
        {
            sharedName = "";
            quality = 1;
            variant = 0;
            worldLevel = 0;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            key = TypeOf(key);
            var p1 = key.IndexOf(Sep);
            if (p1 < 0)
            {
                return false;
            }

            var p2 = key.IndexOf(Sep, p1 + 1);
            if (p2 < 0)
            {
                return false;
            }

            var p3 = key.IndexOf(Sep, p2 + 1);
            if (p3 < 0)
            {
                return false;
            }

            sharedName = key.Substring(0, p1);
            return int.TryParse(key.Substring(p1 + 1, p2 - p1 - 1), out quality)
                && int.TryParse(key.Substring(p2 + 1, p3 - p2 - 1), out variant)
                && int.TryParse(key.Substring(p3 + 1), out worldLevel)
                && sharedName.Length > 0;
        }

        internal static void Persist(Inventory inventory)
        {
            if (inventory?.m_onChanged != null)
            {
                inventory.m_onChanged();
            }
        }

        private static string ReadId(ItemDrop.ItemData item)
        {
            if (item?.m_customData == null)
            {
                return "";
            }

            string id;
            return item.m_customData.TryGetValue(FavouriteIdKey, out id) ? id ?? "" : "";
        }

        private static void WriteId(ItemDrop.ItemData item, string id)
        {
            if (item == null)
            {
                return;
            }

            if (item.m_customData == null)
            {
                item.m_customData = new Dictionary<string, string>();
            }

            item.m_customData[FavouriteIdKey] = id;
        }
    }
}
