using System;
using System.Reflection;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Trident's stable managed entry point. Requests are processed on RoR2's main loop, never the injector thread.</summary>
    public static class Loader
    {
        private static readonly object gate = new object();
        private static int request;
        private static bool subscribed;
        private static GameObject root;
        private static UmbraRuntime runtime;
        private static int attempts;
        private static float retryAt;
        public static string Status { get; private set; } = "Not loaded";

        /// <summary>Queues an idempotent load. Early injection waits for game/content initialization automatically.</summary>
        public static void Load() { Queue(1); }
        /// <summary>Queues cleanup, or cancels a still-pending early load, without touching Unity on the caller thread.</summary>
        public static void Unload() { Queue(2); }
        /// <summary>Same deferred unload entry used by the menu, outside an active GUI iteration.</summary>
        public static void RequestUnload() { Unload(); }

        /// <summary>Serializes competing injector requests; only the latest request is applied.</summary>
        private static void Queue(int value)
        {
            lock (gate)
            {
                request = value; attempts = 0; retryAt = 0;
                Status = value == 1 ? "Waiting for game initialization" : "Unload queued";
                if (!subscribed) { RoR2Application.onUpdate += Pump; subscribed = true; }
            }
        }

        /// <summary>Runs exclusively on the game's Update event; initialization is retried without blocking loading screens.</summary>
        private static void Pump()
        {
            lock (gate)
            {
                if (request == 2)
                {
                    DestroyRuntime(); Status = "Unloaded"; Finish(); return;
                }
                if (request != 1) { Finish(); return; }
                if (!RoR2Application.loadFinished || RoR2Application.isLoading) return;
                if (Time.unscaledTime < retryAt) return;
                if (runtime && runtime.IsReady) { Status = "Ready"; Finish(); return; }
                try
                {
                    // Cross-version replacement waits a frame for the prior component's OnDestroy cleanup.
                    var previous = GameObject.Find("Umbra Menu");
                    if (previous && previous != root)
                    {
                        previous.SetActive(false); UnityEngine.Object.Destroy(previous);
                        Status = "Replacing previous runtime"; return;
                    }
                    EnsureDependencies();
                    root = new GameObject("Umbra Menu");
                    UnityEngine.Object.DontDestroyOnLoad(root);
                    runtime = root.AddComponent<UmbraRuntime>();
                    runtime.Initialize();
                    Status = "Ready"; Finish();
                }
                catch (Exception error)
                {
                    DestroyRuntime(); attempts++;
                    Debug.LogError("Umbra initialization: " + error);
                    Status = "Initialization failed: " + error.Message;
                    if (attempts >= 3) Finish(); else retryAt = Time.unscaledTime + 2f;
                }
            }
        }

        /// <summary>Detaches the temporary dispatcher after completion or a terminal error.</summary>
        private static void Finish()
        {
            request = 0;
            if (subscribed) RoR2Application.onUpdate -= Pump;
            subscribed = false;
        }

        /// <summary>Runs idempotent cleanup immediately, then allows Unity to destroy the object at frame end.</summary>
        private static void DestroyRuntime()
        {
            if (runtime) runtime.Shutdown();
            if (root) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            runtime = null; root = null;
        }

        /// <summary>Loads the repository's embedded Harmony assembly only once, on the main thread.</summary>
        private static void EnsureDependencies()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) if (assembly.GetName().Name == "0Harmony") return;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UmbraMenu.0Harmony.dll"))
            {
                if (stream == null) throw new InvalidOperationException("Embedded Harmony dependency is missing.");
                var bytes = new byte[stream.Length]; int offset = 0;
                while (offset < bytes.Length) { int read = stream.Read(bytes, offset, bytes.Length - offset); if (read == 0) throw new System.IO.EndOfStreamException(); offset += read; }
                Assembly.Load(bytes);
            }
        }
    }
}
