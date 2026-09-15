using NjordWarehouseKeeper.Storage;
using UnityEngine;

namespace NjordWarehouseKeeper.Client
{
    internal static class ItemLookup
    {
        internal static IndexedStack Describe(string key)
        {
            string shared;
            int quality;
            int variant;
            int worldLevel;
            if (!ItemKey.TryParse(key, out shared, out quality, out variant, out worldLevel))
            {
                return null;
            }

            var sample = FindSample(shared);
            var display = sample?.m_shared != null
                ? Localization.instance.Localize(sample.m_shared.m_name)
                : shared;
            Sprite icon = null;
            if (sample != null)
            {
                sample.m_quality = quality;
                sample.m_variant = variant;
                sample.m_worldLevel = worldLevel;
                icon = sample.GetIcon();
            }

            return new IndexedStack
            {
                SharedName = shared,
                DisplayName = display,
                Quality = quality,
                Variant = variant,
                WorldLevel = worldLevel,
                Icon = icon,
                Category = sample != null ? ItemCategories.Of(sample) : ItemCategory.Misc,
            };
        }

        internal static int MaxStack(string key)
        {
            string shared;
            int quality;
            int variant;
            int worldLevel;
            if (!ItemKey.TryParse(key, out shared, out quality, out variant, out worldLevel))
            {
                return 1;
            }

            var sample = FindSample(shared);
            var max = sample?.m_shared != null ? sample.m_shared.m_maxStackSize : 1;
            return Mathf.Max(1, max);
        }

        internal static int CountIn(Inventory inventory, string key)
        {
            if (inventory == null || string.IsNullOrEmpty(key))
            {
                return 0;
            }

            var n = 0;
            foreach (var item in inventory.GetAllItems())
            {
                if (ItemKey.Of(item) == key)
                {
                    n += item.m_stack;
                }
            }

            return n;
        }

        private static ItemDrop.ItemData FindSample(string sharedName)
        {
            var db = ObjectDB.instance;
            if (db == null || db.m_items == null || string.IsNullOrEmpty(sharedName))
            {
                return null;
            }

            for (var i = 0; i < db.m_items.Count; i++)
            {
                var drop = db.m_items[i] == null ? null : db.m_items[i].GetComponent<ItemDrop>();
                if (drop?.m_itemData?.m_shared != null && drop.m_itemData.m_shared.m_name == sharedName)
                {
                    return drop.m_itemData.Clone();
                }
            }

            return null;
        }
    }
}
