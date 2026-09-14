using System.Collections.Generic;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Clearing up: what is on the floor goes in a chest, and what is in a chest goes
    /// with its own kind.
    ///
    /// Sorting is done by one rule, and the rule is the reason it works. A thing belongs
    /// wherever most of that thing already is. Nobody has to declare that the third chest
    /// is the wood chest - it becomes the wood chest the moment it holds more wood than
    /// the others, and then stays it, because every move makes the winner win by more. A
    /// retainer left hauling for an afternoon turns a row of mixed chests into a sorted
    /// one without ever being told the plan, and stops when there is nothing left that
    /// would be better off somewhere else.
    /// </summary>
    internal class HirdmanHaul : HirdmanWork
    {
        private const float Reach = 2f;

        /// <summary>One deliberate act at a time, so the work is watchable.</summary>
        private const float StepInterval = 1.2f;

        private ItemDrop _litter;
        private float _steppedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            // Something on the ground is the most obviously wrong thing in a yard, so it
            // is dealt with before anything inside a chest.
            if (_litter == null)
            {
                _litter = Closest<ItemDrop>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                    d => d.CanPickup(false));
            }

            if (_litter != null)
            {
                if (!body.Approach(dt, _litter.transform.position, Reach))
                {
                    return true;
                }

                body.Take(_litter);
                _litter = null;
                Scoop(body, Reach * 2f, null);
                return true;
            }

            if (Time.time - _steppedAt < StepInterval)
            {
                return Hold(body, order.Anchor, dt);
            }

            var owner = body.Zdo == null ? 0L : HirdmanContract.Read(body.Zdo).Owner;
            var chests = HirdmanStores.Around(order.Anchor, HirdmanPlugin.WorkRadius.Value, owner);
            if (chests.Count == 0)
            {
                return Hold(body, order.Anchor, dt);
            }

            if (Unload(body, chests, dt) || Tidy(body, chests, dt))
            {
                _steppedAt = Time.time;
                return true;
            }

            // Everything is where it should be.
            return Hold(body, order.Anchor, dt);
        }

        /// <summary>Empties the retainer's own arms into the right chests.</summary>
        private static bool Unload(HirdmanBody body, List<Container> chests, float dt)
        {
            var arms = body.Inventory;
            if (arms == null)
            {
                return false;
            }

            foreach (var item in arms.GetAllItems().ToArray())
            {
                if (item == null || HirdmanRetainer.IsKit(item))
                {
                    continue;
                }

                var home = Home(chests, item);
                if (home == null)
                {
                    continue;
                }

                if (body.Approach(dt, home.transform.position, Reach))
                {
                    HirdmanStores.Deposit(home, arms, item);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Picks up one stack that is in the wrong chest. Putting it in the right one is
        /// not this method's problem: it is now in the retainer's arms, and unloading
        /// arms is the first thing that happens next time round.
        /// </summary>
        private static bool Tidy(HirdmanBody body, List<Container> chests, float dt)
        {
            foreach (var chest in chests)
            {
                var contents = HirdmanStores.Contents(chest);
                if (contents == null)
                {
                    continue;
                }

                foreach (var item in contents.GetAllItems().ToArray())
                {
                    if (item == null || !Misplaced(chests, chest, item))
                    {
                        continue;
                    }

                    // Matched by kind rather than by reference: a chest rebuilds its
                    // items from its ZDO whenever it reloads, and a stack that was
                    // picked out a moment ago may be a different object by now. Any
                    // stack of the same thing is equally misplaced anyway.
                    var kind = item.m_shared.m_name;
                    if (body.Approach(dt, chest.transform.position, Reach))
                    {
                        HirdmanStores.Withdraw(chest, body.Inventory,
                            i => i.m_shared != null && i.m_shared.m_name == kind);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is this stack in the wrong chest? Only if some other chest holds strictly
        /// more of the same thing and has room for this too. Strictly, because two
        /// chests holding the same amount would otherwise pass the stack back and forth
        /// for as long as anyone watched.
        /// </summary>
        private static bool Misplaced(List<Container> chests, Container here, ItemDrop.ItemData item)
        {
            var mine = Held(here, item);

            foreach (var chest in chests)
            {
                if (chest == here)
                {
                    continue;
                }

                var contents = HirdmanStores.Contents(chest);
                if (contents != null && contents.CanAddItem(item) && Held(chest, item) > mine)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Where a thing belongs: whichever chest already holds the most of it, or any
        /// chest with room if it is the first of its kind in the steading.
        /// </summary>
        private static Container Home(List<Container> chests, ItemDrop.ItemData item)
        {
            Container fullest = null;
            Container spare = null;
            var most = 0;

            foreach (var chest in chests)
            {
                var contents = HirdmanStores.Contents(chest);
                if (contents == null || !contents.CanAddItem(item))
                {
                    continue;
                }

                if (spare == null)
                {
                    spare = chest;
                }

                var held = contents.CountItems(item.m_shared.m_name, -1, false);
                if (held > most)
                {
                    most = held;
                    fullest = chest;
                }
            }

            return fullest != null ? fullest : spare;
        }

        private static int Held(Container chest, ItemDrop.ItemData item)
        {
            var contents = HirdmanStores.Contents(chest);
            return contents == null ? 0 : contents.CountItems(item.m_shared.m_name, -1, false);
        }
    }
}
