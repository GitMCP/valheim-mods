using System.Collections.Generic;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Keeps hold of what a retainer is carrying.
    ///
    /// The game does not. A player's pack is written into their character file and a
    /// chest's contents into its ZDO, but the inventory of any other humanoid lives only
    /// in memory: it is filled from the creature's default items when the creature
    /// loads, and thrown away with the object when it unloads. Nothing in the game
    /// suffers for that, because nothing in the game ever gives a humanoid anything.
    ///
    /// A retainer is given things constantly, and it is the whole point of the mod, so
    /// the bag has to outlive the object. Without this a retainer sent to gather comes
    /// back with a full load, the player wanders two zones off, and every berry it
    /// picked is gone - along with its axe - by the time they walk back. It is written
    /// the same way a chest writes itself, which is also what makes it travel when the
    /// retainer changes hands between peers.
    /// </summary>
    internal class HirdmanBag
    {
        private const string Key = "hird_bag";

        /// <summary>
        /// How often the bag is written. Often enough that a crash costs a few berries,
        /// seldom enough that a retainer chopping steadily is not a stream of network
        /// traffic to everyone who can see it.
        /// </summary>
        private const float SaveInterval = 3f;

        private readonly ZNetView _nview;
        private readonly Humanoid _humanoid;

        private byte[] _written;
        private float _savedAt;

        internal HirdmanBag(GameObject retainer)
        {
            _nview = retainer.GetComponent<ZNetView>();
            _humanoid = retainer.GetComponent<Humanoid>();
        }

        private Inventory Inventory => _humanoid == null ? null : _humanoid.GetInventory();

        private bool Ready => _nview != null && _nview.IsValid() && Inventory != null;

        /// <summary>
        /// Gives a retainer back the things it had, replacing whatever it was issued on
        /// the way in.
        ///
        /// Replacing rather than adding is the important part. The game hands a creature
        /// its default items every single time it loads, without asking whether it has
        /// been loaded before, so a retainer that kept both would grow another axe and
        /// another pickaxe every time the player walked past. The saved bag already has
        /// the tools in it, because they were in it when it was written.
        /// </summary>
        internal void Load()
        {
            if (!Ready)
            {
                return;
            }

            var saved = _nview.GetZDO().GetByteArray(Key, null);
            if (saved == null)
            {
                // Newly hired. What the game just handed it is right, and is also what
                // the first save will record.
                return;
            }

            // Anything in its hands is about to stop existing, and the hands would go on
            // pointing at it.
            _humanoid.UnequipAllItems();

            Inventory.Load(new ZPackage(saved));
            _written = saved;

            Rearm();
        }

        /// <summary>
        /// Puts back into the retainer's hands whatever was in them.
        ///
        /// The game does this for a player after reading their file, and for the same
        /// reason: the saved items remember that they were equipped, but remembering is
        /// all they do, and what a creature is holding lives in separate fields that a
        /// load does not touch. Skipping it would leave a retainer standing empty-handed
        /// until its next swing, holding an axe only in principle.
        /// </summary>
        private void Rearm()
        {
            var held = new List<ItemDrop.ItemData>();
            foreach (var item in Inventory.GetAllItems())
            {
                if (item != null && item.m_equipped)
                {
                    held.Add(item);
                }
            }

            foreach (var item in held)
            {
                // Cleared first because the game refuses to equip what it believes is
                // already equipped.
                item.m_equipped = false;
                _humanoid.EquipItem(item, false);
            }
        }

        /// <summary>Writes the bag if it is time and there is anything new to write.</summary>
        internal void Keep()
        {
            if (Time.time - _savedAt < SaveInterval)
            {
                return;
            }

            _savedAt = Time.time;
            Save();
        }

        internal void Save()
        {
            if (!Ready || !_nview.IsOwner())
            {
                return;
            }

            var package = new ZPackage();
            Inventory.Save(package);
            var bytes = package.GetArray();

            // An unchanged bag written again still counts as a change, and is sent to
            // every peer that can see the retainer. Most of a retainer's life is spent
            // walking somewhere with the same things in its arms.
            if (Same(bytes, _written))
            {
                return;
            }

            _nview.GetZDO().Set(Key, bytes);
            _written = bytes;
        }

        /// <summary>
        /// Forgets what was last written, so that the next save writes whatever the
        /// retainer holds now. Used when this peer stops owning it, because the peer
        /// that takes over will change the bag without telling this one.
        /// </summary>
        internal void Forget()
        {
            _written = null;
        }

        private static bool Same(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
