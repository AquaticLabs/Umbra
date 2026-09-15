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
        /// <summary>Returns the short description for each user-facing module.</summary>
        private static string PageDescription(int index)
        {
            switch (index)
            {
                case 0: return "Economy, character state, god mode and reversible stat tuning.";
                case 1: return "Camera aim, activation, target rules and visual feedback.";
                case 2: return "Readable world overlays with a restrained deep-blue presentation.";
                case 3: return "Search items and equipment; give, drop or transfer inventory stacks.";
                case 4: return "Host-side stage and teleporter controls.";
                case 5: return "Browse categorized spawn cards and place the exact selected asset.";
                case 6: return "Connected players, host gifts, and session diagnostics.";
                default: return "Movement, cooldowns, interface state, and lifecycle controls.";
            }
        }

        /// <summary>Groups host economy, vitals, god modes and reversible stats without aim clutter.</summary>
        private static void DrawPlayer(float width)
        {
            AddCard(0, "ECONOMY", c =>
            {
                DrawReadout(c, "Current money", UmbraMenu.LocalPlayer ? UmbraMenu.LocalPlayer.money.ToString("N0") : "—");
                DrawInput(c, "Money amount", ref moneyValue);
                DrawButtonRow(c,
                    new ButtonAction("Give", () => GiveMoney(ParseUInt(moneyValue))),
                    new ButtonAction("Set", () => SetMoney(ParseUInt(moneyValue))),
                    new ButtonAction("Zero", () => SetMoney(0)));
                c.Space(8f);
                DrawReadout(c, "Lunar coins", UmbraMenu.LocalNetworkUser ? UmbraMenu.LocalNetworkUser.lunarCoins.ToString("N0") : "—");
                DrawInput(c, "Coin amount", ref coinValue);
                DrawButtonRow(c,
                    new ButtonAction("Give", () => GiveCoins(ParseUInt(coinValue))),
                    new ButtonAction("Set", () => SetCoins(ParseUInt(coinValue))),
                    new ButtonAction("Remove", () => RemoveCoins(ParseUInt(coinValue))));
            }, true);

            AddCard(1, "EXPERIENCE & VITALS", c =>
            {
                CharacterBody body = UmbraMenu.LocalPlayerBody;
                DrawReadout(c, "Level", body ? body.level.ToString("0.0") : "—");
                DrawReadout(c, "Experience", body ? body.experience.ToString("N0") : "—");
                DrawInput(c, "Experience amount", ref experienceValue);
                DrawButtonRow(c,
                    new ButtonAction("Give XP", () => GiveExperience(ParseULong(experienceValue))),
                    new ButtonAction("Heal", HealPlayer),
                    new ButtonAction("Respawn", RespawnPlayer));
                c.Space(7f);
                DrawToggle(c, "God mode", State.Player.GodToggle, SetGodMode, "Host only. Each mode is released when disabled.");
                DrawCycle(c, "God mode type", GodModes.Name, GodModes.Cycle);
                DrawParagraph(c, GodModes.Description);
                DrawToggle(c, "Infinite skills", State.Player.SkillToggle, v => State.Player.SkillToggle = v, null);
            }, true);
            AddCard(0, "MOVEMENT", c =>
            {
                DrawToggle(c, "Always sprint", State.Movement.alwaysSprintToggle, v => State.Movement.alwaysSprintToggle = v, null);
                DrawToggle(c, "Flight", State.Movement.flightToggle, v => State.Movement.flightToggle = v, "Jump ascends; X descends.");
                DrawToggle(c, "Jump pack", State.Movement.jumpPackToggle, v => State.Movement.jumpPackToggle = v, null);
                DrawButtonRow(c, new ButtonAction("Reset movement", ResetMovement));
            });
            AddCard(0, "CHARACTER SPECIFIC", c =>
            {
                DrawToggle(c, "Railgunner Perfect Reload", State.Player.RailgunPerfectReload, v => State.Player.RailgunPerfectReload = v, null);
            });


            AddCard(1, "STAT TUNING", c =>
            {
                DrawToggle(c, "Damage scaling", State.StatsMod.damageToggle, v => State.StatsMod.damageToggle = v, null);
                DrawSlider(c, "Damage / level", ref State.StatsMod.damagePerLvl, 1, 100);
                DrawToggle(c, "Critical scaling", State.StatsMod.critToggle, v => State.StatsMod.critToggle = v, null);
                DrawSlider(c, "Crit / level", ref State.StatsMod.critPerLvl, 0, 100);
                DrawToggle(c, "Attack speed override", State.StatsMod.attackSpeedToggle, v => State.StatsMod.attackSpeedToggle = v, null);
                DrawSlider(c, "Base attack speed", ref State.StatsMod.attackSpeed, 0.5f, 20f, "0.0");
                DrawToggle(c, "Armor override", State.StatsMod.armorToggle, v => State.StatsMod.armorToggle = v, null);
                DrawSlider(c, "Base armor", ref State.StatsMod.armor, 0f, 500f, "0");
                DrawToggle(c, "Move speed override", State.StatsMod.moveSpeedToggle, v => State.StatsMod.moveSpeedToggle = v, null);
                DrawSlider(c, "Base move speed", ref State.StatsMod.moveSpeed, 1f, 60f, "0.0");
            }, true);

        }

        /// <summary>Global overlays, per-category styling, and exact item overrides.</summary>
        private static void DrawVisuals(float width)
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

        /// <summary>Presents teleporter and stage actions with host validation at invocation.</summary>


        /// <summary>Displays session data without pretending a run difficulty write changes lobby rules.</summary>
        private static void DrawLobby(float width)
        {
            DrawLobbyGifts();
            AddCard(0, "SESSION", c =>
            {
                DrawReadout(c, "Connected players", NetworkUser.readOnlyInstancesList.Count.ToString());
                DrawReadout(c, "Local player", UmbraMenu.LocalNetworkUser ? UmbraMenu.LocalNetworkUser.userName : "—");
                DrawReadout(c, "Run seed", Run.instance ? Run.instance.seed.ToString() : "—");
                DrawButtonRow(c, new ButtonAction("Copy run seed", CopyRunSeed));
                DrawToggle(c, "Show active mods", State.Render.renderMods, v => State.Render.renderMods = v, null);
            });

            AddCard(0, "DIFFICULTY", c =>
            {
                DrawReadout(c, "Current", CurrentDifficulty());
                DrawParagraph(c, "Run difficulty is read-only here. Select difficulty in the game lobby before starting.");
            });

            AddCard(2, "MOD STATE", c =>
            {
                DrawReadout(c, "Application", "Modded");
                DrawParagraph(c, "Umbra is intended for private testing. Session-wide changes may require host authority and should be used with everyone informed.");
            });
        }

        /// <summary>Groups movement, cooldown, window appearance, and lifecycle controls.</summary>
        private static void DrawMisc(float width)
        {
            DrawMiscExtras();


            AddCard(1, "COOLDOWNS & STATE", c =>
            {
                DrawToggle(c, "Infinite skills", State.Player.SkillToggle, v => State.Player.SkillToggle = v, null);
                DrawToggle(c, "Infinite equipment", State.Items.noEquipmentCD, v => State.Items.noEquipmentCD = v, null);
                DrawToggle(c, "Active mod strip", State.Render.renderMods, v => State.Render.renderMods = v, null);
                DrawButtonRow(c, new ButtonAction("Disable gameplay mods", DisableGameplayMods));
            });
            AddCard(1, "APPEARANCE", c =>
            {
                DrawSlider(c, "Window opacity", ref Prefs.WindowOpacity, 0.4f, 1f, "0.00");
                DrawSlider(c, "Corner radius (px)", ref Prefs.CornerRadius, 0f, 10f, "0");
                DrawParagraph(c, "Text stays opaque. Panels use subtle fills so the game remains visible through the window.");
                DrawPreferenceActions(c);
            });
            AddCard(2, "UMBRA", c =>
            {
                DrawReadout(c, "Build", UmbraMenu.VERSION + " / Trident proof");
                DrawButtonRow(c,
                    new ButtonAction("Center window", CenterWindow),
                    new ButtonAction("Unload Umbra", Loader.RequestUnload, true));
            });

        }

    }
}
