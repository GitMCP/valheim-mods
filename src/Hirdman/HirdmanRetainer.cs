using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Builds a retainer out of a dvergr.
    ///
    /// Of the 157 humanoids in the game, the dvergr is the only one that arrives with
    /// everything a working companion needs already attached: it is bipedal and clothed,
    /// it carries a <see cref="VisEquipment"/> so the gear in its hands is the gear you
    /// gave it, it has an <see cref="NpcTalk"/> for speech, and its
    /// <see cref="BaseAI.m_pathAgentType"/> is the humanoid one, so it walks and swims
    /// where a person would.
    ///
    /// The player rig would look more human, but it is a <see cref="Player"/> - input,
    /// skills, food, respawn - with no AI at all, and swapping that out for a
    /// <see cref="Humanoid"/> is a different and much larger job. The dvergr is what
    /// makes a working retainer possible now.
    /// </summary>
    internal static class HirdmanRetainer
    {
        internal const string PrefabName = "hirdman_retainer";

        private const string CloneSource = "Dverger";

        /// <summary>
        /// What a retainer carries. Felling a tree or breaking rock with your hands is
        /// not a thing the game lets anyone do, and the tier of what is in its hands is
        /// what decides how much of the world it can touch - a stone axe will not bring
        /// down a birch and an antler pick will not scratch iron, for a retainer exactly
        /// as for a player.
        /// </summary>
        internal static string Axe => HirdmanPlugin.AxeItem.Value;

        internal static string Pickaxe => HirdmanPlugin.PickaxeItem.Value;

        internal static bool Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, CloneSource);
            if (prefab == null)
            {
                HirdmanPlugin.Log.LogError($"Could not clone '{CloneSource}'; has the game changed it?");
                return false;
            }

            if (!Enlist(prefab) || !Calm(prefab))
            {
                return false;
            }

            // No dvergr loot from something that was never a dvergr.
            var drops = prefab.GetComponent<CharacterDrop>();
            if (drops != null)
            {
                drops.m_drops.Clear();
            }

            prefab.AddComponent<HirdmanTag>();
            prefab.AddComponent<HirdmanBrain>();

            var config = new CreatureConfig
            {
                Name = $"${PrefabName}_name",

                // The faction tamed animals use, so nothing in the world treats a
                // retainer as prey and no player can swing at one by accident.
                Faction = Character.Faction.Players,
            };

            var creature = new CustomCreature(prefab, fixReference: false, config);
            if (!CreatureManager.Instance.AddCreature(creature))
            {
                HirdmanPlugin.Log.LogError($"Failed to register creature '{PrefabName}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Is this part of what the retainer came with? Its tools are not loot and must
        /// never end up in a chest, or the first thing a hauling retainer does is put
        /// its own axe away and stop being able to work.
        /// </summary>
        internal static bool IsKit(ItemDrop.ItemData item)
        {
            if (item == null || item.m_dropPrefab == null)
            {
                return false;
            }

            var name = item.m_dropPrefab.name;
            return name == Axe || name == Pickaxe;
        }

        /// <summary>
        /// Makes it yours. A retainer is not tamed by feeding it: it is hired, so it
        /// starts tamed, and it is commandable, which is what gives the vanilla
        /// follow-and-wait handling something to hang on.
        /// </summary>
        private static bool Enlist(GameObject prefab)
        {
            var humanoid = prefab.GetComponent<Humanoid>();
            if (humanoid == null)
            {
                HirdmanPlugin.Log.LogError($"'{CloneSource}' has no Humanoid component.");
                return false;
            }

            humanoid.m_faction = Character.Faction.Players;

            // Its own kit is a dvergr's. What it should carry is decided later, once
            // ObjectDB can resolve an item prefab by name.
            humanoid.m_defaultItems = new GameObject[0];

            var tameable = prefab.GetComponent<Tameable>();
            if (tameable == null)
            {
                tameable = prefab.AddComponent<Tameable>();
            }

            tameable.m_startsTamed = true;
            tameable.m_commandable = true;
            tameable.m_petEffect = new EffectList();
            tameable.m_tamedEffect = new EffectList();
            return true;
        }

        /// <summary>
        /// Stops it behaving like something that lives in Mistlands: no wandering off,
        /// no picking fights. It keeps its alert range, because a retainer that will not
        /// defend itself is worse than no retainer.
        /// </summary>
        private static bool Calm(GameObject prefab)
        {
            var ai = prefab.GetComponent<MonsterAI>();
            if (ai == null)
            {
                HirdmanPlugin.Log.LogError($"'{CloneSource}' has no MonsterAI component.");
                return false;
            }

            // Wandering is this mod's business now: an idle retainer drifts around its
            // home on purpose, and vanilla's own drift would fight with it.
            ai.m_randomMoveRange = 0f;
            ai.m_randomMoveInterval = 0f;

            ai.m_enableHuntPlayer = false;

            // Trolls break walls through this flag. A retainer swinging at your house
            // while it chases a greyling would be worse than the greyling.
            ai.m_attackPlayerObjects = false;

            ai.m_afraidOfFire = false;
            ai.m_avoidFire = false;
            ai.m_fleeIfLowHealth = 0f;
            ai.m_fleeIfNotAlerted = false;

            if (ai.m_consumeItems != null)
            {
                ai.m_consumeItems.Clear();
            }

            return true;
        }

        /// <summary>
        /// Hands every retainer prefab its tools. Separate from <see cref="Register"/>
        /// because default items are item prefabs, and ObjectDB cannot resolve those
        /// until items have been registered.
        /// </summary>
        internal static void RegisterKit()
        {
            var prefab = PrefabManager.Instance.GetPrefab(PrefabName);
            var humanoid = prefab == null ? null : prefab.GetComponent<Humanoid>();
            if (humanoid == null)
            {
                HirdmanPlugin.Log.LogWarning("No retainer prefab to equip.");
                return;
            }

            var kit = new System.Collections.Generic.List<GameObject>();
            foreach (var name in new[] { Axe, Pickaxe })
            {
                var item = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(name);
                if (item == null)
                {
                    HirdmanPlugin.Log.LogWarning($"Could not resolve '{name}'; retainers will go without it.");
                    continue;
                }

                kit.Add(item);
            }

            humanoid.m_defaultItems = kit.ToArray();
            HirdmanPlugin.Log.LogInfo($"Retainers carry {kit.Count} tool(s).");
        }
    }
}
