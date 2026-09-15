using System.Collections.Generic;
using UnityEngine;

namespace GatewayChest.Storage
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

        internal bool SameAs(ItemDrop.ItemData item)
        {
            return item?.m_shared != null
                && item.m_shared.m_name == SharedName
                && item.m_quality == Quality
                && item.m_variant == Variant
                && item.m_worldLevel == WorldLevel;
        }

        internal string Identity()
        {
            return SharedName + "\0" + Quality + "\0" + Variant + "\0" + WorldLevel;
        }

        internal ItemDrop.ItemData FirstLive()
        {
            for (var i = 0; i < Parts.Count; i++)
            {
                var item = Parts[i].Live();
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
