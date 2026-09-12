using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Builds the rideable bicycle out of a lox. Everything that makes riding work -
    /// mounting, control handover between players, stamina, dismounting - already lives
    /// on the lox prefab; what this does is take away the parts that make it an animal.
    /// </summary>
    internal static class BicicretaMount
    {
        internal const string PrefabName = "bicicreta_mount";

        private const string CloneSource = "Lox";

        internal static bool Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, CloneSource);
            if (prefab == null)
            {
                BicicretaPlugin.Log.LogError($"Could not clone '{CloneSource}'; has the game changed it?");
                return false;
            }

            if (!Tame(prefab) || !Domesticate(prefab) || !TuneRiding(prefab))
            {
                return false;
            }

            BicicretaAppearance.Apply(prefab);

            var config = new CreatureConfig
            {
                Name = $"${PrefabName}_name",

                // Players is the faction tamed animals use, so nothing treats the
                // bicycle as prey and it cannot be provoked into fighting back.
                Faction = Character.Faction.Players,
            };

            var creature = new CustomCreature(prefab, fixReference: false, config);
            if (!CreatureManager.Instance.AddCreature(creature))
            {
                BicicretaPlugin.Log.LogError($"Failed to register creature '{PrefabName}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// A bicycle is not tamed by feeding it. It starts tamed, and it keeps its saddle
        /// permanently rather than needing one crafted and fitted.
        /// </summary>
        private static bool Tame(GameObject prefab)
        {
            var tameable = prefab.GetComponent<Tameable>();
            if (tameable == null)
            {
                BicicretaPlugin.Log.LogError($"'{CloneSource}' has no Tameable component.");
                return false;
            }

            tameable.m_startsTamed = true;
            tameable.m_commandable = false;

            // Losing the saddle would leave an unrideable bicycle with no way to refit it,
            // since the lox saddle recipe is not part of this mod.
            tameable.m_dropSaddleOnDeath = false;

            prefab.AddComponent<AlwaysSaddled>();
            return true;
        }

        /// <summary>
        /// Stops it behaving like an animal: no wandering off, no fleeing, no fighting.
        /// </summary>
        private static bool Domesticate(GameObject prefab)
        {
            var ai = prefab.GetComponent<MonsterAI>();
            if (ai == null)
            {
                BicicretaPlugin.Log.LogError($"'{CloneSource}' has no MonsterAI component.");
                return false;
            }

            // A parked bicycle should still be there when you come back for it.
            ai.m_randomMoveRange = 0f;
            ai.m_randomMoveInterval = 0f;

            ai.m_alertRange = 0f;
            ai.m_enableHuntPlayer = false;
            ai.m_attackPlayerObjects = false;
            ai.m_afraidOfFire = false;
            ai.m_avoidFire = false;

            // It has no mouth, so it must not wait to be fed before it can be ridden.
            ai.m_consumeItems?.Clear();

            ai.m_avoidWater = true;
            ai.m_avoidLava = true;

            // Otherwise a destroyed bicycle drops lox meat and hide. What it drops
            // instead is filled in later, once ObjectDB can resolve item prefabs.
            prefab.GetComponent<CharacterDrop>()?.m_drops.Clear();

            return true;
        }

        private static bool TuneRiding(GameObject prefab)
        {
            var saddle = prefab.GetComponentInChildren<Sadle>(includeInactive: true);
            if (saddle == null)
            {
                BicicretaPlugin.Log.LogError($"'{CloneSource}' has no Sadle component to ride.");
                return false;
            }

            saddle.m_hoverText = $"${PrefabName}_ride";

            var drain = BicicretaPlugin.StaminaDrain.Value;
            saddle.m_runStaminaDrain *= drain;
            saddle.m_swimStaminaDrain *= drain;

            var character = prefab.GetComponent<Character>();
            if (character != null)
            {
                var speed = BicicretaPlugin.RideSpeed.Value;
                character.m_speed *= speed;
                character.m_runSpeed *= speed;
                character.m_turnSpeed *= speed;
                character.m_runTurnSpeed *= speed;
            }

            return true;
        }

        /// <summary>
        /// Makes a broken bicycle refund half of what it cost. Runs separately from
        /// <see cref="Register"/> because drops are item prefabs, and ObjectDB cannot
        /// resolve those until items have been registered.
        /// </summary>
        internal static void RegisterDrops()
        {
            var prefab = PrefabManager.Instance.GetPrefab(PrefabName);
            var drops = prefab?.GetComponent<CharacterDrop>();
            if (drops == null)
            {
                BicicretaPlugin.Log.LogWarning("No CharacterDrop on the bicycle; it will refund nothing.");
                return;
            }

            drops.m_drops.Clear();
            foreach (var resource in BicicretaStand.Resources)
            {
                var item = ObjectDB.instance?.GetItemPrefab(resource.Item);
                if (item == null)
                {
                    BicicretaPlugin.Log.LogWarning($"Could not resolve '{resource.Item}' to refund.");
                    continue;
                }

                var refund = resource.Amount / 2;
                drops.m_drops.Add(new CharacterDrop.Drop
                {
                    m_prefab = item,
                    m_amountMin = refund,
                    m_amountMax = refund,
                    m_chance = 1f,
                });
            }

            BicicretaPlugin.Log.LogInfo($"Bicicreta refunds {drops.m_drops.Count} material type(s) when broken.");
        }
    }
}
