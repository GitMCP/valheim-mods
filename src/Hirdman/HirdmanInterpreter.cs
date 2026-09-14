using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Hirdman
{
    /// <summary>
    /// Works out which order a sentence was asking for, using a language model running
    /// on this machine when one is configured.
    ///
    /// Four things about the shape of this are deliberate.
    ///
    /// Keywords are tried first and the model is only asked what they could not answer.
    /// "Follow me" and "go get wood" are most of what anyone types, they are unambiguous,
    /// and answering them instantly is better than answering them cleverly two seconds
    /// later. The model earns its place on the sentences keywords have no hope with -
    /// "the camp needs looking after while I'm gone" - which is exactly where it is
    /// worth waiting for.
    ///
    /// The reply is constrained to a JSON schema rather than trusted. Both llama.cpp and
    /// Ollama enforce a schema in the sampler, so a model physically cannot answer with
    /// anything but one of the job names, and unparseable output stops being a case to
    /// handle. Choosing from a menu of eleven is still a small enough job for a 4B model.
    ///
    /// The menu is generated from <see cref="HirdmanJobs"/> rather than written out, so
    /// adding a job cannot leave the prompt describing a mod that no longer exists.
    ///
    /// It never blocks. A frame is 16 ms and a small model takes one to three seconds, so
    /// the request is a coroutine and the order is given in the callback. Nothing in the
    /// game waits for this, and a retainer whose model is switched off, broken or slow
    /// still takes keyword orders.
    /// </summary>
    internal static class HirdmanInterpreter
    {
        /// <summary>
        /// What each job is, in the words a model should be choosing between. Written
        /// for a reader who has never played the game, because that is what a 4B model
        /// is.
        /// </summary>
        private static string Describe(HirdmanJob job)
        {
            switch (job)
            {
                case HirdmanJob.Follow: return "walk with the speaker wherever they go";
                case HirdmanJob.Guard: return "hold this ground and fight whatever attacks";
                case HirdmanJob.ChopWood: return "fell trees nearby and carry the wood back";
                case HirdmanJob.Explore: return "range around the speaker and map the land";
                case HirdmanJob.Gather: return "pick berries, mushrooms, herbs and other growing things";
                case HirdmanJob.Mine: return "break rock and ore deposits and carry the metal back";
                case HirdmanJob.Farm: return "sow seeds from the chests and harvest ripe crops";
                case HirdmanJob.Cook: return "put raw food on the cooking fires and take it off when done";
                case HirdmanJob.Hunt: return "kill animals or monsters nearby and collect what they drop";
                case HirdmanJob.Haul: return "pick up loose items and sort the chests";
                default: return "wait where you are and do nothing";
            }
        }

        private static string Instruction()
        {
            var menu = new StringBuilder();
            menu.Append("You are the ear of a Norse retainer taking an order from your chieftain. ");
            menu.Append("Pick the single job that best carries out what was said, and name what it ");
            menu.Append("is about if the order mentions something particular.\n");

            foreach (var job in HirdmanJobs.All)
            {
                menu.Append(HirdmanJobs.Name(job)).Append(" - ").Append(Describe(job)).Append('\n');
            }

            menu.Append("subject is one or two words naming what to look for - a plant, an ore, an ");
            menu.Append("animal - or an empty string when the order names nothing in particular.");
            return menu.ToString();
        }

        /// <summary>
        /// Turns a sentence into an order, eventually.
        /// </summary>
        /// <param name="done">
        /// Called exactly once, on the main thread: understood, the order, and what the
        /// retainer should say back.
        /// </param>
        internal static void Interpret(
            string sentence,
            Player speaker,
            GameObject retainer,
            Action<bool, HirdmanOrder, string> done)
        {
            HirdmanOrder order;
            string reply;
            if (HirdmanParser.TryParse(sentence, speaker, retainer, out order, out reply))
            {
                done(true, order, reply);
                return;
            }

            var host = HirdmanPlugin.Instance;
            if (!HirdmanPlugin.ModelEnabled.Value || host == null)
            {
                done(false, order, reply);
                return;
            }

            host.StartCoroutine(Ask(sentence, speaker, retainer, done));
        }

        private static IEnumerator Ask(
            string sentence,
            Player speaker,
            GameObject retainer,
            Action<bool, HirdmanOrder, string> done)
        {
            var endpoint = HirdmanPlugin.ModelEndpoint.Value;
            var request = UnityWebRequest.Post(endpoint, Body(sentence), "application/json");
            request.timeout = HirdmanPlugin.ModelTimeout.Value;

            yield return request.SendWebRequest();

            var failure = request.result != UnityWebRequest.Result.Success
                ? $"{request.result}: {request.error}"
                : null;
            var payload = failure == null ? request.downloadHandler.text : null;
            request.Dispose();

            if (failure != null)
            {
                HirdmanPlugin.Log.LogWarning($"No answer from the model at {endpoint} ({failure}).");
                done(false, default(HirdmanOrder), "I don't follow.");
                yield break;
            }

            HirdmanJob job;
            string subject;
            if (!TryRead(payload, out job, out subject))
            {
                done(false, default(HirdmanOrder), "I don't follow.");
                yield break;
            }

            // A retainer that has wandered off or died while the model was thinking is
            // not one that can be given an order.
            if (retainer == null)
            {
                yield break;
            }

            var order = new HirdmanOrder
            {
                Job = job,
                Anchor = retainer.transform.position,
                Master = HirdmanOrder.Identify(speaker),
                Subject = HirdmanJobs.TakesSubject(job) ? subject : string.Empty,
            };

            done(true, order, order.Acknowledgement());
        }

        private static string Body(string sentence)
        {
            var names = new JArray();
            foreach (var job in HirdmanJobs.All)
            {
                names.Add(HirdmanJobs.Name(job));
            }

            // Written out rather than serialised from a type, because the schema is the
            // interesting half of the request and it reads better whole. Escaping is
            // still left to the serialiser, since a sentence is player input.
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["job"] = new JObject { ["type"] = "string", ["enum"] = names },
                    ["subject"] = new JObject { ["type"] = "string" },
                },
                ["required"] = new JArray("job", "subject"),
            };

            var body = new JObject
            {
                ["model"] = HirdmanPlugin.ModelName.Value,
                ["stream"] = false,

                // Thinking would spend seconds of a player's time on a menu choice.
                ["think"] = false,
                ["options"] = new JObject { ["temperature"] = 0 },
                ["keep_alive"] = HirdmanPlugin.ModelKeepAlive.Value,
                ["format"] = schema,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = Instruction() },
                    new JObject { ["role"] = "user", ["content"] = sentence },
                },
            };

            return body.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static bool TryRead(string payload, out HirdmanJob job, out string subject)
        {
            job = HirdmanJob.Idle;
            subject = string.Empty;

            try
            {
                var root = JObject.Parse(payload);

                // Ollama answers under message, an OpenAI-shaped server under choices.
                // Reading both costs nothing and means either will do.
                var content = (string)root.SelectToken("message.content")
                              ?? (string)root.SelectToken("choices[0].message.content");
                if (string.IsNullOrEmpty(content))
                {
                    HirdmanPlugin.Log.LogWarning("The model answered with no content.");
                    return false;
                }

                var trimmed = content.Trim();
                var word = trimmed;

                if (trimmed.StartsWith("{", StringComparison.Ordinal))
                {
                    var answer = JObject.Parse(trimmed);
                    word = (string)answer["job"];
                    subject = ((string)answer["subject"] ?? string.Empty).Trim();
                }

                if (HirdmanJobs.TryParse((word ?? string.Empty).Trim(), out job))
                {
                    return true;
                }

                HirdmanPlugin.Log.LogWarning($"The model chose '{word}', which is not an order.");
                return false;
            }
            catch (Exception e)
            {
                HirdmanPlugin.Log.LogWarning($"Could not read the model's answer: {e.Message}");
                return false;
            }
        }
    }
}
