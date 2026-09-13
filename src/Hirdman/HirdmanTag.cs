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
    }
}
