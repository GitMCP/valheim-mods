namespace StorageHub.Client
{
    /// <summary>
    /// Stable id for a grouped hub row: shared name plus quality, variant, and
    /// world level, so a crude iron sword is not the same favourite as a fully
    /// upgraded one.
    /// </summary>
    internal static class ItemKey
    {
        private const char Sep = '|';

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
    }
}
