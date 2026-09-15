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
    /// <summary>Items page; owns its local editing state and preserves the user's card layout.</summary>
    internal sealed class ItemsPage : IMenuPage
    {
        public string Title { get { return "Items"; } }
        public string Description { get { return "Browse items and equipment; give, drop, roll and manage inventory."; } }

        private readonly string[] itemGroups = { "All", "Common", "Uncommon", "Legendary", "Boss", "Lunar", "Void", "Equipment", "Other", "Inventory" };
        private string catalogQuery = "", catalogGroup = "All", quantityText = "1", rollText = "5";

        private int catalogPage;


        /// <summary>Provides a searchable paginated catalog and explicit give/drop/transfer actions.</summary>
        public void Build(float width)
        {
            AddCard(0, "ITEM & EQUIPMENT CATALOG", c =>
            {
                DrawInput(c, "Search name / ID", ref catalogQuery);
                DrawChips(c, itemGroups, catalogGroup, g => { catalogGroup = g; catalogPage = 0; });
                var matches = ItemService.Catalog.Where(e => (catalogGroup == "All" || e.Group == catalogGroup || catalogGroup == "Inventory" && ItemService.Owned(e, UmbraRuntime.LocalPlayerInv) > 0)
                    && (e.Name.IndexOf(catalogQuery, StringComparison.OrdinalIgnoreCase) >= 0 || e.Id.IndexOf(catalogQuery, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                int pages = Math.Max(1, (matches.Count + 7) / 8);
                catalogPage = Mathf.Clamp(catalogPage, 0, pages - 1);
                DrawReadout(c, matches.Count + " results", (catalogPage + 1) + " / " + pages);
                foreach (var entry in matches.Skip(catalogPage * 8).Take(8)) DrawCatalogRow(c, entry);
                DrawButtonRow(c, new ButtonAction("Previous", () => catalogPage = Math.Max(0, catalogPage - 1)), new ButtonAction("Next", () => catalogPage = Math.Min(pages - 1, catalogPage + 1)));
                DrawParagraph(c, "Unavailable run/DLC pickups remain browseable; actions validate availability. Inventory shows permanent owned stacks.");
            });
            AddCard(1, "SELECTED PICKUP", c =>
            {
                DrawParagraph(c, SelectedItem == null ? "Select an entry from the catalog." : SelectedItem.Name + " • " + SelectedItem.Group);
                DrawReadout(c, "Owned", ItemService.Owned(SelectedItem, UmbraRuntime.LocalPlayerInv).ToString());
                DrawInput(c, "Quantity (1–100)", ref quantityText);
                DrawButtonRow(c, new ButtonAction("Give", () => ItemService.Give(SelectedItem, UmbraRuntime.LocalPlayer, ItemCount(quantityText))), new ButtonAction("Drop", () => ItemService.Drop(SelectedItem, ItemCount(quantityText), false)));
                DrawButtonRow(c, new ButtonAction("Drop from inventory", () => ItemService.Drop(SelectedItem, ItemCount(quantityText), true)));
                DrawParagraph(c, "Give adds permanent items. Equipment Give replaces the active slot (quantity 1). Drop creates new pickups; Drop from inventory transfers owned copies.");
                DrawButtonRow(c, new ButtonAction("Replace nearest chest", () => ConfirmAction("Replace nearest chest", () => ItemService.ReplaceNearestChest(SelectedItem)), true));
                DrawParagraph(c, "Chest replacement affects the nearest unopened chest within 25 m. Click twice to confirm.");
            }, true);
            AddCard(1, "RANDOM ITEMS", c =>
            {
                DrawInput(c, "Roll count (1–100)", ref rollText);
                DrawReadout(c, "Pool", catalogGroup == "Inventory" ? "All" : catalogGroup);
                DrawButtonRow(c, new ButtonAction("Roll items", () => { ItemService.Roll(ItemCount(rollText), catalogGroup == "Inventory" ? "All" : catalogGroup); Toast("Random items given"); }));
                DrawParagraph(c, "Uniform random rolls from eligible items in the selected tier. Use All for every eligible item tier.");
            }, true);
            AddCard(1, "INVENTORY TOOLS", c =>
            {
                DrawButtonRow(c, new ButtonAction("Give all +1", () => ConfirmAction("Give all +1", ItemService.GiveAll)), new ButtonAction("Restack inventory", () => ConfirmAction("Restack inventory", ItemService.Restack), true));
                DrawButtonRow(c, new ButtonAction("Clear removable items", () => ConfirmAction("Clear removable items", ItemService.ClearInventory), true));
                DrawParagraph(c, "Bulk tools require a second click within 5 seconds. Clear preserves hidden/internal items and equipment. Restack uses Shrine of Order rules.");
            }, true);
        }

        /// <summary>Draws an icon and wrapped name with stable selection and exact catalog identity.</summary>
        private void DrawCatalogRow(CardCursor c, ItemService.Entry entry)
        {
            float height = Mathf.Max(46, labelStyle.CalcHeight(new GUIContent(entry.Name), c.Width - 58) + 12);
            if (!c.Measuring)
            {
                if (GUI.Button(new Rect(0, c.Y, c.Width, height), GUIContent.none, entry == SelectedItem ? navActiveStyle : buttonStyle)) SelectedItem = entry;
                if (entry.Icon) GUI.DrawTexture(new Rect(6, c.Y + 5, 34, 34), entry.Icon, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(48, c.Y + 4, c.Width - 56, height - 8), new GUIContent(entry.Name, entry.Id), labelStyle);
            }
            c.Advance(height + 6);
        }


    }
}
