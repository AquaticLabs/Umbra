using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu.State
{
    // These state-only modules replace the old menu objects. UI controls never construct hidden windows.

    /// <summary>Session-only player toggles; currency and respawn use game APIs through MenuActions.</summary>
    internal static class Player { public static bool GodToggle, SkillToggle, AimBotToggle, RailgunPerfectReload; }

    /// <summary>Requested stat values; StatOverrides owns capture, apply, and restoration.</summary>
    internal static class StatsMod
    {
        public static bool damageToggle, critToggle, attackSpeedToggle, armorToggle, moveSpeedToggle;
        public static int damagePerLvl = 10, critPerLvl = 1;
        public static float attackSpeed = 1f, armor, moveSpeed = 7f;
    }

    /// <summary>Session-only movement switches consumed by the runtime coordinator.</summary>
    internal static class Movement { public static bool alwaysSprintToggle, flightToggle, jumpPackToggle; }

    /// <summary>Director parameters and ownership of spawned interactables.</summary>
    internal static class Spawn
    {
        public static readonly TeamIndex[] team = { TeamIndex.Monster, TeamIndex.Neutral, TeamIndex.Player };
        public static int teamIndex;
        public static float minDistance = 3f, maxDistance = 40f;
        public static readonly List<GameObject> spawnedObjects = new List<GameObject>();
    }

    /// <summary>Overlay master switches; rendering and category preferences live in Overlay/.</summary>
    internal static class Render
    {
        public static bool renderMobs { get { return VisualSettings.Current.EnemyEsp; } set { VisualSettings.Current.EnemyEsp = value; } }
        public static bool renderInteractables { get { return VisualSettings.Current.InteractableEsp; } set { VisualSettings.Current.InteractableEsp = value; } }
        public static bool renderMods { get { return VisualSettings.Current.ActiveMods; } set { VisualSettings.Current.ActiveMods = value; } }
    }

    /// <summary>Equipment cooldown control without a hidden item menu.</summary>
    internal static class Items
    {
        public static bool noEquipmentCD;

        /// <summary>Resets recharge time for the active equipment slot while preserving equipment and charges.</summary>
        public static void NoEquipmentCooldown()
        {
            var inventory = UmbraMenu.LocalPlayerInv;
            uint slot = (uint)inventory.activeEquipmentSlot;
            EquipmentState equipment = inventory.GetEquipment(slot, 0);
            if (equipment.chargeFinishTime != Run.FixedTimeStamp.zero)
                inventory.SetEquipment(new EquipmentState(equipment.equipmentIndex, Run.FixedTimeStamp.zero, equipment.charges), slot, 0);
        }
    }
}
