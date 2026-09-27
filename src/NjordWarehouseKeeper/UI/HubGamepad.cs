using NjordWarehouseKeeper.Client;
using UnityEngine;

namespace NjordWarehouseKeeper.UI
{
    /// <summary>
    /// Gamepad while Njord is open. The stick cursor already clicks Unity
    /// buttons; this moves the lists, maps face buttons onto the tiny star
    /// and dress controls, and keeps B from closing the whole inventory
    /// while search or Preferences has focus.
    /// </summary>
    internal static class HubGamepad
    {
        private static int _eatClicks;
        private static bool _fromPad;

        internal static bool EatDuplicateClick()
        {
            if (_fromPad)
            {
                return false;
            }

            if (_eatClicks <= 0)
            {
                return false;
            }

            _eatClicks--;
            return true;
        }

        internal static void Tick()
        {
            if (!HubInput.Gamepad() || !NjordWarehouseKeeperPanel.IsHubOpen)
            {
                return;
            }

            if (NjordWarehouseKeeperPanel.SearchHasFocus())
            {
                if (HubInput.Down("JoyButtonB"))
                {
                    NjordWarehouseKeeperPanel.UnfocusSearch();
                    HubInput.Eat("JoyButtonB");
                    HubInput.Eat("JoyUse");
                }

                return;
            }

            if (NjordWarehouseKeeperPanel.PrefsOpen)
            {
                if (HubInput.Down("JoyButtonB"))
                {
                    NjordWarehouseKeeperPanel.SetPrefsOpenFromPad(false);
                    HubInput.Eat("JoyButtonB");
                    HubInput.Eat("JoyButtonY");
                    HubInput.Eat("JoyUse");
                }

                return;
            }

            var overItems = NjordWarehouseKeeperPanel.PointerOverList();
            var overRecipes = NjordWarehouseKeeperRecipes.PointerOver();
            if (!overItems && !overRecipes)
            {
                return;
            }

            var scroll = HubInput.Scroll();
            if (Mathf.Abs(scroll) > 0.2f)
            {
                if (overRecipes)
                {
                    NjordWarehouseKeeperRecipes.ScrollBy(scroll);
                }
                else
                {
                    NjordWarehouseKeeperPanel.ScrollBy(scroll);
                }
            }

            var vertical = HubInput.Vertical();
            var horizontal = HubInput.Horizontal();
            if (overRecipes)
            {
                if (vertical != 0)
                {
                    NjordWarehouseKeeperRecipes.MoveSelection(-vertical);
                    EatNav();
                }

                if (horizontal != 0)
                {
                    NjordWarehouseKeeperRecipes.CycleStation(horizontal);
                    EatNav();
                }

                if (HubInput.Down("JoyButtonX") || HubInput.Down("JoyButtonA"))
                {
                    NjordWarehouseKeeperRecipes.WithdrawSelected();
                    HubInput.Eat("JoyButtonX");
                    HubInput.Eat("JoyButtonA");
                    _eatClicks = 1;
                }

                return;
            }

            if (vertical != 0)
            {
                NjordWarehouseKeeperPanel.MovePadRow(-vertical);
                EatNav();
            }

            if (horizontal != 0)
            {
                NjordWarehouseKeeperPanel.CycleCategory(horizontal);
                EatNav();
            }

            var row = NjordWarehouseKeeperPanel.PadTargetRow();
            if (HubInput.Down("JoyButtonA"))
            {
                Activate(row);
                HubInput.Eat("JoyButtonA");
            }

            if (HubInput.Down("JoyButtonX"))
            {
                NjordWarehouseKeeperPanel.FavouriteRow(row);
                HubInput.Eat("JoyButtonX");
            }

            if (HubInput.Down("JoyButtonY"))
            {
                NjordWarehouseKeeperPanel.DressRow(row);
                HubInput.Eat("JoyButtonY");
            }
        }

        private static void Activate(int row)
        {
            _fromPad = true;
            try
            {
                NjordWarehouseKeeperPanel.ActivateRow(row);
            }
            finally
            {
                _fromPad = false;
                _eatClicks = 1;
            }
        }

        private static void EatNav()
        {
            HubInput.Eat("JoyDPadUp");
            HubInput.Eat("JoyDPadDown");
            HubInput.Eat("JoyDPadLeft");
            HubInput.Eat("JoyDPadRight");
        }
    }
}
