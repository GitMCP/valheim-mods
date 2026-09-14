using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Hirdman
{
    /// <summary>
    /// A window for talking to a retainer.
    ///
    /// The console command that came before this proved the machinery, but it is the
    /// wrong thing to hand a player: it is a debug console, it is modal over the whole
    /// game, and typing <c>hird</c> before every sentence is a constant reminder that
    /// you are operating a mod rather than talking to somebody. A window that opens on
    /// one key, names who is listening, and keeps what was said reads as a conversation,
    /// which is the entire ambition of the mod.
    ///
    /// It is rebuilt from nothing every time it opens. A cached panel would have to
    /// survive scene changes, the main menu, and Jotunn rebuilding its GUI roots, and
    /// the whole thing costs less to construct than one frame of the game it is drawn
    /// over.
    /// </summary>
    internal static class HirdmanChatWindow
    {
        private const int Backlog = 12;

        private static readonly List<string> Said = new List<string>();

        private static GameObject _panel;
        private static InputField _entry;
        private static Text _transcript;
        private static GameObject _listener;

        internal static bool IsOpen => _panel != null;

        /// <summary>
        /// Watches for the key, and for the reasons to close again. Driven from the
        /// plugin's own update rather than from a component on the panel, because the
        /// key that opens a window cannot be handled by the window.
        /// </summary>
        internal static void Poll()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            if (IsOpen)
            {
                // The retainer walked off, died, or the world went away underneath it.
                if (_listener == null || Player.m_localPlayer == null ||
                    Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                }

                return;
            }

            if (!HirdmanPlugin.TalkKey.Value.IsDown() || Busy())
            {
                return;
            }

            var player = Player.m_localPlayer;
            var retainer = HirdmanRoster.Listening(player, HirdmanRoster.EarshotRadius);
            if (retainer == null)
            {
                if (player != null)
                {
                    player.Message(MessageHud.MessageType.Center, "Nobody is close enough to hear you.");
                }

                return;
            }

            Open(retainer);
        }

        /// <summary>
        /// Is the player already typing somewhere, or busy with another window? Opening
        /// on G while somebody is writing the word "going" in chat would be unforgivable.
        /// </summary>
        private static bool Busy()
        {
            if (Player.m_localPlayer == null || Minimap.IsOpen())
            {
                return true;
            }

            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return true;
            }

            if (Console.IsVisible() || TextInput.IsVisible())
            {
                return true;
            }

            return InventoryGui.IsVisible() || StoreGui.IsVisible() || Menu.IsVisible();
        }

        internal static void Open(GameObject retainer)
        {
            Close();

            var root = GUIManager.CustomGUIFront;
            if (root == null || GUIManager.Instance == null)
            {
                HirdmanPlugin.Log.LogWarning("No GUI root to draw the window on.");
                return;
            }

            _listener = retainer;

            _panel = GUIManager.Instance.CreateWoodpanel(
                root.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f),
                620f,
                380f,
                true);

            GUIManager.Instance.CreateText(
                HirdmanNames.Of(retainer),
                _panel.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -32f),
                GUIManager.Instance.AveriaSerifBold,
                22,
                GUIManager.Instance.ValheimOrange,
                true,
                Color.black,
                560f,
                40f,
                false);

            _transcript = GUIManager.Instance.CreateText(
                Transcript(),
                _panel.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -70f),
                GUIManager.Instance.AveriaSerif,
                16,
                GUIManager.Instance.ValheimBeige,
                true,
                Color.black,
                560f,
                230f,
                false).GetComponent<Text>();

            // Reading a conversation from the bottom is what every chat window does, and
            // it is the only way the newest line is in the same place every time.
            _transcript.alignment = TextAnchor.LowerLeft;

            _entry = GUIManager.Instance.CreateInputField(
                _panel.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 46f),
                InputField.ContentType.Standard,
                "Say something...",
                16,
                560f,
                40f).GetComponent<InputField>();

            _entry.onEndEdit.AddListener(OnEntered);

            // Without this the game keeps reading the keyboard, and typing "wait here"
            // makes the player jump, swing and open their inventory.
            GUIManager.BlockInput(true);
            _entry.Select();
            _entry.ActivateInputField();
        }

        internal static void Close()
        {
            if (_panel != null)
            {
                Object.Destroy(_panel);
            }

            _panel = null;
            _entry = null;
            _transcript = null;
            _listener = null;
            GUIManager.BlockInput(false);
        }

        /// <summary>
        /// Puts a line in the window if it is open and the speaker is the one being
        /// spoken to, so a retainer's answer lands where the question was asked. Speech
        /// bubbles carry on regardless; this is the transcript, not the speech.
        /// </summary>
        internal static void Hear(GameObject speaker, string line)
        {
            if (!IsOpen || speaker != _listener || string.IsNullOrEmpty(line))
            {
                return;
            }

            Add($"<color=#E0C080>{HirdmanNames.Of(speaker)}:</color> {line}");
        }

        private static void OnEntered(string text)
        {
            // onEndEdit also fires when the field loses focus, and an empty box or a
            // click elsewhere is not somebody speaking.
            if (!IsOpen || string.IsNullOrEmpty(text) ||
                !(Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)))
            {
                return;
            }

            var player = Player.m_localPlayer;
            var retainer = _listener;

            _entry.text = string.Empty;
            _entry.ActivateInputField();

            Add($"<color=#FFFFFF>You:</color> {text}");

            HirdmanInterpreter.Interpret(text, player, retainer, (understood, order, reply) =>
            {
                HirdmanSpeech.Say(retainer, reply);

                if (understood && !HirdmanBrain.Give(retainer, order))
                {
                    Hear(retainer, "I can't right now.");
                }
            });
        }

        private static void Add(string line)
        {
            Said.Add(line);
            while (Said.Count > Backlog)
            {
                Said.RemoveAt(0);
            }

            if (_transcript != null)
            {
                _transcript.text = Transcript();
            }
        }

        private static string Transcript()
        {
            return Said.Count == 0
                ? "<color=#808080>They are waiting for you to say something.</color>"
                : string.Join("\n", Said.ToArray());
        }
    }
}
