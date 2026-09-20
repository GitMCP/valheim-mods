using Jotunn.Managers;
using UnityEngine;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// Player mesh on Njord: dressable like an armour stand, sheathed or drawn,
    /// and idle fidgets, without turning him into a Character.
    /// </summary>
    internal static class NjordLook
    {
        internal static void Attach(GameObject host)
        {
            if (host == null || host.transform.Find("NjordBody") != null)
            {
                return;
            }

            var playerPrefab = PrefabManagerGetPlayer();
            if (playerPrefab == null)
            {
                NjordWarehouseKeeperPlugin.Log.LogWarning("Could not find the Player prefab to dress Njord.");
                return;
            }

            // Instantiate under an inactive holder so Player.Awake never runs:
            // that would register him in s_players, spawn a HUD, and nest a ZNetView.
            var hide = new GameObject("NjordHide");
            hide.SetActive(false);
            var body = Object.Instantiate(playerPrefab, hide.transform);
            body.name = "NjordBody";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = Vector3.one;
            Strip(body);
            WireView(body, host);

            var look = body.GetComponent<NjordLookHook>();
            if (look == null)
            {
                look = body.AddComponent<NjordLookHook>();
            }

            body.transform.SetParent(host.transform, false);
            Object.DestroyImmediate(hide);
            body.SetActive(true);
            look.Bind(host);
        }

        internal static void HideHostVisuals(GameObject host)
        {
            if (host == null)
            {
                return;
            }

            var renderers = host.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || IsBody(renderer.transform))
                {
                    continue;
                }

                renderer.enabled = false;
            }

            var hostAnim = host.GetComponent<Animator>();
            if (hostAnim != null)
            {
                hostAnim.enabled = false;
            }
        }

        internal static void Dress(VisEquipment vis, Container hub = null)
        {
            if (vis == null)
            {
                return;
            }

            var host = hub != null
                ? hub
                : vis.m_nview != null
                    ? vis.m_nview.GetComponent<Container>()
                    : vis.m_nViewOverride != null
                        ? vis.m_nViewOverride.GetComponent<Container>()
                        : null;
            NjordOutfit.Apply(host, vis, vis.GetComponent<Animator>());
        }

        private static void WireView(GameObject body, GameObject host)
        {
            var vis = body.GetComponent<VisEquipment>();
            var view = host == null ? null : host.GetComponent<ZNetView>();
            if (vis == null)
            {
                return;
            }

            vis.m_nViewOverride = view;
            vis.m_nview = view;
            vis.m_playerComponent = null;
            vis.m_isPlayer = true;
            vis.m_isArmorStand = false;
        }

        private static void Strip(GameObject body)
        {
            var behaviours = body.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour is VisEquipment || behaviour is NjordLookHook)
                {
                    continue;
                }

                Object.DestroyImmediate(behaviour, true);
            }

            var views = body.GetComponentsInChildren<ZNetView>(true);
            for (var i = 0; i < views.Length; i++)
            {
                if (views[i] != null)
                {
                    Object.DestroyImmediate(views[i], true);
                }
            }

            var syncs = body.GetComponentsInChildren<ZSyncTransform>(true);
            for (var i = 0; i < syncs.Length; i++)
            {
                if (syncs[i] != null)
                {
                    Object.DestroyImmediate(syncs[i], true);
                }
            }

            var bodies = body.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] != null)
                {
                    Object.DestroyImmediate(bodies[i], true);
                }
            }

            var colliders = body.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Object.DestroyImmediate(colliders[i], true);
                }
            }

            var animator = body.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.fireEvents = false;
                animator.enabled = true;
            }
        }

        private static bool IsBody(Transform t)
        {
            while (t != null)
            {
                if (t.name == "NjordBody")
                {
                    return true;
                }

                t = t.parent;
            }

            return false;
        }

        private static GameObject PrefabManagerGetPlayer()
        {
            return PrefabManager.Instance != null
                ? PrefabManager.Instance.GetPrefab("Player")
                : null;
        }

        internal static string FindArmor(ItemDrop.ItemData.ItemType type, string prefab, params string[] needles)
        {
            var db = ObjectDB.instance;
            if (db == null)
            {
                return prefab;
            }

            if (db.GetItemPrefab(prefab) != null)
            {
                return prefab;
            }

            if (db.m_items == null)
            {
                return prefab;
            }

            for (var i = 0; i < db.m_items.Count; i++)
            {
                var go = db.m_items[i];
                var item = go == null ? null : go.GetComponent<ItemDrop>();
                var shared = item == null ? null : item.m_itemData?.m_shared;
                if (shared == null || shared.m_itemType != type)
                {
                    continue;
                }

                var token = shared.m_name ?? "";
                var name = go.name ?? "";
                for (var n = 0; n < needles.Length; n++)
                {
                    var needle = needles[n];
                    if (token.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return go.name;
                    }
                }
            }

            return prefab;
        }
    }

    internal sealed class NjordLookHook : MonoBehaviour
    {
        private static readonly string[] IdleEmotes =
        {
            "wave",
            "flex",
            "cheer",
            "challenge",
            "thumbsup",
            "nono",
            "point",
            "comehere",
        };

        private const float IdleRange = 14f;

        private GameObject _host;
        private Animator _animator;
        private VisEquipment _vis;
        private float _nextEmote;
        private float _nextApply;

        internal void Bind(GameObject host)
        {
            _host = host;
            _animator = GetComponent<Animator>();
            _vis = GetComponent<VisEquipment>();
            WireAndDress();
            _nextEmote = Time.time + Random.Range(16f, 28f);
        }

        internal void Wave()
        {
            PlayEmote("wave");
        }

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_vis == null)
            {
                _vis = GetComponent<VisEquipment>();
            }
        }

        private void Start()
        {
            WireAndDress();
        }

        private void Update()
        {
            TickPoseKey();
            if (Time.time >= _nextApply)
            {
                _nextApply = Time.time + 0.25f;
                WireAndDress();
            }
            else
            {
                HoldAnimator();
            }

            IdleEmote();
        }

        private void WireAndDress()
        {
            try
            {
                NjordOutfit.Apply(Hub(), _vis, _animator);
            }
            catch (System.Exception ex)
            {
                NjordWarehouseKeeperPlugin.Log.LogWarning("Njord look failed: " + ex);
            }
        }

        private void HoldAnimator()
        {
            var pose = CurrentPose();
            var drawn = pose == NjordPose.Drawn;
            var zdo = Zdo();
            var right = drawn && zdo != null ? zdo.GetInt("njord.handR", 0) : 0;
            var left = drawn && zdo != null ? zdo.GetInt("njord.handL", 0) : 0;
            NjordOutfit.ApplyAnimator(_animator, pose, right, left);
            if (_vis != null && _vis.m_nview != null)
            {
                try
                {
                    _vis.UpdateVisuals();
                }
                catch (System.Exception ex)
                {
                    NjordWarehouseKeeperPlugin.Log.LogWarning("Njord visuals failed: " + ex);
                }
            }
        }

        private void TickPoseKey()
        {
            if (!CanTakePoseInput())
            {
                return;
            }

            if (!ZInput.GetKeyDown(KeyCode.R, false) && !Input.GetKeyDown(KeyCode.R))
            {
                return;
            }

            var hub = Hub();
            if (hub == null)
            {
                return;
            }

            var before = NjordOutfit.ReadPose(hub);
            NjordOutfit.CyclePose(hub);
            var player = Player.m_localPlayer;
            player?.Message(
                MessageHud.MessageType.Center,
                Localization.instance.Localize(NjordOutfit.ActionToken(before)));
        }

        private bool CanTakePoseInput()
        {
            if (Jotunn.Managers.GUIManager.IsHeadless())
            {
                return false;
            }

            try
            {
                if (Console.IsVisible() || Menu.IsVisible() || TextInput.IsVisible())
                {
                    return false;
                }
            }
            catch (System.Exception)
            {
                return false;
            }

            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return false;
            }

            if (UI.NjordWarehouseKeeperPanel.SearchHasFocus())
            {
                return false;
            }

            var hub = Hub();
            if (hub == null)
            {
                return false;
            }

            if (NjordWarehouseKeeperMarker.OpenHub != null)
            {
                return NjordWarehouseKeeperMarker.OpenHub == hub;
            }

            var player = Player.m_localPlayer;
            var hover = player == null ? null : player.GetHoverObject();
            if (hover == null)
            {
                return false;
            }

            var marker = hover.GetComponentInParent<NjordWarehouseKeeperMarker>();
            return marker != null && marker.gameObject == hub.gameObject;
        }

        private void IdleEmote()
        {
            if (Time.time < _nextEmote)
            {
                return;
            }

            _nextEmote = Time.time + Random.Range(16f, 30f);
            if (!PlayerNearby(IdleRange))
            {
                return;
            }

            PlayEmote(IdleEmotes[Random.Range(0, IdleEmotes.Length)]);
        }

        private bool PlayerNearby(float range)
        {
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                return false;
            }

            return Vector3.Distance(player.transform.position, transform.position) <= range;
        }

        private void PlayEmote(string emote)
        {
            if (_animator == null || string.IsNullOrEmpty(emote))
            {
                return;
            }

            _animator.ResetTrigger("emote_stop");
            _animator.SetTrigger("emote_" + emote);
        }

        private NjordPose CurrentPose()
        {
            return NjordOutfit.ReadPose(Hub());
        }

        private Container Hub()
        {
            if (_host != null)
            {
                return _host.GetComponent<Container>();
            }

            var parent = transform.parent;
            return parent == null ? null : parent.GetComponent<Container>();
        }

        private ZDO Zdo()
        {
            var view = NjordOutfit.ViewOf(Hub());
            return view == null || !view.IsValid() ? null : view.GetZDO();
        }
    }
}
