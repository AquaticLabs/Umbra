using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Owns reversible invulnerability and hurtbox changes; regeneration/revive are explicit modes.</summary>
    internal static class GodModes
    {
        private static int mode;
        private static readonly string[] names = { "Invulnerable", "Intangible", "Regeneration", "Auto revive" };
        private static readonly string[] descriptions = { "Blocks incoming damage while enabled.", "Disables your hurtboxes while enabled; does not prevent every scripted death.", "Restores health repeatedly; lethal one-shot damage can still kill.", "Requests a respawn after death; does not prevent a run from ending." };
        private static HealthComponent owner;
        private static bool originalGod, applied;
        private static HurtBoxGroup hurtboxes;
        private static bool wasAlive;
        private static float reviveAt = -1;
        public static string Name { get { return names[mode]; } }
        public static string Description { get { return descriptions[mode]; } }
        /// <summary>Restores the outgoing mode before switching implementations.</summary>
        public static void Cycle() { Restore(); mode = (mode + 1) % names.Length; }
        /// <summary>Applies host-only behavior to the current body and schedules a single revive per death.</summary>
        public static void Update()
        {
            bool enabled = State.Player.GodToggle && ModernMenu.HasHost;
            var health = UmbraMenu.LocalHealth;
            if (!enabled || owner != health) ReleaseOwned();
            if (!enabled) { wasAlive = false; reviveAt = -1; return; }
            bool alive = health && health.alive;
            if (mode == 3 && wasAlive && !alive) reviveAt = Time.unscaledTime + 1f;
            wasAlive = alive;
            if (mode == 3 && reviveAt >= 0 && Time.unscaledTime >= reviveAt)
            {
                reviveAt = -1;
                if (!alive && Run.instance && UmbraMenu.LocalPlayer) UmbraMenu.LocalPlayer.RespawnExtraLife();
            }
            if (!alive) { ReleaseOwned(); return; }
            owner = health;
            if (mode == 0)
            {
                if (!applied) { originalGod = health.godMode; applied = true; }
                health.godMode = true;
            }
            else if (mode == 1 && !hurtboxes && UmbraMenu.LocalPlayerBody.mainHurtBox)
            {
                hurtboxes = UmbraMenu.LocalPlayerBody.mainHurtBox.hurtBoxGroup;
                if (hurtboxes) hurtboxes.hurtBoxesDeactivatorCounter++;
            }
            else if (mode == 2 && health.health < health.fullHealth) health.Heal(health.fullHealth - health.health, default(ProcChainMask), false);
        }
        /// <summary>Releases only state acquired by Umbra, preserving other mods' counters and original god flag.</summary>
        private static void ReleaseOwned()
        {
            if (applied && owner) owner.godMode = originalGod;
            if (hurtboxes) hurtboxes.hurtBoxesDeactivatorCounter--;
            owner = null; hurtboxes = null; applied = false;
        }
        /// <summary>Restores transient state on mode changes, scenes, disable-all, and unload.</summary>
        public static void Restore() { ReleaseOwned(); wasAlive = false; reviveAt = -1; }
    }
}
