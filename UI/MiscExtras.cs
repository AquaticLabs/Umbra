namespace UmbraMenu
{
    internal static partial class ModernMenu
    {
        /// <summary>Provides useful local telemetry and explicit, scene-scoped recovery actions.</summary>
        private static void DrawMiscExtras()
        {
            AddCard(0, "ON-SCREEN TELEMETRY", c =>
            {
                DrawToggle(c, "FPS / ping", MiscFeatures.PerformanceHud, v => MiscFeatures.PerformanceHud = v, "Shown on the right side; host ping is 0 ms.");
                DrawToggle(c, "Run timer", MiscFeatures.RunTimer, v => MiscFeatures.RunTimer = v, null);
                DrawToggle(c, "Coordinates", MiscFeatures.Coordinates, v => MiscFeatures.Coordinates = v, null);
                DrawBinding(c, "Player page shortcut", "Open Player");
                DrawBinding(c, "Items page shortcut", "Open Items");
                DrawBinding(c, "World page shortcut", "Open World");
            });
            AddCard(1, "POSITION BOOKMARK", c =>
            {
                DrawParagraph(c, "Save a safe location and return if stuck. The bookmark expires on stage change. Host only.");
                DrawButtonRow(c, new ButtonAction("Save position", () => { MiscFeatures.Bookmark(); Toast("Position saved for this stage"); }), new ButtonAction("Return", MiscFeatures.Return));
            }, true);
        }
    }
}
