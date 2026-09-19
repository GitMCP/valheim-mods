using NjordWarehouseKeeper.Client;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Puts Njord's favourite star on each occupied pack slot so a player can
    /// star an item from their own inventory, not only from his list. Same
    /// keys as the list: deposit skip and the Favourites filter both see them.
    /// </summary>
    internal static class InventoryFavouriteStars
    {
        private const string StarName = "NjordFavouriteStar";
        private const float Size = 16f;

        internal static void Sync(InventoryGrid grid)
        {
            var gui = InventoryGui.instance;
            if (gui == null || grid == null || grid != gui.m_playerGrid)
            {
                return;
            }

            HubSprites.Load();
            var inventory = grid.GetInventory();
            var elements = grid.m_elements;
            if (elements == null)
            {
                return;
            }

            for (var i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element == null)
                {
                    continue;
                }

                var item = inventory != null
                    ? inventory.GetItemAt(element.Position.x, element.Position.y)
                    : null;
                Bind(EnsureStar(element), item);
            }
        }

        internal static bool PointerOverStar()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = ZInput.instance != null
                    ? (Vector2)ZInput.pointerPosition
                    : (Vector2)Input.mousePosition,
            };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            for (var i = 0; i < hits.Count; i++)
            {
                if (hits[i].gameObject != null
                    && hits[i].gameObject.GetComponent<PackFavouriteStar>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static PackFavouriteStar EnsureStar(InventoryElement element)
        {
            var existing = element.GetComponentInChildren<PackFavouriteStar>(true);
            if (existing != null)
            {
                existing.transform.SetAsLastSibling();
                return existing;
            }

            var go = new GameObject(
                StarName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(PackFavouriteStar));
            go.transform.SetParent(element.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(Size, Size);
            rt.anchoredPosition = new Vector2(-2f, -2f);

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.sprite = HubSprites.StarEmpty;
            image.color = new Color(1f, 1f, 1f, 0.8f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var star = go.GetComponent<PackFavouriteStar>();
            star.Image = image;
            button.onClick.AddListener(star.OnClicked);
            return star;
        }

        private static void Bind(PackFavouriteStar star, ItemDrop.ItemData item)
        {
            if (star == null)
            {
                return;
            }

            var key = ItemKey.Of(item);
            star.Key = key;
            var show = !string.IsNullOrEmpty(key);
            star.gameObject.SetActive(show);
            if (!show || star.Image == null)
            {
                return;
            }

            var favourite = ClientPreferences.IsFavourite(key);
            star.Image.sprite = favourite ? HubSprites.StarFilled : HubSprites.StarEmpty;
            star.Image.color = favourite
                ? new Color(1f, 0.82f, 0.28f, 1f)
                : new Color(1f, 1f, 1f, 0.8f);
        }
    }

    internal sealed class PackFavouriteStar : MonoBehaviour
    {
        internal Image Image;
        internal string Key;

        internal void OnClicked()
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            if (NjordWarehouseKeeperPanel.IsDragging())
            {
                NjordWarehouseKeeperPanel.DepositDragged();
                return;
            }

            var gui = InventoryGui.instance;
            if (gui != null && gui.m_dragItem != null)
            {
                return;
            }

            ClientPreferences.SetFavourite(Key, !ClientPreferences.IsFavourite(Key));
            var favourite = ClientPreferences.IsFavourite(Key);
            if (Image != null)
            {
                Image.sprite = favourite ? HubSprites.StarFilled : HubSprites.StarEmpty;
                Image.color = favourite
                    ? new Color(1f, 0.82f, 0.28f, 1f)
                    : new Color(1f, 1f, 1f, 0.8f);
            }

            if (gui != null)
            {
                InventoryFavouriteStars.Sync(gui.m_playerGrid);
            }

            NjordWarehouseKeeperPanel.RefreshAfterRemote();
        }
    }
}
