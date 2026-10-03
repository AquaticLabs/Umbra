using System;
using System.Collections.Generic;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>One eligibility/targeting policy for replacement and its preview tracer.</summary>
    internal static class ChestReplacement
    {
        private static readonly Dictionary<ChestBehavior, PickupIndex> replacements = new Dictionary<ChestBehavior, PickupIndex>();
        private static readonly System.Reflection.MethodInfo setter = AccessTools.PropertySetter(typeof(ChestBehavior), "currentPickup");
        private static ChestBehavior[] candidates = Array.Empty<ChestBehavior>();
        private static float refreshAt;
        internal const float ReplacementRange = 25f;

        internal static bool Eligible(ChestBehavior chest)
        {
            if (!chest || !chest.gameObject.activeInHierarchy || chest.NetworkisChestOpened || chest.isCommandChest ||
                chest.GetComponent<PickupPickerController>() || DelusionChestController.isDelusionEnable) return false;
            var purchase = chest.GetComponent<PurchaseInteraction>();
            return purchase && purchase.available && chest.dropTransform;
        }

        internal static ChestBehavior Nearest()
        {
            var body = UmbraRuntime.LocalPlayerBody;
            if (!body) return null;
            if (Time.unscaledTime >= refreshAt)
            {
                candidates = UnityEngine.Object.FindObjectsOfType<ChestBehavior>();
                refreshAt = Time.unscaledTime + 1f;
            }
            ChestBehavior nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (var chest in candidates)
            {
                if (!Eligible(chest)) continue;
                float distance = (chest.transform.position - body.corePosition).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = chest; nearestDistance = distance;
            }
            return nearest;
        }

        internal static void Set(ChestBehavior chest, PickupIndex pickup)
        {
            MenuActions.RequireHost(); MenuActions.RequirePlayer();
            if (!Eligible(chest)) throw new InvalidOperationException("This chest is no longer eligible. Choice, Command and Delusion chests are not supported.");
            if (Vector3.Distance(chest.transform.position, UmbraRuntime.LocalPlayerBody.corePosition) > ReplacementRange)
                throw new InvalidOperationException("Move within 25 m of the highlighted chest.");
            if (setter == null) throw new InvalidOperationException("Chest replacement is unavailable on this game build.");
            replacements[chest] = pickup;
            Apply(chest);
        }

        /// <summary>Reasserts a replacement after rerolls and immediately before native drop generation.</summary>
        internal static void Apply(ChestBehavior __instance)
        {
            if (!UnityEngine.Networking.NetworkServer.active || !__instance) return;
            if (replacements.TryGetValue(__instance, out var pickup)) setter.Invoke(__instance, new object[] { new UniquePickup(pickup) });
        }

        internal static void AfterDrop(ChestBehavior __instance) { replacements.Remove(__instance); }
        internal static void Clear() { replacements.Clear(); candidates = Array.Empty<ChestBehavior>(); refreshAt = 0; }
    }
}
