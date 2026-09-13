using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// The whole vocabulary of things a retainer can be told to do.
    ///
    /// Keeping this a short closed list is the central decision of the mod. Work is
    /// written by hand and runs deterministically on whichever peer owns the retainer;
    /// understanding a sentence is a separate job that happens once, on the machine of
    /// whoever spoke, and its only possible output is a value from this enum plus a
    /// couple of numbers. A language model that is asked to choose between four things
    /// can be small, can be wrong without breaking anything, and can be absent.
    /// </summary>
    internal enum HirdmanJob
    {
        /// <summary>Stand where you were left and do nothing.</summary>
        Idle = 0,

        /// <summary>Walk at your master's heel.</summary>
        Follow = 1,

        /// <summary>Hold this ground and answer anything that starts a fight.</summary>
        Guard = 2,

        /// <summary>Fell trees near this ground and carry the wood.</summary>
        ChopWood = 3,
    }

    /// <summary>
    /// A retainer's standing order, and how it survives a logout.
    ///
    /// Orders live in the ZDO rather than in this component, for three reasons: the peer
    /// that owns a creature can change hands mid-task, the world save has to remember
    /// what everyone was doing, and every other peer wants to know without being told.
    /// Because the order is a handful of numbers, all of that is free - which is the
    /// reason prose is never what gets stored.
    /// </summary>
    internal struct HirdmanOrder
    {
        private const string JobKey = "hird_job";
        private const string AnchorKey = "hird_anchor";
        private const string MasterKey = "hird_master";

        internal HirdmanJob Job;

        /// <summary>The ground this order is about: where to stand, or where to work.</summary>
        internal Vector3 Anchor;

        /// <summary>Whose order it is, and who <see cref="HirdmanJob.Follow"/> follows.</summary>
        internal ZDOID Master;

        /// <summary>
        /// Names a player in a way that survives being written down. A character's ZDOID
        /// is what every other reference in the game uses, and unlike a
        /// <see cref="GameObject"/> it still means something on a peer where that player
        /// is not loaded.
        /// </summary>
        internal static ZDOID Identify(Player speaker)
        {
            var nview = speaker == null ? null : speaker.GetComponent<ZNetView>();
            return nview != null && nview.IsValid() ? nview.GetZDO().m_uid : ZDOID.None;
        }

        internal static HirdmanOrder Read(ZDO zdo)
        {
            return new HirdmanOrder
            {
                Job = (HirdmanJob)zdo.GetInt(JobKey, (int)HirdmanJob.Idle),
                Anchor = zdo.GetVec3(AnchorKey, zdo.GetPosition()),
                Master = zdo.GetZDOID(MasterKey),
            };
        }

        internal void Write(ZDO zdo)
        {
            zdo.Set(JobKey, (int)Job);
            zdo.Set(AnchorKey, Anchor);
            zdo.Set(MasterKey, Master);
        }

        /// <summary>What the retainer says back, so an order visibly lands.</summary>
        internal string Acknowledgement()
        {
            switch (Job)
            {
                case HirdmanJob.Follow:
                    return "Right behind you.";
                case HirdmanJob.Guard:
                    return "I'll hold this ground.";
                case HirdmanJob.ChopWood:
                    return "I'll see to the trees.";
                default:
                    return "I'll wait here.";
            }
        }
    }
}
