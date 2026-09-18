using System.Collections.Generic;
using Jotunn.Managers;

namespace NjordWarehouseKeeper
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
                    { $"{NjordWarehouseKeeperPiece.PrefabName}_name", "Njord, Warehouse Keeper" },
                    {
                        $"{NjordWarehouseKeeperPiece.PrefabName}_description",
                        "A keeper who opens a window onto every nearby chest. Deposit and withdraw without walking the room. Costs 200 coins."
                    },
                    { "njord_npc", "Njord" },
                    { "njord_talk_greet_1", "Need something from the stores?" },
                    { "njord_talk_greet_2", "Njord's watching the chests." },
                    { "njord_talk_greet_3", "Come to fetch, or to fill?" },
                    { "njord_talk_idle_1", "Gold in, goods out. That's the work." },
                    { "njord_talk_idle_2", "Those chests are heavier than they look." },
                    { "njord_talk_idle_3", "Keep the aisles clear. I like a tidy hall." },
                    { "njord_talk_idle_4", "If it's in a chest nearby, I can find it." },
                    { "njord_talk_idle_5", "Don't ask me to haul. I keep the books." },
                    { "njord_talk_idle_6", "A place for every stack." },
                    { "njord_talk_idle_7", "The sea takes ships. I take inventories." },
                    { "njord_talk_idle_8", "Count twice. Lose nothing." },
                    { "njord_talk_bye_1", "The chests will still be here." },
                    { "njord_talk_bye_2", "I'll keep an eye on the stores." },
                    { "njord_talk_bye_3", "Come back when the shelves need sorting." },
                    { "njord_capacity", "Used: {0} / {1} slots" },
                    { "njord_chests", "{0} chests" },
                    { "njord_search", "Search…" },
                    { "njord_deposit", "Deposit" },
                    { "njord_quickstack", "Quick Stack" },
                    { "njord_tab_items", "Items" },
                    { "njord_tab_recipes", "Recipes" },
                    { "njord_withdraw", "Withdraw" },
                    { "njord_recipes_empty", "No known recipes." },
                    { "njord_cat_all", "All" },
                    { "njord_cat_weapons", "Weapons" },
                    { "njord_cat_armor", "Armor" },
                    { "njord_cat_food", "Food" },
                    { "njord_cat_materials", "Materials" },
                    { "njord_cat_trophies", "Trophies" },
                    { "njord_cat_misc", "Misc" },
                    { "njord_cat_favourites", "Favourites" },
                    { "njord_sort_name", "Name" },
                    { "njord_sort_qty", "Qty" },
                    { "njord_sort_cat", "Cat" },
                    { "njord_empty", "No items in range." },
                    { "njord_empty_favourites", "No favourite items." },
                    { "njord_nospace", "No space in nearby chests." },
                    { "njord_playerfull", "Inventory full." },
                    { "njord_resupply", "Resupply" },
                    { "njord_resupply_none", "Nothing to resupply." },
                    { "njord_restocked", "Deposited and resupplied from the stores." },
                    { "njord_restock_none", "Nothing to deposit or resupply." },
                    { "njord_hotkey_norange", "No warehouse keeper in range." },
                    { "njord_recipe", "Recipe" },
                    { "njord_recipes", "Recipes" },
                    { "njord_recipe_hint", "Known recipes. Greyed out if nearby chests are short of ingredients. Withdraw takes what is there. Hover an ingredient to see have / need." },
                    { "njord_station_all", "All" },
                    { "njord_station_hand", "Handcraft" },
                    { "njord_station_hammer", "Hammer" },
                    { "njord_recipe_missing", "The nearby chests do not have those ingredients." },
                    { "njord_recipe_ok", "Took the ingredients." },
                    { "njord_recipe_partial", "Took what the stores had." },
                    { "njord_preferences", "Preferences" },
                    { "njord_pref_skip_favourites", "Deposit leaves favourite items in the pack" },
                    { "njord_pref_hotkey", "Deposit + Resupply hotkey" },
                    { "njord_pref_hotkey_none", "None" },
                    { "njord_pref_hotkey_listen", "Press a key…" },
                    { "njord_pref_resupply_hint", "Tick items to keep in your pack. Resupply pulls that many from nearby chests." },
                });
        }
    }
}
