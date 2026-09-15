using UnityEngine;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// Occasional speech bubbles, the same idea as Haldor: a greeting when you
    /// walk up, a line now and then while you linger, a nod when you leave.
    /// </summary>
    internal sealed class NjordTalk : MonoBehaviour
    {
        private static readonly Vector3 Mouth = new Vector3(0f, 1.85f, 0f);
        private const float GreetRange = 8f;
        private const float TalkRange = 12f;
        private const float LeaveRange = 14f;
        private const float BubbleSeconds = 4.5f;
        private const float CullDistance = 24f;

        private static readonly string[] Greets =
        {
            "$njord_talk_greet_1",
            "$njord_talk_greet_2",
            "$njord_talk_greet_3",
        };

        private static readonly string[] Idle =
        {
            "$njord_talk_idle_1",
            "$njord_talk_idle_2",
            "$njord_talk_idle_3",
            "$njord_talk_idle_4",
            "$njord_talk_idle_5",
            "$njord_talk_idle_6",
            "$njord_talk_idle_7",
            "$njord_talk_idle_8",
        };

        private static readonly string[] Goodbyes =
        {
            "$njord_talk_bye_1",
            "$njord_talk_bye_2",
            "$njord_talk_bye_3",
        };

        private bool _near;
        private float _nextTalk;
        private NjordLookHook _look;

        private void Awake()
        {
            _look = GetComponentInChildren<NjordLookHook>(true);
        }

        private void Update()
        {
            if (GUIManagerHeadless())
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null || Chat.instance == null)
            {
                return;
            }

            var dist = Vector3.Distance(player.transform.position, transform.position);
            if (!_near)
            {
                if (dist <= GreetRange)
                {
                    _near = true;
                    Say(Pick(Greets));
                    _look?.Wave();
                    _nextTalk = Time.time + Random.Range(10f, 16f);
                }

                return;
            }

            if (dist > LeaveRange)
            {
                Say(Pick(Goodbyes));
                _near = false;
                return;
            }

            if (dist <= TalkRange && Time.time >= _nextTalk)
            {
                Say(Pick(Idle));
                _nextTalk = Time.time + Random.Range(14f, 24f);
            }
        }

        private void Say(string token)
        {
            if (string.IsNullOrEmpty(token) || Chat.instance == null)
            {
                return;
            }

            Chat.instance.SetNpcText(
                gameObject,
                Mouth,
                CullDistance,
                BubbleSeconds,
                Localization.instance.Localize("$njord_npc"),
                Localization.instance.Localize(token),
                large: false);
        }

        private static string Pick(string[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                return "";
            }

            return lines[Random.Range(0, lines.Length)];
        }

        private static bool GUIManagerHeadless()
        {
            return Jotunn.Managers.GUIManager.IsHeadless();
        }
    }
}
