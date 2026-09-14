using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using BepInEx;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hirdman
{
    /// <summary>
    /// The ear that listens when keywords cannot: a small language model that starts
    /// with the game, runs on the processor, and dies with it.
    ///
    /// Ollama is a good tool and a poor dependency. Asking a player to install a
    /// separate server, pull a multi-gigabyte model, and paste an endpoint into a
    /// config file is how a feature that should be invisible becomes a chore. The
    /// weights are too large to put in the Thunderstore zip, so the first launch
    /// fetches them once, into a folder that survives mod updates. After that, opening
    /// Valheim starts the ear the way it starts everything else.
    ///
    /// It runs on the CPU on purpose. Valheim wants the graphics card. Choosing
    /// between twelve orders is a small enough job that a half-billion-parameter
    /// model does it in a second or two without stealing frames.
    ///
    /// Dedicated servers never start it. Understanding happens on the machine of
    /// whoever spoke; a headless process has no one speaking.
    /// </summary>
    internal static class HirdmanEar
    {
        private const int Port = 17434;

        private const string LlamaTag = "b10809";

        private const string WinZipUrl =
            "https://github.com/ggml-org/llama.cpp/releases/download/b10809/llama-b10809-bin-win-cpu-x64.zip";

        private const string WinZipSha =
            "9df3158ed228a641a4b127942d7f459f24c9e13f04682659d05c00c80099b6b5";

        private const string LinuxTarUrl =
            "https://github.com/ggml-org/llama.cpp/releases/download/b10809/llama-b10809-bin-ubuntu-x64.tar.gz";

        private const string LinuxTarSha =
            "5e34434ddc6d03cd1584f403201aff0d4bd1a5793a72ff7e286532dfd1e4b941";

        private const string GgufUrl =
            "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/qwen2.5-0.5b-instruct-q4_k_m.gguf";

        private const string GgufSha =
            "74a4da8c9fdbcd15bd1f6d01d621410d31c6fc00986f5eb687824e7b93d7a9db";

        private const long GgufBytes = 491400032;

        internal const string ChatModel = "qwen2.5-0.5b-instruct";

        private static readonly object Gate = new object();

        private static Process _server;
        private static Thread _worker;
        private static volatile bool _ready;
        private static volatile bool _failed;
        private static volatile string _chatUrl;

        internal static bool Ready
        {
            get { return _ready; }
        }

        internal static bool Failed
        {
            get { return _failed; }
        }

        internal static string ChatUrl
        {
            get { return _chatUrl; }
        }

        /// <summary>
        /// Whether leftover sentences should go to the bundled ear rather than an
        /// external server the player configured.
        /// </summary>
        internal static bool Bundled
        {
            get
            {
                if (!HirdmanPlugin.ModelEnabled.Value)
                {
                    return false;
                }

                var source = HirdmanPlugin.ModelSource.Value ?? string.Empty;
                return !source.StartsWith("ext", StringComparison.OrdinalIgnoreCase);
            }
        }

        internal static void Begin()
        {
            if (!Bundled || Dedicated())
            {
                return;
            }

            lock (Gate)
            {
                if (_worker != null)
                {
                    return;
                }

                _worker = new Thread(BringUp)
                {
                    IsBackground = true,
                    Name = "HirdmanEar",
                };
                _worker.Start();
            }
        }

        internal static void Stop()
        {
            lock (Gate)
            {
                _ready = false;
                try
                {
                    if (_server != null && !_server.HasExited)
                    {
                        _server.Kill();
                    }
                }
                catch (Exception e)
                {
                    HirdmanPlugin.Log.LogWarning("Could not stop the ear: " + e.Message);
                }

                _server = null;
            }
        }

        private static void BringUp()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var root = Path.Combine(Paths.ConfigPath, "Hirdman", "ear");
                Directory.CreateDirectory(root);

                var runtime = Path.Combine(root, "runtime-" + LlamaTag);
                var model = Path.Combine(root, "qwen2.5-0.5b-instruct-q4_k_m.gguf");
                var server = FindServer(runtime);

                if (string.IsNullOrEmpty(server))
                {
                    HirdmanPlugin.Log.LogInfo("Fetching the ear's runtime, once.");
                    TryDeleteDir(runtime);
                    UnpackRuntime(root, runtime);
                    server = FindServer(runtime);
                }

                if (string.IsNullOrEmpty(server))
                {
                    throw new FileNotFoundException("llama-server was not in the runtime archive.");
                }

                if (!File.Exists(model) || new FileInfo(model).Length != GgufBytes)
                {
                    HirdmanPlugin.Log.LogInfo("Fetching the ear's model (~470 MB), once. Later launches skip this.");
                    Fetch(GgufUrl, model, GgufSha);
                }

                if (Application.platform == RuntimePlatform.LinuxPlayer ||
                    Application.platform == RuntimePlatform.LinuxEditor)
                {
                    TryChmod(server);
                }

                StartServer(server, model);
                _chatUrl = "http://127.0.0.1:" + Port + "/v1/chat/completions";
                _ready = true;
                HirdmanPlugin.Log.LogInfo("The ear is listening.");
            }
            catch (Exception e)
            {
                _failed = true;
                HirdmanPlugin.Log.LogWarning("The ear did not start (" + e.Message + "). Keyword orders still work.");
            }
        }

        private static void UnpackRuntime(string root, string dest)
        {
            Directory.CreateDirectory(dest);
            var windows = Application.platform == RuntimePlatform.WindowsPlayer ||
                          Application.platform == RuntimePlatform.WindowsEditor;
            if (windows)
            {
                var zip = Path.Combine(root, "llama-" + LlamaTag + "-win.zip");
                Fetch(WinZipUrl, zip, WinZipSha);
                ZipFile.ExtractToDirectory(zip, dest);
                TryDelete(zip);
                return;
            }

            var tar = Path.Combine(root, "llama-" + LlamaTag + "-linux.tar.gz");
            Fetch(LinuxTarUrl, tar, LinuxTarSha);
            var unpack = Process.Start(new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = "-xzf \"" + tar + "\" -C \"" + dest + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (unpack == null)
            {
                throw new InvalidOperationException("tar did not start.");
            }

            unpack.WaitForExit();
            if (unpack.ExitCode != 0)
            {
                throw new InvalidOperationException("tar exited " + unpack.ExitCode + ".");
            }

            TryDelete(tar);
        }

        private static void StartServer(string server, string model)
        {
            var folder = Path.GetDirectoryName(server) ?? string.Empty;
            var info = new ProcessStartInfo
            {
                FileName = server,
                Arguments = "--model \"" + model + "\" --host 127.0.0.1 --port " + Port +
                            " --ctx-size 2048 --n-gpu-layers 0 --no-webui --jinja --log-disable",
                WorkingDirectory = folder,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (Application.platform == RuntimePlatform.LinuxPlayer ||
                Application.platform == RuntimePlatform.LinuxEditor)
            {
                var path = info.EnvironmentVariables.ContainsKey("LD_LIBRARY_PATH")
                    ? info.EnvironmentVariables["LD_LIBRARY_PATH"]
                    : Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
                info.EnvironmentVariables["LD_LIBRARY_PATH"] = string.IsNullOrEmpty(path)
                    ? folder
                    : folder + ":" + path;
            }

            var process = Process.Start(info);
            if (process == null)
            {
                throw new InvalidOperationException("llama-server did not start.");
            }

            lock (Gate)
            {
                _server = process;
            }

            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    throw new InvalidOperationException("llama-server exited " + process.ExitCode + ".");
                }

                if (Awake())
                {
                    return;
                }

                Thread.Sleep(250);
            }

            throw new TimeoutException("llama-server did not become ready.");
        }

        private static bool Awake()
        {
            return Ping("http://127.0.0.1:" + Port + "/health")
                   || Ping("http://127.0.0.1:" + Port + "/v1/models");
        }

        private static bool Ping(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 1000;
                request.Method = "GET";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return (int)response.StatusCode < 500;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void Fetch(string url, string dest, string sha)
        {
            var part = dest + ".part";
            TryDelete(part);
            TryDelete(dest);

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "Hirdman";
                client.DownloadFile(url, part);
            }

            if (!Hashed(part, sha))
            {
                TryDelete(part);
                throw new InvalidOperationException("Downloaded file did not match " + Path.GetFileName(dest) + ".");
            }

            File.Move(part, dest);
        }

        private static bool Hashed(string path, string expected)
        {
            using (var file = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(file);
                var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                return string.Equals(hex, expected, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string FindServer(string root)
        {
            if (!Directory.Exists(root))
            {
                return null;
            }

            var name = ServerName();
            foreach (var path in Directory.GetFiles(root, name, SearchOption.AllDirectories))
            {
                return path;
            }

            return null;
        }

        private static string ServerName()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor
                ? "llama-server.exe"
                : "llama-server";
        }

        private static void TryChmod(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = "+x \"" + path + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                })?.WaitForExit();
            }
            catch
            {
                // The binary may already be executable.
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best effort: a locked leftover is retried next launch.
            }
        }

        private static void TryDeleteDir(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // Best effort: UnpackRuntime will fail loudly if the folder is still in the way.
            }
        }

        private static bool Dedicated()
        {
            var name = Paths.ProcessName ?? string.Empty;
            if (name.IndexOf("valheim_server", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }
    }
}
