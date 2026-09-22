using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using NjordWarehouseKeeper.Storage;

namespace NjordWarehouseKeeper.Client
{
    /// <summary>
    /// Client-only hub settings: favourites, deposit skipping those, and the
    /// resupply shopping list. Stored in the local BepInEx config and never
    /// synced — each player keeps their own stars and quantities.
    /// </summary>
    internal static class ClientPreferences
    {
        internal static ConfigEntry<bool> DepositSkipFavourites;
        internal static ConfigEntry<bool> ResetFiltersOnClose;
        internal static ConfigEntry<KeyboardShortcut> RestockHotkey;

        private static ConfigEntry<string> FavouritesRaw;
        private static ConfigEntry<string> ResupplyRaw;
        private static readonly HashSet<string> Favourites = new HashSet<string>();
        private static readonly Dictionary<string, int> Resupply = new Dictionary<string, int>();
        private static readonly List<string> ResupplyOrder = new List<string>();
        private static bool _loading;

        internal static void Bind(ConfigFile config)
        {
            DepositSkipFavourites = config.Bind(
                "Client",
                "DepositSkipFavourites",
                true,
                "Ignore favourite items on deposit. When depositing everything, leave starred items in your pack.");

            ResetFiltersOnClose = config.Bind(
                "Client",
                "ResetFiltersOnClose",
                true,
                "Reset search, category, favourites filter, and sort when Njord's panel closes.");

            FavouritesRaw = config.Bind(
                "Client",
                "Favourites",
                "",
                "Item keys marked with a star in Njord's list or on a pack slot. Unique items (weapons, armour, tools) store a per-instance id. Local to this client.");

            ResupplyRaw = config.Bind(
                "Client",
                "Resupply",
                "",
                "Item keys and counts the Resupply button tries to keep in your pack.");

            RestockHotkey = config.Bind(
                "Client",
                "RestockHotkey",
                KeyboardShortcut.Empty,
                "While in range of Njord, press this to Deposit and then Resupply without opening the panel.");

            FavouritesRaw.SettingChanged += OnTextChanged;
            ResupplyRaw.SettingChanged += OnTextChanged;
            Reload();
        }

        internal static bool IsFavourite(string key)
        {
            return !string.IsNullOrEmpty(key) && Favourites.Contains(key);
        }

        internal static bool IsFavourite(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return false;
            }

            var type = ItemKey.Of(item);
            if (!ItemKey.IsUnique(item))
            {
                return IsFavourite(type);
            }

            return IsFavourite(ItemKey.FavouriteOf(item)) || IsFavourite(type);
        }

        internal static bool IsFavourite(IndexedStack stack)
        {
            if (stack == null)
            {
                return false;
            }

            for (var i = 0; i < stack.Parts.Count; i++)
            {
                var live = stack.Parts[i].Live();
                if (live != null && IsFavourite(live))
                {
                    return true;
                }
            }

            return IsFavourite(stack.Key());
        }

        internal static void SetFavourite(string key, bool on)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var changed = on ? Favourites.Add(key) : Favourites.Remove(key);
            if (changed)
            {
                SaveFavourites();
            }
        }

        /// <summary>
        /// Stars or unstars the clicked item. Stackable goods still share one
        /// key. Unique pieces get their own id so a second sword is left alone.
        /// A leftover type-wide unique key is converted onto this instance.
        /// </summary>
        internal static void ToggleFavourite(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return;
            }

            if (!ItemKey.IsUnique(item))
            {
                var key = ItemKey.Of(item);
                SetFavourite(key, !IsFavourite(key));
                return;
            }

            var type = ItemKey.Of(item);
            var instance = ItemKey.EnsureFavourite(item);
            if (IsFavourite(type) && !IsFavourite(instance))
            {
                Favourites.Remove(type);
                Favourites.Add(instance);
                SaveFavourites();
                return;
            }

            var on = !IsFavourite(instance);
            var changed = on ? Favourites.Add(instance) : Favourites.Remove(instance);
            if (Favourites.Remove(type))
            {
                changed = true;
            }

            if (changed)
            {
                SaveFavourites();
            }
        }

        internal static bool TryGetResupply(string key, out int quantity)
        {
            return Resupply.TryGetValue(key, out quantity) && quantity > 0;
        }

        internal static void SetResupply(string key, int quantity)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (quantity <= 0)
            {
                if (Resupply.Remove(key))
                {
                    ResupplyOrder.Remove(key);
                    SaveResupply();
                }

                return;
            }

            if (Resupply.ContainsKey(key))
            {
                if (Resupply[key] == quantity)
                {
                    return;
                }

                Resupply[key] = quantity;
            }
            else
            {
                Resupply[key] = quantity;
                ResupplyOrder.Add(key);
            }

            SaveResupply();
        }

        internal static List<string> FavouriteKeys()
        {
            var keys = new string[Favourites.Count];
            Favourites.CopyTo(keys);
            Array.Sort(keys, StringComparer.Ordinal);
            return new List<string>(keys);
        }

        internal static List<string> ResupplyKeys()
        {
            return new List<string>(ResupplyOrder);
        }

        private static void OnTextChanged(object sender, EventArgs args)
        {
            if (!_loading)
            {
                Reload();
            }
        }

        private static void Reload()
        {
            Favourites.Clear();
            if (FavouritesRaw != null && !string.IsNullOrEmpty(FavouritesRaw.Value))
            {
                var parts = FavouritesRaw.Value.Split(';');
                for (var i = 0; i < parts.Length; i++)
                {
                    var key = parts[i].Trim();
                    if (key.Length > 0)
                    {
                        Favourites.Add(key);
                    }
                }
            }

            Resupply.Clear();
            ResupplyOrder.Clear();
            if (ResupplyRaw != null && !string.IsNullOrEmpty(ResupplyRaw.Value))
            {
                var parts = ResupplyRaw.Value.Split(';');
                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i].Trim();
                    if (part.Length == 0)
                    {
                        continue;
                    }

                    var eq = part.LastIndexOf('=');
                    if (eq <= 0 || eq == part.Length - 1)
                    {
                        continue;
                    }

                    var key = part.Substring(0, eq);
                    int qty;
                    if (key.Length == 0 || !int.TryParse(part.Substring(eq + 1), out qty) || qty <= 0)
                    {
                        continue;
                    }

                    if (Resupply.ContainsKey(key))
                    {
                        Resupply[key] = qty;
                    }
                    else
                    {
                        Resupply[key] = qty;
                        ResupplyOrder.Add(key);
                    }
                }
            }
        }

        private static void SaveFavourites()
        {
            var keys = new string[Favourites.Count];
            Favourites.CopyTo(keys);
            Array.Sort(keys, StringComparer.Ordinal);
            Write(FavouritesRaw, string.Join(";", keys));
        }

        private static void SaveResupply()
        {
            var parts = new List<string>(ResupplyOrder.Count);
            for (var i = 0; i < ResupplyOrder.Count; i++)
            {
                var key = ResupplyOrder[i];
                int qty;
                if (Resupply.TryGetValue(key, out qty) && qty > 0)
                {
                    parts.Add(key + "=" + qty);
                }
            }

            Write(ResupplyRaw, string.Join(";", parts.ToArray()));
        }

        private static void Write(ConfigEntry<string> entry, string value)
        {
            if (entry == null)
            {
                return;
            }

            _loading = true;
            try
            {
                entry.Value = value ?? "";
            }
            finally
            {
                _loading = false;
            }
        }
    }
}
