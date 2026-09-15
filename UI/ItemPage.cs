using System;
using System.Linq;
using UnityEngine;

namespace UmbraMenu
{
    internal static partial class ModernMenu
    {
        private static readonly string[] itemGroups = { "All", "Common", "Uncommon", "Legendary", "Boss", "Lunar", "Void", "Equipment", "Other", "Inventory" };
        private static string catalogQuery = "", catalogGroup = "All", quantityText = "1", rollText = "5";
        private static ItemService.Entry catalogSelection;
        private static int catalogPage;
        private static string armedAction;
        private static float armedUntil;
        /// <summary>Provides a searchable paginated catalog and explicit give/drop/transfer actions.</summary>
        private static void DrawItems(float width)
        {
            AddCard(0, "ITEM & EQUIPMENT CATALOG", c =>
            {
                DrawInput(c, "Search name / ID", ref catalogQuery);
                DrawChips(c, itemGroups, catalogGroup, g => { catalogGroup = g; catalogPage = 0; });
                var matches = ItemService.Catalog.Where(e => (catalogGroup == "All" || e.Group == catalogGroup || catalogGroup == "Inventory" && ItemService.Owned(e, UmbraMenu.LocalPlayerInv) > 0)
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
                DrawParagraph(c, catalogSelection == null ? "Select an entry from the catalog." : catalogSelection.Name + " • " + catalogSelection.Group);
                DrawReadout(c, "Owned", ItemService.Owned(catalogSelection, UmbraMenu.LocalPlayerInv).ToString());
                DrawInput(c, "Quantity (1–100)", ref quantityText);
                DrawButtonRow(c, new ButtonAction("Give", () => ItemService.Give(catalogSelection, UmbraMenu.LocalPlayer, ItemCount(quantityText))), new ButtonAction("Drop", () => ItemService.Drop(catalogSelection, ItemCount(quantityText), false)));
                DrawButtonRow(c, new ButtonAction("Drop from inventory", () => ItemService.Drop(catalogSelection, ItemCount(quantityText), true)));
                DrawParagraph(c, "Give adds permanent items. Equipment Give replaces the active slot (quantity 1). Drop creates new pickups; Drop from inventory transfers owned copies.");
                DrawButtonRow(c, new ButtonAction("Replace nearest chest", () => ConfirmAction("Replace nearest chest", () => ItemService.ReplaceNearestChest(catalogSelection)), true));
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
        /// <summary>Draws compact wrapping category chips, measuring the same rows in both passes.</summary>
        private static void DrawChips(CardCursor c, string[] choices, string selected, Action<string> select)
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
        /// <summary>Draws an icon and wrapped name with stable selection and exact catalog identity.</summary>
        private static void DrawCatalogRow(CardCursor c, ItemService.Entry entry)
        {
            float height = Mathf.Max(46, labelStyle.CalcHeight(new GUIContent(entry.Name), c.Width - 58) + 12);
            if (!c.Measuring)
            {
                if (GUI.Button(new Rect(0, c.Y, c.Width, height), GUIContent.none, entry == catalogSelection ? navActiveStyle : buttonStyle)) catalogSelection = entry;
                if (entry.Icon) GUI.DrawTexture(new Rect(6, c.Y + 5, 34, 34), entry.Icon, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(48, c.Y + 4, c.Width - 56, height - 8), new GUIContent(entry.Name, entry.Id), labelStyle);
            }
            c.Advance(height + 6);
        }
        /// <summary>Bounds operation sizes before allocating or changing inventory.</summary>
        private static int ItemCount(string value)
        {
            int count; if (!int.TryParse(value, out count) || count < 1 || count > 100) throw new InvalidOperationException("Enter a quantity from 1 to 100."); return count;
        }
        /// <summary>Requires deliberate repeated invocation for bulk/destructive inventory changes.</summary>
        private static void ConfirmAction(string id, Action action)
        {
            if (armedAction != id || Time.unscaledTime > armedUntil) { armedAction = id; armedUntil = Time.unscaledTime + 5; Toast("Click again within 5 seconds: " + id); return; }
            armedAction = null; action(); Toast(id + " completed");
        }
    }
}
