using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Hirdman
{
    /// <summary>
    /// Works out which order a sentence was asking for, using a language model running on
    /// this machine when one is configured.
    ///
    /// Three things about the shape of this are deliberate.
    ///
    /// Keywords are tried first and the model is only asked what they could not answer.
    /// "Follow me" and "go get wood" are most of what anyone types, they are unambiguous,
    /// and answering them instantly is better than answering them cleverly two seconds
    /// later. The model earns its place on the sentences keywords have no hope with -
    /// "the camp needs looking after while I'm gone" - which is exactly where it is worth
    /// waiting for.
    ///
    /// The reply is constrained to a JSON schema rather than trusted. Both llama.cpp and
    /// Ollama enforce a schema in the sampler, so a model physically cannot answer with
    /// anything but one of four words, and unparseable output stops being a case to
    /// handle. A 4B model is plenty for choosing between four things.
    ///
    /// It never blocks. A frame is 16 ms and a small model takes one to three seconds, so
    /// the request is a coroutine and the order is given in the callback. Nothing in the
    /// game waits for this, and a retainer whose model is switched off, broken or slow
    /// still takes keyword orders.
    /// </summary>
    internal static class HirdmanInterpreter
    {
        /// <summary>
        /// Spelled the way a model should answer, which is not how they are spelled in
        /// code. The mapping is here rather than in <see cref="HirdmanJob"/> so that
        /// renaming an order cannot silently change the wire format the model was
        /// prompted against.
        /// </summary>
        private const string Vocabulary = "idle, follow, guard, chop_wood";

        private static readonly string Instruction =
            "You are the ear of a Norse retainer taking an order from your chieftain. " +
            "Pick the single job that best carries out what was said.\n" +
            "idle - wait where you are and do nothing\n" +
            "follow - walk with the speaker\n" +
            "guard - hold this ground and fight whatever attacks\n" +
            "chop_wood - fell trees nearby and carry the wood\n" +
            "Answer with JSON only, in the form {\"job\":\"<one of " + Vocabulary + ">\"}.";

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
            if (!TryReadJob(payload, out job))
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
            };

            done(true, order, order.Acknowledgement());
        }

        private static string Body(string sentence)
        {
            // Written out rather than serialised from a type, because the schema is the
            // interesting half of the request and it reads better whole. Escaping is
            // still left to the serialiser, since a sentence is player input.
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["job"] = new JObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JArray("idle", "follow", "guard", "chop_wood"),
                    },
                },
                ["required"] = new JArray("job"),
            };

            var body = new JObject
            {
                ["model"] = HirdmanPlugin.ModelName.Value,
                ["stream"] = false,

                // Thinking would spend seconds of a player's time on a four-way choice.
                ["think"] = false,
                ["options"] = new JObject { ["temperature"] = 0 },
                ["keep_alive"] = HirdmanPlugin.ModelKeepAlive.Value,
                ["format"] = schema,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = Instruction },
                    new JObject { ["role"] = "user", ["content"] = sentence },
                },
            };

            return body.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static bool TryReadJob(string payload, out HirdmanJob job)
        {
            job = HirdmanJob.Idle;

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
                var word = trimmed.StartsWith("{", StringComparison.Ordinal)
                    ? (string)JObject.Parse(trimmed)["job"]
                    : trimmed;

                switch ((word ?? string.Empty).Trim().ToLowerInvariant())
                {
                    case "follow":
                        job = HirdmanJob.Follow;
                        return true;
                    case "guard":
                        job = HirdmanJob.Guard;
                        return true;
                    case "chop_wood":
                        job = HirdmanJob.ChopWood;
                        return true;
                    case "idle":
                        job = HirdmanJob.Idle;
                        return true;
                    default:
                        HirdmanPlugin.Log.LogWarning($"The model chose '{word}', which is not an order.");
                        return false;
                }
            }
            catch (Exception e)
            {
                HirdmanPlugin.Log.LogWarning($"Could not read the model's answer: {e.Message}");
                return false;
            }
        }
    }
}
