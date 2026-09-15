using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Overlay primitives with constant pixel thickness and one reusable texture.</summary>
    public static class ESPHelper
    {
        private static Texture2D pixel;

        /// <summary>Fills a rectangle and restores the incoming GUI tint.</summary>
        public static void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!pixel)
            {
                pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply(false, true);
            }
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        /// <summary>Draws a full outline or eight corner segments at fixed screen-pixel thickness.</summary>
        public static void Box(Rect r, Color color, float thickness, bool corners = false)
        {
            if (r.width <= 0 || r.height <= 0) return;
            float t = Mathf.Min(Mathf.Clamp(thickness, 1f, 6f), Mathf.Min(r.width, r.height));
            if (!corners)
            {
                // Four non-overlapping strips preserve the selected alpha instead of double-blending it.
                float horizontal = Mathf.Min(t, r.height * 0.5f);
                float vertical = Mathf.Min(t, r.width * 0.5f);
                Fill(new Rect(r.xMin, r.yMin, r.width, horizontal), color);
                Fill(new Rect(r.xMin, r.yMax - horizontal, r.width, horizontal), color);
                Fill(new Rect(r.xMin, r.yMin + horizontal, vertical, r.height - horizontal * 2), color);
                Fill(new Rect(r.xMax - vertical, r.yMin + horizontal, vertical, r.height - horizontal * 2), color);
                return;
            }
            float h = r.width * 0.25f, v = r.height * 0.25f;
            Fill(new Rect(r.xMin, r.yMin, h, t), color);
            Fill(new Rect(r.xMax - h, r.yMin, h, t), color);
            Fill(new Rect(r.xMin, r.yMax - t, h, t), color);
            Fill(new Rect(r.xMax - h, r.yMax - t, h, t), color);
            float cornerWidth = Mathf.Min(t, h), cornerHeight = Mathf.Min(t, v);
            Fill(new Rect(r.xMin, r.yMin + t, cornerWidth, Mathf.Max(0, v - t)), color);
            Fill(new Rect(r.xMax - cornerWidth, r.yMin + t, cornerWidth, Mathf.Max(0, v - t)), color);
            Fill(new Rect(r.xMin, r.yMax - v, cornerWidth, Mathf.Max(0, v - cornerHeight)), color);
            Fill(new Rect(r.xMax - cornerWidth, r.yMax - v, cornerWidth, Mathf.Max(0, v - cornerHeight)), color);
        }

        /// <summary>Draws a rotated line with correct vertical/zero-length handling and matrix restoration.</summary>
        public static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            Vector2 delta = end - start;
            if (Event.current.type != EventType.Repaint || delta.sqrMagnitude < 0.001f) return;
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            Fill(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), color);
            GUI.matrix = matrix;
        }

        /// <summary>Draws the shared FOV disk outline at a radius-dependent segment density.</summary>
        public static void Circle(Vector2 center, float radius, Color color, float thickness)
        {
            int segments = Mathf.Clamp(Mathf.CeilToInt(radius * 0.8f), 96, 720);
            Vector2 previous = center + Vector2.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(previous, next, color, thickness);
                previous = next;
            }
        }

        /// <summary>Releases the only owned GPU resource on unload.</summary>
        public static void Dispose() { if (pixel) Object.Destroy(pixel); pixel = null; }
    }
}
