using System;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Small owned hooks for reliable camera timing and region-specific gameplay input capture.</summary>
    internal static class GameHooks
    {
        private static Harmony harmony;
        public static bool Installed { get; private set; }
        /// <summary>Installs only Umbra's two hooks; cleans up partially installed hooks on failure.</summary>
        public static void Install()
        {
            try
            {
                if (Installed) return;
                harmony = new Harmony("aquatic.umbra.runtime." + Guid.NewGuid().ToString("N"));
                harmony.Patch(AccessTools.PropertyGetter(typeof(LocalUser), "isUIFocused"), prefix: new HarmonyMethod(typeof(GameHooks), nameof(UiFocusPrefix)));
                harmony.Patch(AccessTools.Method(typeof(CameraRigController), "LateUpdate"), prefix: new HarmonyMethod(typeof(GameHooks), nameof(CameraPrefix)));
                harmony.Patch(AccessTools.Method(typeof(CharacterMotor), "UpdateVelocity"), postfix: new HarmonyMethod(typeof(GameHooks), nameof(MotorPostfix)));
                Installed = true;
            }
            catch (Exception error) { Uninstall(); Debug.LogError("Umbra camera/input hooks could not be installed: " + error); }
        }
        /// <summary>Applies flight after native gravity/acceleration but before the motor integrates motion.</summary>
        private static void MotorPostfix(CharacterMotor __instance, ref Vector3 __0)
        { MovementController.AfterMotorVelocity(__instance, ref __0); }
        /// <summary>Suppresses local gameplay input only over Umbra or during a drag that began inside it.</summary>
        private static bool UiFocusPrefix(LocalUser __instance, ref bool __result)
        {
            if (__instance != LocalUserManager.GetFirstLocalUser() || !MenuInput.Capturing) return true;
            __result = true; return false;
        }
        /// <summary>Updates camera aim before the game evaluates its camera mode, avoiding LateUpdate order races.</summary>
        private static void CameraPrefix(CameraRigController __instance)
        {
            if (!UmbraMenu.LocalPlayerBody || __instance.target != UmbraMenu.LocalPlayerBody.gameObject) return;
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
