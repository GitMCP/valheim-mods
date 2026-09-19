using System;
using System.Collections.Generic;
using System.Text;
using NjordWarehouseKeeper.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Craft-column panel opened while talking to Njord. Vanilla crafting is hidden
    /// only for that talk and this wood panel sits in its place: a station dropdown,
    /// the same Craft-tab recipes that bench would list, item detail, and Withdraw.
    /// Greyed recipes still pull whatever of those ingredients is in nearby chests.
    /// </summary>
    internal static class NjordWarehouseKeeperRecipes
    {
        private const float PanelWidth = 520f;
        private const float PanelHeight = 640f;
        private const float RowHeight = 32f;
        private const float HeaderHeight = 58f;
        private const float DropdownHeight = 32f;
        private const float DropdownTop = HeaderHeight + 4f;
        private const float SearchHeight = 30f;
        private const float SearchTop = DropdownTop + DropdownHeight + 6f;
        private const float BodyTop = SearchTop + SearchHeight + 8f;
        private const float ListWidth = 210f;
        private const float WithdrawHeight = 52f;
        private const float IngredientSize = 64f;
        private const int IngredientSlots = 4;
        private const string AllKey = "all";
        private const string HandKey = "hand";
        private const string HammerKey = "hammer";

        private static GameObject _root;
        private static Image _stationIcon;
        private static Text _title;
        private static Transform _rowParent;
        private static Button _stationButton;
        private static Text _stationLabel;
        private static GameObject _stationMenu;
        private static GameObject _stationCatcher;
        private static Transform _stationMenuParent;
        private static Image _detailIcon;
        private static Text _detailName;
        private static Text _detailBody;
        private static Button _withdraw;
        private static Text _withdrawLabel;
        private static Text _empty;
        private static InputField _search;
        private static readonly List<IngredientSlot> _ingredients = new List<IngredientSlot>();
        private static string _station = AllKey;
        private static string _query = "";
        private static readonly List<RecipeRow> _rows = new List<RecipeRow>();
        private static readonly List<StationOpt> _stations = new List<StationOpt>();
        private static string _selectedKey;
        private static CraftOffer _selected;
        private static float _nextRefresh;
        private static float _nextSnap;
        private static Container _pending;

        internal static bool IsOpen
        {
            get { return _root != null && _root.activeSelf; }
        }

        internal static RectTransform RootRect
        {
            get { return _root != null ? _root.GetComponent<RectTransform>() : null; }
        }

        internal static bool SearchHasFocus()
        {
            return IsOpen && _search != null && _search.isFocused;
        }

        internal static void Open()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
            {
                _pending = NjordWarehouseKeeperMarker.OpenHub;
                GUIManager.OnCustomGUIAvailable -= BuildPending;
                GUIManager.OnCustomGUIAvailable += BuildPending;
                return;
            }

            EnsureBuilt();
            if (_root == null)
            {
                return;
            }

            HideVanillaCrafting();
            ResetSearch();
            _nextSnap = 0f;
            SnapToCrafting();
            _root.SetActive(true);
            _nextRefresh = 0f;
            Refresh();
        }

        internal static void SetOpen(bool on)
        {
            if (!on)
            {
                Close();
                return;
            }

            Open();
        }

        internal static void Close()
        {
            _pending = null;
            _selected = null;
            _selectedKey = null;
            SetMenuOpen(false);
            UnfocusSearch();
            ResetSearch();
            HubItemHover.Hide();
            if (_root != null)
            {
                _root.SetActive(false);
            }

            RestoreVanillaCrafting();
        }

        internal static void Tick()
        {
            if (!IsOpen)
            {
                return;
            }

            HideVanillaCrafting();
            if (Time.time >= _nextSnap)
            {
                SnapToCrafting();
            }

            if (Time.time >= _nextRefresh)
            {
                Refresh();
            }
        }

        internal static void EnsureBuilt()
        {
            if (_root != null)
            {
                return;
            }

            var gui = GUIManager.Instance;
            if (gui == null || GUIManager.CustomGUIFront == null)
            {
                return;
            }

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
            _root.name = "NjordWarehouseKeeperRecipes";
            if (_root.GetComponent<RectMask2D>() == null)
            {
                _root.AddComponent<RectMask2D>();
            }

            var iconGo = new GameObject("StationIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(_root.transform, false);
            _stationIcon = iconGo.GetComponent<Image>();
            _stationIcon.preserveAspect = true;
            _stationIcon.raycastTarget = false;
            var iconRt = _stationIcon.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 1f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot = new Vector2(0f, 1f);
            iconRt.sizeDelta = new Vector2(40f, 40f);
            iconRt.anchoredPosition = new Vector2(18f, -12f);

            _title = MakeText(
                gui,
                _root.transform,
                Localization.instance.Localize("$njord_recipes"),
                22,
                gui.ValheimOrange,
                TextAnchor.MiddleCenter);
            Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 0f, -14f, 360f, 32f);

            _stationButton = MakeStationButton(gui);
            MakeSearch(gui);
            BuildStationMenu(gui);

            var listRt = MakeFill(
                "RecipeList",
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(16f, 16f),
                new Vector2(16f + ListWidth, -BodyTop));
            BuildRecipeList(gui, listRt);

            var divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(_root.transform, false);
            var divImage = divider.GetComponent<Image>();
            divImage.color = new Color(0.22f, 0.16f, 0.1f, 0.9f);
            divImage.raycastTarget = false;
            Place(
                divider.GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0.5f, 0.5f),
                16f + ListWidth + 4f,
                0f,
                2f,
                0f);
            var divRt = divider.GetComponent<RectTransform>();
            divRt.offsetMin = new Vector2(16f + ListWidth + 3f, 16f);
            divRt.offsetMax = new Vector2(16f + ListWidth + 5f, -BodyTop);
            divRt.sizeDelta = Vector2.zero;

            BuildDetail(gui);
            _root.SetActive(false);
        }

        private static void BuildPending()
        {
            GUIManager.OnCustomGUIAvailable -= BuildPending;
            if (_pending != null || NjordWarehouseKeeperMarker.OpenHub != null)
            {
                Open();
                _pending = null;
            }
        }

        internal static void HideVanillaCrafting()
        {
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_crafting != null)
            {
                gui.m_crafting.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Vanilla only enables <c>m_crafting</c> in Awake. After we hide it for
        /// Njord it stays off until something turns it back on, so Tab, handcraft,
        /// and every bench go blank.
        /// </summary>
        internal static void RestoreVanillaCrafting()
        {
            var gui = InventoryGui.instance;
            if (gui != null && gui.m_crafting != null)
            {
                gui.m_crafting.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Occupy the same slot as the vanilla crafting column.
        /// </summary>
        private static void SnapToCrafting()
        {
            var gui = InventoryGui.instance;
            if (gui == null || gui.m_crafting == null || _root == null)
            {
                _nextSnap = Time.time + 0.35f;
                return;
            }

            var craft = gui.m_crafting;
            var ours = _root.GetComponent<RectTransform>();
            var parent = craft.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            if (ours.parent != parent)
            {
                ours.SetParent(parent, false);
            }

            ours.SetAsLastSibling();
            ours.anchorMin = craft.anchorMin;
            ours.anchorMax = craft.anchorMax;
            ours.pivot = craft.pivot;
            ours.offsetMin = craft.offsetMin;
            ours.offsetMax = craft.offsetMax;
            ours.localScale = craft.localScale;
            ours.localRotation = craft.localRotation;
            _nextSnap = Time.time + 0.35f;
        }

        internal static void Refresh()
        {
            if (!IsOpen || _rowParent == null)
            {
                return;
            }

            _nextRefresh = Time.time + 0.6f;
            RebuildStations();
            PaintStationChrome();

            var listed = NjordWarehouseKeeperMarker.OpenHub != null
                ? StorageNetwork.ListItems(NjordWarehouseKeeperMarker.OpenHub)
                : new List<IndexedStack>();
            var visible = KnownCrafts(listed);
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

            var selectedIndex = -1;
            for (var i = 0; i < visible.Count; i++)
            {
                BindRow(_rows[i], visible[i], false);
                if (_selectedKey != null && OfferKey(visible[i]) == _selectedKey)
                {
                    selectedIndex = i;
                }
            }

            if (visible.Count == 0)
            {
                _selected = null;
                _selectedKey = null;
            }
            else if (selectedIndex < 0)
            {
                selectedIndex = 0;
                _selected = visible[0];
                _selectedKey = OfferKey(_selected);
            }
            else
            {
                _selected = visible[selectedIndex];
            }

            for (var i = 0; i < visible.Count; i++)
            {
                PaintRow(_rows[i], i == selectedIndex);
            }

            if (_empty != null)
            {
                var none = visible.Count == 0;
                _empty.gameObject.SetActive(none);
                if (none)
                {
                    _empty.text = string.IsNullOrEmpty((_query ?? "").Trim())
                        ? Localization.instance.Localize("$njord_recipes_empty")
                        : Localization.instance.Localize("$njord_recipes_none");
                }
            }

            BindDetail(_selected, listed);
        }

        private static void BuildRecipeList(GUIManager gui, RectTransform host)
        {
            var scroll = gui.CreateScrollView(
                host,
                false,
                true,
                10f,
                3f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0f, 0f, 0f, 0.35f),
                ListWidth,
                400f);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                NjordWarehouseKeeperPanel.FitScrollView(scrollView, RecipeScrollSensitivity());
                scrollView.onValueChanged.AddListener(_ => HubItemHover.Hide());
                _rowParent = scrollView.content;
                var content = _rowParent as RectTransform;
                if (content != null)
                {
                    content.anchorMin = new Vector2(0f, 1f);
                    content.anchorMax = new Vector2(1f, 1f);
                    content.pivot = new Vector2(0.5f, 1f);
                    content.anchoredPosition = Vector2.zero;
                    content.sizeDelta = Vector2.zero;
                }

                var layout = _rowParent.gameObject.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = _rowParent.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.spacing = 1f;
                layout.padding = new RectOffset(2, 14, 2, 4);

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
                host,
                Localization.instance.Localize("$njord_recipes_empty"),
                14,
                Color.white,
                TextAnchor.MiddleCenter);
            Place(_empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, 180f, 48f);
            _empty.gameObject.SetActive(false);
        }

        private static void BuildDetail(GUIManager gui)
        {
            var detail = MakeFill(
                "Detail",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(16f + ListWidth + 14f, 16f),
                new Vector2(-16f, -BodyTop));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(detail, false);
            _detailIcon = iconGo.GetComponent<Image>();
            _detailIcon.preserveAspect = true;
            _detailIcon.raycastTarget = false;
            Place(_detailIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), 0f, 0f, 40f, 40f);

            _detailName = MakeText(gui, detail, "", 20, gui.ValheimOrange, TextAnchor.MiddleLeft);
            Place(_detailName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), 48f, 0f, 0f, 40f);
            _detailName.rectTransform.offsetMin = new Vector2(48f, -40f);
            _detailName.rectTransform.offsetMax = new Vector2(0f, 0f);
            _detailName.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailName.resizeTextForBestFit = true;
            _detailName.resizeTextMinSize = 13;
            _detailName.resizeTextMaxSize = 20;

            var bodyHost = MakeFill(
                "Body",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 16f + WithdrawHeight + 12f + IngredientSize + 18f),
                new Vector2(0f, -48f));
            bodyHost.SetParent(detail, false);
            if (bodyHost.GetComponent<RectMask2D>() == null)
            {
                bodyHost.gameObject.AddComponent<RectMask2D>();
            }

            _detailBody = MakeText(gui, bodyHost, "", 15, Color.white, TextAnchor.UpperLeft);
            var bodyRt = _detailBody.rectTransform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;
            _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            _detailBody.verticalOverflow = VerticalWrapMode.Overflow;
            _detailBody.supportRichText = true;

            BuildIngredientRow(gui, detail);
            BuildWithdraw(gui, detail);
        }

        private static void BuildIngredientRow(GUIManager gui, RectTransform detail)
        {
            var row = MakeFill(
                "Ingredients",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 16f + WithdrawHeight + 8f),
                new Vector2(0f, 16f + WithdrawHeight + 8f + IngredientSize + 16f));
            row.SetParent(detail, false);

            var width = IngredientSize;
            var gap = 8f;
            var total = IngredientSlots * width + (IngredientSlots - 1) * gap;
            for (var i = 0; i < IngredientSlots; i++)
            {
                var x = -total / 2f + width / 2f + i * (width + gap);
                _ingredients.Add(MakeIngredientSlot(gui, row, x));
            }
        }

        private static IngredientSlot MakeIngredientSlot(GUIManager gui, RectTransform parent, float x)
        {
            var go = new GameObject("Ing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(IngredientSize, IngredientSize);
            rt.anchoredPosition = new Vector2(x, -6f);

            var label = MakeText(gui, go.transform, "", 11, Color.white, TextAnchor.LowerCenter);
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), 0f, 2f, IngredientSize + 8f, 16f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 4f, 36f, 36f);

            var amount = MakeText(gui, go.transform, "", 14, gui.ValheimOrange, TextAnchor.LowerRight);
            Place(amount.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), -4f, 2f, 40f, 18f);

            var hover = go.AddComponent<HubItemHover>();
            return new IngredientSlot
            {
                Go = go,
                Icon = icon,
                Amount = amount,
                Label = label,
                Hover = hover,
            };
        }

        private static void BuildWithdraw(GUIManager gui, RectTransform detail)
        {
            var go = gui.CreateButton(
                Localization.instance.Localize("$njord_withdraw"),
                detail,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                0f,
                WithdrawHeight);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, WithdrawHeight);
            rt.anchoredPosition = Vector2.zero;
            _withdraw = go.GetComponent<Button>();
            gui.ApplyButtonStyle(_withdraw, 22);
            _withdraw.onClick.AddListener(OnWithdraw);
            _withdrawLabel = go.GetComponentInChildren<Text>();
            if (_withdrawLabel != null)
            {
                _withdrawLabel.text = Localization.instance.Localize("$njord_withdraw");
                _withdrawLabel.alignment = TextAnchor.MiddleCenter;
                _withdrawLabel.resizeTextForBestFit = true;
                _withdrawLabel.resizeTextMinSize = 16;
                _withdrawLabel.resizeTextMaxSize = 24;
            }
        }

        private static Button MakeStationButton(GUIManager gui)
        {
            var go = gui.CreateButton(
                "",
                _root.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                0f,
                DropdownHeight);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(16f, -(DropdownTop + DropdownHeight));
            rt.offsetMax = new Vector2(-16f, -DropdownTop);
            var button = go.GetComponent<Button>();
            gui.ApplyButtonStyle(button, 14);
            button.onClick.AddListener(ToggleMenu);
            _stationLabel = go.GetComponentInChildren<Text>();
            if (_stationLabel != null)
            {
                _stationLabel.alignment = TextAnchor.MiddleLeft;
                _stationLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                var labelRt = _stationLabel.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(12f, 0f);
                labelRt.offsetMax = new Vector2(-12f, 0f);
            }

            return button;
        }

        private static void MakeSearch(GUIManager gui)
        {
            var mid = new Vector2(0.5f, 0.5f);
            var go = gui.CreateInputField(
                _root.transform,
                mid,
                mid,
                Vector2.zero,
                InputField.ContentType.Standard,
                Localization.instance.Localize("$njord_search"),
                16,
                176f,
                SearchHeight);
            _search = go.GetComponent<InputField>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(16f, -(SearchTop + SearchHeight));
            rt.offsetMax = new Vector2(-16f, -SearchTop);
            _search.interactable = true;
            _search.navigation = new Navigation { mode = Navigation.Mode.None };
            _search.onValueChanged.AddListener(OnSearch);
            SearchField.Decorate(_search, OnSearchCleared);
            var image = go.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = go.AddComponent<EventTrigger>();
            }

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(new UnityAction<BaseEventData>(OnSearchClicked));
            trigger.triggers.Add(entry);
        }

        private static void OnSearch(string value)
        {
            _query = value ?? "";
            SearchField.Sync(_search);
            _nextRefresh = 0f;
            Refresh();
        }

        private static void OnSearchCleared()
        {
            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            _query = "";
            SearchField.SetText(_search, "", OnSearch);
            _nextRefresh = 0f;
            Refresh();
        }

        private static void ResetSearch()
        {
            _query = "";
            SearchField.SetText(_search, "", OnSearch);
        }

        private static void OnSearchClicked(BaseEventData _)
        {
            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            FocusSearch();
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
        }

        private static void BuildStationMenu(GUIManager gui)
        {
            _stationCatcher = new GameObject("StationCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            _stationCatcher.transform.SetParent(_root.transform, false);
            var catchRt = _stationCatcher.GetComponent<RectTransform>();
            catchRt.anchorMin = Vector2.zero;
            catchRt.anchorMax = Vector2.one;
            catchRt.offsetMin = Vector2.zero;
            catchRt.offsetMax = Vector2.zero;
            var catchImage = _stationCatcher.GetComponent<Image>();
            catchImage.color = new Color(0f, 0f, 0f, 0.01f);
            catchImage.raycastTarget = true;
            _stationCatcher.GetComponent<Button>().onClick.AddListener(() => SetMenuOpen(false));

            _stationMenu = new GameObject("StationMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _stationMenu.transform.SetParent(_root.transform, false);
            var menuRt = _stationMenu.GetComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0f, 1f);
            menuRt.anchorMax = new Vector2(1f, 1f);
            menuRt.pivot = new Vector2(0.5f, 1f);
            menuRt.anchoredPosition = Vector2.zero;
            menuRt.offsetMin = new Vector2(16f, -(DropdownTop + DropdownHeight + 2f + 240f));
            menuRt.offsetMax = new Vector2(-16f, -(DropdownTop + DropdownHeight + 2f));
            var menuImage = _stationMenu.GetComponent<Image>();
            menuImage.color = new Color(0.12f, 0.09f, 0.07f, 0.96f);
            menuImage.raycastTarget = true;

            var scroll = gui.CreateScrollView(
                _stationMenu.transform,
                false,
                true,
                8f,
                2f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0f, 0f, 0f, 0.35f),
                480f,
                200f);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(4f, 4f);
            scrollRt.offsetMax = new Vector2(-4f, -4f);
            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                NjordWarehouseKeeperPanel.FitScrollView(scrollView, 80f);
                _stationMenuParent = scrollView.content;
                var content = _stationMenuParent as RectTransform;
                if (content != null)
                {
                    content.anchorMin = new Vector2(0f, 1f);
                    content.anchorMax = new Vector2(1f, 1f);
                    content.pivot = new Vector2(0.5f, 1f);
                    content.anchoredPosition = Vector2.zero;
                    content.sizeDelta = Vector2.zero;
                }

                var layout = _stationMenuParent.gameObject.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = _stationMenuParent.gameObject.AddComponent<VerticalLayoutGroup>();
                }

                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.spacing = 2f;
                layout.padding = new RectOffset(2, 14, 2, 4);

                var fitter = _stationMenuParent.gameObject.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = _stationMenuParent.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            SetMenuOpen(false);
        }

        private static void ToggleMenu()
        {
            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            SetMenuOpen(_stationMenu == null || !_stationMenu.activeSelf);
        }

        private static void SetMenuOpen(bool on)
        {
            if (on)
            {
                UnfocusSearch();
            }

            if (_stationMenu != null)
            {
                _stationMenu.SetActive(on);
            }

            if (_stationCatcher != null)
            {
                _stationCatcher.SetActive(on);
                if (on)
                {
                    _stationCatcher.transform.SetAsLastSibling();
                    if (_stationMenu != null)
                    {
                        _stationMenu.transform.SetAsLastSibling();
                    }
                }
            }
            else if (on && _stationMenu != null)
            {
                _stationMenu.transform.SetAsLastSibling();
            }
        }

        private static void RebuildStations()
        {
            if (_stations.Count > 3 &&
                _stationMenuParent != null &&
                _stationMenuParent.childCount == _stations.Count)
            {
                return;
            }

            _stations.Clear();
            _stations.Add(new StationOpt { Key = AllKey, Token = "$njord_station_all", Icon = HammerIcon() });
            _stations.Add(new StationOpt { Key = HandKey, Token = "$njord_station_hand", ShowsBasic = true });
            _stations.Add(new StationOpt { Key = HammerKey, Token = "$njord_station_hammer", Icon = HammerIcon() });

            var extra = new List<StationOpt>();
            var seen = new HashSet<string>();
            CollectStations(extra, seen);
            extra.Sort((a, b) => CompareLocalized(a.Token, b.Token));
            _stations.AddRange(extra);
            RebuildStationButtons();
        }

        private static Sprite HammerIcon()
        {
            var db = ObjectDB.instance;
            var prefab = db != null ? db.GetItemPrefab("Hammer") : null;
            var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null && drop.m_itemData != null ? drop.m_itemData.GetIcon() : null;
        }

        private static void CollectStations(List<StationOpt> names, HashSet<string> seen)
        {
            var db = ObjectDB.instance;
            if (db?.m_recipes != null)
            {
                for (var i = 0; i < db.m_recipes.Count; i++)
                {
                    AddStation(db.m_recipes[i] != null ? db.m_recipes[i].m_craftingStation : null, names, seen);
                }
            }

            var zns = ZNetScene.instance;
            if (zns?.m_prefabs != null)
            {
                for (var i = 0; i < zns.m_prefabs.Count; i++)
                {
                    var go = zns.m_prefabs[i];
                    if (go == null)
                    {
                        continue;
                    }

                    var cs = go.GetComponent<CraftingStation>();
                    if (cs == null)
                    {
                        cs = go.GetComponentInChildren<CraftingStation>(true);
                    }

                    AddStation(cs, names, seen);
                }
            }
        }

        private static void AddStation(CraftingStation station, List<StationOpt> names, HashSet<string> seen)
        {
            if (!station || string.IsNullOrEmpty(station.m_name) || !seen.Add(station.m_name))
            {
                return;
            }

            names.Add(new StationOpt
            {
                Key = station.m_name,
                Token = station.m_name,
                Icon = station.m_icon,
                ShowsBasic = station.m_showBasicRecipies,
            });
        }

        private static int CompareLocalized(string a, string b)
        {
            return string.Compare(
                Localization.instance.Localize(a),
                Localization.instance.Localize(b),
                StringComparison.OrdinalIgnoreCase);
        }

        private static void RebuildStationButtons()
        {
            if (_stationMenuParent == null)
            {
                return;
            }

            for (var i = _stationMenuParent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_stationMenuParent.GetChild(i).gameObject);
            }

            var gui = GUIManager.Instance;
            for (var i = 0; i < _stations.Count; i++)
            {
                var opt = _stations[i];
                var captured = opt.Key;
                var go = gui.CreateButton(
                    Localization.instance.Localize(opt.Token),
                    _stationMenuParent,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    Vector2.zero,
                    0f,
                    28f);
                var layout = go.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = go.AddComponent<LayoutElement>();
                }

                layout.minHeight = 28f;
                layout.preferredHeight = 28f;
                layout.flexibleWidth = 1f;
                var button = go.GetComponent<Button>();
                gui.ApplyButtonStyle(button, 13);
                var label = go.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var lrt = label.rectTransform;
                    lrt.offsetMin = new Vector2(10f, 0f);
                    lrt.offsetMax = new Vector2(-8f, 0f);
                }

                button.onClick.AddListener(() => SelectStation(captured));
            }
        }

        private static void SelectStation(string key)
        {
            _station = key ?? AllKey;
            _selected = null;
            _selectedKey = null;
            SetMenuOpen(false);
            Refresh();
        }

        private static void PaintStationChrome()
        {
            var opt = CurrentStation();
            var name = Localization.instance.Localize(opt != null ? opt.Token : "$njord_recipes");
            if (_stationLabel != null)
            {
                _stationLabel.text = name + "  ▾";
            }

            if (_title != null)
            {
                _title.text = _station == AllKey
                    ? Localization.instance.Localize("$njord_recipes")
                    : name;
            }

            if (_stationIcon != null)
            {
                var icon = opt != null ? opt.Icon : null;
                _stationIcon.sprite = icon;
                _stationIcon.enabled = icon != null;
            }
        }

        private static StationOpt CurrentStation()
        {
            for (var i = 0; i < _stations.Count; i++)
            {
                if (_stations[i].Key == _station)
                {
                    return _stations[i];
                }
            }

            return null;
        }

        private static List<CraftOffer> KnownCrafts(List<IndexedStack> listed)
        {
            var result = new List<CraftOffer>();
            var seen = new HashSet<string>();
            if (_station != HammerKey)
            {
                AddRecipes(result, seen);
            }

            if (_station == HammerKey)
            {
                AddHammerPieces(result, seen);
            }

            for (var i = 0; i < result.Count; i++)
            {
                var offer = result[i];
                offer.Affordable = offer.Recipe != null
                    ? StorageNetwork.CanAffordRecipe(listed, offer.Recipe)
                    : StorageNetwork.CanAffordPiece(listed, offer.Piece);
            }

            if (!string.IsNullOrEmpty((_query ?? "").Trim()))
            {
                for (var i = result.Count - 1; i >= 0; i--)
                {
                    if (!MatchesQuery(result[i]))
                    {
                        result.RemoveAt(i);
                    }
                }
            }

            result.Sort(CompareOffers);
            return result;
        }

        private static bool MatchesQuery(CraftOffer offer)
        {
            var query = (_query ?? "").Trim();
            if (query.Length == 0 || offer == null)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(offer.Name) &&
                offer.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (offer.Recipe != null && offer.Recipe.m_item && offer.Recipe.m_item.m_itemData?.m_shared != null)
            {
                var item = offer.Recipe.m_item;
                var shared = item.m_itemData.m_shared.m_name;
                if (!string.IsNullOrEmpty(shared) &&
                    (shared.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                     || Localization.instance.Localize(shared).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(item.name) &&
                    item.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            if (offer.Piece != null && !string.IsNullOrEmpty(offer.Piece.m_name))
            {
                var pieceName = offer.Piece.m_name;
                if (pieceName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || Localization.instance.Localize(pieceName).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Same recipes the vanilla Craft tab would list at the selected station:
        /// enabled (or in-season) known crafts, not upgrade-only, matched by
        /// <see cref="Recipe.m_craftingStation"/>. Each recipe asset is kept —
        /// collapsing by item name picks the first ObjectDB row, which is often
        /// an upgrade recipe or a different bench.
        /// </summary>
        private static void AddRecipes(List<CraftOffer> result, HashSet<string> seen)
        {
            var player = Player.m_localPlayer;
            var db = ObjectDB.instance;
            if (player == null || db == null || db.m_recipes == null)
            {
                return;
            }

            var allUnlocked = player.m_noPlacementCost
                || (ZoneSystem.instance != null
                    && ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllRecipesUnlocked));
            var season = player.CurrentSeason;

            for (var i = 0; i < db.m_recipes.Count; i++)
            {
                var recipe = db.m_recipes[i];
                if (!recipe)
                {
                    continue;
                }

                var seasonal = season != null
                    && season.Recipes != null
                    && season.Recipes.Contains(recipe);
                if ((!recipe.m_enabled && !seasonal) || !recipe.m_item)
                {
                    continue;
                }

                if (recipe.m_noCraftOnlyUpgrade)
                {
                    continue;
                }

                if (!StationMatches(recipe.m_craftingStation))
                {
                    continue;
                }

                var data = recipe.m_item.m_itemData;
                if (data?.m_shared == null)
                {
                    continue;
                }

                var shared = data.m_shared.m_name;
                if (!allUnlocked && !player.IsRecipeKnown(shared))
                {
                    continue;
                }

                if (data.m_shared.m_dlc.Length > 0 &&
                    DLCMan.instance != null &&
                    !DLCMan.instance.IsDLCInstalled(data.m_shared.m_dlc))
                {
                    continue;
                }

                if (!seen.Add(OfferKeyForRecipe(recipe)))
                {
                    continue;
                }

                if (!HasRequirements(recipe.m_resources))
                {
                    continue;
                }

                var name = Localization.instance.Localize(shared);
                if (recipe.m_amount > 1)
                {
                    name += " x" + recipe.m_amount;
                }

                result.Add(new CraftOffer
                {
                    Name = name,
                    Icon = data.GetIcon(),
                    Recipe = recipe,
                    Tooltip = Localization.instance.Localize(data.GetTooltip()),
                });
            }
        }

        /// <summary>
        /// Craft-tab station filter as if the player stood at the dropdown
        /// choice: handcraft when the recipe has no station, that bench's
        /// recipes when a bench is picked, plus handcraft on benches that
        /// show basic recipes. Unity fake-null, not C# null.
        /// </summary>
        private static bool StationMatches(CraftingStation station)
        {
            if (_station == AllKey)
            {
                return true;
            }

            if (_station == HammerKey)
            {
                return false;
            }

            if (_station == HandKey)
            {
                return !station;
            }

            if (station && station.m_name == _station)
            {
                return true;
            }

            return !station && CurrentStationShowsBasic();
        }

        private static bool CurrentStationShowsBasic()
        {
            var opt = CurrentStation();
            return opt != null && opt.ShowsBasic;
        }

        private static void AddHammerPieces(List<CraftOffer> result, HashSet<string> seen)
        {
            var player = Player.m_localPlayer;
            var table = HammerTable();
            if (player == null || table == null || table.m_pieces == null)
            {
                return;
            }

            for (var i = 0; i < table.m_pieces.Count; i++)
            {
                var go = table.m_pieces[i];
                var piece = go != null ? go.GetComponent<Piece>() : null;
                if (piece == null || !piece.m_enabled || string.IsNullOrEmpty(piece.m_name))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(piece.m_dlc) &&
                    DLCMan.instance != null &&
                    !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
                {
                    continue;
                }

                if (!PieceKnown(player, piece) || !seen.Add("piece:" + piece.m_name))
                {
                    continue;
                }

                if (!HasRequirements(piece.m_resources))
                {
                    continue;
                }

                result.Add(new CraftOffer
                {
                    Name = Localization.instance.Localize(piece.m_name),
                    Icon = piece.m_icon,
                    Piece = piece,
                    Tooltip = Localization.instance.Localize(piece.m_description ?? ""),
                });
            }
        }

        private static bool HasRequirements(Piece.Requirement[] resources)
        {
            if (resources == null)
            {
                return false;
            }

            for (var i = 0; i < resources.Length; i++)
            {
                var req = resources[i];
                if (req != null && req.m_resItem && !req.m_upgraderResource && req.GetAmount(1) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PieceKnown(Player player, Piece piece)
        {
            return player.HaveRequirements(piece, Player.RequirementMode.IsKnown);
        }

        private static PieceTable HammerTable()
        {
            var db = ObjectDB.instance;
            var prefab = db != null ? db.GetItemPrefab("Hammer") : null;
            var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null && drop.m_itemData?.m_shared != null
                ? drop.m_itemData.m_shared.m_buildPieces
                : null;
        }

        private static int CompareOffers(CraftOffer a, CraftOffer b)
        {
            if (a.Affordable != b.Affordable)
            {
                return a.Affordable ? -1 : 1;
            }

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static RecipeRow MakeRow()
        {
            var go = new GameObject(
                "RecipeRow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(_rowParent, false);
            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1f;
            var bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.04f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.7f, 1f);
            colors.pressedColor = new Color(1f, 0.78f, 0.35f, 1f);
            colors.selectedColor = new Color(1f, 0.78f, 0.35f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 4f, 0f, 24f, 24f);

            var gui = GUIManager.Instance;
            var name = MakeText(gui, go.transform, "", 14, gui.ValheimOrange, TextAnchor.MiddleLeft);
            var nameRt = name.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(32f, 0f);
            nameRt.offsetMax = new Vector2(-4f, 0f);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.raycastTarget = false;

            var view = new RecipeRow
            {
                Go = go,
                Button = button,
                Background = bg,
                Icon = icon,
                Name = name,
            };
            button.onClick.AddListener(() => OnClicked(view));
            return view;
        }

        private static void BindRow(RecipeRow view, CraftOffer offer, bool selected)
        {
            view.Offer = offer;
            if (view.Icon != null)
            {
                view.Icon.sprite = offer.Icon;
                view.Icon.enabled = offer.Icon != null;
            }

            if (view.Name != null)
            {
                view.Name.text = offer.Name;
            }

            PaintRow(view, selected);
        }

        private static void PaintRow(RecipeRow view, bool selected)
        {
            var offer = view.Offer;
            if (offer == null)
            {
                return;
            }

            var can = offer.Affordable;
            var dim = can ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            if (view.Icon != null)
            {
                view.Icon.color = dim;
            }

            if (view.Name != null)
            {
                view.Name.color = can
                    ? (selected ? GUIManager.Instance.ValheimOrange : new Color(0.92f, 0.88f, 0.78f, 1f))
                    : new Color(0.55f, 0.55f, 0.55f, 1f);
            }

            if (view.Background != null)
            {
                view.Background.color = selected
                    ? (can ? new Color(0.85f, 0.5f, 0.12f, 0.85f) : new Color(0.45f, 0.3f, 0.12f, 0.7f))
                    : new Color(1f, 1f, 1f, 0.03f);
            }
        }

        private static void BindDetail(CraftOffer offer, List<IndexedStack> listed)
        {
            var has = offer != null;
            if (_detailIcon != null)
            {
                _detailIcon.enabled = has && offer.Icon != null;
                _detailIcon.sprite = has ? offer.Icon : null;
            }

            if (_detailName != null)
            {
                _detailName.text = has ? offer.Name : "";
            }

            if (_detailBody != null)
            {
                _detailBody.text = has ? (offer.Tooltip ?? "") : "";
            }

            if (_withdraw != null)
            {
                _withdraw.interactable = has;
            }

            var lines = has
                ? (offer.Recipe != null
                    ? StorageNetwork.RecipeIngredients(offer.Recipe, listed)
                    : StorageNetwork.PieceIngredients(offer.Piece, listed))
                : new List<StorageNetwork.IngredientLine>();
            for (var i = 0; i < _ingredients.Count; i++)
            {
                BindIngredient(_ingredients[i], i < lines.Count ? lines[i] : (StorageNetwork.IngredientLine?)null);
            }
        }

        private static void BindIngredient(IngredientSlot slot, StorageNetwork.IngredientLine? line)
        {
            var has = line.HasValue && line.Value.Need > 0;
            slot.Go.SetActive(has);
            if (!has)
            {
                return;
            }

            var ing = line.Value;
            var enough = ing.Have >= ing.Need;
            if (slot.Icon != null)
            {
                slot.Icon.sprite = ing.Icon;
                slot.Icon.enabled = ing.Icon != null;
                slot.Icon.color = enough ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            if (slot.Label != null)
            {
                slot.Label.text = ing.DisplayName;
                slot.Label.color = enough ? Color.white : new Color(0.65f, 0.65f, 0.65f, 1f);
            }

            if (slot.Amount != null)
            {
                slot.Amount.text = ing.Need.ToString();
                slot.Amount.color = enough
                    ? GUIManager.Instance.ValheimOrange
                    : new Color(0.85f, 0.25f, 0.2f, 1f);
            }

            if (slot.Hover != null)
            {
                var body = new StringBuilder();
                if (ing.Have < ing.Need)
                {
                    body.Append("<color=red>");
                    body.Append(ing.Have);
                    body.Append("</color>");
                }
                else
                {
                    body.Append(ing.Have);
                }

                body.Append(" / ");
                body.Append(ing.Need);
                slot.Hover.BindText(ing.DisplayName, body.ToString());
            }
        }

        private static void OnClicked(RecipeRow view)
        {
            if (view == null || view.Offer == null)
            {
                return;
            }

            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            _selected = view.Offer;
            _selectedKey = OfferKey(view.Offer);
            for (var i = 0; i < _rows.Count; i++)
            {
                PaintRow(_rows[i], _rows[i].Offer != null && OfferKey(_rows[i].Offer) == _selectedKey);
            }

            var listed = NjordWarehouseKeeperMarker.OpenHub != null
                ? StorageNetwork.ListItems(NjordWarehouseKeeperMarker.OpenHub)
                : new List<IndexedStack>();
            BindDetail(_selected, listed);
        }

        private static void OnWithdraw()
        {
            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            var offer = _selected;
            if (offer == null)
            {
                return;
            }

            if (offer.Recipe != null)
            {
                StorageNetwork.WithdrawRecipe(Player.m_localPlayer, NjordWarehouseKeeperMarker.OpenHub, offer.Recipe);
            }
            else
            {
                StorageNetwork.WithdrawPiece(Player.m_localPlayer, NjordWarehouseKeeperMarker.OpenHub, offer.Piece);
            }

            NjordWarehouseKeeperPanel.RefreshAfterRemote();
        }

        private static string OfferKey(CraftOffer offer)
        {
            if (offer == null)
            {
                return null;
            }

            if (offer.Recipe != null)
            {
                return OfferKeyForRecipe(offer.Recipe);
            }

            if (offer.Piece != null)
            {
                return "piece:" + offer.Piece.m_name;
            }

            return offer.Name;
        }

        private static string OfferKeyForRecipe(Recipe recipe)
        {
            if (recipe == null)
            {
                return "recipe:";
            }

            var item = "";
            if (recipe.m_item && recipe.m_item.m_itemData?.m_shared != null)
            {
                item = recipe.m_item.m_itemData.m_shared.m_name;
            }

            var station = recipe.m_craftingStation ? recipe.m_craftingStation.m_name : HandKey;
            return "recipe:" + recipe.name + "|" + item + "|" + station + "|" + recipe.m_minStationLevel + "|" + recipe.m_amount;
        }

        private static float RecipeScrollSensitivity()
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

            return recipeSensitivity * (RowHeight / recipePitch);
        }

        private static RectTransform MakeFill(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static Text MakeText(
            GUIManager gui,
            Transform parent,
            string text,
            int size,
            Color color,
            TextAnchor align)
        {
            var mid = new Vector2(0.5f, 0.5f);
            var go = gui.CreateText(
                text,
                parent,
                mid,
                mid,
                Vector2.zero,
                gui.AveriaSerifBold,
                size,
                color,
                true,
                Color.black,
                100f,
                24f,
                false);
            var label = go.GetComponent<Text>();
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            float x,
            float y,
            float width,
            float height)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        private sealed class CraftOffer
        {
            internal string Name;
            internal Sprite Icon;
            internal Recipe Recipe;
            internal Piece Piece;
            internal string Tooltip;
            internal bool Affordable;
        }

        private sealed class StationOpt
        {
            internal string Key;
            internal string Token;
            internal Sprite Icon;
            internal bool ShowsBasic;
        }

        private sealed class RecipeRow
        {
            internal GameObject Go;
            internal Button Button;
            internal Image Background;
            internal Image Icon;
            internal Text Name;
            internal CraftOffer Offer;
        }

        private sealed class IngredientSlot
        {
            internal GameObject Go;
            internal Image Icon;
            internal Text Amount;
            internal Text Label;
            internal HubItemHover Hover;
        }
    }
}
