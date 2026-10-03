using RoR2;
using UnityEngine.Networking;

namespace UmbraMenu
{
    /// <summary>Skips lunar affordability/payment only for the local host's lunar purchases; never writes currency.</summary>
    internal static class PurchaseModifiers
    {
        private static bool Applies(CostTypeDef cost, CharacterBody body)
        {
            return State.Player.NoLunarCost && NetworkServer.active && body &&
                body == UmbraRuntime.LocalPlayerBody && cost == CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarCoin);
        }

        internal static bool AffordablePrefix(CostTypeDef __instance, Interactor __1, ref bool __result)
        {
            if (!Applies(__instance, __1 ? __1.GetComponent<CharacterBody>() : null)) return true;
            __result = true;
            return false;
        }

        internal static bool PayPrefix(CostTypeDef __instance, CostTypeDef.PayCostContext __0)
        {
            return !Applies(__instance, __0.activatorBody);
        }

        internal static void TurretLimitPostfix(CharacterMaster __instance, DeployableSlot __0, ref int __result)
        {
            if (NetworkServer.active && State.Player.UnlimitedTurrets && __instance == UmbraRuntime.LocalPlayer &&
                __0 == DeployableSlot.EngiTurret) __result = int.MaxValue;
        }
    }
}
