using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace Hirdman.Work
{
    /// <summary>
    /// Working a field: lifting what is ready, and sowing what is in the chests.
    ///
    /// Which seed grows into what is never written down here. Every sapling in the game
    /// carries both halves of the answer already - a <see cref="Plant"/> saying what it
    /// turns into, and a <see cref="Piece"/> saying what it costs to plant - so the
    /// mapping is read out of the game once and covers every crop, including ones added
    /// by other mods and ones added by the next update.
    ///
    /// Sowing deliberately refuses more often than a player would. The game decides
    /// whether a spot is plantable inside its own placement code, which needs a player
    /// holding a cultivator, so the checks here are the conservative subset that can be
    /// made from outside: cultivated ground where the crop wants it, and nothing else
    /// growing within the room the crop needs. A retainer that skips a plantable square
    /// costs a few seconds; one that plants in a wall costs a save.
    /// </summary>
    internal class HirdmanFarm : HirdmanWork
    {
        private const float Reach = 2f;
        private const float StepInterval = 1.2f;

        /// <summary>How many spots to try before giving up on this pass.</summary>
        private const int Probes = 24;

        private static readonly Dictionary<GameObject, string> Empty =
            new Dictionary<GameObject, string>();

        private static Dictionary<GameObject, string> _crops;

        private Pickable _ripe;
        private float _steppedAt;

        internal override bool Run(HirdmanBody body, HirdmanOrder order, float dt)
        {
            // Harvest first. A field of ready crops is what a player looks at, and
            // sowing around them would only crowd the ground.
            if (_ripe == null)
            {
                _ripe = Closest<Pickable>(body, order.Anchor, HirdmanPlugin.WorkRadius.Value,
                    p => p.CanBePicked() && IsCrop(p.gameObject) &&
                         HirdmanCatalog.Answers(order.Subject, p.gameObject));
            }

            if (_ripe != null)
            {
                if (!body.Approach(dt, _ripe.transform.position, Reach))
                {
                    return true;
                }

                Harvest(body, _ripe);
                _ripe = null;
                Scoop(body, Reach * 2f, null);
                return true;
            }

            if (Time.time - _steppedAt < StepInterval)
            {
                return Hold(body, order.Anchor, dt);
            }

            _steppedAt = Time.time;
            return Sow(body, order, dt) || Hold(body, order.Anchor, dt);
        }

        private static void Harvest(HirdmanBody body, Pickable crop)
        {
            if (Player.m_localPlayer == null)
            {
                return;
            }

            var nview = crop.m_nview;
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            if (nview.IsOwner())
            {
                crop.Interact(body.Humanoid, false, false);
            }
        }

        /// <summary>
        /// Plants one seed, fetching it from a chest if the retainer is not already
        /// carrying some.
        /// </summary>
        private static bool Sow(HirdmanBody body, HirdmanOrder order, float dt)
        {
            var arms = body.Inventory;
            if (arms == null)
            {
                return false;
            }

            var sapling = Carrying(arms, order.Subject);
            if (sapling == null)
            {
                return Fetch(body, order, dt);
            }

            var spot = Furrow(order.Anchor, sapling);
            if (!spot.HasValue)
            {
                return false;
            }

            if (!body.Approach(dt, spot.Value, Reach))
            {
                return true;
            }

            Plant(body, arms, sapling, spot.Value);
            return true;
        }

        /// <summary>The first crop the retainer already has the seed for.</summary>
        private static GameObject Carrying(Inventory arms, string subject)
        {
            foreach (var crop in Crops())
            {
                if (!HirdmanCatalog.Answers(subject, crop.Key))
                {
                    continue;
                }

                if (HirdmanBody.Find(arms, crop.Value) != null)
                {
                    return crop.Key;
                }
            }

            return null;
        }

        /// <summary>Goes to a chest for seed.</summary>
        private static bool Fetch(HirdmanBody body, HirdmanOrder order, float dt)
        {
            var owner = body.Zdo == null ? 0L : HirdmanContract.Read(body.Zdo).Owner;
            var seeds = new HashSet<string>();
            foreach (var crop in Crops())
            {
                if (HirdmanCatalog.Answers(order.Subject, crop.Key))
                {
                    seeds.Add(crop.Value);
                }
            }

            foreach (var chest in HirdmanStores.Around(order.Anchor, HirdmanPlugin.WorkRadius.Value, owner))
            {
                var contents = HirdmanStores.Contents(chest);
                if (contents == null || !Holds(contents, seeds))
                {
                    continue;
                }

                if (body.Approach(dt, chest.transform.position, Reach))
                {
                    HirdmanStores.Withdraw(chest, body.Inventory,
                        item => item.m_dropPrefab != null && seeds.Contains(item.m_dropPrefab.name));
                }

                return true;
            }

            return false;
        }

        private static bool Holds(Inventory contents, HashSet<string> seeds)
        {
            foreach (var item in contents.GetAllItems())
            {
                if (item != null && item.m_dropPrefab != null && seeds.Contains(item.m_dropPrefab.name))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Plant(HirdmanBody body, Inventory arms, GameObject sapling, Vector3 spot)
        {
            var seed = HirdmanBody.Find(arms, Crops()[sapling]);
            if (seed == null)
            {
                return;
            }

            var sown = Object.Instantiate(sapling, spot, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            // Without a creator the crop belongs to nobody, which matters to wards and
            // to anything that asks who built what.
            var piece = sown.GetComponent<Piece>();
            if (piece != null && body.Zdo != null)
            {
                piece.SetCreator(HirdmanContract.Read(body.Zdo).Owner, default(PlatformUserID));
            }

            arms.RemoveOneItem(seed);
        }

        /// <summary>
        /// Somewhere to put a seed: cultivated if the crop insists on it, clear of
        /// everything else growing, and on ground rather than in the air.
        /// </summary>
        private static Vector3? Furrow(Vector3 anchor, GameObject sapling)
        {
            var plant = sapling.GetComponent<Plant>();
            var room = plant == null ? 0.5f : plant.m_growRadius;
            var cultivated = plant != null && plant.m_needCultivatedGround;

            for (var probe = 0; probe < Probes; probe++)
            {
                var spot = Somewhere(anchor, 1f, HirdmanPlugin.WorkRadius.Value * 0.5f);

                if (cultivated)
                {
                    var ground = Heightmap.FindHeightmap(spot);
                    if (ground == null || !ground.IsCultivated(spot))
                    {
                        continue;
                    }
                }

                if (Crowded(spot, room))
                {
                    continue;
                }

                return spot;
            }

            return null;
        }

        private static bool Crowded(Vector3 spot, float room)
        {
            foreach (var collider in Physics.OverlapSphere(spot, room))
            {
                var thing = collider.transform.root;
                if (thing.GetComponentInChildren<Plant>() != null ||
                    thing.GetComponentInChildren<Pickable>() != null ||
                    thing.GetComponentInChildren<Piece>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCrop(GameObject pickable)
        {
            var name = HirdmanCatalog.PrefabName(pickable);

            foreach (var crop in Crops())
            {
                var grown = crop.Key.GetComponent<Plant>();
                if (grown == null || grown.m_grownPrefabs == null)
                {
                    continue;
                }

                foreach (var result in grown.m_grownPrefabs)
                {
                    if (result != null && result.name == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Throws the crop table away when a world closes. Its keys are prefabs out of
        /// that world's <see cref="ZNetScene"/>, and the next world builds new ones.
        /// </summary>
        internal static void Forget()
        {
            _crops = null;
        }

        /// <summary>
        /// Every sapling in the game, and the seed it is planted from, read once out of
        /// what the game already knows.
        /// </summary>
        private static Dictionary<GameObject, string> Crops()
        {
            if (_crops != null)
            {
                return _crops;
            }

            // Nothing to read yet. Answering with an empty table is fine for one frame;
            // remembering it would be permanent.
            if (ZNetScene.instance == null || ZNetScene.instance.m_prefabs == null)
            {
                return Empty;
            }

            _crops = new Dictionary<GameObject, string>();

            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null || prefab.GetComponent<Plant>() == null)
                {
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                if (piece == null || piece.m_resources == null || piece.m_resources.Length == 0)
                {
                    continue;
                }

                var seed = piece.m_resources[0].m_resItem;
                if (seed != null)
                {
                    _crops[prefab] = seed.gameObject.name;
                }
            }

            HirdmanPlugin.Log.LogInfo($"Retainers know how to sow {_crops.Count} crop(s).");
            return _crops;
        }
    }
}
