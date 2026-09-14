using System.Collections.Generic;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Clearing up: what is on the floor goes in a chest, and what is in a chest goes
    /// with its own kind. Named, it is also "put the wood in the chest" and "get iron
    /// from the chest": the same walking-up-to-a-box work, pointed at one thing.
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

        private const string TakeMark = "take:";
        private const string PutMark = "put:";

        private ItemDrop _litter;
        private float _steppedAt;
        private float _askedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            bool take;
            bool put;
            string item;
            Read(order.Subject, out take, out put, out item);

            if (take)
            {
                return Fetch(body, order, item, dt);
            }

            if (put)
            {
                return Stow(body, order, item, dt);
            }

            return TidyYard(body, order, dt);
        }

        /// <summary>
        /// Turns a sentence and the words left in it into what the haul job stores:
        /// <c>take:wood</c>, <c>put:wood</c>, or just <c>wood</c> for ordinary sorting.
        /// </summary>
        internal static string Label(string sentence, string subject)
        {
            var item = Strip(subject);
            var from = Mentions(sentence, "from");
            var take = Mentions(sentence, "take", "get", "fetch", "bring") || from;
            var put = Mentions(sentence, "put", "store", "stash", "into");

            if (take && (!put || from))
            {
                return TakeMark + item;
            }

            if (put)
            {
                return PutMark + item;
            }

            return item;
        }

        internal static void Read(string subject, out bool take, out bool put, out string item)
        {
            take = false;
            put = false;
            item = subject ?? string.Empty;

            if (item.StartsWith(TakeMark, System.StringComparison.Ordinal))
            {
                take = true;
                item = item.Substring(TakeMark.Length);
                return;
            }

            if (item.StartsWith(PutMark, System.StringComparison.Ordinal))
            {
                put = true;
                item = item.Substring(PutMark.Length);
            }
        }

        /// <summary>Take a named thing out of the chests, and off the floor if it is there.</summary>
        private bool Fetch(HirdmanBody body, HirdmanOrder order, string item, float dt)
        {
            if (_litter == null)
            {
                _litter = Closest<ItemDrop>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                    d => d != null && HirdmanCatalog.Answers(item, d.gameObject));
            }

            if (_litter != null)
            {
                return PickUp(body, dt);
            }

            var owner = body.Zdo == null ? 0L : HirdmanContract.Read(body.Zdo).Owner;
            var chests = HirdmanStores.Around(order.Anchor, HirdmanPlugin.WorkRadius.Value, owner);
            foreach (var chest in chests)
            {
                var contents = HirdmanStores.Contents(chest);
                if (contents == null || !Has(contents, item))
                {
                    continue;
                }

                if (!body.Approach(dt, chest.transform.position, Reach))
                {
                    return true;
                }

                var taken = HirdmanStores.Withdraw(chest, body.Inventory,
                    i => HirdmanCatalog.Answers(item, i));
                if (taken == null)
                {
                    Need(body, item);
                }

                return true;
            }

            if (!Has(body.Inventory, item))
            {
                Need(body, item);
            }

            return Hold(body, order.Anchor, dt);
        }

        /// <summary>Put a named thing, or everything that is not kit, into the chests.</summary>
        private bool Stow(HirdmanBody body, HirdmanOrder order, string item, float dt)
        {
            if (_litter == null)
            {
                _litter = Closest<ItemDrop>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                    d => d != null && HirdmanCatalog.Answers(item, d.gameObject));
            }

            if (_litter != null)
            {
                return PickUp(body, dt);
            }

            var owner = body.Zdo == null ? 0L : HirdmanContract.Read(body.Zdo).Owner;
            var chests = HirdmanStores.Around(order.Anchor, HirdmanPlugin.WorkRadius.Value, owner);
            if (chests.Count == 0)
            {
                Need(body, "a chest");
                return Hold(body, order.Anchor, dt);
            }

            var arms = body.Inventory;
            if (arms == null)
            {
                return Hold(body, order.Anchor, dt);
            }

            foreach (var held in arms.GetAllItems().ToArray())
            {
                if (held == null || HirdmanRetainer.Keep(held) || !HirdmanCatalog.Answers(item, held))
                {
                    continue;
                }

                var home = Home(chests, held);
                if (home == null)
                {
                    continue;
                }

                if (body.Approach(dt, home.transform.position, Reach))
                {
                    HirdmanStores.Deposit(home, arms, held);
                }

                return true;
            }

            return Hold(body, order.Anchor, dt);
        }

        private bool TidyYard(HirdmanBody body, HirdmanOrder order, float dt)
        {
            // Something on the ground is the most obviously wrong thing in a yard, so it
            // is dealt with before anything inside a chest.
            if (_litter == null)
            {
                _litter = Closest<ItemDrop>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value, null);
            }

            if (_litter != null)
            {
                return PickUp(body, dt);
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

        private bool PickUp(HirdmanBody body, float dt)
        {
            if (_litter == null)
            {
                return true;
            }

            if (!body.Approach(dt, _litter.transform.position, Reach))
            {
                return true;
            }

            var drop = _litter;
            if (!body.Take(drop))
            {
                // Still waiting to own it. Give up only once it is ours and still will
                // not go in, which is a full bag rather than a delayed hand-over.
                if (drop != null && drop.CanPickup(false))
                {
                    _litter = null;
                }

                return true;
            }

            _litter = null;
            Scoop(body, Reach * 2f, null);
            return true;
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
                if (item == null || HirdmanRetainer.Keep(item))
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

        private static bool Has(Inventory inventory, string item)
        {
            if (inventory == null)
            {
                return false;
            }

            foreach (var held in inventory.GetAllItems())
            {
                if (HirdmanCatalog.Answers(item, held))
                {
                    return true;
                }
            }

            return false;
        }

        private void Need(HirdmanBody body, string what)
        {
            if (Time.time - _askedAt < 12f)
            {
                return;
            }

            _askedAt = Time.time;
            if (what == "a chest")
            {
                body.Say("There's no chest here.");
                return;
            }

            body.Say(string.IsNullOrEmpty(what) ? "I can't find that." : $"I can't find {what}.");
        }

        private static string Strip(string subject)
        {
            if (string.IsNullOrEmpty(subject))
            {
                return string.Empty;
            }

            var kept = new List<string>();
            foreach (var word in subject.Split(' '))
            {
                if (word.Length < 3 || IsHaulWord(word))
                {
                    continue;
                }

                kept.Add(word);
            }

            return string.Join(" ", kept.ToArray());
        }

        private static bool IsHaulWord(string word)
        {
            switch (word)
            {
                case "put":
                case "take":
                case "get":
                case "fetch":
                case "bring":
                case "store":
                case "stash":
                case "chest":
                case "chests":
                case "tidy":
                case "sort":
                case "haul":
                case "organise":
                case "organize":
                case "things":
                case "stuff":
                case "items":
                case "away":
                    return true;
                default:
                    return false;
            }
        }

        private static bool Mentions(string sentence, params string[] words)
        {
            if (string.IsNullOrEmpty(sentence))
            {
                return false;
            }

            foreach (var word in words)
            {
                if (sentence.IndexOf(word, System.StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
