using static UmbraMenu.MenuController;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    internal static class MenuTheme
    {
        #region Theme resources

        internal static GUIStyle windowStyle, headerStyle, sidebarStyle, cardStyle, titleStyle, subtitleStyle,
            sectionStyle, labelStyle, mutedStyle, valueStyle, navStyle, navActiveStyle, buttonStyle,
            dangerButtonStyle, inputStyle, toggleOnStyle, toggleOffStyle, badgeStyle, sliderStyle,
            sliderThumbStyle, scrollStyle, scrollThumbStyle, colorFieldStyle, colorLabelStyle;
        internal static readonly List<Texture2D> textures = new List<Texture2D>();
        internal static int builtRadius = -1;
        internal static readonly Color32 Accent = new Color32(45, 143, 255, 255);
        internal static readonly Color32 Text = new Color32(231, 240, 250, 255);
        internal static readonly Color32 Muted = new Color32(153, 176, 200, 255);
        internal static readonly Color32 Border = new Color32(36, 66, 95, 255);

        /// <summary>Builds native-size fonts and nine-sliced skins; rebuilds only when corner radius changes.</summary>
        internal static void EnsureStyles()
        {
            int radius = Mathf.RoundToInt(Prefs.CornerRadius);
            if (builtRadius == radius) return;
            DisposeStyles();
            builtRadius = radius;
            windowStyle = BoxStyle(new Color32(5, 15, 28, 255), Border, radius);
            headerStyle = BoxStyle(new Color32(11, 28, 47, 255), Color.clear, radius);
            sidebarStyle = BoxStyle(new Color32(7, 21, 38, 255), Color.clear, 0);
            cardStyle = BoxStyle(new Color32(18, 40, 63, 255), new Color32(75, 110, 146, 255), radius);
            titleStyle = TextStyle(Text, 23, FontStyle.Bold, TextAnchor.MiddleLeft);
            sectionStyle = TextStyle(Text, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            subtitleStyle = TextStyle(Accent, 12, FontStyle.Bold, TextAnchor.MiddleLeft);
            labelStyle = TextStyle(Text, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
            labelStyle.wordWrap = true;
            mutedStyle = TextStyle(Muted, 12, FontStyle.Normal, TextAnchor.UpperLeft);
            mutedStyle.wordWrap = true;
            valueStyle = TextStyle(new Color32(119, 188, 255, 255), 13, FontStyle.Normal, TextAnchor.MiddleRight);
            navStyle = ButtonStyle(new Color32(9, 25, 43, 80), new Color32(24, 49, 76, 180), Text, 13, TextAnchor.MiddleLeft, radius);
            navActiveStyle = ButtonStyle(new Color32(20, 67, 110, 210), new Color32(24, 78, 124, 240), Text, 12, TextAnchor.MiddleCenter, radius);
            buttonStyle = ButtonStyle(new Color32(17, 40, 65, 220), new Color32(26, 64, 100, 245), Text, 12, TextAnchor.MiddleCenter, radius);
            dangerButtonStyle = ButtonStyle(new Color32(65, 28, 42, 220), new Color32(91, 36, 51, 245), new Color32(255, 196, 207, 255), 12, TextAnchor.MiddleCenter, radius);
            inputStyle = ButtonStyle(new Color32(4, 15, 28, 215), new Color32(10, 28, 46, 240), Text, 13, TextAnchor.MiddleRight, radius);
            colorFieldStyle = new GUIStyle(inputStyle) { fontSize = 11, alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(2, 2, 0, 0), wordWrap = false };
            colorLabelStyle = new GUIStyle(mutedStyle) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            toggleOnStyle = ButtonStyle(Accent, new Color32(72, 165, 255, 255), Color.white, 11, TextAnchor.MiddleCenter, radius);
            toggleOffStyle = ButtonStyle(new Color32(27, 48, 69, 220), new Color32(38, 65, 93, 245), Muted, 11, TextAnchor.MiddleCenter, radius);
            badgeStyle = TextStyle(new Color32(51, 216, 181, 255), 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            sliderStyle = BoxStyle(new Color32(28, 56, 84, 255), Color.clear, 2);
            sliderStyle.fixedHeight = 4;
            sliderStyle.margin = new RectOffset(0, 0, 0, 0);
            sliderThumbStyle = BoxStyle(Accent, Accent, 3);
            sliderThumbStyle.fixedWidth = 10; sliderThumbStyle.fixedHeight = 12;
            sliderThumbStyle.overflow = new RectOffset(0, 0, 0, 0);
            scrollStyle = BoxStyle(new Color32(7, 20, 34, 100), Color.clear, 3);
            scrollStyle.fixedWidth = 10;
            scrollThumbStyle = BoxStyle(new Color32(49, 88, 126, 230), Color.clear, 3);
            scrollThumbStyle.fixedWidth = 8;
        }

        /// <summary>Applies opacity to panel backgrounds only, leaving text fully opaque.</summary>
        internal static void Panel(Rect rect, GUIStyle style, float opacity)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1, 1, 1, opacity);
            GUI.Box(rect, GUIContent.none, style);
            GUI.backgroundColor = previous;
        }

        /// <summary>Creates a plain font style without a background or rich-text color overrides.</summary>
        internal static GUIStyle TextStyle(Color color, int size, FontStyle weight, TextAnchor alignment)
        {
            return new GUIStyle { fontSize = size, fontStyle = weight, alignment = alignment,
                normal = { textColor = color }, richText = false, clipping = TextClipping.Clip };
        }

        /// <summary>Uses matching slice borders so corners retain their radius on wide controls.</summary>
        internal static GUIStyle BoxStyle(Color fill, Color border, int radius)
        {
            int slice = radius + 2;
            return new GUIStyle { normal = { background = RoundedTexture(fill, border, radius) },
                border = new RectOffset(slice, slice, slice, slice) };
        }

        /// <summary>Defines all interaction states with the same slice geometry and content padding.</summary>
        internal static GUIStyle ButtonStyle(Color normal, Color hover, Color text, int size, TextAnchor alignment, int radius)
        {
            var style = TextStyle(text, size, FontStyle.Normal, alignment);
            style.normal.background = RoundedTexture(normal, Border, radius);
            style.hover.background = RoundedTexture(hover, Accent, radius);
            style.active.background = RoundedTexture(Accent, Accent, radius);
            style.focused.background = style.hover.background;
            style.focused.textColor = style.hover.textColor = style.active.textColor = Color.white;
            int slice = radius + 2;
            style.border = new RectOffset(slice, slice, slice, slice);
            style.padding = new RectOffset(8, 8, 3, 3);
            return style;
        }

        /// <summary>Generates an antialiased rounded-rectangle SDF at native pixel resolution.</summary>
        internal static Texture2D RoundedTexture(Color fill, Color outline, int radius)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float qy = Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius);
                    float distance = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                    float coverage = Mathf.Clamp01(0.5f - distance);
                    // A transparent outline requests a pure filled shape without a hollow edge.
                    Color color = outline.a > 0 ? Color.Lerp(outline, fill, Mathf.Clamp01(-distance - 0.5f)) : fill;
                    color.a *= coverage;
                    texture.SetPixel(x, y, color);
                }
            texture.Apply(false, true);
            textures.Add(texture);
            return texture;
        }

        /// <summary>Destroys old generated textures before a theme rebuild or component unload.</summary>
        internal static void DisposeStyles()
        {
            foreach (var texture in textures) if (texture) UnityEngine.Object.Destroy(texture);
            textures.Clear();
            builtRadius = -1;
        }

        #endregion
    }
}
