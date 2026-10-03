using System.Collections.Generic;
using System.Linq;

namespace UmbraMenu
{
    /// <summary>Explicit allowlist of repeatable toggles; never replays gifts, currency or spawn commands.</summary>
    internal static class DefaultMods
    {
        internal static readonly string[] Options = { "God mode", "Infinite skills", "Infinite equipment", "Always sprint", "Flight",
            "Jump pack", "Infinite jumps", "Override jump count", "Speed multiplier", "Railgunner Perfect Reload", "Unlimited Engineer turrets" };
        private static bool pending;
        private static VisualPreferences Prefs { get { return VisualSettings.Current; } }
        internal static void Initialize() { pending = Prefs.ApplyDefaultMods; }
        internal static void CancelPending() { pending = false; }
        internal static bool Selected(string name) { return Names().Contains(name); }
        private static HashSet<string> Names() { return new HashSet<string>((Prefs.DefaultMods ?? "").Split('|').Where(Options.Contains)); }

        internal static void Select(string name, bool enabled)
        {
            if (!Options.Contains(name)) return;
            var names = Names();
            if (enabled) names.Add(name); else names.Remove(name);
            Prefs.DefaultMods = string.Join("|", Options.Where(names.Contains).ToArray());
        }

        internal static void Update()
        {
            if (!pending || !UmbraRuntime.characterCollected) return;
            pending = false; Apply();
        }

        internal static void Apply()
        {
            var names = Names();
            bool host = MenuController.HasHost;
            if (host && names.Contains("God mode")) State.Player.GodToggle = true;
            if (host && names.Contains("Infinite skills")) State.Player.SkillToggle = true;
            if (host && names.Contains("Infinite equipment")) State.Items.noEquipmentCD = true;
            if (host && names.Contains("Unlimited Engineer turrets")) State.Player.UnlimitedTurrets = true;
            if (names.Contains("Always sprint")) State.Movement.alwaysSprintToggle = true;
            if (names.Contains("Flight")) State.Movement.flightToggle = true;
            if (names.Contains("Jump pack")) State.Movement.jumpPackToggle = true;
            if (names.Contains("Infinite jumps")) State.Movement.InfiniteJumps = true;
            if (names.Contains("Override jump count")) State.Movement.OverrideJumps = true;
            if (names.Contains("Speed multiplier")) State.Movement.SpeedMultiplier = true;
            if (names.Contains("Railgunner Perfect Reload")) State.Player.RailgunPerfectReload = true;
            MenuController.Toast(host ? "Selected default mods applied" : "Local defaults applied; host-only defaults skipped");
        }
    }
}
