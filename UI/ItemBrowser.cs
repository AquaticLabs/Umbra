using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using static UmbraMenu.MenuController;
using static UmbraMenu.MenuWidgets;
using static UmbraMenu.MenuActions;

namespace UmbraMenu
{
    /// <summary>Reusable item catalog for local inventory tools and a fixed lobby recipient.</summary>
    internal sealed class ItemBrowser
    {
        private static readonly string[] groups = { "All", "Common", "Uncommon", "Legendary", "Boss", "Lunar", "Void", "Equipment", "Other", "Inventory" };
        private string query = "", group = "All", expandedId;
        private string measuredQuery = "";
        private int page, pageCount = 1, resultCount;
        private ItemService.Entry[] visible = Array.Empty<ItemService.Entry>();
        private readonly Dictionary<string, string> quantities = new Dictionary<string, string>();
        internal string RollGroup { get { return group == "Inventory" ? "All" : group; } }

        /// <summary>Measures a stable slice, then paints each entry and its inline actions using that slice.</summary>
        internal void Draw(CardCursor c, Func<CharacterMaster> recipient, bool giftsOnly = false)
        {
            DrawInput(c, "Search name / ID", ref query);
            DrawChips(c, groups, group, next => DeferLayoutChange(() => { group = next; page = 0; expandedId = null; }));
            var master = recipient();
            if (c.Measuring)
            {
                if (measuredQuery != query) { measuredQuery = query; page = 0; expandedId = null; }
                var matches = ItemService.Catalog.Where(e => (group == "All" || e.Group == group || group == "Inventory" && ItemService.Owned(e, master ? master.inventory : null) > 0)
                    && (e.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || e.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                resultCount = matches.Length; pageCount = Math.Max(1, (resultCount + 7) / 8); page = Mathf.Clamp(page, 0, pageCount - 1);
                visible = matches.Skip(page * 8).Take(8).ToArray();
            }
            DrawReadout(c, resultCount + " results", (page + 1) + " / " + pageCount);
            foreach (var entry in visible)
            {
                bool expanded = expandedId == entry.Id;
                CatalogWidgets.Header(c, entry.Name, entry.Id, entry.Icon, expanded, () => expandedId = expanded ? null : entry.Id);
                if (!expanded) continue;
                DrawReadout(c, "Owned", ItemService.Owned(entry, master ? master.inventory : null).ToString());
                string amount;
                if (!quantities.TryGetValue(entry.Id, out amount)) amount = "1";
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && HasHost && master;
                try
                {
                    // Copy the current string for callbacks: ref arguments cannot be captured in lambdas.
                    string requested = amount;
                    var give = new ButtonAction("Give", () =>
                    {
                        var current = recipient();
                        if (!current) throw new InvalidOperationException("The selected player's inventory is unavailable.");
                        ItemService.Give(entry, current, ItemCount(requested));
                        Toast("Given " + requested + " × " + entry.Name);
                    });
                    if (giftsOnly) CatalogWidgets.AmountActions(c, "gift:" + entry.Id, ref amount, give);
                    else
                    {
                        CatalogWidgets.AmountActions(c, "self:" + entry.Id, ref amount, give,
                            new ButtonAction("Drop", () => ItemService.Drop(entry, ItemCount(requested), false)));
                        DrawButtonRow(c, new ButtonAction("Drop from inventory", () => ItemService.Drop(entry, ItemCount(requested), true)));
                        DrawButtonRow(c, new ButtonAction("Replace nearest chest", () => ConfirmAction("Replace nearest chest: " + entry.Id, () => ItemService.ReplaceNearestChest(entry)), true));
                    }
                }
                finally { GUI.enabled = enabled; }
                if (!c.Measuring) quantities[entry.Id] = amount;
                if (entry.Equipment) DrawParagraph(c, "Give replaces the active equipment slot. Use amount 1.");
                else DrawParagraph(c, "Amount: 1–100. Items must be available in this run.");
                if (!giftsOnly) DrawParagraph(c, "Drop creates pickups; Drop from inventory transfers owned copies. Chest replacement requires two clicks and targets an unopened chest within 25 m.");
                c.Space(6);
            }
            if (resultCount == 0) DrawParagraph(c, "No matching items.");
            DrawButtonRow(c,
                new ButtonAction("Previous", () => DeferLayoutChange(() => { page = Math.Max(0, page - 1); expandedId = null; })),
                new ButtonAction("Next", () => DeferLayoutChange(() => { page = Math.Min(pageCount - 1, page + 1); expandedId = null; })));
        }
    }
}
