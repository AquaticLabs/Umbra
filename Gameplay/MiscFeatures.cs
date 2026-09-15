using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;


namespace UmbraMenu
{
    /// <summary>Lightweight right-side telemetry and a scene-scoped host recovery bookmark.</summary>
    internal static class MiscFeatures
    {
        public static bool PerformanceHud { get { return VisualSettings.Current.PerformanceHud; } set { VisualSettings.Current.PerformanceHud = value; } }
        public static bool RunTimer { get { return VisualSettings.Current.RunTimer; } set { VisualSettings.Current.RunTimer = value; } }
        public static bool Coordinates { get { return VisualSettings.Current.Coordinates; } set { VisualSettings.Current.Coordinates = value; } }
        private static float frameTime, refreshAt;
        private static string performance = "FPS …";
        private static GUIStyle style;
        private static Vector3 bookmark;
        private static int bookmarkScene = -1;
        /// <summary>Samples unscaled frame duration; updates text four times per second.</summary>
        public static void Update()
        {
            frameTime = Mathf.Lerp(frameTime <= 0 ? Time.unscaledDeltaTime : frameTime, Time.unscaledDeltaTime, 0.05f);
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + 0.25f;
            string ping = MenuController.HasHost ? "Host / 0 ms" : "Offline";
            if (!MenuController.HasHost && NetworkClient.allClients.Count > 0 && NetworkClient.allClients[0].isConnected)
                ping = NetworkClient.allClients[0].GetRTT() + " ms";
            performance = Mathf.RoundToInt(1f / Mathf.Max(0.0001f, frameTime)) + " FPS  /  " + ping;
        }
        /// <summary>Draws compact telemetry on the right edge without opening another window.</summary>
        public static void Draw()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (style == null) style = new GUIStyle { fontSize = 13, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.78f, 0.9f, 1f) } };
            float y = 18;
            if (PerformanceHud) Row(performance, ref y);
            if (RunTimer && Run.instance) Row("Run " + TimeSpan.FromSeconds(Math.Max(0, Run.instance.GetRunStopwatch())).ToString(@"hh\:mm\:ss"), ref y);
            if (Coordinates && UmbraRuntime.LocalPlayerBody) Row("Position " + UmbraRuntime.LocalPlayerBody.footPosition.ToString("F1"), ref y);
        }
        /// <summary>Places a right-aligned line with a dark readable backdrop.</summary>
        private static void Row(string text, ref float y)
        {
            float width = style.CalcSize(new GUIContent(text)).x + 18;
            var rect = new Rect(Screen.width - width - 18, y, width, 25);
            ESPHelper.Fill(rect, new Color(0.02f, 0.06f, 0.1f, 0.7f));
            GUI.Label(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), text, style); y += 28;
        }
        /// <summary>Saves a temporary position that cannot be reused after a scene change.</summary>
        public static void Bookmark()
        {
            if (!UmbraRuntime.characterCollected) throw new InvalidOperationException("A living character is required.");
            bookmark = UmbraRuntime.LocalPlayerBody.footPosition;
            bookmarkScene = UmbraRuntime.currentScene.handle;
        }
        /// <summary>Returns the host to its bookmark in the same stage only.</summary>
        public static void Return()
        {
            if (!MenuController.HasHost || !UmbraRuntime.characterCollected) throw new InvalidOperationException("A living host character is required.");
            if (bookmarkScene < 0 || bookmarkScene != UmbraRuntime.currentScene.handle) throw new InvalidOperationException("Save a position in this stage first.");
            TeleportHelper.TeleportBody(new TeleportHelper.TeleportBodyArgs
            {
                body = UmbraRuntime.LocalPlayerBody, targetPosition = bookmark,
                targetRotation = UmbraRuntime.LocalPlayerBody.transform.rotation,
                forceOutOfVehicle = true, teleportMinions = true, resetStateMachines = true
            });
        }
        /// <summary>Invalidates position state when leaving a stage.</summary>
        public static void Clear() { bookmarkScene = -1; }

    }
}
