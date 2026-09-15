using RoR2;
using UnityEngine;
using EntityStates.Railgunner.Reload;

namespace UmbraMenu
{
    internal static class RailgunPerfectReload
    {
        /// <summary>Installs a hook to automatically boost the railgun reload when the player has enabled the feature.</summary>
        public static void Update()
        {
            if (!State.Player.RailgunPerfectReload)
                return;

            LocalUser localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser == null)
                return;

            CharacterBody body = localUser.cachedBody;
            if (body == null)
                return;

            EntityStateMachine[] stateMachines =
                body.GetComponents<EntityStateMachine>();

            foreach (EntityStateMachine machine in stateMachines)
            {
                Reloading reloadState = machine.state as Reloading;

                if (reloadState == null)
                    continue;
                    
                if (reloadState.IsInBoostWindow())
                {
                    reloadState.AttemptBoost();
                }

                return;
            }
        }
    }
}