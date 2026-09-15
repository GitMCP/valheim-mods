using System;
using System.Collections.Generic;
using GatewayChest.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace GatewayChest.UI
{
    /// <summary>
    /// Sits in the vanilla container slot (beside the player inventory, not over the
    /// crafting column) and lists the nearby chests as full-width rows.
    /// </summary>
    internal static class GatewayPanel
    {
        private enum SortMode
        {
            Name,
            Quantity,
            Category,
        }

        private const float PanelWidth = 520f;
        private const float PanelHeight = 640f;
        private const float RowHeight = 42f;
        private const float IconSize = 32f;

        private static GameObject _root;
        private static Text _title;
        private static Text _capacity;
        private static Text _empty;
        private static InputField _search;
        private static RectTransform _scrollRect;
        private static Transform _rowParent;
        private static readonly List<GameObject> _rows = new List<GameObject>();
        private static readonly List<Button> _categoryButtons = new List<Button>();
        private static readonly List<Button> _sortButtons = new List<Button>();
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

            SnapToChestSlot();
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

            SnapToChestSlot();
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

        /// <summary>
        /// The overlay canvas is full-screen, so a centred offset lands on the crafting
        /// column. The vanilla container rect is already parked between the backpack
        /// and the craft list; follow that.
        /// </summary>
        private static void SnapToChestSlot()
        {
            var gui = InventoryGui.instance;
            if (gui == null || gui.m_container == null || _root == null)
            {
                return;
            }

            var ours = _root.GetComponent<RectTransform>();
            var slot = gui.m_container;
            if (ours.parent != slot.parent)
            {
                ours.SetParent(slot.parent, false);
                _root.transform.SetAsLastSibling();
            }

            ours.anchorMin = slot.anchorMin;
            ours.anchorMax = slot.anchorMax;
            ours.pivot = slot.pivot;
            ours.anchoredPosition = slot.anchoredPosition + new Vector2(-20f, -30f);
            ours.sizeDelta = new Vector2(
                Mathf.Max(slot.rect.width, PanelWidth),
                Mathf.Max(slot.rect.height, PanelHeight));
        }

        private static void EnsureBuilt()
        {
            if (_root != null)
            {
                return;
            }

            var gui = GUIManager.Instance;
            var parent = GUIManager.CustomGUIFront.transform;
            var mid = new Vector2(0.5f, 0.5f);

            _root = gui.CreateWoodpanel(
                parent,
                mid,
                mid,
                Vector2.zero,
                PanelWidth,
                PanelHeight,
                draggable: false);
            _root.name = "GatewayChestPanel";

            _title = MakeText(
                gui,
                Localization.instance.Localize("$gateway_chest_name"),
                new Vector2(0f, -28f),
                22,
                gui.ValheimOrange,
                480f,
                28f);
            _title.alignment = TextAnchor.MiddleCenter;

            _capacity = MakeText(
                gui,
                "",
                new Vector2(0f, -54f),
                15,
                Color.white,
                480f,
                22f);
            _capacity.alignment = TextAnchor.MiddleCenter;

            _search = gui.CreateInputField(
                _root.transform,
                mid,
                mid,
                new Vector2(-70f, 230f),
                InputField.ContentType.Standard,
                Localization.instance.Localize("$gatewaychest_search"),
                16,
                320f,
                30f).GetComponent<InputField>();
            _search.onValueChanged.AddListener(OnSearch);
            PlaceTop(_search.GetComponent<RectTransform>(), 80f, 320f, 30f);

            var depositGo = gui.CreateButton(
                Localization.instance.Localize("$gatewaychest_deposit"),
                _root.transform,
                mid,
                mid,
                Vector2.zero,
                130f,
                30f);
            gui.ApplyButtonStyle(depositGo.GetComponent<Button>(), 15);
            depositGo.GetComponent<Button>().onClick.AddListener(OnDeposit);
            PlaceTop(depositGo.GetComponent<RectTransform>(), 80f, 130f, 30f, right: true);

            AddCategoryButtons(gui);
            AddSortButtons(gui);

            var scroll = gui.CreateScrollView(
                _root.transform,
                false,
                true,
                10f,
                3f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0f, 0f, 0f, 0.35f),
                480f,
                400f);
            _scrollRect = scroll.GetComponent<RectTransform>();
            PlaceFillBottom(_scrollRect, top: 214f, bottom: 18f, inset: 16f);

            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                scrollView.horizontal = false;
                scrollView.movementType = ScrollRect.MovementType.Clamped;
                _rowParent = scrollView.content;
                StretchContent(_rowParent as RectTransform);

                var layout = _rowParent.gameObject.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = _rowParent.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.spacing = 3f;
                layout.padding = new RectOffset(6, 20, 4, 8);

                var fitter = _rowParent.gameObject.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = _rowParent.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            _empty = MakeText(
                gui,
                Localization.instance.Localize("$gatewaychest_empty"),
                new Vector2(0f, -80f),
                16,
                Color.white,
                400f,
                28f);
            _empty.alignment = TextAnchor.MiddleCenter;

            _root.SetActive(false);
        }

        private static Text MakeText(
            GUIManager gui,
            string text,
            Vector2 pos,
            int size,
            Color color,
            float width,
            float height)
        {
            var mid = new Vector2(0.5f, 0.5f);
            var go = gui.CreateText(
                text,
                _root.transform,
                mid,
                mid,
                pos,
                gui.AveriaSerifBold,
                size,
                color,
                true,
                Color.black,
                width,
                height,
                false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, height);
            return go.GetComponent<Text>();
        }

        private static void PlaceTop(RectTransform rt, float yFromTop, float width, float height, bool right = false)
        {
            rt.anchorMin = new Vector2(right ? 1f : 0f, 1f);
            rt.anchorMax = new Vector2(right ? 1f : 0f, 1f);
            rt.pivot = new Vector2(right ? 1f : 0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(right ? -18f : 18f, -yFromTop);
        }

        private static void PlaceFillBottom(RectTransform rt, float top, float bottom, float inset)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, bottom);
            rt.offsetMax = new Vector2(-inset, -top);
        }

        private static void StretchContent(RectTransform content)
        {
            if (content == null)
            {
                return;
            }

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
        }

        private static void AddCategoryButtons(GUIManager gui)
        {
            _categoryButtons.Clear();
            var row1 = new[]
            {
                ItemCategory.All,
                ItemCategory.Weapons,
                ItemCategory.Armor,
                ItemCategory.Food,
            };
            var row2 = new[]
            {
                ItemCategory.Materials,
                ItemCategory.Trophies,
                ItemCategory.Misc,
            };

            PlaceCategoryRow(gui, row1, 118f);
            PlaceCategoryRow(gui, row2, 150f);
        }

        private static void PlaceCategoryRow(GUIManager gui, ItemCategory[] cats, float yFromTop)
        {
            var width = 118f;
            var gap = 6f;
            var total = cats.Length * width + (cats.Length - 1) * gap;
            var x = -total / 2f + width / 2f;
            foreach (var cat in cats)
            {
                var captured = cat;
                var go = gui.CreateButton(
                    Localization.instance.Localize("$" + ItemCategories.Token(cat)),
                    _root.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(x, -yFromTop),
                    width,
                    28f);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(x, -yFromTop);
                rt.sizeDelta = new Vector2(width, 28f);

                var button = go.GetComponent<Button>();
                gui.ApplyButtonStyle(button, 13);
                var label = go.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 10;
                    label.resizeTextMaxSize = 13;
                }

                button.onClick.AddListener(() =>
                {
                    _category = captured;
                    Refresh();
                });
                _categoryButtons.Add(button);
                x += width + gap;
            }
        }

        private static void AddSortButtons(GUIManager gui)
        {
            _sortButtons.Clear();
            var modes = new[]
            {
                (SortMode.Name, "$gatewaychest_sort_name"),
                (SortMode.Quantity, "$gatewaychest_sort_qty"),
                (SortMode.Category, "$gatewaychest_sort_cat"),
            };

            var width = 110f;
            var gap = 8f;
            var total = modes.Length * width + (modes.Length - 1) * gap;
            var x = -total / 2f + width / 2f;
            foreach (var pair in modes)
            {
                var captured = pair.Item1;
                var go = gui.CreateButton(
                    Localization.instance.Localize(pair.Item2),
                    _root.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    Vector2.zero,
                    width,
                    24f);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(x, -184f);
                rt.sizeDelta = new Vector2(width, 24f);

                var button = go.GetComponent<Button>();
                gui.ApplyButtonStyle(button, 13);
                button.onClick.AddListener(() =>
                {
                    _sort = captured;
                    Refresh();
                });
                _sortButtons.Add(button);
                x += width + gap;
            }
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

            TintFilters();

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

        private static void TintFilters()
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
            for (var i = 0; i < _categoryButtons.Count && i < cats.Length; i++)
            {
                Tint(_categoryButtons[i], cats[i] == _category);
            }

            var sorts = new[] { SortMode.Name, SortMode.Quantity, SortMode.Category };
            for (var i = 0; i < _sortButtons.Count && i < sorts.Length; i++)
            {
                Tint(_sortButtons[i], sorts[i] == _sort);
            }
        }

        private static void Tint(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var colors = button.colors;
            var highlight = new Color(1f, 0.78f, 0.35f, 1f);
            colors.normalColor = selected ? highlight : Color.white;
            colors.highlightedColor = selected ? highlight : new Color(1f, 0.9f, 0.7f, 1f);
            colors.selectedColor = highlight;
            button.colors = colors;
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

            return order != 0 ? order : a.Distance.CompareTo(b.Distance);
        }

        private static GameObject MakeRow(IndexedStack stack)
        {
            var gui = GUIManager.Instance;
            var row = gui.CreateButton(
                "",
                _rowParent,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                0f,
                RowHeight);
            gui.ApplyButtonStyle(row.GetComponent<Button>(), 16);

            var layout = row.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = row.AddComponent<LayoutElement>();
            }

            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1f;

            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            if (stack.Icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(row.transform, false);
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = stack.Icon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var iconRt = icon.rectTransform;
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.sizeDelta = new Vector2(IconSize, IconSize);
                iconRt.anchoredPosition = new Vector2(8f, 0f);
            }

            var nameGo = gui.CreateText(
                stack.DisplayName,
                row.transform,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                Vector2.zero,
                gui.AveriaSerifBold,
                16,
                gui.ValheimOrange,
                true,
                Color.black,
                0f,
                28f,
                false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(48f, 4f);
            nameRt.offsetMax = new Vector2(-72f, -4f);
            var name = nameGo.GetComponent<Text>();
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.raycastTarget = false;

            var qtyGo = gui.CreateText(
                "x" + stack.Quantity,
                row.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                Vector2.zero,
                gui.AveriaSerifBold,
                16,
                Color.white,
                true,
                Color.black,
                64f,
                28f,
                false);
            var qtyRt = qtyGo.GetComponent<RectTransform>();
            qtyRt.anchorMin = new Vector2(1f, 0f);
            qtyRt.anchorMax = new Vector2(1f, 1f);
            qtyRt.pivot = new Vector2(1f, 0.5f);
            qtyRt.sizeDelta = new Vector2(64f, 28f);
            qtyRt.anchoredPosition = new Vector2(-12f, 0f);
            var qty = qtyGo.GetComponent<Text>();
            qty.alignment = TextAnchor.MiddleRight;
            qty.raycastTarget = false;

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
