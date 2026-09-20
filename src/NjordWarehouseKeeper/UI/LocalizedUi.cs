using UnityEngine;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Valheim keeps the original <c>$token</c> on each Text and rewrites the
    /// visible string when the language changes. Pass tokens into the controls,
    /// then <see cref="Capture"/> so Njord follows the same table.
    /// </summary>
    internal static class LocalizedUi
    {
        internal static void Capture(Text text, string token)
        {
            if (text == null || string.IsNullOrEmpty(token))
            {
                return;
            }

            text.text = token;
            if (Localization.instance == null)
            {
                return;
            }

            Localization.instance.RemoveTextFromCache(text);
            Localization.instance.Localize(text.transform);
        }

        internal static void CapturePlaceholder(InputField field, string token)
        {
            if (field == null)
            {
                return;
            }

            Capture(field.placeholder as Text, token);
        }

        internal static void CaptureRoot(Transform root)
        {
            if (root != null && Localization.instance != null)
            {
                Localization.instance.Localize(root);
            }
        }

        internal static void RelocalizeRoot(Transform root)
        {
            if (root != null && Localization.instance != null)
            {
                Localization.instance.ReLocalizeAll(root);
            }
        }
    }
}
