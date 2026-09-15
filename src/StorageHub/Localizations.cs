using System.Collections.Generic;
using Jotunn.Managers;

namespace StorageHub
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
                    { $"{StorageHubPiece.PrefabName}_name", "Storage Hub" },
                    {
                        $"{StorageHubPiece.PrefabName}_description",
                        "Opens a window onto every nearby chest. Deposit and withdraw without walking the room."
                    },
                    { "storagehub_capacity", "Used: {0} / {1} slots" },
                    { "storagehub_chests", "{0} chests" },
                    { "storagehub_search", "Search…" },
                    { "storagehub_deposit", "Deposit" },
                    { "storagehub_cat_all", "All" },
                    { "storagehub_cat_weapons", "Weapons" },
                    { "storagehub_cat_armor", "Armor" },
                    { "storagehub_cat_food", "Food" },
                    { "storagehub_cat_materials", "Materials" },
                    { "storagehub_cat_trophies", "Trophies" },
                    { "storagehub_cat_misc", "Misc" },
                    { "storagehub_sort_name", "Name" },
                    { "storagehub_sort_qty", "Qty" },
                    { "storagehub_sort_cat", "Cat" },
                    { "storagehub_empty", "No items in range." },
                    { "storagehub_nospace", "No space in nearby chests." },
                    { "storagehub_playerfull", "Inventory full." },
                });
        }
    }
}
