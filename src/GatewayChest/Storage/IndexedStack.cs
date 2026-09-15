using UnityEngine;

namespace GatewayChest.Storage
{
    /// <summary>
    /// One stack as it currently sits in a real chest. The UI holds this, not a cloned
    /// ItemData: taking or depositing always talks to the source inventory.
    /// </summary>
    internal sealed class IndexedStack
    {
        internal Container Source;
        internal Vector2i Pos;
        internal string SharedName;
        internal string DisplayName;
        internal int Quantity;
        internal ItemCategory Category;
        internal Sprite Icon;
        internal float Distance;

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
}
