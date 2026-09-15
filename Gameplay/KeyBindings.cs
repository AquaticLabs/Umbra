using System;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Stable, persisted key assignments with shared host checks for UI and shortcuts.</summary>
    internal static class KeyBindings
    {
        internal sealed class Binding
        {
            public string Id;
            public KeyCode Key;
            public bool HostOnly;
            public Action Action;
        }
        private static readonly List<Binding> bindings = new List<Binding>();
        private static readonly KeyCode[] keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        private static Binding capturing;
        private static int captureFrame;
        private static bool loaded, dirty, preserveInvalid;
        private static float retryAt;
        private static string lastSaved;
        public static string LastError { get; private set; }
        public static bool IsCapturing { get { return capturing != null; } }
        private static string Path { get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(VisualSettings.Path), "keys.json"); } }

        /// <summary>Registers every supported toggle before any page is opened, then restores assignments only.</summary>
        public static void Initialize()
        {
            bindings.Clear();
            Toggle("God mode", () => State.Player.GodToggle, v => State.Player.GodToggle = v, true);
            Toggle("Infinite skills", () => State.Player.SkillToggle, v => State.Player.SkillToggle = v, true);
            Toggle("Infinite equipment", () => State.Items.noEquipmentCD, v => State.Items.noEquipmentCD = v, true);
            Toggle("Damage scaling", () => State.StatsMod.damageToggle, v => State.StatsMod.damageToggle = v, true);
            Toggle("Critical scaling", () => State.StatsMod.critToggle, v => State.StatsMod.critToggle = v, true);
            Toggle("Attack speed override", () => State.StatsMod.attackSpeedToggle, v => State.StatsMod.attackSpeedToggle = v, true);
            Toggle("Armor override", () => State.StatsMod.armorToggle, v => State.StatsMod.armorToggle = v, true);
            Toggle("Move speed override", () => State.StatsMod.moveSpeedToggle, v => State.StatsMod.moveSpeedToggle = v, true);
            Toggle("Aimbot enabled", () => ModernAimbot.Enabled, v => { ModernAimbot.Enabled = v; State.Player.AimBotToggle = v; });
            Add("Aim activation", null, KeyCode.Mouse1);
            Toggle("Always sprint", () => State.Movement.alwaysSprintToggle, v => State.Movement.alwaysSprintToggle = v);
            Toggle("Flight", () => State.Movement.flightToggle, v => State.Movement.flightToggle = v, false, KeyCode.C);
            Toggle("Jump pack", () => State.Movement.jumpPackToggle, v => State.Movement.jumpPackToggle = v);
            Toggle("Railgunner Perfect Reload", () => State.Player.RailgunPerfectReload, v => State.Player.RailgunPerfectReload = v);
            Toggle("Enemy ESP", () => State.Render.renderMobs, v => State.Render.renderMobs = v);
            Toggle("Interactable ESP", () => State.Render.renderInteractables, v => State.Render.renderInteractables = v);
            Toggle("Active mod strip", () => State.Render.renderMods, v => State.Render.renderMods = v);
            Toggle("Teleporter", () => VisualSettings.Current.Teleporter, v => VisualSettings.Current.Teleporter = v);
            Toggle("Dropped pickups", () => VisualSettings.Current.Pickups, v => VisualSettings.Current.Pickups = v);
            Toggle("Allies", () => VisualSettings.Current.Allies, v => VisualSettings.Current.Allies = v);
            Toggle("Distance labels", () => VisualSettings.Current.Distances, v => VisualSettings.Current.Distances = v);
            Toggle("Health bars", () => VisualSettings.Current.HealthBars, v => VisualSettings.Current.HealthBars = v);
            Toggle("Corner boxes", () => VisualSettings.Current.CornerBoxes, v => VisualSettings.Current.CornerBoxes = v);
            Toggle("Crosshair", () => VisualSettings.Current.Crosshair, v => VisualSettings.Current.Crosshair = v);
            Toggle("Draw FOV circle", () => VisualSettings.Current.ShowFov, v => VisualSettings.Current.ShowFov = v);
            Toggle("Target line", () => VisualSettings.Current.TargetLine, v => VisualSettings.Current.TargetLine = v);
            Toggle("FPS / ping", () => MiscFeatures.PerformanceHud, v => MiscFeatures.PerformanceHud = v);
            Toggle("Run timer", () => MiscFeatures.RunTimer, v => MiscFeatures.RunTimer = v);
            Toggle("Coordinates", () => MiscFeatures.Coordinates, v => MiscFeatures.Coordinates = v);
            Add("Open Player", () => MenuController.OpenPage(0), KeyCode.Z);
            Add("Open World", () => MenuController.OpenPage(4), KeyCode.B);
            Add("Open Items", () => MenuController.OpenPage(3), KeyCode.I);
            CancelCapture();
            string warning;
            SettingsFile.Read(Path, json =>
            {
                var saved = SettingsJson.ParseObject(json)["Keys"];
                if (saved == null || !saved.IsArray) throw new InvalidDataException("Missing key assignment list.");
                // Stage parsing first, then apply validated entries. Duplicate keys have one deterministic owner.
                var assignments = new List<KeyValuePair<Binding, KeyCode>>();
                foreach (var key in saved.Children)
                {
                    if (!key.IsObject || !key["Id"].IsString || !key["Key"].IsNumber) throw new InvalidDataException("Invalid key assignment.");
                    var binding = Find(key["Id"].Value);
                    int code = key["Key"].AsInt;
                    if (binding != null && Enum.IsDefined(typeof(KeyCode), code) && !Reserved((KeyCode)code))
                        assignments.Add(new KeyValuePair<Binding, KeyCode>(binding, (KeyCode)code));
                }
                foreach (var assignment in assignments) Assign(assignment.Key, assignment.Value);
            }, out warning);
            loaded = true; LastError = warning; preserveInvalid = warning != null && File.Exists(Path);
            dirty = preserveInvalid; retryAt = 0; lastSaved = Serialize();
            if (warning != null) Debug.LogWarning("Umbra key preferences: " + warning);
        }

        /// <summary>Registers a toggle with a live getter, not a stale value captured by a menu frame.</summary>
        private static void Toggle(string id, Func<bool> get, Action<bool> set, bool host = false, KeyCode key = KeyCode.None)
        { Add(id, () => set(!get()), key, host); }
        /// <summary>Registers an action or the dedicated non-action activation key.</summary>
        private static void Add(string id, Action action, KeyCode key = KeyCode.None, bool host = false)
        { bindings.Add(new Binding { Id = id, Action = action, Key = key, HostOnly = host }); }
        /// <summary>Resolves shared labels used in more than one page.</summary>
        public static Binding Find(string id)
        {
            if (id == "Show active mods") id = "Active mod strip";
            if (id == "FOV guide") id = "Draw FOV circle";
            return bindings.Find(b => b.Id == id);
        }
        /// <summary>Shows an assignment or listening state on the compact button.</summary>
        public static string Caption(Binding binding)
        { return binding == null ? "—" : binding == capturing ? "Press…" : binding.Key == KeyCode.None ? "Bind" : binding.Key.ToString(); }
        /// <summary>Starts capture after the click event has finished.</summary>
        public static void BeginCapture(Binding binding) { capturing = binding; captureFrame = Time.frameCount + 1; }
        /// <summary>Cancels without changing any assignment.</summary>
        public static void CancelCapture() { capturing = null; }
        /// <summary>Protects menu lifecycle keys from being reassigned.</summary>
        private static bool Reserved(KeyCode key) { return key == KeyCode.Insert || key == KeyCode.End || key == KeyCode.Escape || key == KeyCode.Delete; }
        /// <summary>Captures keyboard/mouse inputs exclusively; duplicate assignments are moved to the new binding.</summary>
        public static void Update()
        {
            if (capturing != null)
            {
                if (Time.frameCount <= captureFrame) return;
                if (Input.GetKeyDown(KeyCode.Escape)) { CancelCapture(); return; }
                foreach (var key in keys)
                {
                    if (key == KeyCode.None || !Input.GetKeyDown(key) || (Reserved(key) && key != KeyCode.Delete)) continue;
                    KeyCode assigned = key == KeyCode.Delete ? KeyCode.None : key;
                    Assign(capturing, assigned); capturing = null; dirty = true; Save(); return;
                }
                return;
            }
            if (MenuController.IsOpen || GameInput.CursorIsVisible()) return;
            foreach (var binding in bindings)
                if (binding.Action != null && binding.Key != KeyCode.None && (!binding.HostOnly || MenuController.HasHost) && Input.GetKeyDown(binding.Key))
                { binding.Action(); break; }
        }
        /// <summary>Assigns a key to exactly one registered binding; None may be shared.</summary>
        private static void Assign(Binding binding, KeyCode key)
        {
            if (key != KeyCode.None) foreach (var other in bindings) if (other != binding && other.Key == key) other.Key = KeyCode.None;
            binding.Key = key;
        }

        /// <summary>Serializes assignments in stable registration order without gameplay toggle state.</summary>
        private static string Serialize()
        {
            var keys = new JSONArray();
            foreach (var binding in bindings) keys.Add(new JSONObject { ["Id"] = binding.Id, ["Key"] = (int)binding.Key });
            return new JSONObject { ["Keys"] = keys }.ToString(2);
        }

        /// <summary>Retries failed writes without losing the user's in-memory assignment.</summary>
        public static void Tick() { if (dirty && Time.unscaledTime >= retryAt) Save(); }

        /// <summary>Saves atomically and exposes failures in the menu instead of silently discarding them.</summary>
        public static void Save()
        {
            if (!loaded) return;
            try
            {
                string json = Serialize();
                if (json == lastSaved && File.Exists(Path) && !preserveInvalid) { dirty = false; return; }
                if (preserveInvalid) { SettingsFile.PreserveInvalid(Path); preserveInvalid = false; }
                SettingsFile.Write(Path, json);
                dirty = false; LastError = null; lastSaved = json;
            }
            catch (Exception error)
            {
                dirty = true; retryAt = Time.unscaledTime + 5f;
                LastError = "Key assignments could not be saved: " + error.Message;
                Debug.LogWarning(LastError);
            }
        }
    }
}
