using UnityEngine;

namespace UmbraMenu
{
    internal static partial class ModernMenu
    {
        /// <summary>Dedicated camera targeting page, with activation separate from the master toggle key.</summary>
        private static void DrawAimbot(float width)
        {
            AddCard(0, "ACTIVATION & CAMERA", c =>
            {
                DrawToggle(c, "Aimbot enabled", ModernAimbot.Enabled, SetAimbotEnabled, null);
                DrawCycle(c, "Activation", ModernAimbot.HoldActivation ? "Hold key" : "Constant", () => ModernAimbot.HoldActivation = !ModernAimbot.HoldActivation);
                DrawBinding(c, "Activation key (hold)", "Aim activation");
                DrawCycle(c, "Mode", ModernAimbot.ModeName, ModernAimbot.CycleMode);
                DrawSlider(c, "Smoothing speed", ref ModernAimbot.Smoothing, 1, 30, "0.0");
                DrawParagraph(c, "Smooth physically turns the camera. Higher speed pulls faster. Direct snaps; Fire only additionally requires primary fire. Aim pauses over game UI.");
                DrawReadout(c, "Current target", ModernAimbot.TargetName);
                DrawReadout(c, "Camera / input hooks", GameHooks.Installed ? "Ready" : "Unavailable");
                if (!GameHooks.Installed) DrawParagraph(c, "Camera/input hooks failed to install. Check Player.log before using aim or relying on click blocking.");
            });
            AddCard(1, "TARGET RULES", c =>
            {
                DrawCycle(c, "Priority", ModernAimbot.PriorityName, ModernAimbot.CyclePriority);
                DrawCycle(c, "Aim point", ModernAimbot.AimPointName, ModernAimbot.CycleAimPoint);
                DrawSlider(c, "FOV half-angle (degrees)", ref ModernAimbot.FieldOfView, 2, 60, "0");
                DrawSlider(c, "Maximum range (m)", ref ModernAimbot.MaxDistance, 25, 500, "0");
                DrawToggle(c, "Visibility check", ModernAimbot.VisibilityCheck, v => ModernAimbot.VisibilityCheck = v, null);
                DrawToggle(c, "Require primary fire", ModernAimbot.RequireFire, v => ModernAimbot.RequireFire = v, null);
            });
            AddCard(0, "FOV GUIDE", c =>
            {
                DrawToggle(c, "Draw FOV circle", Prefs.ShowFov, v => Prefs.ShowFov = v, null);
                DrawSlider(c, "FOV thickness (px)", ref Prefs.FovThickness, 1, 6, "0.0");
                DrawColor(c, ref Prefs.FovColor);
            });
            AddCard(1, "TARGET LINE", c =>
            {
                DrawToggle(c, "Target line", Prefs.TargetLine, v => Prefs.TargetLine = v, "From the cursor (or crosshair while locked) to the selected aim point.");
                DrawSlider(c, "Line thickness (px)", ref Prefs.TargetLineThickness, 1, 6, "0.0");
                DrawColor(c, ref Prefs.TargetLineColor);
            });
        }
    }
}
