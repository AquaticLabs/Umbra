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
                value = GUI.HorizontalSlider(new Rect(0, c.Y + 30, c.Width, 18), value, min, max, sliderStyle, sliderThumbStyle);
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

        /// <summary>Edits all RGBA channels with a color swatch and hexadecimal readout.</summary>
        internal static void DrawColor(CardCursor c, ref Color color)
        {
            if (!c.Measuring)
            {
                ESPHelper.Fill(new Rect(0, c.Y + 2, 30, 20), color);
                GUI.Label(new Rect(42, c.Y, c.Width - 42, 25), "#" + ColorUtility.ToHtmlStringRGBA(color), valueStyle);
            }
            c.Advance(30);
            DrawColorChannel(c, "R", ref color.r);
            DrawColorChannel(c, "G", ref color.g);
            DrawColorChannel(c, "B", ref color.b);
            DrawColorChannel(c, "A", ref color.a);
            c.Space(6);
        }

        /// <summary>Draws a compact color channel row and its 0–255 numeric value.</summary>
        internal static void DrawColorChannel(CardCursor c, string channel, ref float value)
        {
            if (!c.Measuring)
            {
                GUI.Label(new Rect(0, c.Y, 22, 22), channel, mutedStyle);
                value = GUI.HorizontalSlider(new Rect(28, c.Y + 5, Mathf.Max(30, c.Width - 78), 18), value, 0, 1, sliderStyle, sliderThumbStyle);
                GUI.Label(new Rect(c.Width - 43, c.Y, 43, 22), Mathf.RoundToInt(value * 255).ToString(), valueStyle);
            }
            c.Advance(29);
        }



        /// <summary>Edits one category/item and renders a preview through the same box primitive used in game.</summary>
        internal static void DrawEspStyle(CardCursor c, EspStyle style)
        {
            DrawToggle(c, "Visible", style.Enabled, v => style.Enabled = v, null);
            DrawToggle(c, "Draw boxes", style.Boxes, v => style.Boxes = v, null);
            DrawToggle(c, "Draw labels", style.Labels, v => style.Labels = v, null);
            DrawSlider(c, "Thickness (px)", ref style.Thickness, 1f, 6f, "0.0");
            DrawColor(c, ref style.Color);
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
