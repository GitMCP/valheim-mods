using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// The owner's view of a retainer's pack, drawn in the game's own inventory window
    /// the way a chest is.
    ///
    /// <see cref="InventoryGui"/> will only bind its second grid to a
    /// <see cref="Container"/>, and putting a real chest component on a retainer would
    /// give it a second inventory that fights the one it already carries. The window is
    /// therefore shown empty of a container and this class fills the grid from the
    /// retainer's own bag for as long as it stays open.
    /// </summary>
    internal static class HirdmanPack
    {
        private static Humanoid _holder;
        private static string _name;
        private static bool _resetView;

        internal static bool IsOpen => _holder != null;

        internal static Humanoid Holder => _holder;

        internal static Inventory Inventory => _holder == null ? null : _holder.GetInventory();

        /// <summary>
        /// Opens the pack for the player who owns this retainer. Ownership of the
        /// creature is taken so that moving items writes through to the bag's ZDO;
        /// unlike an order, rummaging a pack is supposed to pause them.
        /// </summary>
        internal static bool Open(Player player, GameObject retainer)
        {
            if (player == null || retainer == null || !HirdmanContract.Of(retainer).BelongsTo(player))
            {
                return false;
            }

            var nview = retainer.GetComponent<ZNetView>();
            var humanoid = retainer.GetComponent<Humanoid>();
            if (nview == null || !nview.IsValid() || humanoid == null || humanoid.GetInventory() == null)
            {
                return false;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            _holder = humanoid;
            _name = HirdmanNames.Of(retainer);
            _resetView = true;

            if (InventoryGui.instance != null)
            {
                InventoryGui.instance.Show(null);
            }

            return true;
        }

        internal static void Close()
        {
            if (_holder != null)
            {
                var brain = _holder.GetComponent<HirdmanBrain>();
                if (brain != null)
                {
                    brain.Stow();
                }
            }

            _holder = null;
            _name = null;
            _resetView = false;
        }

        /// <summary>
        /// Draws the pack into the container half of the inventory window. Returns
        /// false so vanilla's own container update is skipped for this frame.
        /// </summary>
        internal static bool Draw(InventoryGui gui, Player player)
        {
            if (_holder == null)
            {
                return true;
            }

            if (player == null || player.IsDead() || _holder == null ||
                Vector3.Distance(_holder.transform.position, player.transform.position) > gui.m_autoCloseDistance)
            {
                Close();
                gui.Hide();
                return false;
            }

            var inventory = _holder.GetInventory();
            if (inventory == null)
            {
                Close();
                gui.Hide();
                return false;
            }

            gui.m_container.gameObject.SetActive(true);
            gui.m_containerGrid.UpdateInventory(inventory, null, gui.m_dragItem);
            gui.m_containerName.text = _name;
            gui.m_containerWeight.text = Mathf.CeilToInt(inventory.GetTotalWeight()).ToString();

            if (_resetView)
            {
                gui.m_containerGrid.ResetView();
                _resetView = false;
            }

            return false;
        }
    }
}
