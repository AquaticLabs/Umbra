using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Stable palette keys; independent of localized display names.</summary>
    internal enum EspCategory
    {
        Enemy, Boss, Ally, Chest, Printer, Equipment, Lunar, Void, Chance, Blood,
        Combat, Mountain, Woods, Newt, Barrel, Scrapper, Secret, Teleporter,
        CommonItem, UncommonItem, LegendaryItem, BossItem, LunarItem, VoidItem, Other
    }

    /// <summary>One independently editable category or item style. Thickness is in screen pixels.</summary>
    [Serializable]
    internal sealed class EspStyle
    {
        public string Key;
        public Color Color = new Color(0.28f, 0.66f, 1f, 1f);
        public float Thickness = 1.5f;
        public bool Enabled = true;
        public bool Boxes = true;
        public bool Labels = true;

        /// <summary>Creates a palette entry with a stable key and initial color.</summary>
        public EspStyle(string key, Color color) { Key = key; Color = color; }

        /// <summary>Restricts externally edited configuration to usable visual ranges.</summary>
        public void Validate()
        {
            Thickness = VisualSettings.Clamp(Thickness, 1f, 6f, 1.5f);
            Color = VisualSettings.ClampColor(Color);
        }
    }

    /// <summary>Visual-only preferences; enabling gameplay mutations is never persisted here.</summary>
    [Serializable]
    internal sealed class VisualPreferences
    {
        public float WindowOpacity = 0.82f;
        public float CornerRadius = 6f;
        public float WindowX = 40f, WindowY = 40f;
        public bool EnemyEsp, InteractableEsp, ActiveMods = true;
        public bool PerformanceHud = true, RunTimer, Coordinates;
        public bool Crosshair = true;
        public Color CrosshairColor = new Color(0.3f, 0.7f, 1f, 1f);
        public float CrosshairThickness = 1.5f;
        public bool ShowFov = true;
        public Color FovColor = new Color(0.25f, 0.65f, 1f, 0.8f);
        public float FovThickness = 1.5f;
        public bool TargetLine;
        public Color TargetLineColor = new Color(0.35f, 0.8f, 1f, 0.9f);
        public float TargetLineThickness = 1.5f;
        public bool Teleporter = true;
        public bool Pickups;
        public bool Allies;
        public bool Distances = true;
        public bool HealthBars;
        public bool CornerBoxes;
        public float MaxDistance = 300f;
        public int FontSize = 12;
        public List<EspStyle> Styles = new List<EspStyle>();
    }

    /// <summary>Owns the palette and disk persistence, with explicit reset and validation.</summary>
    internal static class VisualSettings
    {
        public static VisualPreferences Current = CreateDefaults();
        public static readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UmbraMenu", "visuals.json");
        public static string LastError { get; private set; }
        private static bool loaded, preserveInvalid;
        private static string lastSaved, observed;
        private static float checkAt, changedAt, retryAt;

        /// <summary>Creates readable defaults for each ESP category.</summary>
        public static VisualPreferences CreateDefaults()
        {
            var result = new VisualPreferences();
            foreach (EspCategory category in Enum.GetValues(typeof(EspCategory)))
            {
                Color color = new Color(0.3f, 0.68f, 1f);
                switch (category)
                {
                    case EspCategory.Enemy: color = new Color(1f, 0.42f, 0.48f); break;
                    case EspCategory.Boss: case EspCategory.BossItem: color = new Color(1f, 0.78f, 0.25f); break;
                    case EspCategory.Ally: case EspCategory.UncommonItem: color = new Color(0.35f, 0.92f, 0.59f); break;
                    case EspCategory.Void: case EspCategory.VoidItem: color = new Color(0.86f, 0.45f, 1f); break;
                    case EspCategory.LegendaryItem: case EspCategory.Blood: color = new Color(1f, 0.3f, 0.36f); break;
                    case EspCategory.Equipment: case EspCategory.Chance: color = new Color(1f, 0.7f, 0.35f); break;
                    case EspCategory.CommonItem: color = new Color(0.93f, 0.96f, 1f); break;
                }
                result.Styles.Add(new EspStyle(category.ToString(), color));
            }
            return result;
        }

        /// <summary>Finds a category style, inserting a default if a configuration omitted it.</summary>
        public static EspStyle For(EspCategory category)
        {
            string key = category.ToString();
            var style = Current.Styles.Find(s => s != null && s.Key == key);
            if (style == null) { style = new EspStyle(key, new Color(0.3f, 0.68f, 1f)); Current.Styles.Add(style); }
            return style;
        }

        /// <summary>Uses an exact catalog override when one exists; otherwise inherits the tier/category.</summary>
        public static EspStyle ForItem(string catalogName, EspCategory fallback)
        {
            return Current.Styles.Find(s => s != null && s.Key == "item:" + catalogName) ?? For(fallback);
        }

        /// <summary>Creates an independent style copied from the item's current category.</summary>
        public static EspStyle OverrideItem(string catalogName, EspCategory fallback)
        {
            string key = "item:" + catalogName;
            var existing = Current.Styles.Find(s => s != null && s.Key == key);
            if (existing != null) return existing;
            var original = For(fallback);
            var result = new EspStyle(key, original.Color) { Thickness = original.Thickness, Boxes = original.Boxes, Labels = original.Labels };
            Current.Styles.Add(result);
            return result;
        }

        /// <summary>Loads visual preferences once; malformed files leave defaults intact.</summary>
        public static void Load()
        {
            Current = CreateDefaults();
            string warning;
            SettingsFile.Read(Path, json =>
            {
                // Overwrite preserves defaults for fields added after the user's file was written.
                var candidate = CreateDefaults();
                SettingsJson.Populate(json, candidate);
                Validate(candidate); Current = candidate;
            }, out warning);
            LastError = warning; preserveInvalid = warning != null && File.Exists(Path);
            loaded = true;
            lastSaved = observed = SettingsJson.Serialize(Current);
            checkAt = changedAt = retryAt = 0;
            if (warning != null) Debug.LogWarning("Umbra visual preferences: " + warning);
        }

        /// <summary>Validates and saves to a temporary sibling before replacing an existing preference file.</summary>
        public static void Save()
        {
            if (!loaded) return;
            Validate(Current);
            string json = SettingsJson.Serialize(Current);
            if (json == lastSaved && File.Exists(Path) && !preserveInvalid) return;
            try
            {
                if (preserveInvalid) { SettingsFile.PreserveInvalid(Path); preserveInvalid = false; }
                SettingsFile.Write(Path, json);
                lastSaved = observed = json; LastError = null;
            }
            catch (Exception error) { LastError = "Visual settings could not be saved: " + error.Message; throw; }
        }

        /// <summary>Debounces changes, including visual hotkeys and an open menu, without writing every frame.</summary>
        public static void Tick()
        {
            if (!loaded || Time.unscaledTime < checkAt) return;
            checkAt = Time.unscaledTime + 0.5f;
            string json = SettingsJson.Serialize(Current);
            if (json != observed) { observed = json; changedAt = Time.unscaledTime; }
            if ((json == lastSaved && !preserveInvalid) || Time.unscaledTime - changedAt < 1f || Time.unscaledTime < retryAt) return;
            try { Save(); }
            catch (Exception error) { retryAt = Time.unscaledTime + 5; Debug.LogWarning(error.Message); }
        }

        /// <summary>Repairs missing palette entries and clamps numeric preferences.</summary>
        private static void Validate(VisualPreferences value)
        {
            value.WindowX = Clamp(value.WindowX, 0, 32768, 40);
            value.WindowY = Clamp(value.WindowY, 0, 32768, 40);
            ValidateValues(value);
        }

        /// <summary>Normalizes a candidate before swapping the active settings object.</summary>
        private static void ValidateValues(VisualPreferences Current)
        {
            Current.WindowOpacity = Clamp(Current.WindowOpacity, 0.4f, 1f, 0.82f);
            Current.CornerRadius = Clamp(Current.CornerRadius, 0f, 10f, 6f);
            Current.FovThickness = Clamp(Current.FovThickness, 1f, 6f, 1.5f);
            Current.TargetLineThickness = Clamp(Current.TargetLineThickness, 1f, 6f, 1.5f);
            Current.TargetLineColor = ClampColor(Current.TargetLineColor);
            Current.CrosshairThickness = Clamp(Current.CrosshairThickness, 1f, 6f, 1.5f);
            Current.MaxDistance = Clamp(Current.MaxDistance, 25f, 1000f, 300f);
            Current.FontSize = Mathf.Clamp(Current.FontSize, 10, 20);
            Current.FovColor = ClampColor(Current.FovColor);
            Current.CrosshairColor = ClampColor(Current.CrosshairColor);
            if (Current.Styles == null) Current.Styles = new List<EspStyle>();
            Current.Styles.RemoveAll(s => s == null || string.IsNullOrEmpty(s.Key));
            foreach (EspStyle style in Current.Styles) style.Validate();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = Current.Styles.Count - 1; i >= 0; i--)
                if (!seen.Add(Current.Styles[i].Key)) Current.Styles.RemoveAt(i);
            foreach (var style in CreateDefaults().Styles)
                if (!seen.Contains(style.Key)) Current.Styles.Add(style);
        }

        /// <summary>Handles NaN and infinity before applying a finite numeric range.</summary>
        public static float Clamp(float value, float min, float max, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }

        /// <summary>Ensures every color channel stays finite and between zero and one.</summary>
        public static Color ClampColor(Color color)
        {
            return new Color(Clamp(color.r, 0, 1, 1), Clamp(color.g, 0, 1, 1), Clamp(color.b, 0, 1, 1), Clamp(color.a, 0, 1, 1));
        }
    }
}
