using System;
using System.Collections.Generic;
using StorageHub.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace StorageHub.UI
{
    /// <summary>
    /// Known crafting recipes and hammer pieces. Rows the hub cannot afford are
    /// greyed out; a clickable row pulls that craft's ingredients into the pack.
    /// </summary>
    internal static class StorageHubRecipes
    {
        private const float RowHeight = 52f;
        private const string AllKey = "all";
        private const string HandKey = "hand";
        private const string HammerKey = "hammer";

        private static GameObject _root;
        private static Transform _rowParent;
        private static Button _stationButton;
        private static Text _stationLabel;
        private static GameObject _stationMenu;
        private static GameObject _stationCatcher;
        private static Transform _stationMenuParent;
        private static string _station = AllKey;
        private static readonly List<RecipeRow> _rows = new List<RecipeRow>();
        private static readonly List<StationOpt> _stations = new List<StationOpt>();

        internal static bool IsOpen
        {
            get { return _root != null && _root.activeSelf; }
        }

        internal static void Build(Transform parent, GUIManager gui)
        {
            _root = new GameObject("Recipes", typeof(RectTransform));
            _root.transform.SetParent(parent, false);
            var rootRt = _root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.offsetMin = new Vector2(16f, 18f);
            rootRt.offsetMax = new Vector2(-16f, -StorageHubPanel.ContentTop);

            _stationButton = MakeStationButton(gui);
            BuildStationMenu(gui);

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
            scrollRt.offsetMax = new Vector2(0f, -40f);

            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                StorageHubPanel.FitScrollView(scrollView, StorageHubPanel.ListScrollSensitivity());
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
            SetMenuOpen(false);
            if (on)
            {
                Refresh();
            }
        }

        internal static void Refresh()
        {
            if (!IsOpen || _rowParent == null)
            {
                return;
            }

            RebuildStations();
            PaintStationButton();

            var visible = KnownCrafts(StorageHubPanel.SearchQuery);
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

            var listed = StorageHubMarker.OpenHub != null
                ? StorageNetwork.ListItems(StorageHubMarker.OpenHub)
                : new List<IndexedStack>();
            for (var i = 0; i < visible.Count; i++)
            {
                BindRow(_rows[i], visible[i], listed);
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
                32f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(0f, -32f);
            rt.offsetMax = Vector2.zero;
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
            menuRt.anchoredPosition = new Vector2(0f, -34f);
            menuRt.sizeDelta = new Vector2(0f, 220f);
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
                StorageHubPanel.FitScrollView(scrollView, 80f);
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
            if (StorageHubPanel.IsDragging())
            {
                StorageHubPanel.DepositDragged();
                return;
            }

            SetMenuOpen(_stationMenu == null || !_stationMenu.activeSelf);
        }

        private static void SetMenuOpen(bool on)
        {
            if (_stationMenu != null)
            {
                _stationMenu.SetActive(on);
                if (on)
                {
                    _stationMenu.transform.SetAsLastSibling();
                }
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
            _stations.Add(new StationOpt { Key = AllKey, Token = "$storagehub_station_all" });
            _stations.Add(new StationOpt { Key = HandKey, Token = "$storagehub_station_hand" });
            _stations.Add(new StationOpt { Key = HammerKey, Token = "$storagehub_station_hammer" });

            var names = new List<string>();
            var seen = new HashSet<string>();
            CollectStations(names, seen);
            names.Sort(CompareLocalized);
            for (var i = 0; i < names.Count; i++)
            {
                _stations.Add(new StationOpt { Key = names[i], Token = names[i] });
            }

            RebuildStationButtons();
        }

        private static void CollectStations(List<string> names, HashSet<string> seen)
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

        private static void AddStation(CraftingStation station, List<string> names, HashSet<string> seen)
        {
            if (station == null || string.IsNullOrEmpty(station.m_name) || !seen.Add(station.m_name))
            {
                return;
            }

            names.Add(station.m_name);
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
            SetMenuOpen(false);
            Refresh();
        }

        private static void PaintStationButton()
        {
            if (_stationLabel == null)
            {
                return;
            }

            var token = "$storagehub_station_all";
            for (var i = 0; i < _stations.Count; i++)
            {
                if (_stations[i].Key == _station)
                {
                    token = _stations[i].Token;
                    break;
                }
            }

            _stationLabel.text = Localization.instance.Localize(token) + "  ▾";
        }

        private static List<CraftOffer> KnownCrafts(string query)
        {
            query = query == null ? "" : query.Trim();
            var result = new List<CraftOffer>();
            var seen = new HashSet<string>();
            if (_station != HammerKey)
            {
                AddRecipes(result, seen, query);
            }

            if (_station == AllKey || _station == HammerKey)
            {
                AddHammerPieces(result, seen, query);
            }

            result.Sort(CompareOffers);
            return result;
        }

        private static void AddRecipes(List<CraftOffer> result, HashSet<string> seen, string query)
        {
            var player = Player.m_localPlayer;
            var db = ObjectDB.instance;
            if (player == null || db == null || db.m_recipes == null)
            {
                return;
            }

            for (var i = 0; i < db.m_recipes.Count; i++)
            {
                var recipe = db.m_recipes[i];
                if (recipe == null || !recipe.m_enabled || recipe.m_item == null)
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
                if (!player.IsRecipeKnown(shared) || !seen.Add("item:" + shared))
                {
                    continue;
                }

                if (data.m_shared.m_dlc.Length > 0 &&
                    DLCMan.instance != null &&
                    !DLCMan.instance.IsDLCInstalled(data.m_shared.m_dlc))
                {
                    continue;
                }

                var name = Localization.instance.Localize(shared);
                if (query.Length > 0 && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                result.Add(new CraftOffer
                {
                    Name = name,
                    Icon = data.GetIcon(),
                    Recipe = recipe,
                });
            }
        }

        private static bool StationMatches(CraftingStation station)
        {
            if (_station == AllKey)
            {
                return true;
            }

            if (_station == HandKey)
            {
                return station == null;
            }

            if (_station == HammerKey)
            {
                return false;
            }

            return station != null && station.m_name == _station;
        }

        private static void AddHammerPieces(List<CraftOffer> result, HashSet<string> seen, string query)
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

                var name = Localization.instance.Localize(piece.m_name);
                if (query.Length > 0 && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                result.Add(new CraftOffer
                {
                    Name = name,
                    Icon = piece.m_icon,
                    Piece = piece,
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
                if (req != null && req.m_resItem != null && req.GetAmount(1) > 0)
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
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static RecipeRow MakeRow()
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

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(40f, 40f);
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
            nameRt.offsetMax = new Vector2(-12f, -4f);
            var name = nameGo.GetComponent<Text>();
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.raycastTarget = false;

            var view = new RecipeRow
            {
                Go = row,
                Button = row.GetComponent<Button>(),
                Icon = icon,
                Name = name,
            };
            view.Button.onClick.AddListener(() => OnClicked(view));
            return view;
        }

        private static void BindRow(RecipeRow view, CraftOffer offer, List<IndexedStack> listed)
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

            var can = offer.Recipe != null
                ? StorageNetwork.CanAffordRecipe(listed, offer.Recipe)
                : StorageNetwork.CanAffordPiece(listed, offer.Piece);
            view.Button.interactable = can;
            var dim = can ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
            if (view.Icon != null)
            {
                view.Icon.color = dim;
            }

            if (view.Name != null)
            {
                view.Name.color = can
                    ? GUIManager.Instance.ValheimOrange
                    : new Color(0.55f, 0.55f, 0.55f, 1f);
            }

            var colors = view.Button.colors;
            colors.normalColor = can ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.highlightedColor = can ? new Color(1f, 0.9f, 0.7f, 1f) : colors.normalColor;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.8f);
            view.Button.colors = colors;
        }

        private static void OnClicked(RecipeRow view)
        {
            if (view == null || view.Offer == null || StorageHubPanel.IsDragging())
            {
                if (StorageHubPanel.IsDragging())
                {
                    StorageHubPanel.DepositDragged();
                }

                return;
            }

            if (view.Offer.Recipe != null)
            {
                StorageNetwork.WithdrawRecipe(Player.m_localPlayer, StorageHubMarker.OpenHub, view.Offer.Recipe);
            }
            else
            {
                StorageNetwork.WithdrawPiece(Player.m_localPlayer, StorageHubMarker.OpenHub, view.Offer.Piece);
            }

            StorageHubPanel.RefreshAfterRemote();
        }

        private sealed class CraftOffer
        {
            internal string Name;
            internal Sprite Icon;
            internal Recipe Recipe;
            internal Piece Piece;
        }

        private sealed class StationOpt
        {
            internal string Key;
            internal string Token;
        }

        private sealed class RecipeRow
        {
            internal GameObject Go;
            internal Button Button;
            internal Image Icon;
            internal Text Name;
            internal CraftOffer Offer;
        }
    }
}
