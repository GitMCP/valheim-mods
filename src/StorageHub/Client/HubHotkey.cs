using Jotunn.Managers;
using StorageHub.Storage;
using StorageHub.UI;
using UnityEngine;

namespace StorageHub.Client
{
    /// <summary>
    /// Client hotkey: while standing in a hub's radius, Deposit then Resupply
    /// without opening the panel.
    /// </summary>
    internal static class HubHotkey
    {
        internal static void Tick()
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }

            if (StorageHubPrefs.IsCapturingHotkey)
            {
                return;
            }

            var shortcut = ClientPreferences.RestockHotkey;
            if (shortcut == null || !shortcut.Value.IsDown())
            {
                return;
            }

            if (Ignore())
            {
                return;
            }

            var player = Player.m_localPlayer;
            var hub = StorageNetwork.FindHubInRange(player);
            if (hub == null)
            {
                player?.Message(MessageHud.MessageType.Center, "$storagehub_hotkey_norange");
                return;
            }

            StorageNetwork.Restock(player, hub);
            if (StorageHubMarker.OpenHub == hub)
            {
                StorageHubPanel.RefreshAfterRemote();
            }
        }

        private static bool Ignore()
        {
            if (Console.IsVisible() || Menu.IsVisible() || TextInput.IsVisible())
            {
                return true;
            }

            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return true;
            }

            if (StorageHubPanel.SearchHasFocus())
            {
                return true;
            }

            var player = Player.m_localPlayer;
            return player == null || player.IsDead() || player.InCutscene() || player.IsTeleporting();
        }
    }
}
