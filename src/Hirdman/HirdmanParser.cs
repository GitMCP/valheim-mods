using System;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Turns a sentence into an order.
    ///
    /// This is the seam the whole mod is built around. It takes the words a player typed
    /// and produces one <see cref="HirdmanOrder"/>, and it is the only place in the mod
    /// that ever looks at prose. A language model belongs here and nowhere else: it would
    /// be a second implementation of this one method, chosen by config, with the same
    /// signature and the same narrow output.
    ///
    /// Keywords are not a placeholder to be thrown away either. They answer instantly,
    /// they need no model installed, and they are what the mod falls back to when a model
    /// is missing, busy or wrong - so they stay.
    /// </summary>
    internal static class HirdmanParser
    {
        /// <summary>
        /// Checked in order, because a sentence can mention more than one of these and
        /// the first match should be the thing actually being asked for. "Stop chopping
        /// and follow me" is an order to follow.
        /// </summary>
        private static readonly Rule[] Rules =
        {
            new Rule(HirdmanJob.Follow, "follow", "come with", "come along", "heel", "with me"),
            new Rule(HirdmanJob.Idle, "stop", "stay", "wait", "hold on", "stand down", "rest"),
            new Rule(HirdmanJob.Guard, "guard", "defend", "protect", "watch over", "keep watch"),
            new Rule(HirdmanJob.ChopWood, "wood", "tree", "chop", "timber", "lumber", "log"),
        };

        internal static bool TryParse(
            string sentence,
            Player speaker,
            GameObject retainer,
            out HirdmanOrder order,
            out string reply)
        {
            order = default(HirdmanOrder);
            reply = null;

            if (string.IsNullOrEmpty(sentence) || retainer == null)
            {
                return false;
            }

            var words = sentence.ToLowerInvariant();
            foreach (var rule in Rules)
            {
                if (!rule.Matches(words))
                {
                    continue;
                }

                order = new HirdmanOrder
                {
                    Job = rule.Job,

                    // Work happens where the retainer is standing when it is told, which
                    // is next to whoever is talking to it. That makes "chop wood here"
                    // and "chop wood" the same order, which is what a player means.
                    Anchor = retainer.transform.position,
                    Master = Identify(speaker),
                };

                reply = order.Acknowledgement();
                return true;
            }

            reply = "I don't follow.";
            return false;
        }

        private static ZDOID Identify(Player speaker)
        {
            var nview = speaker == null ? null : speaker.GetComponent<ZNetView>();
            return nview != null && nview.IsValid() ? nview.GetZDO().m_uid : ZDOID.None;
        }

        private struct Rule
        {
            private readonly string[] _phrases;

            internal Rule(HirdmanJob job, params string[] phrases)
            {
                Job = job;
                _phrases = phrases;
            }

            internal HirdmanJob Job { get; }

            internal bool Matches(string words)
            {
                foreach (var phrase in _phrases)
                {
                    if (words.IndexOf(phrase, StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
