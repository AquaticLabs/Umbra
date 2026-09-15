using System;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UmbraMenu
{
    /// <summary>Small runtime coordinator. Loader owns creation; independent services own gameplay and persistence.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class UmbraMenu : MonoBehaviour
    {
        public const string NAME = "UMBRA", VERSION = "2.4.0";
        public bool IsReady { get; private set; }
        private bool stopped, preferencesLoaded, sceneSubscribed;
        public static Scene currentScene { get; private set; }
        public static bool chatOpen { get; private set; }

        // Stable accessors keep page layouts independent of runtime bookkeeping.
        public static NetworkUser LocalNetworkUser { get { return RuntimeContext.User; } }
        public static CharacterMaster LocalPlayer { get { return RuntimeContext.Master; } }
        public static CharacterBody LocalPlayerBody { get { return RuntimeContext.Body; } }
        public static Inventory LocalPlayerInv { get { return RuntimeContext.Inventory; } }
        public static HealthComponent LocalHealth { get { return RuntimeContext.Health; } }
        public static SkillLocator LocalSkills { get { return RuntimeContext.Skills; } }
        public static CharacterMotor LocalMotor { get { return RuntimeContext.Motor; } }
        public static bool characterCollected { get { return RuntimeContext.Alive; } }

        /// <summary>Called explicitly by Loader after RoR2 finishes loading, never by an injector thread.</summary>
        internal void Initialize()
        {
            if (IsReady) return;
            VisualSettings.Load();
            KeyBindings.Initialize();
            preferencesLoaded = true;
            ModernMenu.Initialize();
            GameHooks.Install();
            if (!GameHooks.Installed) throw new InvalidOperationException("Required input/camera/movement hooks could not be installed. See Player.log.");
            currentScene = SceneManager.GetActiveScene();
            RuntimeContext.Refresh();
            SceneManager.activeSceneChanged += OnSceneChanged; sceneSubscribed = true;
            RoR2Application.isModded = true;
            IsReady = true;
        }

        /// <summary>Samples input and coordinates ready services; body absence is a normal lobby/loading state.</summary>
        private void Update()
        {
            if (!IsReady || stopped) return;
            RuntimeContext.Refresh();
            HandleInput(); MenuInput.Update();
            MovementController.Update(); GodModes.Update(); StatOverrides.Update(LocalPlayerBody);
            if (characterCollected && ModernMenu.HasHost)
            {
                if (State.Player.SkillToggle && LocalSkills) LocalSkills.ApplyAmmoPack();
                if (State.Items.noEquipmentCD && LocalPlayerInv) State.Items.NoEquipmentCooldown();
            }
            MiscFeatures.Update(); SpawnCatalog.Update(); EspRenderer.Update();
            VisualSettings.Tick(); KeyBindings.Tick(); RailgunPerfectReload.Update();
        }

        /// <summary>Keeps the menu cursor usable; camera and flight changes run at their native hook boundaries.</summary>
        private void LateUpdate()
        {
            if (!IsReady) return;
            if (!characterCollected || !ModernAimbot.Enabled) ModernAimbot.ClearTarget();
            if (ModernMenu.IsOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        }

        /// <summary>Draws overlays and the user's menu layout, restoring GUI state even if drawing exits early.</summary>
        private void OnGUI()
        {
            if (!IsReady) return;
            int depth = GUI.depth;
            try { GUI.depth = -50; EspRenderer.Draw(); MiscFeatures.Draw(); ModernMenu.Draw(); }
            finally { GUI.depth = depth; }
        }

        /// <summary>Routes reserved keys and exclusive rebinding before ordinary gameplay shortcuts.</summary>
        private static void HandleInput()
        {
            chatOpen = Utility.CursorIsVisible();
            if (KeyBindings.IsCapturing) { KeyBindings.Update(); return; }
            if (Input.GetKeyDown(KeyCode.Insert)) { ModernMenu.SetOpen(!ModernMenu.IsOpen); return; }
            if (Input.GetKeyDown(KeyCode.End)) { ModernMenu.DisableGameplayMods(); return; }
            KeyBindings.Update();
        }

        /// <summary>Restores state owned by the previous body and invalidates scene caches.</summary>
        private void OnSceneChanged(Scene previous, Scene next)
        {
            currentScene = next;
            Cleanup("scene movement", MovementController.Restore);
            Cleanup("scene stats", StatOverrides.Restore);
            Cleanup("scene god mode", GodModes.Restore);
            ModernAimbot.ClearTarget(); EspRenderer.Clear(); MiscFeatures.Clear();
            RuntimeContext.Clear();
        }

        /// <summary>Flushes preferences when the process exits or loses focus, including while the menu stays closed.</summary>
        private void OnApplicationQuit() { Shutdown(); }
        private void OnApplicationFocus(bool focused) { if (!focused && IsReady) FlushPreferences(); }
        private void OnDestroy() { Shutdown(); }

        /// <summary>Idempotent teardown: one failing service cannot prevent the remaining resources from being released.</summary>
        internal void Shutdown()
        {
            if (stopped) return;
            stopped = true; IsReady = false;
            if (sceneSubscribed) SceneManager.activeSceneChanged -= OnSceneChanged;
            sceneSubscribed = false;
            if (preferencesLoaded) FlushPreferences();
            Cleanup("movement", MovementController.Restore);
            Cleanup("god mode", GodModes.Restore);
            Cleanup("statistics", StatOverrides.Restore);
            Cleanup("gameplay toggles", ModernMenu.DisableGameplayMods);
            Cleanup("menu", ModernMenu.Dispose);
            Cleanup("hooks", GameHooks.Uninstall);
            Cleanup("pointer shield", MenuInput.Dispose);
            Cleanup("spawn assets", SpawnCatalog.Dispose);
            Cleanup("item cache", ItemService.Clear);
            Cleanup("ESP", EspRenderer.Clear);
            Cleanup("overlay texture", ESPHelper.Dispose);
            State.Spawn.spawnedObjects.Clear(); KeyBindings.CancelCapture();
            RuntimeContext.Clear(); chatOpen = false;
        }

        /// <summary>Attempts both independent settings files even when one cannot be written.</summary>
        private static void FlushPreferences() { Cleanup("visual settings", VisualSettings.Save); Cleanup("key settings", KeyBindings.Save); }
        /// <summary>Logs a cleanup error without abandoning subsequent releases.</summary>
        private static void Cleanup(string name, Action action)
        { try { action(); } catch (Exception error) { Debug.LogError("Umbra " + name + ": " + error); } }
    }
}
