using Jotunn.Managers;
using UnityEngine;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// Keeps Njord on the player mesh in a leather tunic and pants, without
    /// turning him into a second Character the HUD or combat systems would
    /// treat as a person.
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

            if (body.GetComponent<LookAt>() == null)
            {
                body.AddComponent<LookAt>();
            }

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

        internal static void Dress(VisEquipment vis)
        {
            if (vis == null)
            {
                return;
            }

            if (vis.m_nview == null && vis.m_nViewOverride != null)
            {
                vis.m_nview = vis.m_nViewOverride;
            }

            if (vis.m_nview == null)
            {
                return;
            }

            vis.SetHelmetItem(0);
            vis.SetShoulderItem(0, 0, 0);
            vis.SetUtilityItem(0);
            vis.SetLeftItem(0, 0, 0);
            vis.SetRightItem(0, 0);
            vis.SetLeftBackItem(0, 0, 0);
            vis.SetRightBackItem(0, 0);
            var chest = FindArmor(ItemDrop.ItemData.ItemType.Chest, "ArmorLeatherChest", "leather");
            var legs = FindArmor(ItemDrop.ItemData.ItemType.Legs, "ArmorLeatherLegs", "leather");
            if (!string.IsNullOrEmpty(chest))
            {
                vis.SetChestItem(chest.GetStableHashCode());
            }

            if (!string.IsNullOrEmpty(legs))
            {
                vis.SetLegItem(legs.GetStableHashCode());
            }

            vis.UpdateVisuals();
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
                if (behaviour == null || behaviour is VisEquipment || behaviour is NjordLookHook || behaviour is LookAt)
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

        private static string FindArmor(ItemDrop.ItemData.ItemType type, string prefab, params string[] needles)
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
        };

        private GameObject _host;
        private Animator _animator;
        private LookAt _lookAt;
        private float _nextEmote;

        internal void Bind(GameObject host)
        {
            _host = host;
            _animator = GetComponent<Animator>();
            _lookAt = GetComponent<LookAt>();
            WireAndDress();
            IdlePose();
            _nextEmote = Time.time + Random.Range(22f, 36f);
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

            if (_lookAt == null)
            {
                _lookAt = GetComponent<LookAt>();
            }
        }

        private void Start()
        {
            WireAndDress();
            IdlePose();
        }

        private void Update()
        {
            IdlePose();
            WatchPlayer();
            if (Time.time >= _nextEmote)
            {
                _nextEmote = Time.time + Random.Range(22f, 36f);
                if (PlayerNearby(12f))
                {
                    PlayEmote(IdleEmotes[Random.Range(0, IdleEmotes.Length)]);
                }
            }
        }

        private void WireAndDress()
        {
            var vis = GetComponent<VisEquipment>();
            var host = _host != null
                ? _host
                : transform.parent != null ? transform.parent.gameObject : null;
            var view = host == null ? null : host.GetComponent<ZNetView>();
            if (vis != null)
            {
                vis.m_nViewOverride = view;
                vis.m_nview = view;
                vis.m_playerComponent = null;
            }

            NjordLook.Dress(vis);
        }

        private void IdlePose()
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetBool("onGround", true);
            _animator.SetBool("encumbered", false);
            _animator.SetBool("crouching", false);
            _animator.SetBool("flying", false);
            _animator.SetBool("inWater", false);
            _animator.SetBool("wakeup", false);
            _animator.SetBool("intro", false);
            _animator.SetBool("dead", false);
            _animator.SetFloat("forward_speed", 0f);
            _animator.SetFloat("sideway_speed", 0f);
            _animator.SetFloat("turn_speed", 0f);
        }

        private void WatchPlayer()
        {
            if (_lookAt == null || GUIManager.IsHeadless())
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null || !PlayerNearby(10f))
            {
                _lookAt.ResetTarget();
                return;
            }

            _lookAt.SetLoockAtTarget(player.GetEyePoint());
        }

        private bool PlayerNearby(float range)
        {
            var player = Player.m_localPlayer;
            if (player == null)
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
    }
}
