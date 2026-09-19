using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;

namespace NjordWarehouseKeeper.Client
{
    /// <summary>
    /// Optional EpicLoot hook. No hard reference: if that plugin is loaded we
    /// call its ItemData extensions so Njord rows can use the same rarity
    /// colours as pack slots. Absent EpicLoot, every call is a no-op.
    /// </summary>
    internal static class EpicLootCompat
    {
        internal const string PluginId = "randyknapp.mods.epicloot";

        private static bool _tried;
        private static bool _ready;
        private static MethodInfo _hasRarity;
        private static MethodInfo _getRarity;
        private static MethodInfo _getRarityColor;
        private static MethodInfo _getDisplayName;
        private static MethodInfo _getMagicBg;

        internal static bool Present
        {
            get
            {
                Bind();
                return _ready;
            }
        }

        internal static string GroupKey(ItemDrop.ItemData item)
        {
            if (!HasRarity(item))
            {
                return "";
            }

            return RarityName(item) + "\0" + RawDisplayName(item);
        }

        internal static string DisplayName(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return "";
            }

            var raw = RawDisplayName(item);
            if (string.IsNullOrEmpty(raw))
            {
                raw = item.m_shared.m_name;
            }

            return Localization.instance != null
                ? Localization.instance.Localize(raw)
                : raw;
        }

        internal static bool TryGetRarityColor(ItemDrop.ItemData item, out Color color)
        {
            color = Color.white;
            Bind();
            if (!_ready || item == null || _getRarityColor == null || !HasRarity(item))
            {
                return false;
            }

            try
            {
                color = (Color)_getRarityColor.Invoke(null, new object[] { item });
                return color.a > 0f;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryGetMagicBackground(out Sprite sprite)
        {
            sprite = null;
            Bind();
            if (!_ready || _getMagicBg == null)
            {
                return false;
            }

            try
            {
                sprite = _getMagicBg.Invoke(null, null) as Sprite;
                return sprite != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasRarity(ItemDrop.ItemData item)
        {
            Bind();
            if (!_ready || item == null || _hasRarity == null)
            {
                return false;
            }

            try
            {
                return (bool)_hasRarity.Invoke(null, new object[] { item });
            }
            catch
            {
                return false;
            }
        }

        private static string RarityName(ItemDrop.ItemData item)
        {
            if (_getRarity == null || item == null)
            {
                return "";
            }

            try
            {
                var value = _getRarity.Invoke(null, new object[] { item });
                return value != null ? value.ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        private static string RawDisplayName(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return "";
            }

            Bind();
            if (_ready && _getDisplayName != null)
            {
                try
                {
                    var name = _getDisplayName.Invoke(null, new object[] { item }) as string;
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                catch
                {
                }
            }

            return item.m_shared.m_name;
        }

        private static void Bind()
        {
            if (_tried)
            {
                return;
            }

            _tried = true;
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(PluginId, out info) || info?.Instance == null)
            {
                return;
            }

            try
            {
                var assembly = info.Instance.GetType().Assembly;
                var extensions = assembly.GetType("EpicLoot.ItemDataExtensions");
                var main = assembly.GetType("EpicLoot.EpicLoot");
                if (extensions == null)
                {
                    return;
                }

                var item = new[] { typeof(ItemDrop.ItemData) };
                _hasRarity = extensions.GetMethod("HasRarity", BindingFlags.Public | BindingFlags.Static, null, item, null);
                _getRarity = extensions.GetMethod("GetRarity", BindingFlags.Public | BindingFlags.Static, null, item, null);
                _getRarityColor = extensions.GetMethod("GetRarityColor", BindingFlags.Public | BindingFlags.Static, null, item, null);
                _getDisplayName = extensions.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.Static, null, item, null);
                _getMagicBg = main != null
                    ? main.GetMethod("GetMagicItemBgSprite", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                    : null;
                _ready = _hasRarity != null && _getRarityColor != null;
            }
            catch (Exception ex)
            {
                _ready = false;
                NjordWarehouseKeeperPlugin.Log.LogWarning("EpicLoot was present but its rarity API could not be bound: " + ex.Message);
            }
        }
    }
}
