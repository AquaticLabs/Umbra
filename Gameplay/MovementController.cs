using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Owns flight gravity/fall-damage changes and applies velocity at the motor's physics boundary.</summary>
    internal static class MovementController
    {
        private static CharacterMotor motor;
        private static CharacterBody body;
        private static bool gravityOwned, ignoredFallDamage;
        private static Vector3 requestedVelocity;
        private static bool jumpRequested;

        /// <summary>Captures controls each frame. UI focus stops movement input, not flight's altitude hold.</summary>
        public static void Update()
        {
            var current = UmbraRuntime.LocalPlayerBody;
            bool active = UmbraRuntime.characterCollected && current && UmbraRuntime.LocalMotor;
            bool altered = active && GameHooks.Installed && (State.Movement.flightToggle || State.Movement.jumpPackToggle);
            if (body != current || motor != UmbraRuntime.LocalMotor || !altered) Restore();
            if (!active) return;
            bool inputAllowed = !MenuController.IsOpen && !MenuInput.Capturing && !UmbraRuntime.chatOpen && Application.isFocused;
            var input = current.inputBank;
            if (inputAllowed && input && State.Movement.alwaysSprintToggle && input.moveVector.sqrMagnitude > 0.01f) current.isSprinting = true;
            if (!altered) return;
            if (!body)
            {
                body = current; motor = UmbraRuntime.LocalMotor;
                ignoredFallDamage = (body.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) != 0;
            }
            body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            if (State.Movement.flightToggle)
            {
                if (!gravityOwned)
                {
                    var gravity = motor.gravityParameters;
                    gravity.environmentalAntiGravityGranterCount++;
                    motor.gravityParameters = gravity;
                    gravityOwned = true;
                }
                requestedVelocity = inputAllowed && input ? input.moveVector * (body.isSprinting ? 40f : 20f) : Vector3.zero;
                requestedVelocity.y = inputAllowed && input ? input.jump.down ? 20f : Input.GetKey(KeyCode.X) ? -20f : 0f : 0f;
                // Keep the public velocity consistent between fixed ticks, including menu-only frames.
                motor.velocity = requestedVelocity;
            }
            else ReleaseGravity();
            jumpRequested = inputAllowed && input && input.jump.down;
        }

        /// <summary>Runs after CharacterMotor.UpdateVelocity so acceleration and gravity cannot reintroduce drift.</summary>
        public static void AfterMotorVelocity(CharacterMotor instance, ref Vector3 velocity)
        {
            if (!body || instance != motor || !UmbraRuntime.characterCollected) return;
            if (State.Movement.flightToggle) velocity = requestedVelocity;
            else if (State.Movement.jumpPackToggle && jumpRequested) velocity.y = 15f;
        }

        /// <summary>Restores the pre-flight gravity flag when switching to jump pack or disabling flight.</summary>
        private static void ReleaseGravity()
        {
            if (gravityOwned && motor)
            {
                var gravity = motor.gravityParameters;
                gravity.environmentalAntiGravityGranterCount = Mathf.Max(0, gravity.environmentalAntiGravityGranterCount - 1);
                motor.gravityParameters = gravity;
            }
            gravityOwned = false;
        }

        /// <summary>Restores only owned state on disable, body changes, scene changes and unload.</summary>
        public static void Restore()
        {
            ReleaseGravity();
            if (body && !ignoredFallDamage) body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            body = null; motor = null; requestedVelocity = Vector3.zero; jumpRequested = false;
        }
    }
}
