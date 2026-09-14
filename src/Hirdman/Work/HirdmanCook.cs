using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Working the fires: raw food out of the chests and onto them, cooked food off
    /// before it burns.
    ///
    /// What a station will accept is its own business and it already knows - every
    /// cooking station and oven carries the list of what turns into what - so this never
    /// names a food. It asks the station.
    ///
    /// Taking food off is done by calling the station's own handler rather than the
    /// interaction a player triggers, because that interaction reaches for the local
    /// player to award cooking skill and roll for a bonus portion. A retainer has no
    /// skill to raise and there may be no local player at all, so it takes the plain
    /// portion and leaves the bonus to the cook who earned it.
    /// </summary>
    internal class HirdmanCook : HirdmanWork
    {
        private const float Reach = 2.2f;
        private const float StepInterval = 1.5f;

        private float _steppedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            // Anything spat out by a station a moment ago.
            Scoop(body, Reach * 2f, null);

            if (Time.time - _steppedAt < StepInterval)
            {
                return Hold(body, order.Anchor, dt);
            }

            var station = Closest<CookingStation>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                s => s.m_nview != null && s.m_nview.IsValid() && (Done(s) || Room(s)));

            if (station == null)
            {
                return Fetch(body, order, dt) || Hold(body, order.Anchor, dt);
            }

            if (!body.Approach(dt, station.transform.position, Reach))
            {
                return true;
            }

            _steppedAt = Time.time;

            if (Done(station))
            {
                Serve(body, station);
                return true;
            }

            Feed(body, station);
            return true;
        }

        private static bool Done(CookingStation station)
        {
            return station.HaveDoneItem();
        }

        private static bool Room(CookingStation station)
        {
            return station.GetFreeSlot() != -1;
        }

        /// <summary>Takes a cooked portion off, onto the ground, and then picks it up.</summary>
        private static void Serve(HirdmanBody body, CookingStation station)
        {
            // Sent to whichever peer holds the station rather than taken over, so that a
            // player standing at the same fire is not interrupted. Only an ownerless
            // station is claimed, which is what the game does for its own interactions.
            Attend(station);
            station.m_nview.InvokeRPC("RPC_RemoveDoneItem", body.Position, 1);
        }

        /// <summary>
        /// A message to a station nobody owns goes nowhere, and the ingredient that was
        /// taken out of the retainer's hands to send it is gone.
        /// </summary>
        private static void Attend(CookingStation station)
        {
            if (!station.m_nview.HasOwner())
            {
                station.m_nview.ClaimOwnership();
            }
        }

        /// <summary>Puts one raw thing on, if the retainer is carrying anything the fire wants.</summary>
        private static void Feed(HirdmanBody body, CookingStation station)
        {
            var arms = body.Inventory;
            if (arms == null)
            {
                return;
            }

            Stoke(station, arms);

            var raw = station.FindCookableItem(arms);
            if (raw != null)
            {
                station.CookItem(body.Humanoid, raw);
            }
        }

        /// <summary>
        /// Keeps a fuelled station burning. Ovens want wood; a cooking station over an
        /// open fire wants nothing and says so.
        /// </summary>
        private static void Stoke(CookingStation station, Inventory arms)
        {
            if (!station.m_useFuel || station.m_fuelItem == null ||
                station.GetFuel() >= station.m_maxFuel - 1f)
            {
                return;
            }

            var fuel = arms.GetItem(station.m_fuelItem.gameObject.name, -1, true);
            if (fuel == null)
            {
                return;
            }

            Attend(station);
            arms.RemoveOneItem(fuel);
            station.m_nview.InvokeRPC("RPC_AddFuel");
        }

        /// <summary>
        /// Goes to the chests for something worth cooking, or for the wood to cook it
        /// with. What counts as worth cooking is whatever some station nearby has a
        /// recipe for.
        /// </summary>
        private static bool Fetch(HirdmanBody body, HirdmanOrder order, float dt)
        {
            var owner = body.Zdo == null ? 0L : HirdmanContract.Read(body.Zdo).Owner;
            var radius = HirdmanPlugin.WorkRadius.Value;

            var kitchen = Closest<CookingStation>(body, order.Anchor, radius, null);
            if (kitchen == null)
            {
                return false;
            }

            foreach (var chest in HirdmanStores.Around(order.Anchor, radius, owner))
            {
                var contents = HirdmanStores.Contents(chest);
                if (contents == null)
                {
                    continue;
                }

                if (kitchen.FindCookableItem(contents) == null && !NeedsFuelFrom(kitchen, contents))
                {
                    continue;
                }

                if (body.Approach(dt, chest.transform.position, Reach))
                {
                    HirdmanStores.Withdraw(chest, body.Inventory,
                        item => Wanted(kitchen, item, order.Subject));
                }

                return true;
            }

            return false;
        }

        private static bool NeedsFuelFrom(CookingStation station, Inventory contents)
        {
            return station.m_useFuel && station.m_fuelItem != null &&
                   station.GetFuel() < station.m_maxFuel - 1f &&
                   contents.GetItem(station.m_fuelItem.gameObject.name, -1, true) != null;
        }

        private static bool Wanted(CookingStation station, ItemDrop.ItemData item, string subject)
        {
            if (item == null || item.m_dropPrefab == null)
            {
                return false;
            }

            if (station.m_useFuel && station.m_fuelItem != null &&
                item.m_dropPrefab.name == station.m_fuelItem.gameObject.name)
            {
                return true;
            }

            return station.IsItemAllowed(item) &&
                   HirdmanCatalog.Answers(subject, item.m_dropPrefab);
        }
    }
}
