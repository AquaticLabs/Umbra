using System;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Throttled orthographic world camera; marker projection uses the same camera as the terrain.</summary>
    internal static class MinimapRenderer
    {
        private static Camera mapCamera;
        private static RenderTexture texture;
        private static float nextRender;
        private static GUIStyle caption;
        private static VisualPreferences Prefs { get { return VisualSettings.Current; } }

        internal static void Update()
        {
            if (!Prefs.Minimap || !UmbraRuntime.characterCollected) { if (mapCamera) Dispose(); return; }
            if (Time.unscaledTime < nextRender) return;
            nextRender = Time.unscaledTime + 1f / Mathf.Clamp(Prefs.MapRefreshRate, 1f, 15f);
            try
            {
                EnsureCamera();
                var position = UmbraRuntime.LocalPlayerBody.corePosition;
                mapCamera.transform.SetPositionAndRotation(position + Vector3.up * 500f, Quaternion.Euler(90f, 0, 0));
                mapCamera.orthographicSize = Mathf.Clamp(Prefs.MapRange, 20f, 500f);
                mapCamera.Render();
            }
            catch (Exception error)
            {
                Prefs.Minimap = false; Dispose();
                Debug.LogError("Umbra minimap disabled: " + error);
                MenuController.Toast("Minimap rendering failed; see Player.log.");
            }
        }

        private static void EnsureCamera()
        {
            if (mapCamera) return;
            var owner = new GameObject("Umbra Minimap Camera") { hideFlags = HideFlags.HideAndDontSave };
            mapCamera = owner.AddComponent<Camera>();
            mapCamera.enabled = false;
            mapCamera.orthographic = true; mapCamera.aspect = 1f;
            mapCamera.nearClipPlane = 0.1f; mapCamera.farClipPlane = 1200f;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.025f, 0.055f, 0.085f);
            mapCamera.cullingMask = LayerIndex.world.mask;
            mapCamera.allowHDR = false; mapCamera.allowMSAA = false; mapCamera.useOcclusionCulling = false;
            texture = new RenderTexture(512, 512, 16) { name = "Umbra Minimap", hideFlags = HideFlags.HideAndDontSave };
            texture.Create(); mapCamera.targetTexture = texture;
        }

        internal static void Draw()
        {
            if (Event.current.type != EventType.Repaint || !Prefs.Minimap || !mapCamera || !texture || !UmbraRuntime.characterCollected) return;
            float size = Mathf.Min(Mathf.Clamp(Prefs.MapSize, 140f, 500f), Mathf.Min(Screen.width - 16f, Screen.height - 40f));
            var rect = new Rect(Mathf.Clamp(Prefs.MapX, 0, Screen.width - size), Mathf.Clamp(Prefs.MapY, 22, Screen.height - size), size, size);
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
            ESPHelper.Box(rect, new Color(0.2f, 0.55f, 0.85f), 1.5f, false);
            if (caption == null) caption = new GUIStyle { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            GUI.Label(new Rect(rect.x, rect.y - 21, rect.width, 20), "N ↑   MAP / " + (Prefs.MapRange * 2).ToString("0") + " m", caption);
            DrawMarkers(rect);
        }

        private static void DrawMarkers(Rect rect)
        {
            var local = UmbraRuntime.LocalPlayerBody;
            foreach (var body in CharacterBody.readOnlyInstancesList)
            {
                if (!body || body == local || !body.healthComponent || !body.healthComponent.alive || !body.teamComponent) continue;
                if (local.teamComponent && body.teamComponent.teamIndex == local.teamComponent.teamIndex) continue;
                if (body.teamComponent.teamIndex == TeamIndex.Neutral || body.teamComponent.teamIndex == TeamIndex.None) continue;
                Dot(rect, body.corePosition, Prefs.MapEnemyColor, body.isBoss ? 4f : 2.5f);
            }
            foreach (var position in EspRenderer.LootPositions()) Dot(rect, position, Prefs.MapLootColor, 2.5f);
            Dot(rect, local.corePosition, Color.white, 4f);
        }

        private static void Dot(Rect rect, Vector3 position, Color color, float radius)
        {
            var point = mapCamera.WorldToViewportPoint(position);
            if (point.z < 0 || point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) return;
            float x = Mathf.Clamp(rect.x + point.x * rect.width, rect.xMin + radius, rect.xMax - radius);
            float y = Mathf.Clamp(rect.y + (1 - point.y) * rect.height, rect.yMin + radius, rect.yMax - radius);
            ESPHelper.Fill(new Rect(x - radius, y - radius, radius * 2, radius * 2), color);
        }

        internal static void Dispose()
        {
            if (mapCamera) { mapCamera.targetTexture = null; UnityEngine.Object.Destroy(mapCamera.gameObject); }
            if (texture) { texture.Release(); UnityEngine.Object.Destroy(texture); }
            mapCamera = null; texture = null; nextRender = 0; caption = null;
        }
    }
}
