using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// What a retainer says back.
    ///
    /// Speech is deliberately local: the bubble is drawn on the machine of whoever gave
    /// the order and is never sent anywhere. An acknowledgement is feedback, not an
    /// event, and the thing that genuinely has to reach every peer - the order itself -
    /// already travels in the ZDO.
    /// </summary>
    internal static class HirdmanSpeech
    {
        private static readonly Vector3 MouthOffset = new Vector3(0f, 1.8f, 0f);

        private const float CullDistance = 24f;
        private const float SecondsOnScreen = 5f;

        internal static void Say(GameObject speaker, string text)
        {
            if (speaker == null || string.IsNullOrEmpty(text) || Chat.instance == null)
            {
                return;
            }

            var name = speaker.GetComponent<Character>()?.GetHoverName();
            Chat.instance.SetNpcText(
                speaker,
                MouthOffset,
                CullDistance,
                SecondsOnScreen,
                string.IsNullOrEmpty(name) ? "Retainer" : name,
                text,
                large: false);

            // A bubble lasts five seconds. If the player is mid-conversation with this
            // particular retainer, what it said also belongs in the conversation.
            HirdmanChatWindow.Hear(speaker, text);
        }
    }
}
