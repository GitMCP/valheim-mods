using System;
using System.Collections.Generic;
using StorageHub.Storage;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace StorageHub.UI
{
    /// <summary>
    /// Known crafting recipes. Rows the hub cannot afford are greyed out; a
    /// clickable row pulls that craft's ingredients into the pack.
    /// </summary>
    internal static class StorageHubRecipes
    {
        private const float RowHeight = 42f;

        private static GameObject _root;
        private static Transform _rowParent;
        private static readonly List<RecipeRow> _rows = new List<RecipeRow>();

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
            rootRt.offsetMax = new Vector2(-16f, -118f);

            var heading = MakeLabel(
                gui,
                Localization.instance.Localize("$storagehub_recipe_hint"),
                13,
                new Color(1f, 0.9f, 0.75f, 1f));
            var headingRt = heading.GetComponent<RectTransform>();
            headingRt.anchorMin = new Vector2(0f, 1f);
            headingRt.anchorMax = new Vector2(1f, 1f);
            headingRt.pivot = new Vector2(0f, 1f);
            headingRt.anchoredPosition = new Vector2(4f, 0f);
            headingRt.sizeDelta = new Vector2(-8f, 28f);
            heading.horizontalOverflow = HorizontalWrapMode.Wrap;

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
            scrollRt.offsetMax = new Vector2(0f, -32f);

            var scrollView = scroll.GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null)
            {
                scrollView.horizontal = false;
                scrollView.movementType = ScrollRect.MovementType.Clamped;
                scrollView.scrollSensitivity = 60f;
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
        }

        internal static void Refresh()
        {
            if (!IsOpen || _rowParent == null)
            {
                return;
            }

            var visible = KnownRecipes(StorageHubPanel.SearchQuery);
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

        private static List<Recipe> KnownRecipes(string query)
        {
            query = query == null ? "" : query.Trim();
            var result = new List<Recipe>();
            var seen = new HashSet<string>();
            var player = Player.m_localPlayer;
            var db = ObjectDB.instance;
            if (player == null || db == null || db.m_recipes == null)
            {
                return result;
            }

            for (var i = 0; i < db.m_recipes.Count; i++)
            {
                var recipe = db.m_recipes[i];
                if (recipe == null || !recipe.m_enabled || recipe.m_item == null)
                {
                    continue;
                }

                var data = recipe.m_item.m_itemData;
                if (data?.m_shared == null)
                {
                    continue;
                }

                var shared = data.m_shared.m_name;
                if (!player.IsRecipeKnown(shared) || !seen.Add(shared))
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

                result.Add(recipe);
            }

            result.Sort(Compare);
            return result;
        }

        private static int Compare(Recipe a, Recipe b)
        {
            var an = Localization.instance.Localize(a.m_item.m_itemData.m_shared.m_name);
            var bn = Localization.instance.Localize(b.m_item.m_itemData.m_shared.m_name);
            return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
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
            iconRt.sizeDelta = new Vector2(32f, 32f);
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

        private static void BindRow(RecipeRow view, Recipe recipe, List<IndexedStack> listed)
        {
            view.Recipe = recipe;
            var data = recipe.m_item.m_itemData;
            if (view.Icon != null)
            {
                view.Icon.sprite = data != null ? data.GetIcon() : null;
                view.Icon.enabled = view.Icon.sprite != null;
            }

            if (view.Name != null && data?.m_shared != null)
            {
                view.Name.text = Localization.instance.Localize(data.m_shared.m_name);
            }

            var can = StorageNetwork.CanAffordRecipe(listed, recipe);
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
            if (view == null || view.Recipe == null || StorageHubPanel.IsDragging())
            {
                if (StorageHubPanel.IsDragging())
                {
                    StorageHubPanel.DepositDragged();
                }

                return;
            }

            StorageNetwork.WithdrawRecipe(Player.m_localPlayer, StorageHubMarker.OpenHub, view.Recipe);
            Refresh();
        }

        private static Text MakeLabel(GUIManager gui, string text, int size, Color color)
        {
            var go = gui.CreateText(
                text,
                _root.transform,
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
            label.alignment = TextAnchor.UpperLeft;
            label.raycastTarget = false;
            return label;
        }

        private sealed class RecipeRow
        {
            internal GameObject Go;
            internal Button Button;
            internal Image Icon;
            internal Text Name;
            internal Recipe Recipe;
        }
    }
}
