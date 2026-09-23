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

        private readonly ItemBrowser browser = new ItemBrowser();
        private string rollText = "5";

        /// <summary>Provides a searchable paginated catalog and explicit give/drop/transfer actions.</summary>
        public void Build(float width)
        {
            AddCard(0, "ITEM & EQUIPMENT CATALOG", c => browser.Draw(c, () => UmbraRuntime.LocalPlayer));
            AddCard(1, "RANDOM ITEMS", c =>
            {
                DrawInput(c, "Roll count (1–100)", ref rollText);
                DrawReadout(c, "Pool", browser.RollGroup);
                DrawButtonRow(c, new ButtonAction("Roll items", () => { ItemService.Roll(ItemCount(rollText), browser.RollGroup); Toast("Random items given"); }));
                DrawParagraph(c, "Uniform random rolls from eligible items in the selected tier. Use All for every eligible item tier.");
            }, true);
            AddCard(1, "INVENTORY TOOLS", c =>
            {
                DrawButtonRow(c, new ButtonAction("Give all +1", () => ConfirmAction("Give all +1", ItemService.GiveAll)), new ButtonAction("Restack inventory", () => ConfirmAction("Restack inventory", ItemService.Restack), true));
                DrawButtonRow(c, new ButtonAction("Clear removable items", () => ConfirmAction("Clear removable items", ItemService.ClearInventory), true));
                DrawParagraph(c, "Bulk tools require a second click within 5 seconds. Clear preserves hidden/internal items and equipment. Restack uses Shrine of Order rules.");
            }, true);
        }

    }
}
