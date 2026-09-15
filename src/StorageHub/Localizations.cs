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
                    { $"{StorageHubPiece.PrefabName}_name", "Njord, Warehouse Keeper" },
                    {
                        $"{StorageHubPiece.PrefabName}_description",
                        "A keeper who opens a window onto every nearby chest. Deposit and withdraw without walking the room. Costs 200 coins."
                    },
                    { "storagehub_npc", "Njord" },
                    { "storagehub_talk_greet_1", "Need something from the stores?" },
                    { "storagehub_talk_greet_2", "Njord's watching the chests." },
                    { "storagehub_talk_greet_3", "Come to fetch, or to fill?" },
                    { "storagehub_talk_idle_1", "Gold in, goods out. That's the work." },
                    { "storagehub_talk_idle_2", "Those chests are heavier than they look." },
                    { "storagehub_talk_idle_3", "Keep the aisles clear. I like a tidy hall." },
                    { "storagehub_talk_idle_4", "If it's in a chest nearby, I can find it." },
                    { "storagehub_talk_idle_5", "Don't ask me to haul. I keep the books." },
                    { "storagehub_talk_idle_6", "A place for every stack." },
                    { "storagehub_talk_idle_7", "The sea takes ships. I take inventories." },
                    { "storagehub_talk_idle_8", "Count twice. Lose nothing." },
                    { "storagehub_talk_bye_1", "The chests will still be here." },
                    { "storagehub_talk_bye_2", "I'll keep an eye on the stores." },
                    { "storagehub_talk_bye_3", "Come back when the shelves need sorting." },
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
                    { "storagehub_restocked", "Deposited and resupplied from the stores." },
                    { "storagehub_restock_none", "Nothing to deposit or resupply." },
                    { "storagehub_hotkey_norange", "No warehouse keeper in range." },
                    { "storagehub_recipe", "Recipe" },
                    { "storagehub_recipes", "Recipes" },
                    { "storagehub_recipe_hint", "Known recipes. Greyed out if nearby chests are short of ingredients. Click to take them." },
                    { "storagehub_station_all", "All" },
                    { "storagehub_station_hand", "Handcraft" },
                    { "storagehub_station_hammer", "Hammer" },
                    { "storagehub_recipe_missing", "The nearby chests do not have those ingredients." },
                    { "storagehub_recipe_ok", "Took the ingredients." },
                    { "storagehub_preferences", "Preferences" },
                    { "storagehub_pref_skip_favourites", "Deposit leaves favourite items in the pack" },
                    { "storagehub_pref_hotkey", "Deposit + Resupply hotkey" },
                    { "storagehub_pref_hotkey_none", "None" },
                    { "storagehub_pref_hotkey_listen", "Press a key…" },
                    { "storagehub_pref_resupply_hint", "Tick items to keep in your pack. Resupply pulls that many from nearby chests." },
                });
        }
    }
}
