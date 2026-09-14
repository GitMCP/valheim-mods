using System.Collections.Generic;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Gives a retainer a face of its own.
    ///
    /// The player rig already knows how to look like a person: two body meshes, a drawer
    /// of hair and beards, skin and hair colour. What it will not do on its own is pick,
    /// because a real player arrives with a saved appearance. A retainer arrives with
    /// none, so one is chosen here and written into the same ZDO fields the rig already
    /// reads, which is why a look survives a logout without any extra storage.
    /// </summary>
    internal static class HirdmanLooks
    {
        private static readonly List<string> Hair = new List<string>();
        private static readonly List<string> Beards = new List<string>();

        internal static void Dress(GameObject retainer)
        {
            var player = retainer == null ? null : retainer.GetComponent<Player>();
            var vis = retainer == null ? null : retainer.GetComponent<VisEquipment>();
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            if (player == null || vis == null || nview == null || !nview.IsValid())
            {
                return;
            }

            var zdo = nview.GetZDO();
            if (!nview.IsOwner())
            {
                return;
            }

            // Already chosen, on this spawn or an earlier one. The rig will read it back
            // out of the ZDO on its own.
            if (zdo.GetInt(ZDOVars.s_hairItem) != 0 || zdo.GetBool(ZDOVars.s_noHair))
            {
                return;
            }

            Gather();

            var woman = Random.value < 0.5f;
            player.SetPlayerModel(woman ? 1 : 0);

            var tone = Random.Range(0.38f, 1.05f);
            player.SetSkinColor(new Vector3(
                tone,
                tone * Random.Range(0.72f, 0.92f),
                tone * Random.Range(0.52f, 0.78f)));

            var shine = Random.Range(0.05f, 0.45f);
            player.SetHairColor(new Vector3(
                shine + Random.Range(0f, 0.35f),
                shine * Random.Range(0.6f, 1f),
                shine * Random.Range(0.3f, 0.7f)));

            if (Hair.Count > 0 && Random.value > 0.08f)
            {
                player.SetHair(Hair[Random.Range(0, Hair.Count)]);
            }
            else
            {
                zdo.Set(ZDOVars.s_noHair, true);
            }

            if (!woman && Beards.Count > 0 && Random.value > 0.25f)
            {
                player.SetBeard(Beards[Random.Range(0, Beards.Count)]);
            }
            else
            {
                player.SetBeard(string.Empty);
                zdo.Set(ZDOVars.s_noBeard, true);
            }
        }

        private static void Gather()
        {
            if (Hair.Count > 0 || ObjectDB.instance == null)
            {
                return;
            }

            foreach (var prefab in ObjectDB.instance.m_items)
            {
                var drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData?.m_shared == null)
                {
                    continue;
                }

                if (drop.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Customization)
                {
                    continue;
                }

                var name = prefab.name;
                if (name.StartsWith("Hair"))
                {
                    Hair.Add(name);
                }
                else if (name.StartsWith("Beard"))
                {
                    Beards.Add(name);
                }
            }
        }
    }
}
