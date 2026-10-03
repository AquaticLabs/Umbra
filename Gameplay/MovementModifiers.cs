using RoR2;
using UnityEngine;
using HarmonyLib;

namespace UmbraMenu
{
    /// <summary>Applies local movement modifiers after native stat calculation; never multiplies a prior result.</summary>
    internal static class MovementModifiers
    {
        private static CharacterBody owner;
        private static readonly System.Reflection.MethodInfo speedSetter = AccessTools.PropertySetter(typeof(CharacterBody), "moveSpeed");
        private static readonly System.Reflection.MethodInfo jumpsSetter = AccessTools.PropertySetter(typeof(CharacterBody), "maxJumpCount");
        private static float lastSpeed;
        private static int lastJumps;
        private static bool lastInfinite, lastOverride, lastEnabled;
        private static VisualPreferences Prefs { get { return VisualSettings.Current; } }

        internal static void Update()
        {
            var body = UmbraRuntime.LocalPlayerBody;
            if (owner != body) { Restore(); owner = body; }
            if (!body) return;
            if (lastSpeed == Prefs.SpeedMultiplier && lastJumps == Prefs.JumpCount &&
                lastInfinite == State.Movement.InfiniteJumps && lastOverride == State.Movement.OverrideJumps &&
                lastEnabled == State.Movement.SpeedMultiplier) return;
            lastSpeed = Prefs.SpeedMultiplier; lastJumps = Prefs.JumpCount;
            lastInfinite = State.Movement.InfiniteJumps; lastOverride = State.Movement.OverrideJumps;
            lastEnabled = State.Movement.SpeedMultiplier;
            body.RecalculateStats();
        }

        internal static void AfterStats(CharacterBody body)
        {
            if (!body || body != owner) return;
            if (State.Movement.SpeedMultiplier) speedSetter.Invoke(body, new object[] { body.moveSpeed * VisualSettings.Clamp(Prefs.SpeedMultiplier, 0.1f, 20f, 1f) });
            if (State.Movement.OverrideJumps || State.Movement.InfiniteJumps)
                jumpsSetter.Invoke(body, new object[] { State.Movement.InfiniteJumps ? int.MaxValue : Mathf.Clamp(Prefs.JumpCount, 1, 100) });
        }

        internal static void AdjustSpeed(float direction)
        {
            Prefs.SpeedMultiplier = Mathf.Clamp(Prefs.SpeedMultiplier + direction * Prefs.SpeedStep, 0.1f, 20f);
            State.Movement.SpeedMultiplier = true;
            MenuController.Toast("Speed ×" + Prefs.SpeedMultiplier.ToString("0.00"));
        }

        internal static void Restore()
        {
            var previous = owner;
            owner = null;
            if (previous) previous.RecalculateStats();
            lastSpeed = -1; lastJumps = -1;
        }
    }
}
