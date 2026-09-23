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
    /// <summary>Visuals page; owns its local editing state and preserves the user's card layout.</summary>
    internal sealed class VisualsPage : IMenuPage
    {
        public string Title { get { return "Visuals"; } }
        public string Description { get { return "World overlays, category styling and item-specific overrides."; } }

        private EspCategory selectedCategory;
        private bool otherOverrides;
        private string query = "", measuredQuery = "", expandedKey;
        private int overridePage, overridePages = 1, matchCount;
        private OverrideEntry[] visible = new OverrideEntry[0];

        private sealed class OverrideEntry
        {
            public string Key, Name;
            public EspCategory Category;
            public Texture Icon;
        }
        /// <summary>Global overlays, per-category styling, and exact item overrides.</summary>
        public void Build(float width)
        {
            AddCard(2, "VISUAL PREFERENCES", DrawPreferenceActions);

            AddCard(0, "WORLD OVERLAYS", c =>
            {
                DrawToggle(c, "Enemy ESP", State.Render.renderMobs, v => State.Render.renderMobs = v, null);
                DrawToggle(c, "Interactable ESP", State.Render.renderInteractables, SetInteractableEsp, null);
                DrawToggle(c, "Teleporter", Prefs.Teleporter, v => Prefs.Teleporter = v, null);
                DrawToggle(c, "Dropped pickups", Prefs.Pickups, v => Prefs.Pickups = v, null);
                DrawToggle(c, "Allies", Prefs.Allies, v => Prefs.Allies = v, null);
                DrawToggle(c, "Distance labels", Prefs.Distances, v => Prefs.Distances = v, null);
                DrawToggle(c, "Health bars", Prefs.HealthBars, v => Prefs.HealthBars = v, null);
                DrawToggle(c, "Corner boxes", Prefs.CornerBoxes, v => Prefs.CornerBoxes = v, null);
                DrawSlider(c, "Render range (m)", ref Prefs.MaxDistance, 25f, 1000f, "0");
                DrawSlider(c, "Label size (px)", ref Prefs.FontSize, 10, 20);
                DrawButtonRow(c, new ButtonAction("Enable essentials", EnableEssentialVisuals), new ButtonAction("Disable all", DisableVisuals));
                DrawButtonRow(c, new ButtonAction("Refresh objects", RefreshWorld));
            });
            AddCard(1, "CATEGORY STYLE", c =>
            {
                DrawCategoryPicker(c);
                DrawEspStyle(c, VisualSettings.For(selectedCategory));
                DrawParagraph(c, "Each category has independent visibility, boxes, labels, RGBA color, and thickness.");
            });
            AddCard(1, "RETICLE", c =>
            {
                DrawToggle(c, "Crosshair", Prefs.Crosshair, v => Prefs.Crosshair = v, null);
                DrawSlider(c, "Crosshair thickness", ref Prefs.CrosshairThickness, 1f, 6f, "0.0");
                DrawColor(c, ref Prefs.CrosshairColor, "Crosshair");
                DrawToggle(c, "FOV guide", Prefs.ShowFov, v => Prefs.ShowFov = v, null);
                DrawSlider(c, "FOV thickness (px)", ref Prefs.FovThickness, 1f, 6f, "0.0");
                DrawColor(c, ref Prefs.FovColor, "FOV color");
                DrawReadout(c, "Current target", ModernAimbot.TargetName);
            });
            AddCard(2, "ESP OVERRIDES", DrawOverrides);
        }
        /// <summary>Offers direct access to every category without cycling through hidden controls.</summary>
        private void DrawCategoryPicker(CardCursor c)
        {
            var categories = (EspCategory[])Enum.GetValues(typeof(EspCategory));
            int columns = c.Width >= 310 ? 3 : 2;
            float width = (c.Width - (columns - 1) * 6) / columns;
            for (int i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                if (!c.Measuring && GUI.Button(new Rect((i % columns) * (width + 6), c.Y + (i / columns) * 33, width, 28),
                    SplitName(category.ToString()), category == selectedCategory ? navActiveStyle : buttonStyle)) selectedCategory = category;
            }
            c.Advance(Mathf.CeilToInt(categories.Length / (float)columns) * 33 + 12);
            DrawReadout(c, "Editing", SplitName(selectedCategory.ToString()));
        }
        /// <summary>Separates pickup styles from specific bodies and interactables, with inline editors.</summary>
        private void DrawOverrides(CardCursor c)
        {
            if (!c.Measuring)
            {
                float half = (c.Width - 8) / 2;
                if (GUI.Button(new Rect(0, c.Y, half, 40), "Item Overrides", !otherOverrides ? navActiveStyle : buttonStyle))
                    DeferLayoutChange(() => { otherOverrides = false; overridePage = 0; expandedKey = null; });
                if (GUI.Button(new Rect(half + 8, c.Y, half, 40), "Other Overrides", otherOverrides ? navActiveStyle : buttonStyle))
                    DeferLayoutChange(() => { otherOverrides = true; overridePage = 0; expandedKey = null; });
            }
            c.Advance(48);
            DrawInput(c, "Search name / ID", ref query);
            if (c.Measuring)
            {
                if (measuredQuery != query) { measuredQuery = query; overridePage = 0; expandedKey = null; }
                var entries = new List<OverrideEntry>();
                if (otherOverrides)
                {
                    EspTargetCatalog.Refresh();
                    entries.AddRange(EspTargetCatalog.Entries.Select(e => new OverrideEntry { Key = e.Key, Name = e.Name, Category = e.Category }));
                }
                else
                {
                    foreach (var index in ItemCatalog.allItems)
                    {
                        var item = ItemCatalog.GetItemDef(index);
                        if (item) entries.Add(new OverrideEntry { Key = "item:" + item.name, Name = Language.GetString(item.nameToken),
                            Category = EspRenderer.ItemCategory(item), Icon = item.pickupIconTexture });
                    }
                }
                var matches = entries.Where(e => e.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).OrderBy(e => e.Name).ToArray();
                matchCount = matches.Length;
                overridePages = Math.Max(1, (matchCount + 7) / 8);
                overridePage = Mathf.Clamp(overridePage, 0, overridePages - 1);
                visible = matches.Skip(overridePage * 8).Take(8).ToArray();
            }
            DrawReadout(c, matchCount + " results", (overridePage + 1) + " / " + overridePages);
            foreach (var entry in visible)
            {
                bool expanded = expandedKey == entry.Key;
                CatalogWidgets.Header(c, entry.Name, entry.Key, entry.Icon, expanded, () => expandedKey = expanded ? null : entry.Key);
                if (!expanded) continue;
                var custom = Prefs.Styles.FirstOrDefault(style => style.Key == entry.Key);
                DrawReadout(c, "Style", custom != null ? "Specific override" : "Inherits " + SplitName(entry.Category.ToString()));
                if (custom == null)
                    DrawButtonRow(c, new ButtonAction("Customize this object", () => DeferLayoutChange(() => VisualSettings.OverrideObject(entry.Key, entry.Category))));
                else
                {
                    DrawEspStyle(c, custom);
                    DrawButtonRow(c, new ButtonAction("Use category style", () => DeferLayoutChange(() => Prefs.Styles.RemoveAll(style => style.Key == entry.Key))));
                }
            }
            if (matchCount == 0) DrawParagraph(c, otherOverrides ? "No matching objects yet. Spawn assets load in the background; scene objects are added during a run." : "No matching items loaded.");
            DrawButtonRow(c,
                new ButtonAction("Previous", () => DeferLayoutChange(() => { overridePage = Math.Max(0, overridePage - 1); expandedKey = null; })),
                new ButtonAction("Next", () => DeferLayoutChange(() => { overridePage = Math.Min(overridePages - 1, overridePage + 1); expandedKey = null; })));
            DrawParagraph(c, otherOverrides
                ? "Specific enemies, bosses, chests, printers and other objects. Every chest variant inherits Chest unless it has its own override."
                : "Item styles also apply to revealed chest contents. Remove an override to inherit its tier again.");
        }
    }
}

