using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Inventory-style item tooltip. Vanilla <see cref="UITooltip"/> waits 0.5s
    /// after pointer-enter; this also requires the cursor <em>and</em> the row
    /// to stay still so a scrolling list does not flash a tooltip on every row
    /// that slides under the mouse. The tip is parented to a front overlay so
    /// the list <see cref="RectMask2D"/> cannot crop it.
    /// </summary>
    internal sealed class HubItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float ShowDelay = 0.5f;
        private const float MoveSlop = 4f;
        private const int OverlaySort = 12000;

        private static GameObject Shown;
        private static HubItemHover Owner;
        private static Transform Overlay;

        internal string Topic;
        internal string Body;

        private bool _over;
        private float _still;
        private Vector2 _origin;
        private Vector2 _rowOrigin;
        private RectTransform _rect;

        public void OnPointerEnter(PointerEventData eventData)
        {
            BeginHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _over = false;
            HideMine();
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
        }

        private void OnDisable()
        {
            _over = false;
            HideMine();
        }

        private void Update()
        {
            var mouse = Mouse();
            if (!PointerOverSelf(mouse))
            {
                if (_over)
                {
                    _over = false;
                    HideMine();
                }

                return;
            }

            if (!_over)
            {
                BeginHover();
                return;
            }

            if ((mouse - _origin).sqrMagnitude > MoveSlop * MoveSlop ||
                (RowPos() - _rowOrigin).sqrMagnitude > MoveSlop * MoveSlop)
            {
                _origin = mouse;
                _rowOrigin = RowPos();
                _still = 0f;
                HideMine();
                return;
            }

            if (string.IsNullOrEmpty(Topic) && string.IsNullOrEmpty(Body))
            {
                return;
            }

            _still += Time.deltaTime;
            if (_still < ShowDelay)
            {
                return;
            }

            Show(mouse);
        }

        internal void Bind(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                BindText("", "");
                return;
            }

            BindText(item.m_shared.m_name, item.GetTooltip());
        }

        internal void BindText(string topic, string body)
        {
            Topic = topic ?? "";
            Body = body ?? "";
            if (string.IsNullOrEmpty(Topic) && string.IsNullOrEmpty(Body))
            {
                HideMine();
                return;
            }

            if (Owner == this && Shown != null)
            {
                ApplyText(Shown);
            }
        }

        internal static void Hide()
        {
            if (Shown != null)
            {
                UnityEngine.Object.Destroy(Shown);
                Shown = null;
            }

            Owner = null;
        }

        private void BeginHover()
        {
            _over = true;
            _still = 0f;
            _origin = Mouse();
            _rowOrigin = RowPos();
            HideMine();
        }

        private void HideMine()
        {
            if (Owner == this)
            {
                Hide();
            }
        }

        private void Show(Vector2 mouse)
        {
            if (Shown != null && Owner == this)
            {
                Shown.transform.position = mouse;
                Clamp(Shown);
                return;
            }

            var prefab = Prefab();
            var parent = OverlayRoot();
            if (prefab == null || parent == null)
            {
                return;
            }

            Hide();
            Shown = UnityEngine.Object.Instantiate(prefab, parent);
            Owner = this;
            IgnoreRaycasts(Shown);
            ApplyText(Shown);
            Shown.transform.position = mouse;
            Clamp(Shown);
            parent.SetAsLastSibling();
        }

        private void ApplyText(GameObject tip)
        {
            var text = FindTmp(tip.transform, "Text");
            if (text != null)
            {
                text.text = Localization.instance.Localize(Body ?? "");
            }

            var topic = FindTmp(tip.transform, "Topic");
            if (topic != null)
            {
                topic.text = Localization.instance.Localize(Topic ?? "");
            }
        }

        private Vector2 RowPos()
        {
            return _rect != null ? (Vector2)_rect.position : Vector2.zero;
        }

        private bool PointerOverSelf(Vector2 screen)
        {
            if (_rect == null)
            {
                return false;
            }

            var canvas = GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(_rect, screen, cam);
        }

        private static Vector2 Mouse()
        {
            return ZInput.instance != null ? (Vector2)ZInput.pointerPosition : (Vector2)Input.mousePosition;
        }

        private static Transform OverlayRoot()
        {
            if (Overlay != null)
            {
                return Overlay;
            }

            Transform parent = null;
            if (GUIManager.CustomGUIFront != null)
            {
                parent = GUIManager.CustomGUIFront.transform;
            }
            else if (InventoryGui.instance != null)
            {
                parent = InventoryGui.instance.transform;
            }

            if (parent == null)
            {
                return null;
            }

            var go = new GameObject(
                "NjordHoverOverlay",
                typeof(RectTransform),
                typeof(Canvas));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var canvas = go.GetComponent<Canvas>();
            var parentCanvas = parent.GetComponent<Canvas>() ?? parent.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvas.renderMode = parentCanvas.renderMode;
                canvas.worldCamera = parentCanvas.worldCamera;
                canvas.sortingLayerID = parentCanvas.sortingLayerID;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = OverlaySort;
            Overlay = go.transform;
            return Overlay;
        }

        private static void IgnoreRaycasts(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var graphics = root.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].raycastTarget = false;
                }
            }
        }

        private static GameObject Prefab()
        {
            var gui = InventoryGui.instance;
            var grid = gui != null ? gui.m_playerGrid : null;
            if (grid == null || grid.m_elementPrefab == null)
            {
                return null;
            }

            var tip = grid.m_elementPrefab.GetComponent<UITooltip>();
            if (tip == null)
            {
                tip = grid.m_elementPrefab.GetComponentInChildren<UITooltip>(true);
            }

            return tip != null ? tip.m_tooltipPrefab : null;
        }

        private static TMP_Text FindTmp(Transform root, string name)
        {
            var child = Utils.FindChild(root, name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static void Clamp(GameObject tip)
        {
            if (tip == null || tip.transform.childCount == 0)
            {
                return;
            }

            var child = tip.transform.GetChild(0) as RectTransform;
            if (child != null)
            {
                Utils.ClampUIToScreen(child);
            }
        }
    }
}
