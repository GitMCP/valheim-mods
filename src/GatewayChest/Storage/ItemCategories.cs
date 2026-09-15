namespace GatewayChest.Storage
{
    internal enum ItemCategory
    {
        All,
        Weapons,
        Armor,
        Food,
        Materials,
        Trophies,
        Misc,
    }

    internal static class ItemCategories
    {
        internal static ItemCategory Of(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return ItemCategory.Misc;
            }

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Tool:
                    return ItemCategory.Weapons;
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Trinket:
                    return ItemCategory.Armor;
                case ItemDrop.ItemData.ItemType.Consumable:
                    return ItemCategory.Food;
                case ItemDrop.ItemData.ItemType.Material:
                    return ItemCategory.Materials;
                case ItemDrop.ItemData.ItemType.Trophy:
                    return ItemCategory.Trophies;
                default:
                    return ItemCategory.Misc;
            }
        }

        internal static string Token(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapons:
                    return "gatewaychest_cat_weapons";
                case ItemCategory.Armor:
                    return "gatewaychest_cat_armor";
                case ItemCategory.Food:
                    return "gatewaychest_cat_food";
                case ItemCategory.Materials:
                    return "gatewaychest_cat_materials";
                case ItemCategory.Trophies:
                    return "gatewaychest_cat_trophies";
                case ItemCategory.Misc:
                    return "gatewaychest_cat_misc";
                default:
                    return "gatewaychest_cat_all";
            }
        }
    }
}
