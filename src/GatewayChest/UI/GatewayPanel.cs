using System;
using System.Collections.Generic;
using GatewayChest.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace GatewayChest.UI
{
    /// <summary>
    /// Replaces the vanilla container panel while a Gateway Chest is open. Player
    /// inventory stays on the left; this wood panel is the aggregated list, search,
    /// categories, sort, capacity, and deposit control.
    /// </summary>
    internal static class GatewayPanel
    {
        private enum SortMode
        {
            Name,
            Quantity,
            Category,
        }

        private const float PanelWidth = 540f;
        private const float PanelHeight = 620f;
        private const float RowHeight = 36f;

        private static GameObject _root;
        private static Text _title;
        private static Text _capacity;
        private static Text _empty;
        private static InputField _search;
        private static Transform _rowParent;
        private static readonly List<GameObject> _rows = new List<GameObject>();
        private static ItemCategory _category = ItemCategory.All;
        private static SortMode _sort = SortMode.Name;
        private static string _query = "";
        private static float _nextRefresh;
        private static Container _pending;

        internal static void Open(Container hub)
        {
            if (GUIManager.IsHeadless() || hub == null)
            {
                return;
            }

            GatewayChestHub.OpenHub = hub;
            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
            {
                _pending = hub;
                GUIManager.OnCustomGUIAvailable -= BuildPending;
                GUIManager.OnCustomGUIAvailable += BuildPending;
                return;
            }

            EnsureBuilt();
            if (_root == null)
            {
                return;
            }

            _root.SetActive(true);
            _nextRefresh = 0f;
            Refresh();
        }

        internal static void Close()
        {
            GatewayChestHub.OpenHub = null;
            _pending = null;
            if (_root != null)
            {
                _root.SetActive(false);
            }

            GUIManager.BlockInput(false);
        }

        internal static void Tick()
        {
            if (GatewayChestHub.OpenHub == null || _root == null || !_root.activeSelf)
            {
                return;
            }

            if (!InventoryGui.IsVisible())
            {
                Close();
                return;
            }

            if (Time.time >= _nextRefresh)
            {
                Refresh();
            }
        }

        private static void BuildPending()
        {
            GUIManager.OnCustomGUIAvailable -= BuildPending;
            if (_pending != null)
            {
                Open(_pending);
                _pending = null;
            }
        }

        private static void EnsureBuilt()
        {
            if (_root != null)
            {
                return;
            }

            var gui = GUIManager.Instance;
            var parent = GUIManager.CustomGUIFront.transform;
            var center = new Vector2(0.5f, 0.5f);

            _root = gui.CreateWoodpanel(
                parent,
                center,
                center,
                new Vector2(430f, 20f),
                PanelWidth,
                PanelHeight,
                draggable: false);
            _root.name = "GatewayChestPanel";

            _title = gui.CreateText(
                Localization.instance.Localize("$gateway_chest_name"),
                _root.transform,
                center,
                center,
                new Vector2(0f, 280f),
                gui.AveriaSerifBold,
                22,
                gui.ValheimOrange,
                true,
                Color.black,
                500f,
                32f,
                false).GetComponent<Text>();
            _title.alignment = TextAnchor.MiddleCenter;

            _capacity = gui.CreateText(
                "",
                _root.transform,
                center,
                center,
                new Vector2(0f, 248f),
                gui.AveriaSerif,
                16,
                Color.white,
                true,
                Color.black,
                500f,
                24f,
                false).GetComponent<Text>();
            _capacity.alignment = TextAnchor.MiddleCenter;

            _search = gui.CreateInputField(
                _root.transform,
                center,
                center,
                new Vector2(-70f, 210f),
                InputField.ContentType.Standard,
                Localization.instance.Localize("$gatewaychest_search"),
                16,
                280f,
                32f).GetComponent<InputField>();
            _search.onValueChanged.AddListener(OnSearch);

            var deposit = gui.CreateButton(
                Localization.instance.Localize("$gatewaychest_deposit"),
                _root.transform,
                center,
                center,
                new Vector2(175f, 210f),
                140f,
                32f).GetComponent<Button>();
            deposit.onClick.AddListener(OnDeposit);

            AddCategoryButtons(gui);
            AddSortButtons(gui);

            var scroll = gui.CreateScrollView(
                _root.transform,
                false,
                true,
                8f,
                4f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0f, 0f, 0f, 0.4f),
                500f,
                400f);
            var scrollRect = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollRect != null)
            {
                var scrollTf = scroll.GetComponent<RectTransform>();
                if (scrollTf != null)
                {
                    scrollTf.anchoredPosition = new Vector2(0f, -70f);
                }

                _rowParent = scrollRect.content;
                var layout = _rowParent.gameObject.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = _rowParent.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.spacing = 2f;
                layout.padding = new RectOffset(4, 18, 4, 4);

                var fitter = _rowParent.gameObject.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = _rowParent.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            _empty = gui.CreateText(
                Localization.instance.Localize("$gatewaychest_empty"),
                _root.transform,
                center,
                center,
                new Vector2(0f, -40f),
                gui.AveriaSerif,
                16,
                Color.white,
                true,
                Color.black,
                400f,
                28f,
                false).GetComponent<Text>();
            _empty.alignment = TextAnchor.MiddleCenter;

            _root.SetActive(false);
        }

        private static void AddCategoryButtons(GUIManager gui)
        {
            var cats = new[]
            {
                ItemCategory.All,
                ItemCategory.Weapons,
                ItemCategory.Armor,
                ItemCategory.Food,
                ItemCategory.Materials,
                ItemCategory.Trophies,
                ItemCategory.Misc,
            };

            var center = new Vector2(0.5f, 0.5f);
            var x = -228f;
            foreach (var cat in cats)
            {
                var captured = cat;
                var button = gui.CreateButton(
                    Localization.instance.Localize("$" + ItemCategories.Token(cat)),
                    _root.transform,
                    center,
                    center,
                    new Vector2(x, 172f),
                    64f,
                    26f).GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    _category = captured;
                    Refresh();
                });
                x += 66f;
            }
        }

        private static void AddSortButtons(GUIManager gui)
        {
            var center = new Vector2(0.5f, 0.5f);
            AddSort(gui, SortMode.Name, "$gatewaychest_sort_name", -140f, center);
            AddSort(gui, SortMode.Quantity, "$gatewaychest_sort_qty", 0f, center);
            AddSort(gui, SortMode.Category, "$gatewaychest_sort_cat", 140f, center);
        }

        private static void AddSort(GUIManager gui, SortMode mode, string token, float x, Vector2 center)
        {
            var button = gui.CreateButton(
                Localization.instance.Localize(token),
                _root.transform,
                center,
                center,
                new Vector2(x, 142f),
                120f,
                24f).GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                _sort = mode;
                Refresh();
            });
        }

        private static void OnSearch(string value)
        {
            _query = value ?? "";
            Refresh();
        }

        private static void OnDeposit()
        {
            StorageNetwork.DepositAll(Player.m_localPlayer, GatewayChestHub.OpenHub);
            Refresh();
        }

        private static void Refresh()
        {
            _nextRefresh = Time.time + 0.45f;
            var hub = GatewayChestHub.OpenHub;
            if (hub == null || _rowParent == null)
            {
                return;
            }

            if (_title != null)
            {
                _title.text = Localization.instance.Localize("$gateway_chest_name");
            }

            var snapshot = StorageNetwork.Snapshot(hub);
            if (_capacity != null)
            {
                _capacity.text =
                    Localization.instance.Localize("$gatewaychest_capacity")
                        .Replace("{0}", snapshot.UsedSlots.ToString())
                        .Replace("{1}", snapshot.TotalSlots.ToString())
                    + "   ·   " +
                    Localization.instance.Localize("$gatewaychest_chests")
                        .Replace("{0}", snapshot.Chests.Count.ToString());
            }

            foreach (var row in _rows)
            {
                UnityEngine.Object.Destroy(row);
            }

            _rows.Clear();

            var visible = Filter(StorageNetwork.ListItems(hub));
            if (_empty != null)
            {
                _empty.gameObject.SetActive(visible.Count == 0);
            }

            foreach (var stack in visible)
            {
                _rows.Add(MakeRow(stack));
            }
        }

        private static List<IndexedStack> Filter(List<IndexedStack> items)
        {
            var query = _query.Trim();
            var filtered = new List<IndexedStack>();
            foreach (var item in items)
            {
                if (_category != ItemCategory.All && item.Category != _category)
                {
                    continue;
                }

                if (query.Length > 0 &&
                    item.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                filtered.Add(item);
            }

            filtered.Sort(Compare);
            return filtered;
        }

        private static int Compare(IndexedStack a, IndexedStack b)
        {
            int order;
            switch (_sort)
            {
                case SortMode.Quantity:
                    order = b.Quantity.CompareTo(a.Quantity);
                    break;
                case SortMode.Category:
                    order = a.Category.CompareTo(b.Category);
                    break;
                default:
                    order = string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                    break;
            }

            if (order != 0)
            {
                return order;
            }

            return a.Distance.CompareTo(b.Distance);
        }

        private static GameObject MakeRow(IndexedStack stack)
        {
            var gui = GUIManager.Instance;
            var label = stack.DisplayName + "   x" + stack.Quantity;
            var row = gui.CreateButton(
                label,
                _rowParent,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                0f,
                RowHeight);
            var layout = row.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = row.AddComponent<LayoutElement>();
            }

            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1f;

            var image = row.GetComponentInChildren<Image>();
            if (image != null && stack.Icon != null)
            {
                // Keep the wood button; put the item sprite on a child so the row still clicks.
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(row.transform, false);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = stack.Icon;
                icon.preserveAspect = true;
                var rt = icon.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(28f, 28f);
                rt.anchoredPosition = new Vector2(6f, 0f);
            }

            var captured = stack;
            row.GetComponent<Button>().onClick.AddListener(() =>
            {
                StorageNetwork.Withdraw(Player.m_localPlayer, captured);
                Refresh();
            });

            return row;
        }
    }
}
