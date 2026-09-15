using System;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Camera-space targeting with final-point FOV, range, team, health, and visibility validation.</summary>
    internal static class ModernAimbot
    {
        public static bool Enabled, RequireFire;
        public static bool HoldActivation = true;
        public static Vector3 TargetPoint { get; private set; }
        public static bool HasTarget { get { return target; } }
        public static bool VisibilityCheck = true;
        public static float FieldOfView = 24f, MaxDistance = 180f, Smoothing = 12f;
        private static int mode, priority, aimPoint;
        private static CharacterBody target;
        private static readonly string[] Modes = { "Direct", "Smooth", "Fire only" };
        private static readonly string[] Priorities = { "Crosshair", "Distance", "Lowest health %", "Boss first" };
        private static readonly string[] AimPoints = { "Weak point", "Center mass", "Nearest hurtbox" };
        public static string ModeName { get { return Modes[mode]; } }
        public static string PriorityName { get { return Priorities[priority]; } }
        public static string AimPointName { get { return AimPoints[aimPoint]; } }
        public static string TargetName { get { return target ? target.GetDisplayName() : "None"; } }

        /// <summary>Cycles how the validated aim vector is applied.</summary>
        public static void CycleMode() { mode = (mode + 1) % Modes.Length; ClearTarget(); }
        /// <summary>Cycles scoring; no priority can bypass acceptance filters.</summary>
        public static void CyclePriority() { priority = (priority + 1) % Priorities.Length; ClearTarget(); }
        /// <summary>Cycles the final point used by all subsequent checks.</summary>
        public static void CycleAimPoint() { aimPoint = (aimPoint + 1) % AimPoints.Length; ClearTarget(); }
        /// <summary>Clears stale HUD state when input or scene state changes.</summary>
        public static void ClearTarget() { target = null; TargetPoint = Vector3.zero; }

        /// <summary>Revalidates every frame using camera projection, never the previous redirected aim vector.</summary>
        public static void Update()
        {
            ClearTarget();
            var body = UmbraRuntime.LocalPlayerBody;
            if (!Enabled || !body || !UmbraRuntime.characterCollected || MenuInput.Capturing || KeyBindings.IsCapturing || UmbraRuntime.chatOpen || GameInput.CursorIsVisible()) return;
            var activation = KeyBindings.Find("Aim activation");
            if (HoldActivation && (activation == null || activation.Key == KeyCode.None || !Input.GetKey(activation.Key))) return;
            var input = body.inputBank;
            var camera = OverlayGeometry.GetCamera();
            if (!input || !camera || !body.teamComponent) return;
            if ((RequireFire || mode == 2) && !input.skill1.down) return;
            Vector3 point = Vector3.zero;
            double bestScore = double.MaxValue;
            foreach (var candidate in CharacterBody.readOnlyInstancesList)
            {
                if (!candidate || candidate == body || !candidate.healthComponent || !candidate.healthComponent.alive || !candidate.teamComponent) continue;
                TeamIndex team = candidate.teamComponent.teamIndex;
                if (team == body.teamComponent.teamIndex || team == TeamIndex.Neutral || team == TeamIndex.None) continue;
                Vector3 finalPoint = AimPosition(candidate, input.aimOrigin);
                Vector3 delta = finalPoint - input.aimOrigin;
                if (delta.sqrMagnitude > MaxDistance * MaxDistance || delta.sqrMagnitude < 0.001f) continue;
                Vector2 screen;
                if (!OverlayGeometry.InsideFov(camera, finalPoint, out screen)) continue;
                if (VisibilityCheck && Physics.Linecast(input.aimOrigin, finalPoint, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                double score = Score(candidate, screen, camera, delta.sqrMagnitude);
                if (score < bestScore) { bestScore = score; target = candidate; point = finalPoint; }
            }
            if (!target) return;
            TargetPoint = point;
            // RedirectCamera expects Euler angles and updates the game's persistent camera mode state.
            Vector3 desired = (point - camera.transform.position).normalized;
            float blend = 1f - Mathf.Exp(-Smoothing * Time.unscaledDeltaTime);
            Vector3 cameraDirection = mode == 1 ? Vector3.Slerp(camera.transform.forward, desired, blend).normalized : desired;
            var controller = UmbraRuntime.LocalNetworkUser ? UmbraRuntime.LocalNetworkUser.masterController : null;
            if (controller) controller.RedirectCamera(Quaternion.LookRotation(cameraDirection).eulerAngles);
            input.aimDirection = mode == 1 ? cameraDirection : (point - input.aimOrigin).normalized;
        }

        /// <summary>Ranks only already validated candidates, with deterministic screen-distance tie breaking.</summary>
        private static double Score(CharacterBody body, Vector2 screen, Camera camera, float distanceSquared)
        {
            float screenDistance = (screen - OverlayGeometry.Center(camera)).sqrMagnitude;
            switch (priority)
            {
                case 1: return distanceSquared;
                case 2: return body.healthComponent.health / Mathf.Max(1f, body.healthComponent.fullHealth) + screenDistance * 1e-12;
                case 3: return (body.isBoss ? 0d : 1e12) + screenDistance;
                default: return screenDistance;
            }
        }

        /// <summary>Resolves a weak point or nearest hurtbox before visibility, distance, and FOV tests.</summary>
        private static Vector3 AimPosition(CharacterBody body, Vector3 origin)
        {
            if (aimPoint == 1) return body.corePosition;
            HurtBox main = body.mainHurtBox;
            if (!main) return body.corePosition;
            var group = main.hurtBoxGroup;
            Vector3 point = main.transform.position;
            float nearest = float.MaxValue;
            if (group && group.hurtBoxes != null)
                foreach (var box in group.hurtBoxes)
                {
                    if (!box) continue;
                    if (aimPoint == 0 && box.isSniperTarget) return box.transform.position;
                    float distance = (box.transform.position - origin).sqrMagnitude;
                    if (aimPoint == 2 && distance < nearest) { nearest = distance; point = box.transform.position; }
                }
            return point;
        }
    }
}
