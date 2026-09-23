using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoR2;
using UnityEngine;
using static UmbraMenu.MenuController;
using static UmbraMenu.MenuWidgets;
using static UmbraMenu.MenuTheme;
using static UmbraMenu.MenuActions;

namespace UmbraMenu
{
    /// <summary>Misc page; owns its local editing state and preserves the user's card layout.</summary>
    internal sealed class MiscPage : IMenuPage
    {
        public string Title { get { return "Misc"; } }
        public string Description { get { return "Telemetry, interface settings and lifecycle controls."; } }

        /// <summary>Provides useful local telemetry and explicit, scene-scoped recovery actions.</summary>
        private void DrawMiscExtras()
        {
            AddCard(0, "ON-SCREEN TELEMETRY", c =>
            {
                DrawToggle(c, "Active mod strip", State.Render.renderMods, v => State.Render.renderMods = v, null);
                DrawToggle(c, "FPS / ping", MiscFeatures.PerformanceHud, v => MiscFeatures.PerformanceHud = v, "Shown on the right side; host ping is 0 ms.");
                DrawToggle(c, "Run timer", MiscFeatures.RunTimer, v => MiscFeatures.RunTimer = v, null);
                DrawToggle(c, "Coordinates", MiscFeatures.Coordinates, v => MiscFeatures.Coordinates = v, null);
                DrawBinding(c, "Player page shortcut", "Open Player");
                DrawBinding(c, "Items page shortcut", "Open Items");
                DrawBinding(c, "World page shortcut", "Open World");
            });
            AddCard(1, "POSITION BOOKMARK", c =>
            {
                DrawParagraph(c, "Save a safe location and return if stuck. The bookmark expires on stage change. Host only.");
                DrawButtonRow(c, new ButtonAction("Save position", () => { MiscFeatures.Bookmark(); Toast("Position saved for this stage"); }), new ButtonAction("Return", MiscFeatures.Return));
            }, true);
        }
        /// <summary>Groups telemetry, window appearance, and lifecycle controls.</summary>
        public void Build(float width)
        {
            DrawMiscExtras();


            AddCard(1, "APPEARANCE", c =>
            {
                DrawSlider(c, "Window opacity", ref Prefs.WindowOpacity, 0.4f, 1f, "0.00");
                DrawSlider(c, "Corner radius (px)", ref Prefs.CornerRadius, 0f, 10f, "0");
                DrawParagraph(c, "Text stays opaque. Panels use subtle fills so the game remains visible through the window.");
                DrawPreferenceActions(c);
            });
            AddCard(2, "UMBRA", c =>
            {
                DrawReadout(c, "Build", UmbraRuntime.VERSION + " / Trident proof");
                DrawButtonRow(c,
                    new ButtonAction("Center window", CenterWindow),
                    new ButtonAction("Unload Umbra", Loader.RequestUnload, true));
            });

        }
    }
}
