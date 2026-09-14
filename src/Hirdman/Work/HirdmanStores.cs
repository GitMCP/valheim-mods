using System.Collections.Generic;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Using the chests around a steading.
    ///
    /// Three jobs need this and none of them should have to know how a chest works. What
    /// a chest is, underneath, is an <see cref="Inventory"/> serialised into one ZDO
    /// field: reading it is free, and writing it means owning the ZDO first and saying so
    /// afterwards. Every rule about who may open what lives here too, because a retainer
    /// helping itself to a stranger's private chest is the kind of thing that ends a
    /// server, and it is much easier to be sure of when it is written once.
    /// </summary>
    internal static class HirdmanStores
    {
        /// <summary>
        /// The chests a retainer may use, nearest first is not needed - callers want them
        /// all, to choose by what is inside rather than by distance.
        /// </summary>
        internal static List<Container> Around(Vector3 centre, float radius, long owner)
        {
            var found = new List<Container>();

            foreach (var collider in Physics.OverlapSphere(centre, radius))
            {
                var container = collider.GetComponentInParent<Container>();
                if (container == null || found.Contains(container) ||
                    container.GetComponentInParent<HirdmanTag>() != null ||
                    !Allowed(container, owner))
                {
                    continue;
                }

                found.Add(container);
            }

            return found;
        }

        /// <summary>
        /// A chest is fair game if it is nobody's in particular, or its employer's. The
        /// game's own rule is reused rather than reimplemented, so a retainer's idea of
        /// a private chest cannot drift from the player's.
        /// </summary>
        internal static bool Allowed(Container container, long owner)
        {
            var nview = container.m_nview;
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            // Somebody has it open. Editing it underneath them loses whichever of the two
            // edits is written second.
            if (container.IsInUse())
            {
                return false;
            }

            if (container.m_privacy == Container.PrivacySetting.Public)
            {
                return true;
            }

            return container.m_piece != null && container.CheckAccess(owner);
        }

        /// <summary>
        /// Reads what is in a chest. Valid to call from any peer: the contents arrive in
        /// the ZDO like everything else.
        /// </summary>
        internal static Inventory Contents(Container container)
        {
            return container == null ? null : container.GetInventory();
        }

        /// <summary>
        /// Takes something out of a chest, whole stack and all. Stacks are never split:
        /// a retainer that carries three of a thing because three is what would fit is a
        /// chest sorter nobody asked for.
        /// </summary>
        internal static ItemDrop.ItemData Withdraw(
            Container container, Inventory into, System.Func<ItemDrop.ItemData, bool> wanted)
        {
            var from = Contents(container);
            if (from == null || into == null || !Claim(container))
            {
                return null;
            }

            foreach (var item in from.GetAllItems().ToArray())
            {
                if (item == null || !wanted(item) || !into.CanAddItem(item))
                {
                    continue;
                }

                into.MoveItemToThis(from, item);
                Commit(container);
                return item;
            }

            return null;
        }

        /// <summary>Puts something away.</summary>
        internal static bool Deposit(Container container, Inventory from, ItemDrop.ItemData item)
        {
            var into = Contents(container);
            if (into == null || item == null || !into.CanAddItem(item) || !Claim(container))
            {
                return false;
            }

            into.MoveItemToThis(from, item);
            Commit(container);
            return true;
        }

        /// <summary>
        /// Only the peer that owns a ZDO may write it, and a chest standing in a field
        /// belongs to whoever happened to load it.
        /// </summary>
        private static bool Claim(Container container)
        {
            var nview = container.m_nview;
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            return nview.IsOwner();
        }

        /// <summary>
        /// Writes the chest back. Without this the edit lives only in this peer's copy
        /// and is thrown away the next time the chest reloads itself from its ZDO.
        /// </summary>
        private static void Commit(Container container)
        {
            container.Save();
        }
    }
}
