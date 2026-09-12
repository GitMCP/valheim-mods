using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Stops the bicycle sounding like a lox.
    ///
    /// The noises are not components on the prefab; they are separate effect prefabs that
    /// the game spawns from <see cref="EffectList"/> fields when the creature idles, is
    /// hurt, dies, or puts a foot down. Rather than name the lox's individual sounds,
    /// which a game update could change, every effect that carries an
    /// <see cref="AudioSource"/> is dropped and everything silent is kept, so hit sparks
    /// and the like survive.
    /// </summary>
    internal static class BicicretaSilence
    {
        internal static void Apply(GameObject prefab)
        {
            var removed = 0;

            foreach (var behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                foreach (var field in behaviour.GetType()
                             .GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.FieldType == typeof(EffectList))
                    {
                        removed += Hush(field.GetValue(behaviour) as EffectList);
                    }
                }

                if (behaviour is FootStep steps)
                {
                    removed += Hush(steps);
                }
            }

            // Anything looping straight off the prefab, such as breathing.
            foreach (var source in prefab.GetComponentsInChildren<AudioSource>(includeInactive: true))
            {
                source.Stop();
                source.playOnAwake = false;
                source.enabled = false;
                removed++;
            }

            BicicretaPlugin.Log.LogInfo($"Silenced {removed} lox sound source(s).");
        }

        private static int Hush(EffectList list)
        {
            if (list?.m_effectPrefabs == null)
            {
                return 0;
            }

            var kept = new List<EffectList.EffectData>();
            var removed = 0;
            foreach (var effect in list.m_effectPrefabs)
            {
                if (MakesSound(effect?.m_prefab))
                {
                    removed++;
                    continue;
                }

                kept.Add(effect);
            }

            list.m_effectPrefabs = kept.ToArray();
            return removed;
        }

        private static int Hush(FootStep steps)
        {
            var removed = 0;
            foreach (var step in steps.m_effects)
            {
                var kept = new List<GameObject>();
                foreach (var effect in step.m_effectPrefabs)
                {
                    if (MakesSound(effect))
                    {
                        removed++;
                        continue;
                    }

                    kept.Add(effect);
                }

                step.m_effectPrefabs = kept.ToArray();
            }

            return removed;
        }

        private static bool MakesSound(GameObject effect)
        {
            return effect != null
                   && effect.GetComponentInChildren<AudioSource>(includeInactive: true) != null;
        }
    }
}
