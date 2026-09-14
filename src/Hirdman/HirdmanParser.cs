using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Turns a sentence into an order, by recognising words.
    ///
    /// This is the seam the whole mod is built around. It takes the words a player typed
    /// and produces one <see cref="HirdmanOrder"/>, and it and
    /// <see cref="HirdmanInterpreter"/> are the only places in the mod that ever look at
    /// prose.
    ///
    /// Keywords are not a placeholder to be thrown away once a model is wired up. They
    /// answer instantly, they need nothing installed, they are right about the sentences
    /// people actually type, and they are what the mod falls back to when a model is
    /// missing, busy or wrong - so they stay, and the model is only asked what they
    /// could not place.
    /// </summary>
    internal static class HirdmanParser
    {
        /// <summary>
        /// Checked in order, because a sentence can mention more than one of these and
        /// the first match should be the thing actually being asked for. "Stop chopping
        /// and follow me" is an order to follow.
        ///
        /// The ordering within the work jobs is by how specific the words are rather
        /// than by anything else: "chop" only ever means one thing, "gather" is the
        /// catch-all for picking things up, so gathering is asked last.
        /// </summary>
        private static readonly Rule[] Rules =
        {
            new Rule(HirdmanJob.Follow, "follow", "come with", "come along", "heel", "with me"),
            new Rule(HirdmanJob.Idle, "stop", "stay", "wait", "hold on", "stand down", "rest", "at ease"),
            new Rule(HirdmanJob.Guard, "guard", "defend", "protect", "watch over", "keep watch", "stand watch"),
            new Rule(HirdmanJob.ChopWood, "chop", "timber", "lumber", "fell", "firewood", "wood", "tree"),
            new Rule(HirdmanJob.Mine, "mine", "mining", "ore", "dig", "quarry", "pickaxe", "rock"),
            new Rule(HirdmanJob.Farm, "farm", "sow", "plant", "seed", "field", "crop", "harvest", "garden"),
            new Rule(HirdmanJob.Cook, "cook", "kitchen", "roast", "meal", "supper", "oven", "food"),
            new Rule(HirdmanJob.Hunt, "hunt", "kill", "slay", "meat", "prey", "quarry"),
            new Rule(HirdmanJob.Explore, "explore", "scout", "survey", "map", "look around", "range", "wander"),
            new Rule(HirdmanJob.Haul, "haul", "tidy", "sort", "organise", "organize", "put away", "chest", "store"),
            new Rule(HirdmanJob.Gather, "gather", "pick", "forage", "collect", "berr", "mushroom", "flower"),
        };

        /// <summary>
        /// Words that are in a sentence because it is a sentence. What is left after
        /// these and the job's own words are removed is what the order is about.
        /// </summary>
        private static readonly HashSet<string> Filler = new HashSet<string>
        {
            "a", "an", "the", "some", "any", "all", "more", "go", "and", "then", "to", "for",
            "of", "at", "in", "on", "near", "by", "here", "there", "over", "out", "up", "down",
            "get", "bring", "fetch", "find", "look", "please", "now", "me", "my", "mine", "you",
            "your", "us", "our", "we", "i", "will", "can", "could", "would", "should", "do",
            "keep", "start", "begin", "back", "around", "some", "them", "it", "that", "this",
            "with", "from", "off", "away", "while", "if", "is", "are", "be", "been",
        };

        /// <summary>
        /// Long enough for "hare and boar", short enough that a rambling sentence does
        /// not turn into a subject that matches half the world.
        /// </summary>
        private const int SubjectWords = 4;

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
            if (IsChestTalk(words))
            {
                order = Compose(HirdmanJob.Haul, words, "chest", speaker, retainer);
                reply = order.Acknowledgement();
                return true;
            }

            foreach (var rule in Rules)
            {
                var hit = rule.Match(words);
                if (hit == null)
                {
                    continue;
                }

                order = Compose(rule.Job, words, hit, speaker, retainer);
                reply = order.Acknowledgement();
                return true;
            }

            reply = "I don't follow.";
            return false;
        }

        /// <summary>
        /// Builds the order around a job that has been recognised.
        /// </summary>
        internal static HirdmanOrder Compose(
            HirdmanJob job, string sentence, string matched, Player speaker, GameObject retainer)
        {
            return new HirdmanOrder
            {
                Job = job,

                // Work happens where the retainer is standing when it is told, which is
                // next to whoever is talking to it. That makes "chop wood here" and
                // "chop wood" the same order, which is what a player means.
                Anchor = retainer.transform.position,
                Master = HirdmanOrder.Identify(speaker),
                Subject = SubjectFor(job, sentence, matched),
            };
        }

        private static string SubjectFor(HirdmanJob job, string sentence, string matched)
        {
            if (!HirdmanJobs.TakesSubject(job))
            {
                return string.Empty;
            }

            var subject = Subject(sentence, matched);
            return job == HirdmanJob.Haul
                ? Work.HirdmanHaul.Label(sentence, subject)
                : subject;
        }

        /// <summary>
        /// "Put wood in the chest" and "get iron from the chest" would otherwise be
        /// chopping and mining, because those jobs match on the item's name first. A
        /// sentence that is about a chest is a haul, whatever else it names.
        /// </summary>
        private static bool IsChestTalk(string words)
        {
            if (words.IndexOf("chest", System.StringComparison.Ordinal) < 0 &&
                words.IndexOf("store", System.StringComparison.Ordinal) < 0 &&
                words.IndexOf("stash", System.StringComparison.Ordinal) < 0)
            {
                return false;
            }

            return words.IndexOf("put", System.StringComparison.Ordinal) >= 0
                   || words.IndexOf("take", System.StringComparison.Ordinal) >= 0
                   || words.IndexOf("get", System.StringComparison.Ordinal) >= 0
                   || words.IndexOf("fetch", System.StringComparison.Ordinal) >= 0
                   || words.IndexOf("bring", System.StringComparison.Ordinal) >= 0
                   || words.IndexOf("from", System.StringComparison.Ordinal) >= 0
                   || words.IndexOf("into", System.StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// What is left of a sentence once the grammar and the order itself are taken
        /// out of it. "Go and gather some raspberries for me" leaves "raspberries".
        /// </summary>
        internal static string Subject(string sentence, string matched)
        {
            if (string.IsNullOrEmpty(sentence))
            {
                return string.Empty;
            }

            var kept = new List<string>();

            foreach (var raw in sentence.Split(' ', ',', '.', '!', '?', ';', ':'))
            {
                var word = raw.Trim();
                if (word.Length < 3 || Filler.Contains(word) || IsJobWord(word, matched))
                {
                    continue;
                }

                kept.Add(word);
                if (kept.Count == SubjectWords)
                {
                    break;
                }
            }

            return string.Join(" ", kept.ToArray());
        }

        /// <summary>
        /// A word belongs to the order rather than to its subject if it is part of the
        /// phrase that chose the job. Other jobs' words are left alone: "put wood in the
        /// chest" has to keep "wood", even though chopping also answers to that word.
        /// </summary>
        private static bool IsJobWord(string word, string matched)
        {
            return !string.IsNullOrEmpty(matched) &&
                   matched.IndexOf(word, StringComparison.Ordinal) >= 0;
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

            /// <summary>The phrase that matched, or null.</summary>
            internal string Match(string words)
            {
                foreach (var phrase in _phrases)
                {
                    if (words.IndexOf(phrase, StringComparison.Ordinal) >= 0)
                    {
                        return phrase;
                    }
                }

                return null;
            }
        }
    }
}
