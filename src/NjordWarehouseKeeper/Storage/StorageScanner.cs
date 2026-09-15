using System.Collections.Generic;
using UnityEngine;

namespace NjordWarehouseKeeper.Storage
{
    /// <summary>
    /// Every <see cref="Container"/> close enough to the hub that the player is allowed
    /// to open. Other keepers are skipped so two in one room do not nest.
    /// Incinerators (auto-destroy-empty) are skipped so a deposit cannot feed a fire.
    /// </summary>
    internal static class StorageScanner
    {
        internal struct Network
        {
            internal List<Container> Chests;
            internal int UsedSlots;
            internal int TotalSlots;
        }

        internal static Network Scan(Container hub)
        {
            var result = new Network
            {
                Chests = new List<Container>(),
                UsedSlots = 0,
                TotalSlots = 0,
            };

            if (hub == null)
            {
                return result;
            }

            var origin = hub.transform.position;
            var radius = NjordWarehouseKeeperPlugin.Radius.Value;
            var playerId = Game.instance != null && Game.instance.GetPlayerProfile() != null
                ? Game.instance.GetPlayerProfile().GetPlayerID()
                : 0L;

            foreach (var container in Object.FindObjectsByType<Container>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!Include(container, hub, origin, radius, playerId))
                {
                    continue;
                }

                result.Chests.Add(container);
                var inventory = container.GetInventory();
                if (inventory == null)
                {
                    continue;
                }

                result.UsedSlots += inventory.NrOfItems();
                result.TotalSlots += inventory.GetWidth() * inventory.GetHeight();
            }

            result.Chests.Sort((a, b) =>
                Vector3.Distance(origin, a.transform.position)
                    .CompareTo(Vector3.Distance(origin, b.transform.position)));

            return result;
        }

        internal static bool Include(Container container, Container hub, Vector3 origin, float radius, long playerId)
        {
            if (container == null || container == hub)
            {
                return false;
            }

            if (container.GetComponent<NjordWarehouseKeeperMarker>() != null)
            {
                return false;
            }

            var view = container.m_nview;
            if (view == null || !view.IsValid())
            {
                return false;
            }

            if (container.m_autoDestroyEmpty)
            {
                return false;
            }

            if (Vector3.Distance(origin, container.transform.position) > radius)
            {
                return false;
            }

            if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, flash: false))
            {
                return false;
            }

            if (!container.CheckAccess(playerId))
            {
                return false;
            }

            if (container.IsInUse() && !container.IsOwner())
            {
                return false;
            }

            if (NjordWarehouseKeeperPlugin.RequireLineOfSight.Value && !HasLineOfSight(origin, container))
            {
                return false;
            }

            return container.GetInventory() != null;
        }

        private static bool HasLineOfSight(Vector3 origin, Container container)
        {
            var from = origin + Vector3.up * 0.6f;
            var to = container.transform.position + Vector3.up * 0.6f;
            if (!Physics.Linecast(from, to, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.collider != null && hit.collider.GetComponentInParent<Container>() == container;
        }
    }
}
