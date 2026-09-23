using System;
using UnityEngine;
using static UmbraMenu.MenuTheme;
using static UmbraMenu.MenuController;
using static UmbraMenu.MenuActions;
using static UmbraMenu.MenuWidgets;

namespace UmbraMenu
{
    /// <summary>Shared expandable catalog headers and compact amount/action rows.</summary>
    internal static class CatalogWidgets
    {
        internal static void Header(CardCursor c, string name, string id, Texture icon, bool expanded, Action toggle, Sprite sprite = null)
        {
            float height = Mathf.Max(46, labelStyle.CalcHeight(new GUIContent(name), Mathf.Max(40, c.Width - 82)) + 12);
            if (!c.Measuring)
            {
                if (GUI.Button(new Rect(0, c.Y, c.Width, height), GUIContent.none, expanded ? navActiveStyle : buttonStyle))
                    DeferLayoutChange(() => { GUI.FocusControl(null); toggle(); });
                var image = new Rect(6, c.Y + 6, 32, 32);
                if (sprite)
                {
                    var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                    GUI.DrawTextureWithTexCoords(image, sprite.texture, new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
                }
                else if (icon) GUI.DrawTexture(image, icon, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(46, c.Y + 4, c.Width - 78, height - 8), new GUIContent(name, id), labelStyle);
                GUI.Label(new Rect(c.Width - 28, c.Y, 24, height), expanded ? "−" : "+", badgeStyle);
            }
            c.Advance(height + 6);
        }

        /// <summary>Keeps amount and Give/Drop together where possible; narrow cards wrap without losing controls.</summary>
        internal static void AmountActions(CardCursor c, string id, ref string amount, params ButtonAction[] actions)
        {
            if (c.Width < 210)
            {
                DrawInput(c, "Amount (1–100)", ref amount);
                DrawButtonRow(c, actions);
                return;
            }
            const float inputWidth = 70, gap = 6;
            float buttonWidth = (c.Width - inputWidth - gap * actions.Length) / actions.Length;
            if (!c.Measuring)
            {
                GUI.SetNextControlName("Umbra:Amount:" + id);
                amount = GUI.TextField(new Rect(0, c.Y, inputWidth, 32), amount, 3, inputStyle);
                for (int i = 0; i < actions.Length; i++)
                    if (GUI.Button(new Rect(inputWidth + gap + i * (buttonWidth + gap), c.Y, buttonWidth, 32), actions[i].Label, buttonStyle))
                        SafeAction(actions[i].Action, actions[i].Label);
            }
            c.Advance(40);
        }
    }
}
