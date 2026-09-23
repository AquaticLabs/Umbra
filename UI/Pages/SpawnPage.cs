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
    /// <summary>Spawn page; owns its local editing state and preserves the user's card layout.</summary>
    internal sealed class SpawnPage : IMenuPage
    {
        public string Title { get { return "Spawn"; } }
        public string Description { get { return "Browse categorized spawn cards and place the exact selected asset."; } }

        private readonly string[] spawnGroups = { "All", "Common", "Boss", "Chest", "Shrine", "Drone", "Printer", "Portal", "Other" };
        private string spawnQuery = "", spawnGroup = "All";
        private int spawnPage, pages = 1, matchCount;
        private string measuredQuery = "";
        private SpawnCatalog.Entry[] visible = Array.Empty<SpawnCatalog.Entry>();
        private SpawnCatalog.Entry spawnSelection;
        /// <summary>Offers exact, categorized spawn cards with search and pagination instead of ambiguous substring presets.</summary>
        public void Build(float width)
        {
            SpawnCatalog.Request();
            AddCard(0, "SPAWN CATALOG", c =>
            {
                DrawReadout(c, "Catalog", SpawnCatalog.Status);
                DrawInput(c, "Search name / card ID", ref spawnQuery);
                DrawChips(c, spawnGroups, spawnGroup, group => DeferLayoutChange(() => { spawnGroup = group; spawnPage = 0; spawnSelection = null; }));
                if (c.Measuring)
                {
                    if (measuredQuery != spawnQuery) { measuredQuery = spawnQuery; spawnPage = 0; spawnSelection = null; }
                    var matches = SpawnCatalog.Entries.Where(e => (spawnGroup == "All" || e.Category == spawnGroup) && (e.Name.IndexOf(spawnQuery, StringComparison.OrdinalIgnoreCase) >= 0 || e.Card.name.IndexOf(spawnQuery, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    matchCount = matches.Count;
                    pages = Math.Max(1, (matchCount + 7) / 8);
                    spawnPage = Math.Max(0, Math.Min(pages - 1, spawnPage));
                    visible = matches.Skip(spawnPage * 8).Take(8).ToArray();
                }
                DrawReadout(c, matchCount + " matches", (spawnPage + 1) + " / " + pages);
                foreach (var entry in visible)
                {
                    CatalogWidgets.Header(c, entry.Name, entry.Card.name, entry.Icon, entry == spawnSelection,
                        () => spawnSelection = spawnSelection == entry ? null : entry, entry.IconSprite);
                    if (entry != spawnSelection) continue;
                    DrawCycle(c, "Monster team", State.Spawn.team[State.Spawn.teamIndex].ToString(), CycleSpawnTeam);
                    DrawSlider(c, "Minimum distance", ref State.Spawn.minDistance, 1, 30, "0");
                    DrawSlider(c, "Maximum distance", ref State.Spawn.maxDistance, 10, 100, "0");
                    bool enabled = GUI.enabled;
                    GUI.enabled = enabled && HasHost;
                    try { DrawButtonRow(c, new ButtonAction("Spawn", () => { SpawnCatalog.Spawn(entry); Toast("Spawned " + entry.Name); })); }
                    finally { GUI.enabled = enabled; }
                }
                DrawButtonRow(c,
                    new ButtonAction("Previous", () => DeferLayoutChange(() => { spawnPage = Math.Max(0, spawnPage - 1); spawnSelection = null; })),
                    new ButtonAction("Next", () => DeferLayoutChange(() => { spawnPage = Math.Min(pages - 1, spawnPage + 1); spawnSelection = null; })));
                DrawParagraph(c, "Expand an entry for placement controls. Common/boss follows the monster body's champion classification; variants remain separate.");
            });
            AddCard(1, "SPAWN TOOLS", c =>
            {
                DrawParagraph(c, "Spawns one object. Team applies to monsters only. Portals appear immediately and still follow their game's destination/stage rules.");
                DrawButtonRow(c, new ButtonAction("Clean up Umbra spawns", () => ConfirmAction("Clean up Umbra spawns", DestroyUmbraSpawns), true));
            }, true);
        }

    }
}
