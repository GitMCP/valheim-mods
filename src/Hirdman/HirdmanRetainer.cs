using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Builds a retainer out of a player.
    ///
    /// The dvergr was a working companion with the wrong face and the wrong animator:
    /// <c>swing_axe</c> is a player clip, and a dvergr has no such state, so a swing
    /// returned true and hit nothing. Armour numbers lived on <see cref="Player"/> too,
    /// applied only when <see cref="Character.IsPlayer"/> was true. There is no way to
    /// keep the look, the clips and the armour math without keeping the component,
    /// because <see cref="Player"/> is the <see cref="Humanoid"/>.
    ///
    /// What is added is the AI a player does not have. What is taken away is everything
    /// the component then does because it believes it is the person at the keyboard -
    /// that work is in <c>PlayerPatch</c>. What is not given is tools: a retainer who
    /// arrives with an axe will never need one handed to them, and the whole point of
    /// an inventory is that the player decides what goes in it.
    /// </summary>
    internal static class HirdmanRetainer
    {
        internal const string PrefabName = "hirdman_retainer";

        private const string CloneSource = "Player";
        private const string AiDonor = "Dverger";

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

            prefab.AddComponent<HirdmanTag>();
            prefab.AddComponent<HirdmanBrain>();

            var config = new CreatureConfig
            {
                Name = $"${PrefabName}_name",
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
        /// Gear the retainer should keep. Hauling sorts everything else into chests, and
        /// without this the first thing a hauling retainer does is put its axe away and
        /// stop being able to work.
        /// </summary>
        internal static bool Keep(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return true;
            }

            if (item.m_equipped)
            {
                return true;
            }

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Shield:
                    return true;
            }

            switch (item.m_shared.m_skillType)
            {
                case Skills.SkillType.Axes:
                case Skills.SkillType.Pickaxes:
                    return true;
            }

            return false;
        }

        private static bool Enlist(GameObject prefab)
        {
            var humanoid = prefab.GetComponent<Humanoid>();
            if (humanoid == null)
            {
                HirdmanPlugin.Log.LogError($"'{CloneSource}' has no Humanoid component.");
                return false;
            }

            humanoid.m_faction = Character.Faction.Players;
            humanoid.m_defaultItems = new GameObject[0];
            humanoid.m_randomWeapon = new GameObject[0];
            humanoid.m_randomArmor = new GameObject[0];
            humanoid.m_randomShield = new GameObject[0];
            humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];

            var controller = prefab.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

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

        private static bool Calm(GameObject prefab)
        {
            if (prefab.GetComponent<MonsterAI>() == null && !BorrowAi(prefab))
            {
                HirdmanPlugin.Log.LogError($"Could not give '{PrefabName}' a MonsterAI.");
                return false;
            }

            var ai = prefab.GetComponent<MonsterAI>();
            ai.m_randomMoveRange = 0f;
            ai.m_randomMoveInterval = 0f;
            ai.m_enableHuntPlayer = false;
            ai.m_attackPlayerObjects = false;
            ai.m_afraidOfFire = false;
            ai.m_avoidFire = false;
            ai.m_fleeIfLowHealth = 0f;
            ai.m_fleeIfNotAlerted = false;
            ai.m_pathAgentType = Pathfinding.AgentType.Humanoid;

            if (ai.m_consumeItems != null)
            {
                ai.m_consumeItems.Clear();
            }

            return true;
        }

        /// <summary>
        /// A player has no AI. The dvergr does, and its humanoid pathing, view and
        /// hearing are the right starting point for a person who walks.
        /// </summary>
        private static bool BorrowAi(GameObject dest)
        {
            var donor = PrefabManager.Instance.GetPrefab(AiDonor);
            var from = donor == null ? null : donor.GetComponent<MonsterAI>();
            var to = dest.AddComponent<MonsterAI>();
            if (from == null)
            {
                return to != null;
            }

            for (var type = typeof(MonsterAI); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (field.IsLiteral)
                    {
                        continue;
                    }

                    var value = field.GetValue(from);
                    if (value is Component component && component != null &&
                        component.transform.root == from.transform.root)
                    {
                        continue;
                    }

                    field.SetValue(to, value);
                }
            }

            return true;
        }
    }
}
