using System.Collections.Generic;
using Jotunn.Managers;

namespace GatewayChest
{
    internal static class Localizations
    {
        internal static void Register()
        {
            var localization = LocalizationManager.Instance.GetLocalization();
            localization.AddTranslation(
                "English",
                new Dictionary<string, string>
                {
                    { $"{GatewayChestPiece.PrefabName}_name", "Gateway Chest" },
                    {
                        $"{GatewayChestPiece.PrefabName}_description",
                        "Opens a window onto every nearby chest. Deposit and withdraw without walking the room."
                    },
                    { "gatewaychest_capacity", "Used: {0} / {1} slots" },
                    { "gatewaychest_chests", "{0} chests" },
                    { "gatewaychest_search", "Search…" },
                    { "gatewaychest_deposit", "Deposit" },
                    { "gatewaychest_cat_all", "All" },
                    { "gatewaychest_cat_weapons", "Weapons" },
                    { "gatewaychest_cat_armor", "Armor" },
                    { "gatewaychest_cat_food", "Food" },
                    { "gatewaychest_cat_materials", "Materials" },
                    { "gatewaychest_cat_trophies", "Trophies" },
                    { "gatewaychest_cat_misc", "Misc" },
                    { "gatewaychest_sort_name", "Name" },
                    { "gatewaychest_sort_qty", "Qty" },
                    { "gatewaychest_sort_cat", "Cat" },
                    { "gatewaychest_empty", "No items in range." },
                    { "gatewaychest_nospace", "No space in nearby chests." },
                    { "gatewaychest_playerfull", "Inventory full." },
                });
        }
    }
}
