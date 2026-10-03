using System;
using UmbraMenu;

// Only the engine-facing data holders are stubbed. The linked production formatting,
// startup selection and projection arithmetic run unchanged, without loading Unity.
namespace UnityEngine { public struct Color { } }
namespace RoR2
{
    public enum CostTypeIndex { None, Money, PercentHealth, LunarCoin, WhiteItem, GreenItem, RedItem, Equipment,
        VolatileBattery, LunarItemOrEquipment, BossItem, ArtifactShellKillerItem, TreasureCacheItem, TreasureCacheVoidItem, VoidCoin, SoulCost }
}
namespace UmbraMenu
{
    internal class VisualPreferences
    {
        public bool ApplyDefaultMods;
        public string DefaultMods = "";
        public UnityEngine.Color HealthCostColor = new UnityEngine.Color(), SoulCostColor = new UnityEngine.Color();
    }
    internal static class VisualSettings { internal static VisualPreferences Current = new VisualPreferences(); }
    internal static class UmbraRuntime { internal static bool characterCollected; }
    internal static class MenuController { internal static bool HasHost; internal static void Toast(string text) { } }
}
namespace UmbraMenu.State
{
    internal static class Player { internal static bool GodToggle, SkillToggle, UnlimitedTurrets, RailgunPerfectReload; }
    internal static class Items { internal static bool noEquipmentCD; }
    internal static class Movement { internal static bool alwaysSprintToggle, flightToggle, jumpPackToggle, InfiniteJumps, OverrideJumps, SpeedMultiplier; }
}
internal static class LogicChecks
{
    private static int passed;
    private static void Equal<T>(string name, T expected, T actual)
    {
        if (!Equals(expected, actual)) throw new Exception(name + ": expected " + expected + ", got " + actual);
        passed++; Console.WriteLine("PASS " + name);
    }
    private static void Main()
    {
        Equal("Money label uses dollar notation", "($30)", CostLabels.Format(RoR2.CostTypeIndex.Money, 30));
        Equal("Health sacrifice is readable", "-%50 Health", CostLabels.Format(RoR2.CostTypeIndex.PercentHealth, 50));
        Equal("Soul sacrifice is readable", "-%30 Soul", CostLabels.Format(RoR2.CostTypeIndex.SoulCost, 30));
        Equal("Printer cost names the tier", "1 Common item(s)", CostLabels.Format(RoR2.CostTypeIndex.WhiteItem, 1));
        Equal("No cost has no label", "", CostLabels.Format(RoR2.CostTypeIndex.None, 0));
        Equal("Lunar single coin is singular", "1 Lunar coin", CostLabels.Format(RoR2.CostTypeIndex.LunarCoin, 1));
        Equal("FOV includes its boundary", true, ProjectionMath.InsideDisk(10, 0, 0, 0, 10));
        Equal("FOV rejects outside point", false, ProjectionMath.InsideDisk(11, 0, 0, 0, 10));
        Equal("FOV rejects invalid point", false, ProjectionMath.InsideDisk(float.NaN, 0, 0, 0, 10));
        RejectUnsafeDefaults();
        WaitForBodyThenApplyOnce();
        CancelBeforeReady();
        SkipHostDefaultsOnClient();
        Console.WriteLine(passed + " checks passed. Unity/network behavior is not covered by these checks.");
    }
    private static void Reset()
    {
        VisualSettings.Current = new VisualPreferences();
        UmbraRuntime.characterCollected = false; MenuController.HasHost = false;
        UmbraMenu.State.Player.GodToggle = false; UmbraMenu.State.Movement.flightToggle = false;
        DefaultMods.CancelPending();
    }
    private static void RejectUnsafeDefaults()
    {
        Reset();
        DefaultMods.Select("No Lunar Cost", true);
        Equal("Currency bypass cannot be opted into startup", "", VisualSettings.Current.DefaultMods);
    }
    private static void WaitForBodyThenApplyOnce()
    {
        Reset(); VisualSettings.Current.ApplyDefaultMods = true; DefaultMods.Select("Flight", true);
        DefaultMods.Initialize(); DefaultMods.Update();
        Equal("Defaults wait for a character", false, UmbraMenu.State.Movement.flightToggle);

        UmbraRuntime.characterCollected = true; DefaultMods.Update();
        Equal("Selected default enables on readiness", true, UmbraMenu.State.Movement.flightToggle);

        UmbraMenu.State.Movement.flightToggle = false; DefaultMods.Update();
        Equal("Manual disable is not undone next frame", false, UmbraMenu.State.Movement.flightToggle);
    }
    private static void CancelBeforeReady()
    {
        Reset(); VisualSettings.Current.ApplyDefaultMods = true; DefaultMods.Select("Flight", true);
        DefaultMods.Initialize(); DefaultMods.CancelPending();
        UmbraRuntime.characterCollected = true; DefaultMods.Update();
        Equal("Emergency stop cancels pending defaults", false, UmbraMenu.State.Movement.flightToggle);
    }
    private static void SkipHostDefaultsOnClient()
    {
        Reset(); DefaultMods.Select("God mode", true);
        DefaultMods.Apply();
        Equal("Client cannot apply host-only defaults", false, UmbraMenu.State.Player.GodToggle);
    }
}
