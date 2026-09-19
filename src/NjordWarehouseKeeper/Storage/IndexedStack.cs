using System.Collections.Generic;
using NjordWarehouseKeeper.Client;
using UnityEngine;

namespace NjordWarehouseKeeper.Storage
{
    /// <summary>
    /// One slot in a real chest that currently holds part of a grouped row.
    /// </summary>
    internal sealed class StackPart
    {
        internal Container Source;
        internal Vector2i Pos;
        internal string SharedName;

        internal ItemDrop.ItemData Live()
        {
            var inventory = Source == null ? null : Source.GetInventory();
            if (inventory == null)
            {
                return null;
            }

            var item = inventory.GetItemAt(Pos.x, Pos.y);
            if (item == null || item.m_shared == null || item.m_shared.m_name != SharedName)
            {
                return null;
            }

            return item;
        }
    }

    /// <summary>
    /// Every matching stack across the network, shown as one row. Withdraw still
    /// talks to the live inventories, closest chest first.
    /// </summary>
    internal sealed class IndexedStack
    {
        internal readonly List<StackPart> Parts = new List<StackPart>();
        internal string SharedName;
        internal string DisplayName;
        internal int Quantity;
        internal int Quality;
        internal int Variant;
        internal int WorldLevel;
        internal ItemCategory Category;
        internal Sprite Icon;
        internal float Distance;
        internal string MagicKey;

        internal bool SameAs(ItemDrop.ItemData item)
        {
            return item?.m_shared != null
                && item.m_shared.m_name == SharedName
                && item.m_quality == Quality
                && item.m_variant == Variant
                && item.m_worldLevel == WorldLevel
                && (MagicKey ?? "") == EpicLootCompat.GroupKey(item);
        }

        internal string Identity()
        {
            return SharedName + "\0" + Quality + "\0" + Variant + "\0" + WorldLevel + "\0" + (MagicKey ?? "");
        }

        internal string Key()
        {
            return ItemKey.Of(SharedName, Quality, Variant, WorldLevel);
        }

        internal StackPart FirstLivePart()
        {
            for (var i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].Live() != null)
                {
                    return Parts[i];
                }
            }

            return null;
        }

        internal ItemDrop.ItemData FirstLive()
        {
            var part = FirstLivePart();
            return part == null ? null : part.Live();
        }
    }
}
