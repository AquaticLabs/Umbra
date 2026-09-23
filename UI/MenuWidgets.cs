using static UmbraMenu.MenuController;
using static UmbraMenu.MenuTheme;
using static UmbraMenu.MenuActions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Reusable, measured UI controls. Pages own values; this class owns neither window nor gameplay state.</summary>
    internal static class MenuWidgets
    {
        private static int dragControl;
        private static float dragStartX, dragStartValue;
        private static bool pickerOpen, pickerClosePending;
        private static int pickerColorControl;
        private static string pickerLabel = "Color", pickerHex = "FFFFFFFF";
        private static Color pickerColor = Color.white, pickerOriginal = Color.white;
        private static float pickerHue, pickerSaturation, pickerValue, pickerAlpha = 1f, pickerTextureHue = -1f;
        private static Texture2D hueTexture, saturationValueTexture;

        internal static bool ColorPickerOpen { get { return pickerOpen; } }

        #region Measured controls

        /// <summary>Draws a label/value row with ellipsis-safe widths.</summary>
        internal static void DrawReadout(CardCursor c, string label, string value)
        {
            if (!c.Measuring)
            {
                GUI.Label(new Rect(0, c.Y, c.Width * 0.58f, 26), label, labelStyle);
                GUI.Label(new Rect(c.Width * 0.58f, c.Y, c.Width * 0.42f, 26), new GUIContent(value, value), valueStyle);
            }
            c.Advance(32);
        }

        /// <summary>Measures and draws a text field; narrow cards stack its label above the field.</summary>
        internal static void DrawInput(CardCursor c, string label, ref string value)
        {
            bool stacked = c.Width < 290;
            if (!c.Measuring)
            {
                GUI.Label(new Rect(0, c.Y, stacked ? c.Width : c.Width * 0.48f, 26), label, labelStyle);
                Rect input = stacked ? new Rect(0, c.Y + 27, c.Width, 30) : new Rect(c.Width * 0.5f, c.Y, c.Width * 0.5f, 30);
                GUI.SetNextControlName("Umbra:" + label);
                value = GUI.TextField(input, value, 80, inputStyle);
            }
            c.Advance(stacked ? 65 : 40);
        }

        /// <summary>Draws a switch and measures its wrapped hint instead of reserving a fixed line count.</summary>
        internal static void DrawToggle(CardCursor c, string label, bool current, Action<bool> setter, string hint)
        {
            var binding = KeyBindings.Find(label);
            float reserved = binding == null ? 62 : 132;
            float labelHeight = Mathf.Max(28, labelStyle.CalcHeight(new GUIContent(label), Mathf.Max(60, c.Width - reserved)));
            if (!c.Measuring)
            {
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && (binding == null || !binding.HostOnly || HasHost);
                GUI.Label(new Rect(0, c.Y, c.Width - reserved, labelHeight), label, labelStyle);
                if (GUI.Button(new Rect(c.Width - 52, c.Y, 52, 26), current ? "ON" : "OFF", current ? toggleOnStyle : toggleOffStyle))
                    SafeAction(() => setter(!current), label);
                GUI.enabled = enabled;
                if (binding != null && GUI.Button(new Rect(c.Width - 124, c.Y, 66, 26), KeyBindings.Caption(binding), buttonStyle)) KeyBindings.BeginCapture(binding);
            }
            c.Advance(labelHeight + 8);
            if (!string.IsNullOrEmpty(hint)) DrawParagraph(c, hint);
        }

        /// <summary>Edits a hold key or action binding independently from its enabled state.</summary>
        internal static void DrawBinding(CardCursor c, string label, string key)
        {
            var binding = KeyBindings.Find(key);
            DrawCycle(c, label, KeyBindings.Caption(binding), () => KeyBindings.BeginCapture(binding));
        }

        /// <summary>Cycles discrete options using an explicit labeled button.</summary>
        internal static void DrawCycle(CardCursor c, string label, string value, Action action)
        {
            if (!c.Measuring)
            {
                GUI.Label(new Rect(0, c.Y, c.Width, 23), label, labelStyle);
                if (GUI.Button(new Rect(0, c.Y + 26, c.Width, 30), value + "   >", buttonStyle)) SafeAction(action, label);
            }
            c.Advance(66);
        }

        /// <summary>Draws a slider with stable spacing and an always visible current value.</summary>
        internal static void DrawSlider(CardCursor c, string label, ref float value, float min, float max, string format)
        {
            if (!c.Measuring)
            {
                GUI.Label(new Rect(0, c.Y, c.Width - 60, 26), label, labelStyle);
                GUI.Label(new Rect(c.Width - 60, c.Y, 60, 26), value.ToString(format, CultureInfo.InvariantCulture), valueStyle);
                value = DrawValueSlider(new Rect(0, c.Y + 29, c.Width, 16), value, min, max);
            }
            c.Advance(58);
        }

        /// <summary>Rounds integer slider values only during the interactive pass.</summary>
        internal static void DrawSlider(CardCursor c, string label, ref int value, int min, int max)
        {
            float temporary = value;
            DrawSlider(c, label, ref temporary, min, max, "0");
            if (!c.Measuring) value = Mathf.RoundToInt(temporary);
        }

        /// <summary>Draws an explicitly centered track/thumb and owns pointer capture for click-drag changes.</summary>
        private static float DrawValueSlider(Rect rect, float value, float min, float max)
        {
            const float thumbWidth = 10f, thumbHeight = 12f, trackHeight = 4f;
            int id = GUIUtility.GetControlID(FocusType.Passive, rect);
            Event current = Event.current;
            if (GUI.enabled && current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                GUIUtility.hotControl = id;
                value = SliderValueAt(rect, current.mousePosition.x, min, max, thumbWidth);
                current.Use();
            }
            if (GUIUtility.hotControl == id && current.type == EventType.MouseDrag)
            {
                value = SliderValueAt(rect, current.mousePosition.x, min, max, thumbWidth);
                current.Use();
            }
            if (GUIUtility.hotControl == id && current.rawType == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                current.Use();
            }
            float centerY = rect.y + rect.height * 0.5f;
            Rect track = new Rect(rect.x, centerY - trackHeight * 0.5f, rect.width, trackHeight);
            float normalized = Mathf.InverseLerp(min, max, value);
            float centerX = Mathf.Lerp(rect.x + thumbWidth * 0.5f, rect.xMax - thumbWidth * 0.5f, normalized);
            Rect thumb = new Rect(centerX - thumbWidth * 0.5f, centerY - thumbHeight * 0.5f, thumbWidth, thumbHeight);
            GUI.Box(track, GUIContent.none, sliderStyle);
            GUI.Box(thumb, GUIContent.none, sliderThumbStyle);
            return Mathf.Clamp(value, min, max);
        }

        private static float SliderValueAt(Rect rect, float mouseX, float min, float max, float thumbWidth)
        {
            float normalized = Mathf.InverseLerp(rect.x + thumbWidth * 0.5f, rect.xMax - thumbWidth * 0.5f, mouseX);
            return Mathf.Lerp(min, max, normalized);
        }

        /// <summary>Measures the actual wrapped height of explanatory text.</summary>
        internal static void DrawParagraph(CardCursor c, string value)
        {
            float height = Mathf.Max(20, mutedStyle.CalcHeight(new GUIContent(value), c.Width));
            if (!c.Measuring) GUI.Label(new Rect(0, c.Y, c.Width, height), value, mutedStyle);
            c.Advance(height + 10);
        }

        /// <summary>Wraps action buttons to additional rows when the card is narrow.</summary>
        internal static void DrawButtonRow(CardCursor c, params ButtonAction[] actions)
        {
            int perRow = Mathf.Max(1, Mathf.Min(actions.Length, Mathf.FloorToInt((c.Width + 8) / 104f)));
            float width = (c.Width - 8 * (perRow - 1)) / perRow;
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                if (!c.Measuring && GUI.Button(new Rect((i % perRow) * (width + 8), c.Y + (i / perRow) * 40, width, 32), action.Label, action.Danger ? dangerButtonStyle : buttonStyle))
                    SafeAction(action.Action, action.Label);
            }
            c.Advance(Mathf.CeilToInt(actions.Length / (float)perRow) * 40);
        }

        /// <summary>Edits RGBA values with ImGui-style horizontal drags and a click-to-open swatch.</summary>
        internal static void DrawColor(CardCursor c, ref Color color, string label = "Color")
        {
            const float gap = 4f, swatchWidth = 26f;
            float fieldMinimum = 0f;
            foreach (string channel in new[] { "R:255", "G:255", "B:255", "A:255" })
                fieldMinimum = Mathf.Max(fieldMinimum, colorFieldStyle.CalcSize(new GUIContent(channel)).x + 4f);
            float labelWidth = colorLabelStyle.CalcSize(new GUIContent(label)).x;
            float previewWidth = swatchWidth + 6f + labelWidth;
            bool stacked = c.Width < fieldMinimum * 4f + gap * 3f + 8f + previewWidth;
            float fieldsWidth = stacked ? c.Width : c.Width - previewWidth - 8f;
            int columns = fieldsWidth < fieldMinimum * 4f + gap * 3f ? 2 : 4;
            float fieldWidth = (fieldsWidth - gap * (columns - 1)) / columns;
            float fieldsHeight = columns == 4 ? 26f : 56f;
            if (!c.Measuring)
            {
                int colorControl = GUIUtility.GetControlID(label.GetHashCode(), FocusType.Passive, new Rect(0, c.Y, c.Width, fieldsHeight));
                if (pickerColorControl == colorControl)
                {
                    color = pickerColor;
                    if (pickerClosePending) { pickerClosePending = false; pickerColorControl = 0; }
                }
                byte r = ToByte(color.r), g = ToByte(color.g), b = ToByte(color.b), a = ToByte(color.a);
                byte nr = DrawByteDrag(new Rect(0, c.Y, fieldWidth, 26), "R", r);
                byte ng = DrawByteDrag(new Rect(fieldWidth + gap, c.Y, fieldWidth, 26), "G", g);
                byte nb = DrawByteDrag(new Rect(columns == 4 ? (fieldWidth + gap) * 2 : 0, c.Y + (columns == 4 ? 0 : 30), fieldWidth, 26), "B", b);
                byte na = DrawByteDrag(new Rect(columns == 4 ? (fieldWidth + gap) * 3 : fieldWidth + gap, c.Y + (columns == 4 ? 0 : 30), fieldWidth, 26), "A", a);
                if (nr != r || ng != g || nb != b || na != a) color = new Color32(nr, ng, nb, na);
                Rect swatch = new Rect(stacked ? 0 : fieldsWidth + 8f, c.Y + (stacked ? fieldsHeight + 4f : 0f), swatchWidth, 26f);
                if (GUI.Button(swatch, GUIContent.none, buttonStyle)) OpenColorPicker(colorControl, label, color);
                ESPHelper.Fill(new Rect(swatch.x + 4, swatch.y + 4, swatch.width - 8, swatch.height - 8), color);
                GUI.Label(new Rect(swatch.xMax + 6, swatch.y, Mathf.Max(0, c.Width - swatch.xMax - 6), 26), label, colorLabelStyle);
            }
            c.Advance(fieldsHeight + (stacked ? 30f : 0f) + 10f);
        }

        /// <summary>Draws a compact 0–255 field whose horizontal drag clamps at the channel limits.</summary>
        private static byte DrawByteDrag(Rect rect, string channel, byte value)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive, rect);
            Event current = Event.current;
            if (GUI.enabled && current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                dragControl = id; dragStartX = current.mousePosition.x; dragStartValue = value;
                GUIUtility.hotControl = id; current.Use();
            }
            if (GUIUtility.hotControl == id && dragControl == id && current.type == EventType.MouseDrag)
            {
                value = (byte)Mathf.Clamp(Mathf.RoundToInt(dragStartValue + current.mousePosition.x - dragStartX), 0, 255);
                current.Use();
            }
            if (GUIUtility.hotControl == id && current.rawType == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0; dragControl = 0; current.Use();
            }
            GUI.Box(rect, channel + ":" + value, colorFieldStyle);
            return value;
        }

        private static byte ToByte(float value) { return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255); }

        private static void OpenColorPicker(int control, string label, Color color)
        {
            pickerColorControl = control;
            pickerLabel = string.IsNullOrEmpty(label) ? "Color" : label;
            pickerOriginal = color;
            pickerOpen = true;
            pickerClosePending = false;
            SetPickerColor(color);
        }

        internal static void CloseColorPicker(bool keepCurrent)
        {
            if (!pickerOpen) return;
            if (!keepCurrent) SetPickerColor(pickerOriginal);
            pickerOpen = false;
            pickerClosePending = true;
            GUIUtility.hotControl = 0;
            dragControl = 0;
        }

        /// <summary>Draws the shared modal color editor above cards so scroll/group clipping cannot cut it off.</summary>
        internal static void DrawColorPickerOverlay()
        {
            if (!pickerOpen) return;
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            { CloseColorPicker(false); current.Use(); return; }

            float popupWidth = Mathf.Min(430f, Mathf.Max(270f, WindowRect.width - 30f));
            float popupHeight = Mathf.Min(430f, Mathf.Max(390f, WindowRect.height - 40f));
            Rect popup = new Rect((WindowRect.width - popupWidth) * 0.5f, (WindowRect.height - popupHeight) * 0.5f, popupWidth, popupHeight);
            ESPHelper.Fill(new Rect(0, 65, WindowRect.width, WindowRect.height - 65), new Color(0f, 0.02f, 0.05f, 0.68f));
            GUI.BeginGroup(popup);
            Panel(new Rect(0, 0, popupWidth, popupHeight), windowStyle, 0.99f);
            Panel(new Rect(1, 1, popupWidth - 2, 44), headerStyle, 0.98f);
            GUI.Label(new Rect(16, 7, popupWidth - 58, 30), pickerLabel, titleStyle);
            if (GUI.Button(new Rect(popupWidth - 39, 8, 28, 27), "×", buttonStyle)) CloseColorPicker(true);

            float editorTop = 56f;
            float editorBottom = popupHeight - 132f;
            float editorHeight = Mathf.Max(150f, editorBottom - editorTop);
            float svWidth = Mathf.Max(100f, popupWidth - 170f);
            Rect svRect = new Rect(16, editorTop, svWidth, editorHeight);
            Rect hueRect = new Rect(svRect.xMax + 8, editorTop, 18, editorHeight);
            float previewX = hueRect.xMax + 10;
            float previewWidth = Mathf.Max(54f, popupWidth - previewX - 16);
            EnsurePickerTextures();
            GUI.DrawTexture(svRect, saturationValueTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hueRect, hueTexture, ScaleMode.StretchToFill);

            Vector2 normalized;
            if (DragPickerArea(svRect, 19231, out normalized))
            { pickerSaturation = normalized.x; pickerValue = 1f - normalized.y; SyncPickerFromHsv(); }
            if (DragPickerArea(hueRect, 19232, out normalized))
            { pickerHue = normalized.y; SyncPickerFromHsv(); }

            Vector2 svPoint = new Vector2(svRect.x + pickerSaturation * svRect.width, svRect.y + (1f - pickerValue) * svRect.height);
            ESPHelper.Circle(svPoint, 6f, Color.white, 2f);
            ESPHelper.Fill(new Rect(hueRect.x - 2, hueRect.y + pickerHue * hueRect.height - 1, hueRect.width + 4, 2), Color.white);

            GUI.Label(new Rect(previewX, editorTop, previewWidth, 20), "Current", mutedStyle);
            Rect currentSwatch = new Rect(previewX, editorTop + 22, previewWidth, 48);
            GUI.Box(currentSwatch, GUIContent.none, buttonStyle);
            ESPHelper.Fill(new Rect(currentSwatch.x + 4, currentSwatch.y + 4, currentSwatch.width - 8, currentSwatch.height - 8), pickerColor);
            GUI.Label(new Rect(previewX, editorTop + 80, previewWidth, 20), "Original", mutedStyle);
            Rect originalSwatch = new Rect(previewX, editorTop + 102, previewWidth, 48);
            if (GUI.Button(originalSwatch, GUIContent.none, buttonStyle)) SetPickerColor(pickerOriginal);
            ESPHelper.Fill(new Rect(originalSwatch.x + 4, originalSwatch.y + 4, originalSwatch.width - 8, originalSwatch.height - 8), pickerOriginal);

            float rowY = popupHeight - 120f;
            DrawPickerRgbaRow(new Rect(16, rowY, popupWidth - 32, 26));
            DrawPickerHsvaRow(new Rect(16, rowY + 31, popupWidth - 32, 26));
            DrawPickerHex(new Rect(16, rowY + 62, popupWidth - 32, 26));
            float buttonWidth = (popupWidth - 40) * 0.5f;
            if (GUI.Button(new Rect(16, popupHeight - 32, buttonWidth, 24), "Cancel", buttonStyle)) CloseColorPicker(false);
            if (GUI.Button(new Rect(24 + buttonWidth, popupHeight - 32, buttonWidth, 24), "Apply", toggleOnStyle)) CloseColorPicker(true);
            GUI.EndGroup();
        }

        private static bool DragPickerArea(Rect rect, int hint, out Vector2 normalized)
        {
            int id = GUIUtility.GetControlID(hint, FocusType.Passive, rect);
            Event current = Event.current;
            bool changed = false;
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            { GUIUtility.hotControl = id; changed = true; current.Use(); }
            else if (GUIUtility.hotControl == id && current.type == EventType.MouseDrag)
            { changed = true; current.Use(); }
            if (GUIUtility.hotControl == id && current.rawType == EventType.MouseUp)
            { GUIUtility.hotControl = 0; current.Use(); }
            normalized = new Vector2(Mathf.InverseLerp(rect.x, rect.xMax, current.mousePosition.x), Mathf.InverseLerp(rect.y, rect.yMax, current.mousePosition.y));
            return changed;
        }

        private static void DrawPickerRgbaRow(Rect rect)
        {
            byte r = ToByte(pickerColor.r), g = ToByte(pickerColor.g), b = ToByte(pickerColor.b), a = ToByte(pickerAlpha);
            float gap = 5, width = (rect.width - gap * 3) * .25f;
            byte nr = DrawByteDrag(new Rect(rect.x, rect.y, width, rect.height), "R", r);
            byte ng = DrawByteDrag(new Rect(rect.x + width + gap, rect.y, width, rect.height), "G", g);
            byte nb = DrawByteDrag(new Rect(rect.x + (width + gap) * 2, rect.y, width, rect.height), "B", b);
            byte na = DrawByteDrag(new Rect(rect.x + (width + gap) * 3, rect.y, width, rect.height), "A", a);
            if (nr != r || ng != g || nb != b || na != a) SetPickerColor(new Color32(nr, ng, nb, na));
        }

        private static void DrawPickerHsvaRow(Rect rect)
        {
            byte h = ToByte(pickerHue), s = ToByte(pickerSaturation), v = ToByte(pickerValue), a = ToByte(pickerAlpha);
            float gap = 5, width = (rect.width - gap * 3) * .25f;
            byte nh = DrawByteDrag(new Rect(rect.x, rect.y, width, rect.height), "H", h);
            byte ns = DrawByteDrag(new Rect(rect.x + width + gap, rect.y, width, rect.height), "S", s);
            byte nv = DrawByteDrag(new Rect(rect.x + (width + gap) * 2, rect.y, width, rect.height), "V", v);
            byte na = DrawByteDrag(new Rect(rect.x + (width + gap) * 3, rect.y, width, rect.height), "A", a);
            if (nh != h || ns != s || nv != v || na != a)
            { pickerHue = nh / 255f; pickerSaturation = ns / 255f; pickerValue = nv / 255f; pickerAlpha = na / 255f; SyncPickerFromHsv(); }
        }

        private static void DrawPickerHex(Rect rect)
        {
            GUI.SetNextControlName("Umbra:ColorHex");
            string next = GUI.TextField(rect, "#" + pickerHex, 9, inputStyle);
            next = next.Trim().TrimStart('#').ToUpperInvariant();
            if (next == pickerHex) return;
            pickerHex = next;
            Color parsed;
            if ((next.Length == 6 || next.Length == 8) && ColorUtility.TryParseHtmlString("#" + next, out parsed)) SetPickerColor(parsed);
        }

        private static void SetPickerColor(Color color)
        {
            pickerColor = VisualSettings.ClampColor(color);
            pickerAlpha = pickerColor.a;
            Color.RGBToHSV(pickerColor, out pickerHue, out pickerSaturation, out pickerValue);
            pickerHex = ColorUtility.ToHtmlStringRGBA(pickerColor);
        }

        private static void SyncPickerFromHsv()
        {
            Color rgb = Color.HSVToRGB(Mathf.Repeat(pickerHue, 1f), Mathf.Clamp01(pickerSaturation), Mathf.Clamp01(pickerValue));
            rgb.a = Mathf.Clamp01(pickerAlpha);
            pickerColor = rgb;
            pickerHex = ColorUtility.ToHtmlStringRGBA(pickerColor);
        }

        private static void EnsurePickerTextures()
        {
            if (!hueTexture)
            {
                hueTexture = new Texture2D(8, 256, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < hueTexture.height; y++)
                {
                    Color color = Color.HSVToRGB(1f - y / (hueTexture.height - 1f), 1f, 1f);
                    for (int x = 0; x < hueTexture.width; x++) hueTexture.SetPixel(x, y, color);
                }
                hueTexture.Apply(false, true);
            }
            if (saturationValueTexture && Mathf.Abs(pickerTextureHue - pickerHue) < .001f) return;
            if (saturationValueTexture) UnityEngine.Object.Destroy(saturationValueTexture);
            saturationValueTexture = new Texture2D(128, 128, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < saturationValueTexture.height; y++)
                for (int x = 0; x < saturationValueTexture.width; x++)
                    saturationValueTexture.SetPixel(x, y, Color.HSVToRGB(pickerHue, x / (saturationValueTexture.width - 1f), y / (saturationValueTexture.height - 1f)));
            saturationValueTexture.Apply(false, true);
            pickerTextureHue = pickerHue;
        }

        internal static void DisposeColorPicker()
        {
            if (hueTexture) UnityEngine.Object.Destroy(hueTexture);
            if (saturationValueTexture) UnityEngine.Object.Destroy(saturationValueTexture);
            hueTexture = saturationValueTexture = null;
            pickerTextureHue = -1f; pickerOpen = pickerClosePending = false; pickerColorControl = 0;
        }



        /// <summary>Edits one category/item and renders a preview through the same box primitive used in game.</summary>
        internal static void DrawEspStyle(CardCursor c, EspStyle style)
        {
            DrawToggle(c, "Visible", style.Enabled, v => style.Enabled = v, null);
            DrawToggle(c, "Draw boxes", style.Boxes, v => style.Boxes = v, null);
            DrawToggle(c, "Draw labels", style.Labels, v => style.Labels = v, null);
            DrawSlider(c, "Thickness (px)", ref style.Thickness, 1f, 6f, "0.0");
            DrawColor(c, ref style.Color, "Style color");
            if (!c.Measuring)
            {
                Rect preview = new Rect(4, c.Y + 3, c.Width - 8, 45);
                ESPHelper.Box(preview, style.Color, style.Thickness, Prefs.CornerBoxes);
                GUI.Label(preview, "Style preview", badgeStyle);
            }
            c.Advance(60);
        }



        /// <summary>Saves/restores all visual settings together; gameplay toggles are excluded.</summary>
        internal static void DrawPreferenceActions(CardCursor c)
        {
            DrawButtonRow(c, new ButtonAction("Save visuals", () => { VisualSettings.Save(); Toast("Visual preferences saved"); }),
                new ButtonAction("Reset visuals", () => { VisualSettings.Current = VisualSettings.CreateDefaults(); Toast("Visual defaults restored"); }));
            DrawParagraph(c, "Visual preferences also save when you close Umbra.");
        }

        /// <summary>Converts stable enum keys into readable labels.</summary>
        internal static string SplitName(string value) { return System.Text.RegularExpressions.Regex.Replace(value, "(?<=[a-z])(?=[A-Z])", " "); }

        /// <summary>Draws compact wrapping category chips, measuring the same rows in both passes.</summary>
        internal static void DrawChips(CardCursor c, string[] choices, string selected, Action<string> select)
        {
            int columns = Mathf.Max(1, Mathf.FloorToInt((c.Width + 6) / 96));
            float width = (c.Width - (columns - 1) * 6) / columns;
            for (int i = 0; i < choices.Length; i++)
            {
                string choice = choices[i];
                if (!c.Measuring && GUI.Button(new Rect((i % columns) * (width + 6), c.Y + (i / columns) * 34, width, 28), choice, selected == choice ? navActiveStyle : buttonStyle)) select(choice);
            }
            c.Advance(Mathf.CeilToInt(choices.Length / (float)columns) * 34 + 8);
        }
        #endregion
    }
}
