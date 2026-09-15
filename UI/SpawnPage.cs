using System;
using System.Linq;
using UnityEngine;

namespace UmbraMenu
{
    internal static partial class ModernMenu
    {
        private static readonly string[] spawnGroups = { "All", "Common", "Boss", "Chest", "Shrine", "Drone", "Printer", "Portal", "Other" };
        private static string spawnQuery = "", spawnGroup = "All";
        private static int spawnPage;
        private static SpawnCatalog.Entry spawnSelection;
        /// <summary>Offers exact, categorized spawn cards with search and pagination instead of ambiguous substring presets.</summary>
        private static void DrawSpawn(float width)
        {
            SpawnCatalog.Request();
            AddCard(0, "SPAWN CATALOG", c =>
            {
                DrawReadout(c, "Catalog", SpawnCatalog.Status);
                DrawInput(c, "Search name / card ID", ref spawnQuery);
                DrawChips(c, spawnGroups, spawnGroup, group => { spawnGroup = group; spawnPage = 0; });
                var matches = SpawnCatalog.Entries.Where(e => (spawnGroup == "All" || e.Category == spawnGroup) && (e.Name.IndexOf(spawnQuery, StringComparison.OrdinalIgnoreCase) >= 0 || e.Card.name.IndexOf(spawnQuery, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                int pages = Math.Max(1, (matches.Count + 7) / 8);
                spawnPage = Math.Max(0, Math.Min(pages - 1, spawnPage));
                DrawReadout(c, matches.Count + " matches", (spawnPage + 1) + " / " + pages);
                foreach (var entry in matches.Skip(spawnPage * 8).Take(8))
                {
                    DrawSpawnRow(c, entry);
                }
                DrawButtonRow(c, new ButtonAction("Previous", () => spawnPage = Math.Max(0, spawnPage - 1)), new ButtonAction("Next", () => spawnPage = Math.Min(pages - 1, spawnPage + 1)));
                DrawParagraph(c, "Common/boss follows the monster body's champion classification. Select the exact asset; variants remain separate.");
            });
            AddCard(1, "PLACEMENT & SPAWN", c =>
            {
                DrawParagraph(c, spawnSelection == null ? "Choose a spawn card." : spawnSelection.Name + "\n" + spawnSelection.Card.name);
                DrawCycle(c, "Monster team", State.Spawn.team[State.Spawn.teamIndex].ToString(), CycleSpawnTeam);
                DrawSlider(c, "Minimum distance", ref State.Spawn.minDistance, 1, 30, "0");
                DrawSlider(c, "Maximum distance", ref State.Spawn.maxDistance, 10, 100, "0");
                DrawButtonRow(c, new ButtonAction("Spawn selected", () => { SpawnCatalog.Spawn(spawnSelection); Toast("Spawned " + spawnSelection.Name); }));
                DrawParagraph(c, "Spawns one object. Team applies to monsters only. Portals appear immediately and still follow their game's destination/stage rules.");
                DrawButtonRow(c, new ButtonAction("Clean up Umbra spawns", () => ConfirmAction("Clean up Umbra spawns", DestroyUmbraSpawns), true));
            }, true);
        }

        /// <summary>Preserves the catalog layout while adding native portraits/inspect sprites and a consistent fallback.</summary>
        private static void DrawSpawnRow(CardCursor c, SpawnCatalog.Entry entry)
        {
            float height = Mathf.Max(46, labelStyle.CalcHeight(new GUIContent(entry.Name), c.Width - 58) + 12);
            if (!c.Measuring)
            {
                if (GUI.Button(new Rect(0, c.Y, c.Width, height), GUIContent.none, entry == spawnSelection ? navActiveStyle : buttonStyle)) spawnSelection = entry;
                Rect imageRect = new Rect(6, c.Y + 5, 34, 34);
                if (entry.IconSprite)
                {
                    // Sprite.rect addresses only this icon when the texture is a shared atlas.
                    var source = entry.IconSprite.rect;
                    var texture = entry.IconSprite.texture;
                    GUI.DrawTextureWithTexCoords(imageRect, texture, new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height));
                }
                else if (entry.Icon) GUI.DrawTexture(imageRect, entry.Icon, ScaleMode.ScaleToFit);
                else GUI.Label(imageRect, entry.Category.Substring(0, 1), badgeStyle);
                GUI.Label(new Rect(48, c.Y + 4, c.Width - 56, height - 8), new GUIContent(entry.Name, entry.Card.name), labelStyle);
            }
            c.Advance(height + 6);
        }

    }
}
