using RoR2;
using EntityStates.Railgunner.Reload;

namespace UmbraMenu
{
    internal static class RailgunPerfectReload
    {
        private static CharacterBody cachedBody;
        private static EntityStateMachine[] machines;

        /// <summary>Samples the local reload window; component discovery is cached once per body rather than every frame.</summary>
        public static void Update()
        {
            var body = UmbraRuntime.LocalPlayerBody;
            if (!State.Player.RailgunPerfectReload || !UmbraRuntime.characterCollected || !body || !body.hasEffectiveAuthority)
            { Clear(); return; }
            if (cachedBody != body)
            { cachedBody = body; machines = body.GetComponents<EntityStateMachine>(); }
            foreach (var machine in machines)
            {
                if (!machine || !(machine.state is Reloading reloadState)) continue;
                if (reloadState.IsInBoostWindow()) reloadState.AttemptBoost();
                return;
            }
        }

        /// <summary>Releases body references on disable, scene change and unload.</summary>
        public static void Clear() { cachedBody = null; machines = null; }
    }
}
