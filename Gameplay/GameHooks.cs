using System;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Owned hooks for camera timing, flight velocity and region-specific input capture.</summary>
    internal static class GameHooks
    {
        private static Harmony harmony;
        public static bool Installed { get; private set; }
        /// <summary>Installs owned runtime hooks; cleans up partially installed hooks on failure.</summary>
        public static void Install()
        {
            try
            {
                if (Installed) return;
                harmony = new Harmony("aquatic.umbra.runtime." + Guid.NewGuid().ToString("N"));
                harmony.Patch(AccessTools.PropertyGetter(typeof(LocalUser), "isUIFocused"), prefix: new HarmonyMethod(typeof(GameHooks), nameof(UiFocusPrefix)));
                harmony.Patch(AccessTools.Method(typeof(CameraRigController), "LateUpdate"), prefix: new HarmonyMethod(typeof(GameHooks), nameof(CameraPrefix)));
                harmony.Patch(AccessTools.Method(typeof(CharacterMotor), "UpdateVelocity"), postfix: new HarmonyMethod(typeof(GameHooks), nameof(MotorPostfix)));
                harmony.Patch(AccessTools.Method(typeof(CharacterBody), "RecalculateStats"), postfix: new HarmonyMethod(typeof(GameHooks), nameof(StatsPostfix)));
                harmony.Patch(AccessTools.Method(typeof(CostTypeDef), "IsAffordable"), prefix: new HarmonyMethod(typeof(PurchaseModifiers), nameof(PurchaseModifiers.AffordablePrefix)));
                harmony.Patch(AccessTools.Method(typeof(CostTypeDef), "PayCost"), prefix: new HarmonyMethod(typeof(PurchaseModifiers), nameof(PurchaseModifiers.PayPrefix)));
                harmony.Patch(AccessTools.Method(typeof(CharacterMaster), "GetDeployableSameSlotLimit"), postfix: new HarmonyMethod(typeof(PurchaseModifiers), nameof(PurchaseModifiers.TurretLimitPostfix)));
                harmony.Patch(AccessTools.Method(typeof(ChestBehavior), "Roll"), postfix: new HarmonyMethod(typeof(ChestReplacement), nameof(ChestReplacement.Apply)));
                harmony.Patch(AccessTools.Method(typeof(ChestBehavior), "BaseItemDrop"),
                    prefix: new HarmonyMethod(typeof(ChestReplacement), nameof(ChestReplacement.Apply)),
                    postfix: new HarmonyMethod(typeof(ChestReplacement), nameof(ChestReplacement.AfterDrop)));
                Installed = true;
            }
            catch (Exception error) { Uninstall(); Debug.LogError("Umbra runtime hooks could not be installed: " + error); }
        }
        /// <summary>Applies flight after native gravity/acceleration but before the motor integrates motion.</summary>
        private static void MotorPostfix(CharacterMotor __instance, ref Vector3 __0)
        { MovementController.AfterMotorVelocity(__instance, ref __0); }
        private static void StatsPostfix(CharacterBody __instance) { MovementModifiers.AfterStats(__instance); }
        /// <summary>Suppresses local gameplay input only over Umbra or during a drag that began inside it.</summary>
        private static bool UiFocusPrefix(LocalUser __instance, ref bool __result)
        {
            if (__instance != LocalUserManager.GetFirstLocalUser() || !MenuInput.Capturing) return true;
            __result = true; return false;
        }
        /// <summary>Updates camera aim before the game evaluates its camera mode, avoiding LateUpdate order races.</summary>
        private static void CameraPrefix(CameraRigController __instance)
        {
            if (!UmbraRuntime.LocalPlayerBody || __instance.target != UmbraRuntime.LocalPlayerBody.gameObject) return;
            ModernAimbot.Update();
        }
        /// <summary>Removes only this runtime's patches on unload.</summary>
        public static void Uninstall()
        {
            Installed = false;
            if (harmony != null) harmony.UnpatchAll(harmony.Id);
            harmony = null;
        }
    }
}
