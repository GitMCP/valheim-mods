using System;
using System.Collections.Generic;
using NjordWarehouseKeeper.Client;
using NjordWarehouseKeeper.Storage;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Client-only hub settings shown when the cog is open. Settings holds the
    /// deposit skip toggle, reset-filters toggle, restock hotkey, and Reorganize.
    /// Resupply holds search and the list of items to keep in the pack.
    /// </summary>
    internal static class NjordWarehouseKeeperPrefs
    {
        private const float RowHeight = 42f;
        private const float SettingsTitleY = 0f;
        private const float ToggleY = 26f;
        private const float ResetFiltersY = 56f;
        private const float HotkeyY = 86f;
        private const float ReorgY = 120f;
        private const float ResupplyTitleY = 162f;
        private const float HintY = 186f;
        private const float SearchY = 222f;
        private const float ListTop = 258f;

        private static GameObject _root;
        private static Toggle _skipFavourites;
        private static Toggle _resetFilters;
        private static Button _hotkeyButton;
        private static Text _hotkeyLabel;
        private static InputField _search;
        private static Transform _rowParent;
        private static readonly List<PrefRow> _rows = new List<PrefRow>();
        private static string _query = "";
        private static bool _suppress;
        private static bool _capturing;

        internal static bool IsCapturingHotkey
        {
            get { return _capturing; }
        }

        internal static bool TickCapture()
        {
            if (!_capturing)
            {
                return false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _capturing = false;
                PaintHotkeyButton();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                if (ClientPreferences.RestockHotkey != null)
                {
                    ClientPreferences.RestockHotkey.Value = KeyboardShortcut.Empty;
                }

                _capturing = false;
                PaintHotkeyButton();
                return true;
            }

            var codes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));
            for (var i = 0; i < codes.Length; i++)
            {
                var key = codes[i];
                if ((int)key == 0 || IsMouse(key) || IsModifier(key))
                {
                    continue;
                }

                if (!Input.GetKeyDown(key))
                {
                    continue;
                }

                if (ClientPreferences.RestockHotkey != null)
                {
                    ClientPreferences.RestockHotkey.Value = new KeyboardShortcut(key, CurrentModifiers());
                }

                _capturing = false;
                PaintHotkeyButton();
                return true;
            }

            return true;
        }

        internal static bool IsOpen
        {
            get { return _root != null && _root.activeSelf; }
        }

        internal static void OnLanguageChanged()
        {
            PaintHotkeyButton();
        }

        internal static void ForgetBuilt()
        {
            _root = null;
            _skipFavourites = null;
            _resetFilters = null;
            _hotkeyButton = null;
            _hotkeyLabel = null;
            _search = null;
            _rowParent = null;
            _rows.Clear();
            _query = "";
            _suppress = false;
            _capturing = false;
        }

        internal static void Build(Transform parent, GUIManager gui)
        {
            ForgetBuilt();
            _root = new GameObject("Preferences", typeof(RectTransform));
            _root.transform.SetParent(parent, false);
            var rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.offsetMin = new Vector2(16f, 18f);
            rootRt.offsetMax = new Vector2(-16f, -NjordWarehouseKeeperPanel.ContentTop);

            PlaceSectionTitle(
                gui,
                "$njord_pref_settings",
                SettingsTitleY);

            var toggleGo = gui.CreateToggle(_root.transform, 26f, 26f);
            toggleGo.transform.SetParent(_root.transform, false);
            _skipFavourites = toggleGo.GetComponent<Toggle>();
            HideToggleLabel(toggleGo);
            PlaceTopLeft(toggleGo.GetComponent<RectTransform>(), 4f, ToggleY, 26f, 26f);
            _skipFavourites.onValueChanged.AddListener(OnSkipChanged);

            var skipLabel = MakeLabel(
                gui,
                _root.transform,
                "$njord_pref_skip_favourites",
                15,
                Color.white,
                TextAnchor.MiddleLeft);
            PlaceTopStretch(skipLabel.GetComponent<RectTransform>(), 38f, ToggleY, 26f, 0f);

            var resetGo = gui.CreateToggle(_root.transform, 26f, 26f);
            resetGo.transform.SetParent(_root.transform, false);
            _resetFilters = resetGo.GetComponent<Toggle>();
            HideToggleLabel(resetGo);
            PlaceTopLeft(resetGo.GetComponent<RectTransform>(), 4f, ResetFiltersY, 26f, 26f);
            _resetFilters.onValueChanged.AddListener(OnResetFiltersChanged);

            var resetLabel = MakeLabel(
                gui,
                _root.transform,
                "$njord_pref_reset_filters",
                15,
                Color.white,
                TextAnchor.MiddleLeft);
            PlaceTopStretch(resetLabel.GetComponent<RectTransform>(), 38f, ResetFiltersY, 26f, 0f);

            var hotkeyLabel = MakeLabel(
                gui,
                _root.transform,
                "$njord_pref_hotkey",
                15,
                Color.white,
                TextAnchor.MiddleLeft);
            PlaceTopStretch(hotkeyLabel.GetComponent<RectTransform>(), 4f, HotkeyY, 28f, 150f);

            var hotkeyGo = gui.CreateButton(
                "",
                _root.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                140f,
                28f);
            gui.ApplyButtonStyle(hotkeyGo.GetComponent<Button>(), 13);
            var hotkeyBtnRt = hotkeyGo.GetComponent<RectTransform>();
            hotkeyBtnRt.anchorMin = new Vector2(1f, 1f);
            hotkeyBtnRt.anchorMax = new Vector2(1f, 1f);
            hotkeyBtnRt.pivot = new Vector2(1f, 1f);
            hotkeyBtnRt.anchoredPosition = new Vector2(0f, -HotkeyY);
            hotkeyBtnRt.sizeDelta = new Vector2(140f, 28f);
            _hotkeyButton = hotkeyGo.GetComponent<Button>();
            _hotkeyLabel = hotkeyGo.GetComponentInChildren<Text>();
            _hotkeyButton.onClick.AddListener(BeginCaptureHotkey);
            PaintHotkeyButton();

            var reorgGo = gui.CreateButton(
                "$njord_reorganize",
                _root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                0f,
                32f);
            PlaceTopStretch(reorgGo.GetComponent<RectTransform>(), 4f, ReorgY, 32f, 4f);
            var reorgButton = reorgGo.GetComponent<Button>();
            gui.ApplyButtonStyle(reorgButton, 15);
            reorgButton.onClick.AddListener(OnReorganize);
            var reorgLabel = reorgGo.GetComponentInChildren<Text>();
            if (reorgLabel != null)
            {
                reorgLabel.alignment = TextAnchor.MiddleCenter;
            }

            PlaceSectionTitle(
                gui,
                "$njord_resupply",
                ResupplyTitleY);

            var hint = MakeLabel(
                gui,
                _root.transform,
                "$njord_pref_resupply_hint",
                13,
                new Color(1f, 0.9f, 0.75f, 1f),
                TextAnchor.UpperLeft);
            PlaceTopStretch(hint.GetComponent<RectTransform>(), 4f, HintY, 32f, 4f);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.verticalOverflow = VerticalWrapMode.Overflow;

            _search = gui.CreateInputField(
                _root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                InputField.ContentType.Standard,
                "$njord_search",
                16,
                176f,
                30f).GetComponent<InputField>();
            _search.onValueChanged.AddListener(OnSearch);
            PlaceTopStretch(_search.GetComponent<RectTransform>(), 0f, SearchY, 30f, 0f);
            _search.interactable = true;
            _search.navigation = new Navigation { mode = Navigation.Mode.None };
            SearchField.Decorate(_search, OnSearchCleared);

            var scroll = gui.CreateScrollView(
                _root.transform,
                false,
                true,
                10f,
                3f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock,
                new Color(0f, 0f, 0f, 0.35f),
                480f,
                300f);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 0f);
            scrollRt.offsetMax = new Vector2(0f, -ListTop);

            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                NjordWarehouseKeeperPanel.StretchFill(scrollView.transform as RectTransform);
                NjordWarehouseKeeperPanel.FitScrollView(scrollView, NjordWarehouseKeeperPanel.ListScrollSensitivity());
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

                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.spacing = 3f;
                layout.padding = new RectOffset(4, 18, 4, 8);

                var fitter = _rowParent.gameObject.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = _rowParent.gameObject.AddComponent<ContentSizeFitter>();
                }

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            _root.SetActive(false);
        }

        internal static void SetOpen(bool on)
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(on);
            if (on)
            {
                Refresh();
            }
            else
            {
                _capturing = false;
                ResetSearch();
                UnfocusInputs();
            }
        }

        internal static bool InputHasFocus()
        {
            if (_capturing)
            {
                return true;
            }

            if (!IsOpen)
            {
                return false;
            }

            if (_search != null && _search.isFocused)
            {
                return true;
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Qty != null && _rows[i].Qty.isFocused)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void Refresh()
        {
            if (!IsOpen || _rowParent == null)
            {
                return;
            }

            _suppress = true;
            if (_skipFavourites != null && ClientPreferences.DepositSkipFavourites != null)
            {
                _skipFavourites.isOn = ClientPreferences.DepositSkipFavourites.Value;
            }

            if (_resetFilters != null && ClientPreferences.ResetFiltersOnClose != null)
            {
                _resetFilters.isOn = ClientPreferences.ResetFiltersOnClose.Value;
            }

            _suppress = false;

            PaintHotkeyButton();

            var visible = Candidates(_query);
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

        internal static void ResetSearch()
        {
            _query = "";
            SearchField.SetText(_search, "", OnSearch);
        }

        private static void OnSearch(string value)
        {
            _query = value ?? "";
            SearchField.Sync(_search);
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
            Refresh();
        }

        private static void OnSkipChanged(bool on)
        {
            if (_suppress || ClientPreferences.DepositSkipFavourites == null)
            {
                return;
            }

            ClientPreferences.DepositSkipFavourites.Value = on;
        }

        private static void OnResetFiltersChanged(bool on)
        {
            if (_suppress || ClientPreferences.ResetFiltersOnClose == null)
            {
                return;
            }

            ClientPreferences.ResetFiltersOnClose.Value = on;
        }

        private static void OnReorganize()
        {
            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
            }

            var hub = NjordWarehouseKeeperMarker.OpenHub;
            var player = Player.m_localPlayer;
            if (hub == null)
            {
                return;
            }

            var freed = StorageNetwork.ReorganizeNow(hub);
            if (player != null)
            {
                player.Message(
                    MessageHud.MessageType.Center,
                    freed > 0
                        ? Localization.instance.Localize("$njord_reorganize_ok").Replace("{0}", freed.ToString())
                        : Localization.instance.Localize("$njord_reorganize_none"));
            }

            NjordWarehouseKeeperPanel.RefreshAfterRemote();
            Refresh();
        }

        private static List<IndexedStack> Candidates(string query)
        {
            query = query == null ? "" : query.Trim();
            var hub = NjordWarehouseKeeperMarker.OpenHub;
            var listed = hub != null ? StorageNetwork.ListItems(hub) : new List<IndexedStack>();
            var byKey = new Dictionary<string, IndexedStack>();
            for (var i = 0; i < listed.Count; i++)
            {
                var key = listed[i].Key();
                if (!byKey.ContainsKey(key))
                {
                    byKey.Add(key, listed[i]);
                }
            }

            var extra = ClientPreferences.ResupplyKeys();
            AddMissing(byKey, extra);
            AddMissing(byKey, ClientPreferences.FavouriteKeys());

            var result = new List<IndexedStack>(byKey.Count);
            foreach (var pair in byKey)
            {
                var item = pair.Value;
                if (query.Length > 0 &&
                    item.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                result.Add(item);
            }

            result.Sort(Compare);
            return result;
        }

        private static void AddMissing(Dictionary<string, IndexedStack> byKey, List<string> keys)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = ItemKey.TypeOf(keys[i]);
                if (string.IsNullOrEmpty(key) || byKey.ContainsKey(key))
                {
                    continue;
                }

                var described = ItemLookup.Describe(key);
                if (described != null)
                {
                    byKey.Add(key, described);
                }
            }
        }

        private static int Compare(IndexedStack a, IndexedStack b)
        {
            var af = ClientPreferences.IsFavourite(a);
            var bf = ClientPreferences.IsFavourite(b);
            if (af != bf)
            {
                return af ? -1 : 1;
            }

            var ar = ClientPreferences.TryGetResupply(a.Key(), out var aQty);
            var br = ClientPreferences.TryGetResupply(b.Key(), out var bQty);
            if (ar != br)
            {
                return ar ? -1 : 1;
            }

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static PrefRow MakeRow()
        {
            var gui = GUIManager.Instance;
            var row = new GameObject(
                "ResupplyRow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            row.transform.SetParent(_rowParent, false);
            var bg = row.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);
            var layout = row.GetComponent<LayoutElement>();
            layout.minHeight = RowHeight;
            layout.preferredHeight = RowHeight;
            layout.flexibleWidth = 1f;

            var toggleGo = gui.CreateToggle(row.transform, 22f, 22f);
            toggleGo.transform.SetParent(row.transform, false);
            var include = toggleGo.GetComponent<Toggle>();
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0f, 0.5f);
            toggleRt.anchorMax = new Vector2(0f, 0.5f);
            toggleRt.pivot = new Vector2(0f, 0.5f);
            toggleRt.anchoredPosition = new Vector2(8f, 0f);
            toggleRt.sizeDelta = new Vector2(22f, 22f);
            HideToggleLabel(toggleGo);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(32f, 32f);
            iconRt.anchoredPosition = new Vector2(36f, 0f);

            var nameGo = gui.CreateText(
                "",
                row.transform,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                Vector2.zero,
                gui.AveriaSerifBold,
                15,
                gui.ValheimOrange,
                true,
                Color.black,
                0f,
                28f,
                false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(74f, 4f);
            nameRt.offsetMax = new Vector2(-86f, -4f);
            var name = nameGo.GetComponent<Text>();
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.raycastTarget = false;

            var qtyGo = gui.CreateInputField(
                row.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                Vector2.zero,
                InputField.ContentType.IntegerNumber,
                "0",
                15,
                72f,
                28f);
            var qty = qtyGo.GetComponent<InputField>();
            qty.characterLimit = 4;
            qty.navigation = new Navigation { mode = Navigation.Mode.None };
            var qtyRt = qtyGo.GetComponent<RectTransform>();
            qtyRt.anchorMin = new Vector2(1f, 0.5f);
            qtyRt.anchorMax = new Vector2(1f, 0.5f);
            qtyRt.pivot = new Vector2(1f, 0.5f);
            qtyRt.sizeDelta = new Vector2(72f, 28f);
            qtyRt.anchoredPosition = new Vector2(-8f, 0f);

            var view = new PrefRow
            {
                Go = row,
                Include = include,
                Icon = icon,
                Name = name,
                Qty = qty,
            };
            include.onValueChanged.AddListener(_ => OnIncludeChanged(view));
            qty.onEndEdit.AddListener(_ => OnQtyChanged(view));
            return view;
        }

        private static void BindRow(PrefRow view, IndexedStack stack)
        {
            view.Key = stack.Key();
            _suppress = true;
            if (view.Icon != null)
            {
                view.Icon.sprite = stack.Icon;
                view.Icon.enabled = stack.Icon != null;
            }

            if (view.Name != null)
            {
                view.Name.text = stack.DisplayName;
            }

            int qty;
            var included = ClientPreferences.TryGetResupply(view.Key, out qty);
            if (view.Include != null)
            {
                view.Include.isOn = included;
            }

            if (view.Qty != null)
            {
                view.Qty.text = included ? qty.ToString() : "";
                view.Qty.interactable = included;
                var placeholder = view.Qty.placeholder as Text;
                if (placeholder != null)
                {
                    placeholder.text = included ? "" : "0";
                }
            }

            _suppress = false;
        }

        private static void OnIncludeChanged(PrefRow view)
        {
            if (_suppress || view == null || string.IsNullOrEmpty(view.Key) || view.Include == null)
            {
                return;
            }

            if (view.Include.isOn)
            {
                var qty = ParseQty(view, ItemLookup.MaxStack(view.Key));
                if (qty <= 0)
                {
                    qty = Mathf.Min(50, ItemLookup.MaxStack(view.Key));
                    if (qty <= 0)
                    {
                        qty = 1;
                    }
                }

                ClientPreferences.SetResupply(view.Key, qty);
            }
            else
            {
                ClientPreferences.SetResupply(view.Key, 0);
            }

            _suppress = true;
            if (view.Qty != null)
            {
                int stored;
                var included = ClientPreferences.TryGetResupply(view.Key, out stored);
                view.Qty.interactable = included;
                view.Qty.text = included ? stored.ToString() : "";
            }

            _suppress = false;
        }

        private static void OnQtyChanged(PrefRow view)
        {
            if (_suppress || view == null || string.IsNullOrEmpty(view.Key) || view.Include == null || !view.Include.isOn)
            {
                return;
            }

            var qty = ParseQty(view, 1);
            var max = ItemLookup.MaxStack(view.Key);
            if (max > 1)
            {
                qty = Mathf.Min(qty, max * 20);
            }

            ClientPreferences.SetResupply(view.Key, Mathf.Max(1, qty));
            _suppress = true;
            if (view.Qty != null)
            {
                view.Qty.text = ClientPreferences.TryGetResupply(view.Key, out qty)
                    ? qty.ToString()
                    : "1";
            }

            _suppress = false;
        }

        private static int ParseQty(PrefRow view, int fallback)
        {
            int qty;
            if (view.Qty != null && int.TryParse(view.Qty.text, out qty) && qty > 0)
            {
                return qty;
            }

            return fallback;
        }

        private static void HideToggleLabel(GameObject toggle)
        {
            var label = toggle.transform.Find("Label");
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }

        private static void UnfocusInputs()
        {
            if (_search != null && _search.isFocused)
            {
                _search.DeactivateInputField();
            }

            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Qty != null && _rows[i].Qty.isFocused)
                {
                    _rows[i].Qty.DeactivateInputField();
                }
            }
        }

        private static void PlaceSectionTitle(GUIManager gui, string text, float yFromTop)
        {
            var title = MakeLabel(gui, _root.transform, text, 18, gui.ValheimOrange, TextAnchor.MiddleLeft);
            PlaceTopStretch(title.GetComponent<RectTransform>(), 4f, yFromTop, 22f, 4f);
        }

        private static void PlaceTopLeft(RectTransform rt, float x, float yFromTop, float width, float height)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceTopStretch(RectTransform rt, float left, float yFromTop, float height, float right)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(left, -yFromTop);
            rt.sizeDelta = new Vector2(-(left + right), height);
        }

        private static Text MakeLabel(
            GUIManager gui,
            Transform parent,
            string text,
            int size,
            Color color,
            TextAnchor align)
        {
            var go = gui.CreateText(
                text,
                parent,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                gui.AveriaSerifBold,
                size,
                color,
                true,
                Color.black,
                0f,
                24f,
                false);
            var label = go.GetComponent<Text>();
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }

        private static void BeginCaptureHotkey()
        {
            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            _capturing = true;
            PaintHotkeyButton();
        }

        private static void PaintHotkeyButton()
        {
            if (_hotkeyLabel == null)
            {
                return;
            }

            if (_capturing)
            {
                LocalizedUi.Capture(_hotkeyLabel, "$njord_pref_hotkey_listen");
                return;
            }

            var shortcut = ClientPreferences.RestockHotkey;
            if (shortcut == null || shortcut.Value.MainKey == KeyCode.None)
            {
                LocalizedUi.Capture(_hotkeyLabel, "$njord_pref_hotkey_none");
                return;
            }

            if (Localization.instance != null)
            {
                Localization.instance.RemoveTextFromCache(_hotkeyLabel);
            }

            _hotkeyLabel.text = shortcut.Value.Serialize();
        }

        private static bool IsMouse(KeyCode key)
        {
            return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
        }

        private static bool IsModifier(KeyCode key)
        {
            return key == KeyCode.LeftShift || key == KeyCode.RightShift
                || key == KeyCode.LeftControl || key == KeyCode.RightControl
                || key == KeyCode.LeftAlt || key == KeyCode.RightAlt
                || key == KeyCode.LeftCommand || key == KeyCode.RightCommand
                || key == KeyCode.LeftApple || key == KeyCode.RightApple;
        }

        private static KeyCode[] CurrentModifiers()
        {
            var mods = new List<KeyCode>();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                mods.Add(KeyCode.LeftShift);
            }

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                mods.Add(KeyCode.LeftControl);
            }

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                mods.Add(KeyCode.LeftAlt);
            }

            return mods.ToArray();
        }

        private sealed class PrefRow
        {
            internal GameObject Go;
            internal Toggle Include;
            internal Image Icon;
            internal Text Name;
            internal InputField Qty;
            internal string Key;
        }
    }
}
