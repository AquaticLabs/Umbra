using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    internal static partial class ModernMenu
    {
        /// <summary>Provides useful local telemetry and explicit, scene-scoped recovery actions.</summary>
        private static void DrawWorld(float width)
        {
            DrawPortalCards();
            AddCard(1, "TELEPORTER", c =>
            {
                DrawReadout(c, "Detected", TeleporterInteraction.instance ? "YES" : "NO");
                DrawButtonRow(c,
                    new ButtonAction("Instant charge", InstantTeleporter),
                    new ButtonAction("Mountain +1", AddMountain));
                DrawParagraph(c, "Portals spawn directly from the individual portal catalog; each button is independent.");
            }, true);

            AddCard(1, "STAGE", c =>
            {
                DrawReadout(c, "Scene", UmbraMenu.currentScene.IsValid() ? UmbraMenu.currentScene.name : "—");
                DrawReadout(c, "Run active", Run.instance ? "YES" : "NO");
                DrawParagraph(c, "Host authority is required for stage and director mutations.");
                DrawButtonRow(c,
                    new ButtonAction("Skip stage", SkipStage),
                    new ButtonAction("Kill enemies", KillEnemies, true));
            }, true);

            AddCard(2, "CLEANUP", c =>
            {
                DrawParagraph(c, "Remove only Umbra-created pickups, monsters, portals and interactables. Native stage objects are left untouched. Click twice to confirm.");
                DrawButtonRow(c, new ButtonAction("Destroy Umbra spawns", () => ConfirmAction("Clean up Umbra spawns", DestroyUmbraSpawns), true));
            }, true);
        }
                /// <summary>Lists every loaded portal card independently, including DLC variants, with no all-portals side effects.</summary>
        private static void DrawPortalCards()
        {
            SpawnCatalog.Request();
            AddCard(0, "INDIVIDUAL PORTALS", c =>
            {
                DrawParagraph(c, SpawnCatalog.Status);
                foreach (var entry in SpawnCatalog.Entries.Where(e => e.Category == "Portal").ToArray())
                {
                    var portal = entry;
                    DrawButtonRow(c, new ButtonAction(portal.Name, () => { SpawnCatalog.Spawn(portal); Toast("Spawned " + portal.Name); }));
                }
                DrawParagraph(c, "Blue, gold, green, celestial/obliterate, void, tech and other installed portal cards are loaded here. DLC and destination rules still apply. Each button spawns only that portal.");
            }, true);
        }
    }
}
