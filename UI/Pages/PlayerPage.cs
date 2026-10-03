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
    /// <summary>Player page; owns its local editing state and preserves the user's card layout.</summary>
    internal sealed class PlayerPage : IMenuPage
    {
        public string Title { get { return "Player"; } }
        public string Description { get { return "Economy, character state, movement and reversible stat tuning."; } }

        private string moneyValue = "1000", coinValue = "10", experienceValue = "1000", speedText = "1";
        private void SetSpeed()
        {
            if (!float.TryParse(speedText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                float.IsNaN(value) || float.IsInfinity(value) || value < 0.1f || value > 20f)
                throw new InvalidOperationException("Choose a speed multiplier from 0.1 to 20.");
            Prefs.SpeedMultiplier = value;
            State.Movement.SpeedMultiplier = true;
        }
        /// <summary>Groups host economy, vitals, god modes and reversible stats without aim clutter.</summary>
        public void Build(float width)
        {
            AddCard(0, "VITALS", c =>
            {
                DrawButtonRow(c, new ButtonAction("Heal", HealPlayer), new ButtonAction("Respawn", RespawnPlayer));
                DrawToggle(c, "God mode", State.Player.GodToggle, SetGodMode, "Host only. Each mode is released when disabled.");
                DrawCycle(c, "God mode type", GodModes.Name, GodModes.Cycle);
                DrawParagraph(c, GodModes.Description);
            }, true);

            AddCard(1, "ECONOMY", c =>
            {
                DrawReadout(c, "Current money", UmbraRuntime.LocalPlayer ? UmbraRuntime.LocalPlayer.money.ToString("N0") : "—");
                DrawInput(c, "Money amount", ref moneyValue);
                DrawButtonRow(c,
                    new ButtonAction("Give", () => GiveMoney(ParseUInt(moneyValue))),
                    new ButtonAction("Set", () => SetMoney(ParseUInt(moneyValue))),
                    new ButtonAction("Zero", () => SetMoney(0)));
                c.Space(8f);
                DrawReadout(c, "Lunar coins", UmbraRuntime.LocalNetworkUser ? UmbraRuntime.LocalNetworkUser.lunarCoins.ToString("N0") : "—");
                DrawToggle(c, "No Lunar Cost", State.Player.NoLunarCost, v => State.Player.NoLunarCost = v, "Host only. Lunar purchases work at zero coins without deducting or changing your saved balance.");
                DrawInput(c, "Coin amount", ref coinValue);
                DrawButtonRow(c,
                    new ButtonAction("Give", () => GiveCoins(ParseUInt(coinValue))),
                    new ButtonAction("Set", () => SetCoins(ParseUInt(coinValue))),
                    new ButtonAction("Remove", () => RemoveCoins(ParseUInt(coinValue))));
            }, true);
            AddCard(0, "COOLDOWNS & STATE", c =>
            {
                DrawToggle(c, "Infinite skills", State.Player.SkillToggle, v => State.Player.SkillToggle = v, null);
                DrawToggle(c, "Infinite equipment", State.Items.noEquipmentCD, v => State.Items.noEquipmentCD = v, null);
                DrawButtonRow(c, new ButtonAction("Disable gameplay mods", DisableGameplayMods));
            });
            AddCard(0, "MOVEMENT", c =>
            {
                DrawToggle(c, "Always sprint", State.Movement.alwaysSprintToggle, v => State.Movement.alwaysSprintToggle = v, null);
                DrawToggle(c, "Flight", State.Movement.flightToggle, v => State.Movement.flightToggle = v, "Jump ascends; X descends.");
                DrawToggle(c, "Jump pack", State.Movement.jumpPackToggle, v => State.Movement.jumpPackToggle = v, null);
                DrawToggle(c, "Infinite jumps", State.Movement.InfiniteJumps, v => State.Movement.InfiniteJumps = v, null);
                DrawToggle(c, "Override jump count", State.Movement.OverrideJumps, v => State.Movement.OverrideJumps = v, "Total jumps including the ground jump; infinite takes priority.");
                DrawSlider(c, "Jump count", ref Prefs.JumpCount, 1, 100);
                DrawToggle(c, "Speed multiplier", State.Movement.SpeedMultiplier, v => State.Movement.SpeedMultiplier = v, "Multiplies calculated speed, including items and base speed overrides.");
                DrawSlider(c, "Multiplier", ref Prefs.SpeedMultiplier, 0.1f, 20f, "0.00");
                DrawInput(c, "Set multiplier", ref speedText);
                DrawButtonRow(c, new ButtonAction("−", () => MovementModifiers.AdjustSpeed(-1)),
                    new ButtonAction("Set", SetSpeed), new ButtonAction("+", () => MovementModifiers.AdjustSpeed(1)));
                DrawSlider(c, "Adjustment step", ref Prefs.SpeedStep, 0.05f, 5f, "0.00");
                DrawBinding(c, "Increase speed (Mouse4)", "Increase speed");
                DrawBinding(c, "Decrease speed (Mouse5)", "Decrease speed");
                DrawButtonRow(c, new ButtonAction("Reset movement", ResetMovement));
            });
            AddCard(1, "EXPERIENCE", c =>
            {
                CharacterBody body = UmbraRuntime.LocalPlayerBody;
                DrawReadout(c, "Level", body ? body.level.ToString("0.0") : "—");
                DrawReadout(c, "Experience", body ? body.experience.ToString("N0") : "—");
                DrawInput(c, "Experience amount", ref experienceValue);
                DrawButtonRow(c, new ButtonAction("Give XP", () => GiveExperience(ParseULong(experienceValue))));
            }, true);

            AddCard(1, "CHARACTER SPECIFIC", c =>
            {
                DrawToggle(c, "Railgunner Perfect Reload", State.Player.RailgunPerfectReload, v => State.Player.RailgunPerfectReload = v, null);
                DrawToggle(c, "Unlimited Engineer turrets", State.Player.UnlimitedTurrets, v => State.Player.UnlimitedTurrets = v, "Host only. Removes your turret cap; cooldowns are separate. Excessive turrets can hurt performance.");
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
    }
}
