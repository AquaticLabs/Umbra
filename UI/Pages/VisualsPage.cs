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
        private string itemQuery = "";
        private ItemDef selectedItem;
        private string lastItemQuery;
        private readonly List<ItemDef> itemMatches = new List<ItemDef>();
        /// <summary>Global overlays, per-category styling, and exact item overrides.</summary>
        public void Build(float width)
        {
            if (selectedItem && !Prefs.Styles.Any(s => s.Key == "item:" + selectedItem.name)) selectedItem = null;
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
            AddCard(0, "ITEM OVERRIDES", c =>
            {
                DrawInput(c, "Name / catalog ID", ref itemQuery);
                DrawItemMatches(c);
                if (selectedItem != null)
                {
                    DrawReadout(c, "Selected", selectedItem.name);
                    DrawEspStyle(c, VisualSettings.ForItem(selectedItem.name, EspRenderer.ItemCategory(selectedItem)));
                    DrawButtonRow(c, new ButtonAction("Use tier style", () => { Prefs.Styles.RemoveAll(s => s.Key == "item:" + selectedItem.name); selectedItem = null; }));
                }
                DrawParagraph(c, "Choose a search result to create its own style. Item colors also apply to revealed chest contents.");
            });
            AddCard(1, "RETICLE", c =>
            {
                DrawToggle(c, "Crosshair", Prefs.Crosshair, v => Prefs.Crosshair = v, null);
                DrawSlider(c, "Crosshair thickness", ref Prefs.CrosshairThickness, 1f, 6f, "0.0");
                DrawColor(c, ref Prefs.CrosshairColor);
                DrawToggle(c, "FOV guide", Prefs.ShowFov, v => Prefs.ShowFov = v, null);
                DrawSlider(c, "FOV thickness (px)", ref Prefs.FovThickness, 1f, 6f, "0.0");
                DrawColor(c, ref Prefs.FovColor);
                DrawReadout(c, "Current target", ModernAimbot.TargetName);
            });
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
        /// <summary>Caches six matching catalog entries; selecting a result creates an independent override.</summary>
        private void DrawItemMatches(CardCursor c)
        {
            if (c.Measuring && lastItemQuery != itemQuery)
            {
                lastItemQuery = itemQuery;
                itemMatches.Clear();
                if (!string.IsNullOrWhiteSpace(itemQuery))
                    foreach (var index in ItemCatalog.allItems)
                    {
                        var item = ItemCatalog.GetItemDef(index);
                        if (!item) continue;
                        if (item.name.IndexOf(itemQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                            Language.GetString(item.nameToken).IndexOf(itemQuery, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        itemMatches.Add(item);
                        if (itemMatches.Count == 6) break;
                    }
            }
            foreach (var item in itemMatches)
                DrawButtonRow(c, new ButtonAction(Language.GetString(item.nameToken), () =>
                {
                    VisualSettings.OverrideItem(item.name, EspRenderer.ItemCategory(item));
                    selectedItem = item;
                }));
            if (itemMatches.Count == 0) DrawParagraph(c, string.IsNullOrWhiteSpace(itemQuery) ? "Search by display name or catalog ID." : "No matching items loaded.");
        }
    }
}

