using static UmbraMenu.MenuController;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Validated commands shared by pages, with host checks and user-visible error feedback.</summary>
    internal static class MenuActions
    {
        /// <summary>Adds run money after checking host authority and unsigned overflow.</summary>
        internal static void GiveMoney(uint amount)
        {
            RequirePlayer();
            RequireHost();
            if (amount > uint.MaxValue - UmbraRuntime.LocalPlayer.money) throw new InvalidOperationException("This amount would overflow the money balance.");
            UmbraRuntime.LocalPlayer.GiveMoney(amount);
            Toast("Added " + amount.ToString("N0") + " money");
        }

        /// <summary>Sets the authoritative master balance through its game property.</summary>
        internal static void SetMoney(uint target)
        {
            RequirePlayer();
            RequireHost();
            UmbraRuntime.LocalPlayer.money = target;
            Toast("Money set to " + target.ToString("N0"));
        }

        /// <summary>Awards persistent lunar currency through the game API, rejecting overflow.</summary>
        internal static void GiveCoins(uint amount)
        {
            RequireHost();
            RequireNetworkUser();
            if (amount > uint.MaxValue - UmbraRuntime.LocalNetworkUser.lunarCoins) throw new InvalidOperationException("This amount would overflow the lunar balance.");
            UmbraRuntime.LocalNetworkUser.AwardLunarCoins(amount);
            Toast("Lunar coin award requested: " + amount.ToString("N0"));
        }

        /// <summary>Deducts at most the current balance through the authoritative game API.</summary>
        internal static void RemoveCoins(uint amount)
        {
            RequireNetworkUser();
            RequireHost();
            uint remove = Math.Min(UmbraRuntime.LocalNetworkUser.lunarCoins, amount);
            UmbraRuntime.LocalNetworkUser.DeductLunarCoins(remove);
            Toast("Lunar coin deduction requested: " + remove.ToString("N0"));
        }

        /// <summary>Converts a target balance to a checked award or deduction.</summary>
        internal static void SetCoins(uint target)
        {
            RequireNetworkUser();
            uint current = UmbraRuntime.LocalNetworkUser.lunarCoins;
            if (target >= current) GiveCoins(target - current);
            else RemoveCoins(current - target);
        }

        /// <summary>Requires a local network user, but does not require a living body.</summary>
        internal static void RequireNetworkUser()
        {
            if (!UmbraRuntime.LocalNetworkUser) throw new InvalidOperationException("Join or host a session first.");
        }

        /// <summary>Awards experience to the local character master.</summary>
        internal static void GiveExperience(ulong amount)
        {
            RequirePlayer();
            RequireHost();
            UmbraRuntime.LocalPlayer.GiveExperience(amount);
            Toast("Added " + amount.ToString("N0") + " experience");
        }

        /// <summary>Restores the active local body's health.</summary>
        internal static void HealPlayer()
        {
            RequirePlayer();
            RequireHost();
            UmbraRuntime.LocalHealth.Heal(float.MaxValue, new ProcChainMask(), false);
            Toast("Health restored");
        }

        /// <summary>Requests respawn through the surviving master even when the body is absent.</summary>
        internal static void RespawnPlayer()
        {
            RequireHost();
            if (!UmbraRuntime.LocalPlayer) throw new InvalidOperationException("No local character master is available.");
            UmbraRuntime.LocalPlayer.RespawnExtraLife();
            Toast("Respawn requested");
        }

        /// <summary>Applies host-only invulnerability and safely clears it on disable.</summary>
        internal static void SetGodMode(bool enabled)
        {
            if (enabled) RequireHost();
            State.Player.GodToggle = enabled;
        }

        /// <summary>Keeps the active-mod indicator synchronized with aim activation.</summary>
        internal static void SetAimbotEnabled(bool enabled)
        {
            ModernAimbot.Enabled = enabled;
            State.Player.AimBotToggle = enabled;
        }

        /// <summary>Synchronizes the legacy hotkey toggle with the cached overlay service.</summary>
        internal static void SetInteractableEsp(bool enabled)
        {
            State.Render.renderInteractables = enabled;
            if (enabled) EspRenderer.Refresh();
        }

        /// <summary>Requests a refresh of cached object and renderer references.</summary>
        internal static void RefreshWorld()
        {
            EspRenderer.Refresh();
            Toast("World cache refreshed");
        }

        /// <summary>Enables enemies, interactables, teleporter, and crosshair together.</summary>
        internal static void EnableEssentialVisuals()
        {
            State.Render.renderMobs = true;
            SetInteractableEsp(true);
            Prefs.Teleporter = true;
            Prefs.Crosshair = true;
            Toast("Essential overlays enabled");
        }

        /// <summary>Clears every overlay master switch, including pickups and active-mod text.</summary>
        internal static void DisableVisuals()
        {
            State.Render.renderMobs = false;
            State.Render.renderMods = false;
            Prefs.Pickups = false;
            Prefs.Allies = false;
            SetInteractableEsp(false);
            Prefs.Teleporter = false;
            Prefs.Crosshair = false;
            Prefs.ShowFov = false;
            Toast("Overlays disabled");
        }

        /// <summary>Accelerates the active teleporter charge.</summary>
        internal static void InstantTeleporter()
        {
            RequireHost();
            if (!TeleporterInteraction.instance || !TeleporterInteraction.instance.holdoutZoneController) throw new InvalidOperationException("No teleporter is active.");
            TeleporterInteraction.instance.holdoutZoneController.baseChargeDuration = 1f;
            Toast("Teleporter charge accelerated");
        }

        /// <summary>Adds one stack only when an actual teleporter exists.</summary>
        internal static void AddMountain()
        {
            RequireHost();
            if (!TeleporterInteraction.instance) throw new InvalidOperationException("No teleporter is active.");
            TeleporterInteraction.instance.AddShrineStack();
            Toast("Mountain stack added");
        }

        /// <summary>Requests a stage transition for an active run.</summary>
        internal static void SkipStage()
        {
            if (!Run.instance) throw new InvalidOperationException("No run is active.");
            RequireHost();
            Run.instance.AdvanceStage(Run.instance.nextStageScene);
            Toast("Stage advance requested");
        }

        /// <summary>Removes hostile bodies once each without targeting player or neutral teams.</summary>
        internal static void KillEnemies()
        {
            RequirePlayer();
            RequireHost();
            foreach (var body in CharacterBody.readOnlyInstancesList.ToArray())
            {
                if (!body || !body.healthComponent || !body.healthComponent.alive || !body.teamComponent) continue;
                TeamIndex team = body.teamComponent.teamIndex;
                if (team == TeamIndex.Monster || team == TeamIndex.Void || team == TeamIndex.Lunar)
                    body.healthComponent.Suicide();
            }
            Toast("Enemy cleanup requested");
        }

        /// <summary>Removes only tracked Umbra-created objects on the host.</summary>
        internal static void DestroyUmbraSpawns()
        {
            RequireHost();
            foreach (var spawned in State.Spawn.spawnedObjects)
                if (spawned) UnityEngine.Networking.NetworkServer.Destroy(spawned);
            State.Spawn.spawnedObjects.Clear();
            Toast("Tracked Umbra spawns removed");
        }

        /// <summary>Cycles the explicit director team override.</summary>
        internal static void CycleSpawnTeam()
        {
            State.Spawn.teamIndex = (State.Spawn.teamIndex + 1) % State.Spawn.team.Length;
        }

        /// <summary>Copies the current seed to the system clipboard.</summary>
        internal static void CopyRunSeed()
        {
            if (!Run.instance) throw new InvalidOperationException("No run is active.");
            GUIUtility.systemCopyBuffer = Run.instance.seed.ToString();
            Toast("Run seed copied");
        }

        /// <summary>Reads current run difficulty for display only.</summary>
        internal static string CurrentDifficulty()
        {
            if (!Run.instance) return "—";
            return Run.instance.selectedDifficulty.ToString();
        }

        /// <summary>Disables movement modifications together.</summary>
        internal static void ResetMovement()
        {
            State.Movement.alwaysSprintToggle = false;
            State.Movement.flightToggle = false;
            State.Movement.jumpPackToggle = false;
            MovementController.Restore();
            Toast("Movement options reset");
        }

        /// <summary>Turns gameplay modifiers off and restores captured base statistics.</summary>
        public static void DisableGameplayMods()
        {
            ModernAimbot.Enabled = false;
            ModernAimbot.ClearTarget();
            State.Player.AimBotToggle = false;
            State.Player.SkillToggle = false;
            State.Player.RailgunPerfectReload = false;
            RailgunPerfectReload.Clear();
            SetGodMode(false);
            GodModes.Restore();
            State.Items.noEquipmentCD = false;
            State.StatsMod.damageToggle = false;
            State.StatsMod.critToggle = false;
            State.StatsMod.attackSpeedToggle = false;
            State.StatsMod.armorToggle = false;
            State.StatsMod.moveSpeedToggle = false;
            StatOverrides.Restore();
            ResetMovement();
            Toast("Gameplay modifiers disabled");
        }



        /// <summary>Rejects session mutations when this game instance has no server authority.</summary>
        internal static void RequireHost()
        {
            if (!UnityEngine.Networking.NetworkServer.active) throw new InvalidOperationException("This action requires the host.");
        }

        /// <summary>Rejects body-dependent actions during lobby, death, or loading.</summary>
        internal static void RequirePlayer()
        {
            if (!UmbraRuntime.characterCollected || !UmbraRuntime.LocalPlayer || !UmbraRuntime.LocalPlayerBody)
            {
                throw new InvalidOperationException("Enter a run and wait for the local player to spawn.");
            }
        }

        /// <summary>Validates a nonnegative 32-bit currency amount before mutation.</summary>
        internal static uint ParseUInt(string value)
        {
            uint parsed;
            if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidOperationException("Enter a whole number from 0 to 4,294,967,295.");
            return parsed;
        }

        /// <summary>Validates a nonnegative 64-bit experience amount.</summary>
        internal static ulong ParseULong(string value)
        {
            ulong parsed;
            if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidOperationException("Enter a positive whole-number experience amount.");
            return parsed;
        }

        /// <summary>Reports action failures locally without breaking GUI rendering.</summary>
        internal static void SafeAction(Action action, string label)
        {
            try { action(); }
            catch (ExitGUIException) { throw; }
            catch (Exception exception)
            {
                Debug.LogError("Umbra / " + label + ": " + exception);
                Toast(exception.Message);
            }
        }




        private static string armedAction;
        private static float armedUntil;
        /// <summary>Bounds operation sizes before allocating or changing inventory.</summary>
        internal static int ItemCount(string value)
        {
            int count; if (!int.TryParse(value, out count) || count < 1 || count > 100) throw new InvalidOperationException("Enter a quantity from 1 to 100."); return count;
        }
        /// <summary>Requires deliberate repeated invocation for bulk/destructive inventory changes.</summary>
        internal static void ConfirmAction(string id, Action action)
        {
            if (armedAction != id || Time.unscaledTime > armedUntil) { armedAction = id; armedUntil = Time.unscaledTime + 5; Toast("Click again within 5 seconds: " + id); return; }
            armedAction = null; action(); Toast(id + " completed");
        }
    }
}
