using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Marks a creature as one of ours.
    ///
    /// The patches this mod installs sit on methods every tamed creature in the game runs
    /// through, so each one has to be able to tell a retainer from someone's boar before
    /// it changes any behaviour. Asking for a component is cheaper and steadier than
    /// comparing prefab names, which change.
    /// </summary>
    internal class HirdmanTag : MonoBehaviour
    {
        /// <summary>
        /// Is this one of ours? Used by patches that sit on <see cref="Player"/>, because
        /// a retainer is now a player rig and those methods run for every player in the
        /// scene. The prefab name covers the moment of cloning, when this component has
        /// not been added yet and <see cref="Player.Awake"/> has already run.
        /// </summary>
        internal static bool On(Component thing)
        {
            if (thing == null)
            {
                return false;
            }

            if (thing.GetComponent<HirdmanTag>() != null)
            {
                return true;
            }

            var name = thing.name;
            return name.StartsWith(HirdmanRetainer.PrefabName, System.StringComparison.Ordinal);
        }
    }
}
