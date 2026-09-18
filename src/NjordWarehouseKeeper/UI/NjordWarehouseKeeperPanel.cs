using System;
using System.Collections.Generic;
using NjordWarehouseKeeper.Client;
using NjordWarehouseKeeper.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Sits in the gap between the backpack and the crafting column and lists the
    /// nearby chests as full-width rows. Recipes live in a separate panel that
    /// replaces vanilla crafting on the right.
    /// </summary>
    internal static class NjordWarehouseKeeperPanel
    {
        private enum SortMode
        {
            Name,
            Quantity,
            Category,
        }

        private const float PanelWidth = 520f;
        private const float PanelHeight = 640f;
        private const float RowHeight = 52f;
        private const float RowSpacing = 3f;
        private const float IconSize = 40f;
        private const float ActionY = 82f;
        private const float ActionHeight = 38f;
        private const float SearchY = 128f;
        private const float CatRow1Y = 166f;
        private const float CatRow2Y = 198f;
        private const float SortY = 232f;
        private const float ListTop = 264f;
        private const float ListBottom = 28f;

        /// <summary>
        /// Top inset for the preferences overlay, just below search.
        /// </summary>
        internal const float ContentTop = 164f;

        private static GameObject _root;
        private static Text _title;
        private static Text _capacity;
        private static Text _empty;
        private static InputField _search;
        private static RectTransform _scrollRect;
        private static Transform _rowParent;
        private static GameObject _depositGo;
        private static GameObject _resupplyGo;
        private static GameObject _quickStackGo;
        private static Button _cogButton;
        private static Button _favFilterButton;
        private static readonly List<RowView> _rows = new List<RowView>();
        private static readonly List<Button> _categoryButtons = new List<Button>();
        private static readonly List<Button> _sortButtons = new List<Button>();
        private static readonly List<GameObject> _itemTabUi = new List<GameObject>();
        private static readonly List<GameObject> _chromeUi = new List<GameObject>();
        private static ItemCategory _category = ItemCategory.All;
        private static SortMode _sort = SortMode.Name;
        private static string _query = "";
        private static bool _favouritesOnly;
        private static bool _prefsOpen;
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

            NjordWarehouseKeeperMarker.OpenHub = hub;
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

            SetPrefsOpen(false);
            _nextSnap = 0f;
            SnapBetweenInventoryAndCrafting();
            _root.SetActive(true);
            _nextRefresh = 0f;
            Refresh();
            NjordWarehouseKeeperRecipes.Open();
        }

        internal static void Close()
        {
            NjordWarehouseKeeperMarker.OpenHub = null;
            _pending = null;
            _splitGroup = null;
            UnfocusSearch();
            HubItemHover.Hide();
            SetPrefsOpen(false);
            NjordWarehouseKeeperRecipes.Close();
            Patches.InventoryGuiPatch.HubClosed();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        internal static void Tick()
        {
            if (NjordWarehouseKeeperMarker.OpenHub == null || _root == null || !_root.activeSelf)
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

            NjordWarehouseKeeperRecipes.Tick();
            SyncSearchFocus();
            if (_prefsOpen)
            {
                return;
            }

            if (Time.time >= _nextRefresh)
            {
                Refresh();
            }
        }

        internal static string SearchQuery
        {
            get { return _query ?? ""; }
        }

        internal static bool SearchHasFocus()
        {
            if (_root == null || !_root.activeSelf)
            {
                return false;
            }

            if (_search != null && _search.isFocused)
            {
                return true;
            }

            if (NjordWarehouseKeeperRecipes.SearchHasFocus())
            {
                return true;
            }

            return NjordWarehouseKeeperPrefs.InputHasFocus();
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
            var rightPanel = NjordWarehouseKeeperRecipes.IsOpen
                ? NjordWarehouseKeeperRecipes.RootRect
                : gui.m_crafting;
            if (rightPanel == null)
            {
                rightPanel = gui.m_crafting;
            }

            if (rightPanel != null)
            {
                var left = EdgeX(parent, gui.m_player, right: true);
                if (gui.m_info != null &&
                    parent.InverseTransformPoint(gui.m_info.position).x <
                    parent.InverseTransformPoint(rightPanel.position).x)
                {
                    left = Mathf.Max(left, EdgeX(parent, gui.m_info, right: true));
                }

                var right = EdgeX(parent, rightPanel, right: false);
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

            HubSprites.Load();
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
            _root.name = "NjordWarehouseKeeperPanel";
            if (_root.GetComponent<RectMask2D>() == null)
            {
                _root.AddComponent<RectMask2D>();
            }

            _title = MakeText(
                gui,
                Localization.instance.Localize("$njord_npc"),
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

            _cogButton = MakeIconButton(
                gui,
                HubSprites.Cog,
                28f,
                () =>
                {
                    if (IsDragging())
                    {
                        DepositDragged();
                        return;
                    }

                    SetPrefsOpen(!_prefsOpen);
                });
            PlaceTopRight(_cogButton.GetComponent<RectTransform>(), 18f, 28f, 28f, 16f);

            _search = gui.CreateInputField(
                _root.transform,
                mid,
                mid,
                Vector2.zero,
                InputField.ContentType.Standard,
                Localization.instance.Localize("$njord_search"),
                16,
                176f,
                30f).GetComponent<InputField>();
            _search.onValueChanged.AddListener(OnSearch);
            PlaceTop(_search.GetComponent<RectTransform>(), SearchY, 484f, 30f);
            _search.interactable = true;
            _search.navigation = new Navigation { mode = Navigation.Mode.None };
            WireSearchFocus();
            WirePanelDrop();

            _quickStackGo = MakeActionButton(
                gui,
                "$njord_quickstack",
                HubSprites.QuickStack,
                OnQuickStack);
            _depositGo = MakeActionButton(
                gui,
                "$njord_deposit",
                HubSprites.Deposit,
                OnDeposit);
            _resupplyGo = MakeActionButton(
                gui,
                "$njord_resupply",
                HubSprites.Resupply,
                OnResupply);
            PlaceActionButtons();
            _chromeUi.Add(_quickStackGo);
            _chromeUi.Add(_depositGo);
            _chromeUi.Add(_resupplyGo);

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
            PlaceFillBottom(_scrollRect, top: ListTop, bottom: ListBottom, inset: 16f);

            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                FitScrollView(scrollView, ListScrollSensitivity());
                scrollView.onValueChanged.AddListener(_ => HubItemHover.Hide());
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
                Localization.instance.Localize("$njord_empty"),
                new Vector2(0f, -80f),
                16,
                Color.white,
                400f,
                28f);
            _empty.alignment = TextAnchor.MiddleCenter;
            _itemTabUi.Add(scroll);
            if (_empty != null)
            {
                _itemTabUi.Add(_empty.gameObject);
            }

            NjordWarehouseKeeperPrefs.Build(_root.transform, gui);
            NjordWarehouseKeeperRecipes.EnsureBuilt();
            _root.SetActive(false);
        }

        /// <summary>
        /// Same wheel-notch distance, in rows, as the vanilla crafting recipe list.
        /// </summary>
        internal static float ListScrollSensitivity()
        {
            return RecipeMatchedScrollSensitivity();
        }

        internal static void FitScrollView(ScrollRect scrollView, float sensitivity)
        {
            if (scrollView == null)
            {
                return;
            }

            scrollView.horizontal = false;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.scrollSensitivity = sensitivity;

            var viewport = scrollView.viewport;
            if (viewport == null)
            {
                var found = scrollView.transform.Find("Viewport");
                viewport = found as RectTransform;
                scrollView.viewport = viewport;
            }

            if (viewport != null)
            {
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f, 0.5f);
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = new Vector2(-16f, 0f);
                if (viewport.GetComponent<RectMask2D>() == null)
                {
                    viewport.gameObject.AddComponent<RectMask2D>();
                }

                var mask = viewport.GetComponent<Mask>();
                if (mask != null)
                {
                    mask.enabled = false;
                }

                var image = viewport.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                }
            }

            var bar = scrollView.verticalScrollbar;
            if (bar != null)
            {
                var barRt = bar.transform as RectTransform;
                if (barRt != null)
                {
                    barRt.anchorMin = new Vector2(1f, 0f);
                    barRt.anchorMax = new Vector2(1f, 1f);
                    barRt.pivot = new Vector2(1f, 0.5f);
                    barRt.sizeDelta = new Vector2(10f, 0f);
                    barRt.anchoredPosition = new Vector2(-4f, 0f);
                }
            }
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

        private static void PlaceTopRight(RectTransform rt, float yFromTop, float width, float height, float fromRight)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(-fromRight, -yFromTop);
        }

        private static Button MakeIconButton(GUIManager gui, Sprite sprite, float size, UnityAction onClick)
        {
            var go = gui.CreateButton(
                "",
                _root.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                size,
                size);
            gui.ApplyButtonStyle(go.GetComponent<Button>(), 12);
            var label = go.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "";
                label.enabled = false;
            }

            var image = go.GetComponent<Image>();
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = new Color(1f, 0.9f, 0.7f, 1f);
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(size - 10f, size - 10f);
            iconRt.anchoredPosition = Vector2.zero;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            return button;
        }

        private static void SetPrefsOpen(bool on)
        {
            _prefsOpen = on;
            NjordWarehouseKeeperPrefs.SetOpen(on);
            ApplyChrome();
        }

        private static void ApplyChrome()
        {
            var prefs = _prefsOpen;
            SetActiveAll(_chromeUi, !prefs);
            SetActiveAll(_itemTabUi, !prefs);

            if (_search != null)
            {
                _search.gameObject.SetActive(true);
            }

            if (_title != null)
            {
                var token = prefs ? "$njord_preferences" : "$njord_npc";
                _title.text = Localization.instance.Localize(token);
            }

            Tint(_cogButton, prefs);
            HubItemHover.Hide();

            if (!prefs)
            {
                Refresh();
            }
        }

        private static void SetActiveAll(List<GameObject> list, bool on)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    list[i].SetActive(on);
                }
            }
        }

        private static GameObject MakeActionButton(GUIManager gui, string token, Sprite icon, UnityAction onClick)
        {
            const float width = 154f;
            var go = gui.CreateButton(
                Localization.instance.Localize(token),
                _root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                width,
                ActionHeight);
            var button = go.GetComponent<Button>();
            gui.ApplyButtonStyle(button, 15);
            button.onClick.AddListener(onClick);

            var label = go.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 11;
                label.resizeTextMaxSize = 15;
                var labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(28f, 2f);
                labelRt.offsetMax = new Vector2(-8f, -2f);
            }

            if (go.GetComponent<RectMask2D>() == null)
            {
                go.AddComponent<RectMask2D>();
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var image = iconGo.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.maskable = true;
            image.enabled = icon != null;
            image.color = Color.white;
            var iconRt = image.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(18f, 18f);
            iconRt.anchoredPosition = new Vector2(14f, 0f);
            return go;
        }

        private static void PlaceActionButtons()
        {
            const float width = 154f;
            const float gap = 8f;
            var total = 3f * width + 2f * gap;
            var x = -total / 2f + width / 2f;
            PlaceCentered(_quickStackGo.GetComponent<RectTransform>(), x, ActionY, width, ActionHeight);
            PlaceCentered(_depositGo.GetComponent<RectTransform>(), x + width + gap, ActionY, width, ActionHeight);
            PlaceCentered(_resupplyGo.GetComponent<RectTransform>(), x + 2f * (width + gap), ActionY, width, ActionHeight);
        }

        private static void PlaceCentered(RectTransform rt, float x, float yFromTop, float width, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
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
                ItemCategory.Favourites,
            };

            PlaceCategoryRow(gui, row1, CatRow1Y);
            PlaceCategoryRow(gui, row2, CatRow2Y);
        }

        private static void PlaceCategoryRow(GUIManager gui, ItemCategory[] cats, float yFromTop)
        {
            var width = 108f;
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
                _itemTabUi.Add(go);
                x += width + gap;
            }
        }

        private static void AddSortButtons(GUIManager gui)
        {
            _sortButtons.Clear();
            var modes = new[] { SortMode.Name, SortMode.Quantity, SortMode.Category };
            var tokens = new[]
            {
                "$njord_sort_name",
                "$njord_sort_qty",
                "$njord_sort_cat",
            };

            var width = 100f;
            var gap = 8f;
            const float starSize = 28f;
            var total = modes.Length * width + modes.Length * gap + starSize;
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
                rt.anchoredPosition = new Vector2(x, -SortY);
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
                _itemTabUi.Add(go);
                x += width + gap;
            }

            _favFilterButton = MakeIconButton(
                gui,
                HubSprites.StarEmpty,
                28f,
                OnFavouritesFilter);
            var favRt = _favFilterButton.GetComponent<RectTransform>();
            favRt.anchorMin = new Vector2(0.5f, 1f);
            favRt.anchorMax = new Vector2(0.5f, 1f);
            favRt.pivot = new Vector2(0.5f, 1f);
            favRt.anchoredPosition = new Vector2(x - width * 0.5f + starSize * 0.5f, -SortY);
            favRt.sizeDelta = new Vector2(starSize, starSize);
            _itemTabUi.Add(_favFilterButton.gameObject);
        }

        private static void OnFavouritesFilter()
        {
            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            _favouritesOnly = !_favouritesOnly;
            Refresh();
        }

        private static void OnSearch(string value)
        {
            _query = value ?? "";
            if (_prefsOpen)
            {
                NjordWarehouseKeeperPrefs.Refresh();
            }
            else
            {
                Refresh();
            }
        }

        private static void OnDeposit()
        {
            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            StorageNetwork.DepositAll(Player.m_localPlayer, NjordWarehouseKeeperMarker.OpenHub);
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_dragGo != null)
            {
                gui.SetupDragItem(null, null, 1);
            }

            Refresh();
        }

        private static void OnResupply()
        {
            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            StorageNetwork.Resupply(Player.m_localPlayer, NjordWarehouseKeeperMarker.OpenHub);
            Refresh();
        }

        private static void OnQuickStack()
        {
            if (IsDragging())
            {
                DepositDragged();
                return;
            }

            StorageNetwork.Restock(Player.m_localPlayer, NjordWarehouseKeeperMarker.OpenHub);
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_dragGo != null)
            {
                gui.SetupDragItem(null, null, 1);
            }

            Refresh();
        }

        internal static void RefreshAfterRemote()
        {
            if (_root != null && _root.activeSelf)
            {
                Refresh();
            }

            if (NjordWarehouseKeeperRecipes.IsOpen)
            {
                NjordWarehouseKeeperRecipes.Refresh();
            }
        }

        private static void Refresh()
        {
            _nextRefresh = Time.time + 0.6f;
            var hub = NjordWarehouseKeeperMarker.OpenHub;
            if (hub == null || _rowParent == null)
            {
                return;
            }

            if (_title != null && !_prefsOpen)
            {
                _title.text = Localization.instance.Localize("$njord_npc");
            }

            var snapshot = StorageNetwork.Snapshot(hub);
            if (_capacity != null)
            {
                _capacity.text =
                    Localization.instance.Localize("$njord_capacity")
                        .Replace("{0}", snapshot.UsedSlots.ToString())
                        .Replace("{1}", snapshot.TotalSlots.ToString())
                    + "   ·   " +
                    Localization.instance.Localize("$njord_chests")
                        .Replace("{0}", snapshot.Chests.Count.ToString());
            }

            TintFilters();
            if (_prefsOpen)
            {
                return;
            }

            var visible = Filter(StorageNetwork.ListItems(hub));
            if (_empty != null)
            {
                _empty.text = Localization.instance.Localize(
                    _category == ItemCategory.Favourites || _favouritesOnly
                        ? "$njord_empty_favourites"
                        : "$njord_empty");
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
                ItemCategory.Favourites,
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

            if (_favFilterButton != null)
            {
                Tint(_favFilterButton, _favouritesOnly);
                var icon = _favFilterButton.transform.Find("Icon");
                var image = icon != null ? icon.GetComponent<Image>() : null;
                if (image != null)
                {
                    image.sprite = _favouritesOnly ? HubSprites.StarFilled : HubSprites.StarEmpty;
                    image.color = _favouritesOnly
                        ? new Color(1f, 0.82f, 0.28f, 1f)
                        : new Color(1f, 0.9f, 0.7f, 1f);
                }
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
                var wantFavourite = _category == ItemCategory.Favourites || _favouritesOnly;
                if (wantFavourite && !ClientPreferences.IsFavourite(item.Key()))
                {
                    continue;
                }

                if (_category != ItemCategory.All &&
                    _category != ItemCategory.Favourites &&
                    item.Category != _category)
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
            nameRt.offsetMin = new Vector2(56f, 4f);
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

            var starGo = new GameObject(
                "Star",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            starGo.transform.SetParent(row.transform, false);
            var star = starGo.GetComponent<Image>();
            star.preserveAspect = true;
            star.sprite = HubSprites.StarEmpty;
            star.color = new Color(1f, 1f, 1f, 0.85f);
            var starRt = star.rectTransform;
            starRt.anchorMin = new Vector2(0f, 0.5f);
            starRt.anchorMax = new Vector2(0f, 0.5f);
            starRt.pivot = new Vector2(0.5f, 0.5f);
            starRt.sizeDelta = new Vector2(18f, 18f);
            starRt.anchoredPosition = new Vector2(8f + IconSize - 2f, IconSize * 0.5f - 6f);
            var starBtn = starGo.GetComponent<Button>();
            starBtn.targetGraphic = star;
            starBtn.transition = Selectable.Transition.None;
            starBtn.navigation = new Navigation { mode = Navigation.Mode.None };

            var view = new RowView
            {
                Go = row,
                Icon = icon,
                Name = name,
                Qty = qty,
                Star = star,
                Hover = row.AddComponent<HubItemHover>(),
            };
            row.GetComponent<Button>().onClick.AddListener(() => OnRowClicked(view));
            starBtn.onClick.AddListener(() => OnStarClicked(view));
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

            if (view.Hover != null)
            {
                view.Hover.Bind(stack.FirstLive());
            }

            var favourite = ClientPreferences.IsFavourite(stack.Key());
            if (view.Star != null)
            {
                view.Star.sprite = favourite ? HubSprites.StarFilled : HubSprites.StarEmpty;
                view.Star.color = favourite
                    ? new Color(1f, 0.82f, 0.28f, 1f)
                    : new Color(1f, 1f, 1f, 0.8f);
            }
        }

        private static void OnStarClicked(RowView view)
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

            var key = view.Stack.Key();
            ClientPreferences.SetFavourite(key, !ClientPreferences.IsFavourite(key));
            Refresh();
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

            if (IsCtrl())
            {
                StorageNetwork.Withdraw(Player.m_localPlayer, view.Stack, view.Stack.Quantity);
                Refresh();
                return;
            }

            BeginDragStack(view.Stack);
        }

        private static void BeginDragStack(IndexedStack group)
        {
            var gui = InventoryGui.instance;
            var part = StorageNetwork.PrepareDragStack(group);
            var item = part == null ? null : part.Live();
            var inventory = part == null || part.Source == null ? null : part.Source.GetInventory();
            if (gui == null || item == null || inventory == null)
            {
                return;
            }

            StorageNetwork.EnsureOwner(part.Source);
            gui.SetupDragItem(item, inventory, item.m_stack);
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
            return ZInput.GetKey(KeyCode.LeftShift)
                || ZInput.GetKey(KeyCode.RightShift)
                || ZInput.GetButton("JoyLTrigger");
        }

        private static bool IsCtrl()
        {
            return ZInput.GetKey(KeyCode.LeftControl)
                || ZInput.GetKey(KeyCode.RightControl)
                || ZInput.GetButton("JoyLBumper");
        }

        internal static bool IsDragging()
        {
            var gui = InventoryGui.instance;
            return gui != null && gui.m_dragGo != null && gui.m_dragItem != null;
        }

        internal static void DepositDragged()
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            var hub = NjordWarehouseKeeperMarker.OpenHub;
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

            StorageNetwork.RouteAmount(from, item, gui.m_dragAmount, hub, allowHub: false);
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
            NjordWarehouseKeeperRecipes.UnfocusSearch();
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
            internal Image Star;
            internal HubItemHover Hover;
            internal IndexedStack Stack;
        }
    }
}
