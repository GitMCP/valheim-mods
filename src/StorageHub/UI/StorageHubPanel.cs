using System;
using System.Collections.Generic;
using StorageHub.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StorageHub.UI
{
    /// <summary>
    /// Sits in the gap between the backpack and the crafting column and lists the
    /// nearby chests as full-width rows.
    /// </summary>
    internal static class StorageHubPanel
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
        private const float RowSpacing = 3f;
        private const float IconSize = 32f;

        private static GameObject _root;
        private static Text _title;
        private static Text _capacity;
        private static Text _empty;
        private static InputField _search;
        private static RectTransform _scrollRect;
        private static Transform _rowParent;
        private static readonly List<RowView> _rows = new List<RowView>();
        private static readonly List<Button> _categoryButtons = new List<Button>();
        private static readonly List<Button> _sortButtons = new List<Button>();
        private static ItemCategory _category = ItemCategory.All;
        private static SortMode _sort = SortMode.Name;
        private static string _query = "";
        private static float _nextRefresh;
        private static float _nextSnap;
        private static Container _pending;
        private static IndexedStack _splitGroup;
        private static bool _searchBlocked;
        private static readonly Vector3[] Corners = new Vector3[4];

        internal static void Open(Container hub)
        {
            if (GUIManager.IsHeadless() || hub == null)
            {
                return;
            }

            StorageHubMarker.OpenHub = hub;
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

            _nextSnap = 0f;
            SnapBetweenInventoryAndCrafting();
            _root.SetActive(true);
            _nextRefresh = 0f;
            Refresh();
        }

        internal static void Close()
        {
            StorageHubMarker.OpenHub = null;
            _pending = null;
            _splitGroup = null;
            UnfocusSearch();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        internal static void Tick()
        {
            if (StorageHubMarker.OpenHub == null || _root == null || !_root.activeSelf)
            {
                return;
            }

            if (!InventoryGui.IsVisible())
            {
                Close();
                return;
            }

            if (Time.time >= _nextSnap)
            {
                SnapBetweenInventoryAndCrafting();
            }

            SyncSearchFocus();
            if (Time.time >= _nextRefresh)
            {
                Refresh();
            }
        }

        internal static bool SearchHasFocus()
        {
            return _search != null && _search.isFocused && _root != null && _root.activeSelf;
        }

        internal static bool HandleSplitOk()
        {
            if (_splitGroup == null)
            {
                return false;
            }

            var gui = InventoryGui.instance;
            var amount = gui != null && gui.m_splitDialog != null
                ? Mathf.Max(1, (int)gui.m_splitDialog.SliderValue)
                : 1;
            var group = _splitGroup;
            _splitGroup = null;
            gui?.HideSplitDialog();
            StorageNetwork.Withdraw(Player.m_localPlayer, group, amount);
            Refresh();
            return true;
        }

        internal static void ClearSplit()
        {
            _splitGroup = null;
        }

        internal static bool TryDepositDrag()
        {
            if (!IsDragging() || !PointerOverPanel())
            {
                return false;
            }

            DepositDragged();
            return true;
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
        /// Stay on the inventory canvas and sit in the open gap between the backpack
        /// and the crafting column, vertically centered.
        /// </summary>
        private static void SnapBetweenInventoryAndCrafting()
        {
            var gui = InventoryGui.instance;
            if (gui == null || gui.m_player == null || _root == null)
            {
                return;
            }

            var ours = _root.GetComponent<RectTransform>();
            var parent = gui.m_player.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            if (ours.parent != parent)
            {
                ours.SetParent(parent, false);
                _root.transform.SetAsLastSibling();
            }

            ours.anchorMin = new Vector2(0.5f, 0.5f);
            ours.anchorMax = new Vector2(0.5f, 0.5f);
            ours.pivot = new Vector2(0.5f, 0.5f);
            if (ours.sizeDelta.x != PanelWidth || ours.sizeDelta.y != PanelHeight)
            {
                ours.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            }

            var center = parent.rect.center;
            var pos = Vector2.zero;
            if (gui.m_crafting != null)
            {
                var left = EdgeX(parent, gui.m_player, right: true);
                if (gui.m_info != null &&
                    parent.InverseTransformPoint(gui.m_info.position).x <
                    parent.InverseTransformPoint(gui.m_crafting.position).x)
                {
                    left = Mathf.Max(left, EdgeX(parent, gui.m_info, right: true));
                }

                var right = EdgeX(parent, gui.m_crafting, right: false);
                var midX = (left + right) * 0.5f;
                var half = PanelWidth * 0.5f;
                const float pad = 12f;
                if (midX + half > right - pad)
                {
                    midX = right - pad - half;
                }

                pos = new Vector2(midX - center.x, 0f);
            }

            if ((ours.anchoredPosition - pos).sqrMagnitude > 4f)
            {
                ours.anchoredPosition = pos;
            }

            _nextSnap = Time.time + 0.35f;
        }

        private static float EdgeX(RectTransform parent, RectTransform child, bool right)
        {
            child.GetWorldCorners(Corners);
            var a = parent.InverseTransformPoint(Corners[right ? 2 : 0]).x;
            var b = parent.InverseTransformPoint(Corners[right ? 3 : 1]).x;
            return right ? Mathf.Max(a, b) : Mathf.Min(a, b);
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
            _root.name = "StorageHubPanel";

            _title = MakeText(
                gui,
                Localization.instance.Localize("$storage_hub_name"),
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
                Localization.instance.Localize("$storagehub_search"),
                16,
                320f,
                30f).GetComponent<InputField>();
            _search.onValueChanged.AddListener(OnSearch);
            PlaceTop(_search.GetComponent<RectTransform>(), 80f, 320f, 30f);
            _search.interactable = true;
            _search.navigation = new Navigation { mode = Navigation.Mode.None };
            WireSearchFocus();
            WirePanelDrop();

            var depositGo = gui.CreateButton(
                Localization.instance.Localize("$storagehub_deposit"),
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
                scrollView.scrollSensitivity = RecipeMatchedScrollSensitivity();
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
                layout.spacing = RowSpacing;
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
                Localization.instance.Localize("$storagehub_empty"),
                new Vector2(0f, -80f),
                16,
                Color.white,
                400f,
                28f);
            _empty.alignment = TextAnchor.MiddleCenter;

            _root.SetActive(false);
        }

        /// <summary>
        /// Jötunn's CreateScrollView uses 35px per wheel notch. Crafting recipes sit
        /// 30px apart, so a notch moves a bit more than one recipe. Hub rows are
        /// taller, so scale the same notch to the same number of lines.
        /// </summary>
        private static float RecipeMatchedScrollSensitivity()
        {
            const float valheimStyle = 40f;
            var recipePitch = 30f;
            var recipeSensitivity = valheimStyle;

            var inv = InventoryGui.instance;
            if (inv != null)
            {
                if (inv.m_recipeListSpace > 1f)
                {
                    recipePitch = inv.m_recipeListSpace;
                }

                var recipeScroll = inv.m_recipeEnsureVisible != null
                    ? inv.m_recipeEnsureVisible.GetComponent<ScrollRect>()
                    : null;
                if (recipeScroll != null && recipeScroll.scrollSensitivity > 0f)
                {
                    recipeSensitivity = recipeScroll.scrollSensitivity;
                }
            }

            return recipeSensitivity * ((RowHeight + RowSpacing) / recipePitch);
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
                    if (IsDragging())
                    {
                        DepositDragged();
                        return;
                    }

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
            var modes = new[] { SortMode.Name, SortMode.Quantity, SortMode.Category };
            var tokens = new[]
            {
                "$storagehub_sort_name",
                "$storagehub_sort_qty",
                "$storagehub_sort_cat",
            };

            var width = 110f;
            var gap = 8f;
            var total = modes.Length * width + (modes.Length - 1) * gap;
            var x = -total / 2f + width / 2f;
            for (var i = 0; i < modes.Length; i++)
            {
                var captured = modes[i];
                var go = gui.CreateButton(
                    Localization.instance.Localize(tokens[i]),
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
                    if (IsDragging())
                    {
                        DepositDragged();
                        return;
                    }

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
            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            StorageNetwork.DepositAll(Player.m_localPlayer, StorageHubMarker.OpenHub);
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_dragGo != null)
            {
                gui.SetupDragItem(null, null, 1);
            }

            Refresh();
        }

        private static void Refresh()
        {
            _nextRefresh = Time.time + 0.6f;
            var hub = StorageHubMarker.OpenHub;
            if (hub == null || _rowParent == null)
            {
                return;
            }

            if (_title != null)
            {
                _title.text = Localization.instance.Localize("$storage_hub_name");
            }

            var snapshot = StorageNetwork.Snapshot(hub);
            if (_capacity != null)
            {
                _capacity.text =
                    Localization.instance.Localize("$storagehub_capacity")
                        .Replace("{0}", snapshot.UsedSlots.ToString())
                        .Replace("{1}", snapshot.TotalSlots.ToString())
                    + "   ·   " +
                    Localization.instance.Localize("$storagehub_chests")
                        .Replace("{0}", snapshot.Chests.Count.ToString());
            }

            TintFilters();

            var visible = Filter(StorageNetwork.ListItems(hub));
            if (_empty != null)
            {
                _empty.gameObject.SetActive(visible.Count == 0);
            }

            while (_rows.Count > visible.Count)
            {
                var extra = _rows[_rows.Count - 1];
                _rows.RemoveAt(_rows.Count - 1);
                if (extra.Go != null)
                {
                    UnityEngine.Object.Destroy(extra.Go);
                }
            }

            while (_rows.Count < visible.Count)
            {
                _rows.Add(MakeRow());
            }

            for (var i = 0; i < visible.Count; i++)
            {
                BindRow(_rows[i], visible[i]);
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

        private static RowView MakeRow()
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

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(IconSize, IconSize);
            iconRt.anchoredPosition = new Vector2(8f, 0f);

            var nameGo = gui.CreateText(
                "",
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
                "",
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

            var view = new RowView
            {
                Go = row,
                Icon = icon,
                Name = name,
                Qty = qty,
            };
            row.GetComponent<Button>().onClick.AddListener(() => OnRowClicked(view));
            return view;
        }

        private static void BindRow(RowView view, IndexedStack stack)
        {
            view.Stack = stack;
            if (view.Go != null && !view.Go.activeSelf)
            {
                view.Go.SetActive(true);
            }

            if (view.Icon != null)
            {
                view.Icon.sprite = stack.Icon;
                view.Icon.enabled = stack.Icon != null;
            }

            if (view.Name != null)
            {
                view.Name.text = stack.DisplayName;
            }

            if (view.Qty != null)
            {
                view.Qty.text = "x" + stack.Quantity;
            }
        }

        private static void OnRowClicked(RowView view)
        {
            if (view == null || view.Stack == null)
            {
                return;
            }

            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            if (IsShift() && view.Stack.Quantity > 1)
            {
                BeginSplitWithdraw(view.Stack);
                return;
            }

            StorageNetwork.Withdraw(Player.m_localPlayer, view.Stack, view.Stack.Quantity);
            Refresh();
        }

        private static void BeginSplitWithdraw(IndexedStack group)
        {
            var gui = InventoryGui.instance;
            var sample = group == null ? null : group.FirstLive();
            if (gui == null || sample == null)
            {
                return;
            }

            _splitGroup = group;
            var sourceInv = group.Parts.Count > 0 && group.Parts[0].Source != null
                ? group.Parts[0].Source.GetInventory()
                : null;
            gui.ShowSplitDialog(sample, sourceInv);
            gui.m_splitDialog.UpdateLimits(group.Quantity, false);
        }

        private static bool IsShift()
        {
            return ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);
        }

        private static bool IsDragging()
        {
            var gui = InventoryGui.instance;
            return gui != null && gui.m_dragGo != null && gui.m_dragItem != null;
        }

        private static void DepositDragged()
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            var hub = StorageHubMarker.OpenHub;
            if (gui == null || player == null || hub == null || gui.m_dragItem == null || gui.m_dragInventory == null)
            {
                return;
            }

            var from = gui.m_dragInventory;
            var item = gui.m_dragItem;
            if (from != player.GetInventory() || !from.ContainsItem(item))
            {
                gui.SetupDragItem(null, null, 1);
                return;
            }

            StorageNetwork.RouteAmount(from, item, gui.m_dragAmount, hub, allowHub: true);
            gui.SetupDragItem(null, null, 1);
            Refresh();
        }

        private static bool PointerOverPanel()
        {
            if (_root == null)
            {
                return false;
            }

            var rt = _root.GetComponent<RectTransform>();
            var cam = null as Camera;
            var canvas = _root.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam);
        }

        private static void WirePanelDrop()
        {
            var graphic = _root.GetComponent<Image>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            var trigger = _root.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _root.AddComponent<EventTrigger>();
            }

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(new UnityAction<BaseEventData>(OnBackgroundClicked));
            trigger.triggers.Add(entry);
        }

        private static void OnBackgroundClicked(BaseEventData _)
        {
            if (IsDragging())
            {
                DepositDragged();
            }
        }

        private static void WireSearchFocus()
        {
            if (_search == null)
            {
                return;
            }

            var image = _search.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            _search.onEndEdit.AddListener(OnSearchEndEdit);

            var trigger = _search.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _search.gameObject.AddComponent<EventTrigger>();
            }

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(new UnityAction<BaseEventData>(OnSearchClicked));
            trigger.triggers.Add(entry);
        }

        private static void OnSearchClicked(BaseEventData _)
        {
            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            FocusSearch();
        }

        private static void OnSearchEndEdit(string _)
        {
            SetSearchBlock(false);
        }

        private static void SyncSearchFocus()
        {
            if (_search == null)
            {
                SetSearchBlock(false);
                return;
            }

            if (ZInput.GetMouseButtonDown(0) && PointerOverSearch())
            {
                if (IsDragging())
                {
                    DepositDragged();
                    return;
                }

                FocusSearch();
            }

            SetSearchBlock(SearchHasFocus());
        }

        private static bool PointerOverSearch()
        {
            if (_search == null)
            {
                return false;
            }

            var cam = null as Camera;
            var canvas = _search.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                _search.GetComponent<RectTransform>(),
                Input.mousePosition,
                cam);
        }

        private static void FocusSearch()
        {
            if (_search == null)
            {
                return;
            }

            _search.ActivateInputField();
            _search.Select();
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_search.gameObject);
            }

            SetSearchBlock(true);
        }

        internal static void UnfocusSearch()
        {
            if (_search != null && _search.isFocused)
            {
                _search.DeactivateInputField();
            }

            if (EventSystem.current != null &&
                _search != null &&
                EventSystem.current.currentSelectedGameObject == _search.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            SetSearchBlock(false);
        }

        private static void SetSearchBlock(bool on)
        {
            if (on == _searchBlocked)
            {
                return;
            }

            GUIManager.BlockInput(on);
            _searchBlocked = on;
        }

        private sealed class RowView
        {
            internal GameObject Go;
            internal Image Icon;
            internal Text Name;
            internal Text Qty;
            internal IndexedStack Stack;
        }
    }
}
