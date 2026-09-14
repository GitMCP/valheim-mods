using System.Collections.Generic;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// The chores around a standing job: emptying a full bag at home, mending a worn
    /// tool at a bench, walking back to the trees.
    ///
    /// An order is still one closed job. Language is not in the loop, and cannot be:
    /// a frame is 16 ms and a model is not. What a person told to chop wood all
    /// afternoon would also do — put the wood away, sharpen the axe, come back — is
    /// written here, so "chop wood" means keep chopping until a different order
    /// arrives, not fell the nearest stand and stop.
    /// </summary>
    internal sealed class HirdmanShift
    {
        private const float ChestReach = 2f;
        private const float BenchReach = 2.2f;
        private const float WornBelow = 0.25f;
        private const float FullAt = 0.55f;

        private enum Chore
        {
            None,
            Stash,
            Mend,
            Return,
        }

        private Chore _chore;
        private Vector3 _site;
        private bool _marked;
        private float _askedAt;

        /// <summary>Remembers where the work is so a trip home can find its way back.</summary>
        internal void Mark(Vector3 site)
        {
            _site = site;
            _marked = true;
        }

        /// <summary>
        /// Spends the frame on a chore if one is due. Returns true when the job itself
        /// should wait.
        /// </summary>
        internal bool Busy(HirdmanBody body, HirdmanOrder order, float dt, Skills.SkillType tool,
            bool canLeave)
        {
            if (_chore == Chore.None && canLeave)
            {
                if (NeedsMend(body, tool))
                {
                    _chore = Chore.Mend;
                }
                else if (Packed(body))
                {
                    _chore = Chore.Stash;
                }
            }

            switch (_chore)
            {
                case Chore.Stash:
                    return Stash(body, order, dt);
                case Chore.Mend:
                    return Mend(body, order, dt, tool);
                case Chore.Return:
                    return WalkBack(body, order, dt);
                default:
                    return false;
            }
        }

        private bool Stash(HirdmanBody body, HirdmanOrder order, float dt)
        {
            var home = body.Home;
            var owner = body.Zdo == null ? 0L : HirdmanContract.Read(body.Zdo).Owner;
            var radius = HirdmanPlugin.WorkRadius.Value;
            var chests = HirdmanStores.Around(home, radius, owner);
            if (chests.Count == 0)
            {
                chests = HirdmanStores.Around(order.Anchor, radius, owner);
            }

            if (chests.Count == 0)
            {
                if (!body.Near(home, radius))
                {
                    body.Approach(dt, home, HirdmanBody.ArriveDistance);
                    return true;
                }

                Ask(body, "I have no chest to put this in.");
                _chore = Chore.None;
                return false;
            }

            var arms = body.Inventory;
            if (arms == null)
            {
                _chore = Chore.Return;
                return true;
            }

            foreach (var held in arms.GetAllItems().ToArray())
            {
                if (held == null || HirdmanRetainer.Keep(held))
                {
                    continue;
                }

                var chest = HirdmanStores.BestFor(chests, held);
                if (chest == null)
                {
                    continue;
                }

                if (!body.Approach(dt, chest.transform.position, ChestReach))
                {
                    return true;
                }

                HirdmanStores.Deposit(chest, arms, held);
                return true;
            }

            _chore = Chore.Return;
            return true;
        }

        private bool Mend(HirdmanBody body, HirdmanOrder order, float dt, Skills.SkillType skill)
        {
            var worn = WornTool(body, skill);
            if (worn == null)
            {
                _chore = Chore.Return;
                return true;
            }

            var bench = ClosestBench(body, order, worn);
            if (bench == null)
            {
                Ask(body, "My tools need a bench.");
                _chore = Chore.None;
                return false;
            }

            if (!body.Approach(dt, bench.transform.position, BenchReach))
            {
                return true;
            }

            var mended = 0;
            var arms = body.Inventory;
            if (arms != null)
            {
                foreach (var item in arms.GetAllItems())
                {
                    if (item != null && Fits(bench, item) && item.GetDurabilityPercentage() < 1f)
                    {
                        item.m_durability = item.GetMaxDurability();
                        mended++;
                    }
                }
            }

            if (mended == 0)
            {
                Ask(body, "This bench will not mend what I have.");
                _chore = Chore.None;
                return false;
            }

            _chore = Chore.Return;
            return true;
        }

        private bool WalkBack(HirdmanBody body, HirdmanOrder order, float dt)
        {
            var site = _marked ? _site : order.Anchor;
            if (body.Near(site, HirdmanPlugin.WorkRadius.Value * 0.4f))
            {
                _chore = Chore.None;
                return false;
            }

            body.Approach(dt, site, HirdmanBody.ArriveDistance);
            return true;
        }

        private static bool Packed(HirdmanBody body)
        {
            return body.Burden() >= FullAt;
        }

        private static bool NeedsMend(HirdmanBody body, Skills.SkillType skill)
        {
            return WornTool(body, skill) != null;
        }

        private static ItemDrop.ItemData WornTool(HirdmanBody body, Skills.SkillType skill)
        {
            var inventory = body.Inventory;
            if (inventory == null)
            {
                return null;
            }

            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || !item.m_shared.m_useDurability)
                {
                    continue;
                }

                if (item.m_shared.m_skillType != skill)
                {
                    continue;
                }

                if (item.GetDurabilityPercentage() <= WornBelow)
                {
                    return item;
                }
            }

            return null;
        }

        private static CraftingStation ClosestBench(HirdmanBody body, HirdmanOrder order,
            ItemDrop.ItemData item)
        {
            var roam = HirdmanWork.Roam;
            var bench = ClosestAt(body.Position, body.Position, roam, item)
                        ?? ClosestAt(body.Position, order.Anchor, roam, item)
                        ?? ClosestAt(body.Position, body.Home, roam, item);
            return bench;
        }

        private static CraftingStation ClosestAt(Vector3 from, Vector3 centre, float radius,
            ItemDrop.ItemData item)
        {
            CraftingStation closest = null;
            var shortest = float.MaxValue;

            foreach (var collider in Physics.OverlapSphere(centre, radius))
            {
                var bench = collider.GetComponentInParent<CraftingStation>();
                if (bench == null || !bench.m_canRepair || !Fits(bench, item))
                {
                    continue;
                }

                var distance = Vector3.Distance(from, bench.transform.position);
                if (distance < shortest)
                {
                    shortest = distance;
                    closest = bench;
                }
            }

            return closest;
        }

        private static bool Fits(CraftingStation bench, ItemDrop.ItemData item)
        {
            if (bench == null || item?.m_shared == null || !item.m_shared.m_canBeReparied)
            {
                return false;
            }

            if (ObjectDB.instance == null)
            {
                return bench.m_canRepair;
            }

            var recipe = ObjectDB.instance.GetRecipe(item);
            if (recipe == null)
            {
                return bench.m_canRepair;
            }

            var repair = recipe.m_repairStation != null ? recipe.m_repairStation.m_name : null;
            var craft = recipe.m_craftingStation != null ? recipe.m_craftingStation.m_name : null;
            if (repair == null && craft == null)
            {
                return false;
            }

            if ((repair != null && repair == bench.m_name) || (craft != null && craft == bench.m_name))
            {
                return Mathf.Min(bench.GetLevel(), 4) >= recipe.m_minStationLevel;
            }

            return false;
        }

        private void Ask(HirdmanBody body, string line)
        {
            if (Time.time - _askedAt < 12f)
            {
                return;
            }

            _askedAt = Time.time;
            body.Say(line);
        }
    }
}
