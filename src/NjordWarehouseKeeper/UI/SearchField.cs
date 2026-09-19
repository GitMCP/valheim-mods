using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Puts a clear (X) control on the right of a search field and keeps typed
    /// text from running under it.
    /// </summary>
    internal static class SearchField
    {
        private const string ClearName = "NjordSearchClear";
        private const float ClearSize = 16f;
        private const float ClearInset = 6f;
        private const float TextRightPad = 24f;

        internal static void Decorate(InputField field, Action onClear)
        {
            if (field == null)
            {
                return;
            }

            HubSprites.Load();
            PadText(field.textComponent);
            PadText(field.placeholder as Text);
            var clear = EnsureClear(field, onClear);
            Sync(field, clear);
        }

        internal static void Sync(InputField field)
        {
            if (field == null)
            {
                return;
            }

            var clear = field.transform.Find(ClearName);
            Sync(field, clear != null ? clear.gameObject : null);
        }

        internal static void SetText(InputField field, string value, UnityAction<string> listener)
        {
            if (field == null)
            {
                return;
            }

            value = value ?? "";
            if (field.text == value)
            {
                Sync(field);
                return;
            }

            if (listener != null)
            {
                field.onValueChanged.RemoveListener(listener);
            }

            field.text = value;
            if (listener != null)
            {
                field.onValueChanged.AddListener(listener);
            }

            Sync(field);
        }

        private static void Sync(InputField field, GameObject clear)
        {
            if (clear == null || field == null)
            {
                return;
            }

            clear.SetActive(!string.IsNullOrEmpty(field.text));
            clear.transform.SetAsLastSibling();
        }

        private static void PadText(Text text)
        {
            if (text == null)
            {
                return;
            }

            var rt = text.rectTransform;
            rt.offsetMax = new Vector2(-TextRightPad, rt.offsetMax.y);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private static GameObject EnsureClear(InputField field, Action onClear)
        {
            var existing = field.transform.Find(ClearName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(
                ClearName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(field.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(ClearSize, ClearSize);
            rt.anchoredPosition = new Vector2(-ClearInset, 0f);

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.sprite = HubSprites.Cross;
            image.color = new Color(1f, 0.92f, 0.78f, 0.9f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                if (onClear != null)
                {
                    onClear();
                    return;
                }

                field.text = "";
            });
            return go;
        }
    }
}
