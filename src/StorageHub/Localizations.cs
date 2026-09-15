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
                    { "storagehub_quickstack", "Quick Stack" },
                    { "storagehub_tab_items", "Items" },
                    { "storagehub_tab_recipes", "Recipes" },
                    { "storagehub_cat_all", "All" },
                    { "storagehub_cat_weapons", "Weapons" },
                    { "storagehub_cat_armor", "Armor" },
                    { "storagehub_cat_food", "Food" },
                    { "storagehub_cat_materials", "Materials" },
                    { "storagehub_cat_trophies", "Trophies" },
                    { "storagehub_cat_misc", "Misc" },
                    { "storagehub_cat_favourites", "Favourites" },
                    { "storagehub_sort_name", "Name" },
                    { "storagehub_sort_qty", "Qty" },
                    { "storagehub_sort_cat", "Cat" },
                    { "storagehub_empty", "No items in range." },
                    { "storagehub_empty_favourites", "No favourite items." },
                    { "storagehub_nospace", "No space in nearby chests." },
                    { "storagehub_playerfull", "Inventory full." },
                    { "storagehub_resupply", "Resupply" },
                    { "storagehub_resupply_none", "Nothing to resupply." },
                    { "storagehub_restocked", "Deposited and resupplied from the hub." },
                    { "storagehub_restock_none", "Nothing to deposit or resupply." },
                    { "storagehub_hotkey_norange", "No Storage Hub in range." },
                    { "storagehub_recipe", "Recipe" },
                    { "storagehub_recipes", "Recipes" },
                    { "storagehub_recipe_hint", "Known recipes. Greyed out if the hub is short of ingredients. Click to take them." },
                    { "storagehub_recipe_missing", "The hub does not have those ingredients." },
                    { "storagehub_recipe_ok", "Took the ingredients." },
                    { "storagehub_preferences", "Preferences" },
                    { "storagehub_pref_skip_favourites", "Deposit leaves favourite items in the pack" },
                    { "storagehub_pref_hotkey", "Deposit + Resupply hotkey" },
                    { "storagehub_pref_hotkey_none", "None" },
                    { "storagehub_pref_hotkey_listen", "Press a key…" },
                    { "storagehub_pref_resupply_hint", "Tick items to keep in your pack. Resupply pulls that many from the hub." },
                });
        }
    }
}
