using System;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// The whole vocabulary of things a retainer can be told to do.
    ///
    /// Keeping this a closed list is the central decision of the mod. Work is written by
    /// hand and runs deterministically on whichever peer owns the retainer; understanding
    /// a sentence is a separate job that happens once, on the machine of whoever spoke,
    /// and its only possible output is a value from this enum plus a place and a subject.
    /// A language model that is asked to choose from a menu can be small, can be wrong
    /// without breaking anything, and can be absent.
    /// </summary>
    internal enum HirdmanJob
    {
        /// <summary>Stay around this spot, and look busy doing it.</summary>
        Idle = 0,

        /// <summary>Walk at your master's heel.</summary>
        Follow = 1,

        /// <summary>Hold this ground and answer anything that starts a fight.</summary>
        Guard = 2,

        /// <summary>Fell trees near this ground and carry the wood.</summary>
        ChopWood = 3,

        /// <summary>Walk outwards from here, and let your master see what you see.</summary>
        Explore = 4,

        /// <summary>Pick what grows near this ground.</summary>
        Gather = 5,

        /// <summary>Break ore near this ground and carry the metal.</summary>
        Mine = 6,

        /// <summary>Sow the seeds in the chests here, and lift what has come up.</summary>
        Farm = 7,

        /// <summary>Put raw food on the fires here, and take it off before it burns.</summary>
        Cook = 8,

        /// <summary>Kill what lives near this ground and bring back what it drops.</summary>
        Hunt = 9,

        /// <summary>Put loose things away, and put like with like.</summary>
        Haul = 10,

        /// <summary>Leave service and disappear.</summary>
        Dismissed = 11,
    }

    /// <summary>
    /// The names the jobs go by outside the assembly: in a player's sentence, in a
    /// model's answer, and in the console.
    ///
    /// Kept apart from the enum on purpose. Renaming <see cref="HirdmanJob.ChopWood"/>
    /// should be a refactor, not a silent change to the wire format a model was prompted
    /// against and a player has learned to type.
    /// </summary>
    internal static class HirdmanJobs
    {
        internal static readonly HirdmanJob[] All =
        {
            HirdmanJob.Idle, HirdmanJob.Follow, HirdmanJob.Guard, HirdmanJob.ChopWood,
            HirdmanJob.Explore, HirdmanJob.Gather, HirdmanJob.Mine, HirdmanJob.Farm,
            HirdmanJob.Cook, HirdmanJob.Hunt, HirdmanJob.Haul, HirdmanJob.Dismissed,
        };

        internal static string Name(HirdmanJob job)
        {
            switch (job)
            {
                case HirdmanJob.Follow: return "follow";
                case HirdmanJob.Guard: return "guard";
                case HirdmanJob.ChopWood: return "chop_wood";
                case HirdmanJob.Explore: return "explore";
                case HirdmanJob.Gather: return "gather";
                case HirdmanJob.Mine: return "mine";
                case HirdmanJob.Farm: return "farm";
                case HirdmanJob.Cook: return "cook";
                case HirdmanJob.Hunt: return "hunt";
                case HirdmanJob.Haul: return "haul";
                case HirdmanJob.Dismissed: return "dismissed";
                default: return "idle";
            }
        }

        internal static bool TryParse(string word, out HirdmanJob job)
        {
            foreach (var candidate in All)
            {
                if (string.Equals(Name(candidate), word, StringComparison.OrdinalIgnoreCase))
                {
                    job = candidate;
                    return true;
                }
            }

            job = HirdmanJob.Idle;
            return false;
        }

        /// <summary>
        /// The jobs that are about a particular thing, and so can be narrowed by naming
        /// one. "Gather raspberries" means something; "follow raspberries" does not.
        /// </summary>
        internal static bool TakesSubject(HirdmanJob job)
        {
            switch (job)
            {
                case HirdmanJob.Gather:
                case HirdmanJob.Mine:
                case HirdmanJob.Hunt:
                case HirdmanJob.Farm:
                case HirdmanJob.Cook:
                case HirdmanJob.Haul:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// A retainer's standing order, and how it survives a logout.
    ///
    /// Orders live in the ZDO rather than in a component, for three reasons: the peer
    /// that owns a creature can change hands mid-task, the world save has to remember
    /// what everyone was doing, and every other peer wants to know without being told.
    /// Because an order is a handful of numbers and at most one short word, all of that
    /// is free - which is the reason prose is never what gets stored.
    /// </summary>
    internal struct HirdmanOrder
    {
        private const string JobKey = "hird_job";
        private const string AnchorKey = "hird_anchor";
        private const string MasterKey = "hird_master";
        private const string SubjectKey = "hird_subject";

        internal HirdmanJob Job;

        /// <summary>The ground this order is about: where to stand, or where to work.</summary>
        internal Vector3 Anchor;

        /// <summary>Whose order it is, and who <see cref="HirdmanJob.Follow"/> follows.</summary>
        internal ZDOID Master;

        /// <summary>
        /// What to look for, as the player said it: "raspberries", "copper", "boar".
        /// Empty means anything the job applies to, which is the common case.
        /// </summary>
        internal string Subject;

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
                Subject = zdo.GetString(SubjectKey, string.Empty),
            };
        }

        internal void Write(ZDO zdo)
        {
            zdo.Set(JobKey, (int)Job);
            zdo.Set(AnchorKey, Anchor);
            zdo.Set(MasterKey, Master);
            zdo.Set(SubjectKey, Subject ?? string.Empty);
        }

        /// <summary>
        /// An order as a message, for the peer that owns the retainer rather than
        /// whoever spoke. The ZDO is still what stores it; this is only how it travels
        /// to the machine that is allowed to write that ZDO.
        /// </summary>
        internal ZPackage Pack()
        {
            var package = new ZPackage();
            package.Write((int)Job);
            package.Write(Anchor);
            package.Write(Master);
            package.Write(Subject ?? string.Empty);
            return package;
        }

        internal static HirdmanOrder Unpack(ZPackage package)
        {
            return new HirdmanOrder
            {
                Job = (HirdmanJob)package.ReadInt(),
                Anchor = package.ReadVector3(),
                Master = package.ReadZDOID(),
                Subject = package.ReadString(),
            };
        }

        internal bool SameAs(HirdmanOrder other)
        {
            return Job == other.Job
                   && Anchor == other.Anchor
                   && Master == other.Master
                   && string.Equals(Subject ?? string.Empty, other.Subject ?? string.Empty, StringComparison.Ordinal);
        }

        /// <summary>What the retainer says back, so an order visibly lands.</summary>
        internal string Acknowledgement()
        {
            var about = string.IsNullOrEmpty(Subject) ? null : Subject;

            switch (Job)
            {
                case HirdmanJob.Follow:
                    return "Right behind you.";
                case HirdmanJob.Guard:
                    return "I'll hold this ground.";
                case HirdmanJob.ChopWood:
                    return "I'll see to the trees.";
                case HirdmanJob.Explore:
                    return "I'll walk the land and remember it for you.";
                case HirdmanJob.Gather:
                    return about == null ? "I'll pick what grows here." : $"I'll look for {about}.";
                case HirdmanJob.Mine:
                    return about == null ? "I'll break some rock." : $"I'll dig for {about}.";
                case HirdmanJob.Farm:
                    return "I'll tend the field.";
                case HirdmanJob.Cook:
                    return "I'll get something on the fire.";
                case HirdmanJob.Hunt:
                    return about == null ? "I'll go hunting." : $"I'll hunt {about}.";
                case HirdmanJob.Haul:
                    return HaulReply();
                case HirdmanJob.Dismissed:
                    return "I'll be on my way.";
                default:
                    return "I'll wait here.";
            }
        }

        private string HaulReply()
        {
            string item;
            bool take;
            bool put;
            Hirdman.Work.HirdmanHaul.Read(Subject, out take, out put, out item);

            if (take)
            {
                return string.IsNullOrEmpty(item)
                    ? "I'll get that from the chests."
                    : $"I'll get {item} from the chests.";
            }

            if (put)
            {
                return string.IsNullOrEmpty(item)
                    ? "I'll put that in a chest."
                    : $"I'll put the {item} away.";
            }

            return "I'll put things away.";
        }
    }
}
