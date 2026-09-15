using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Small game integration helpers shared by input and spawn actions.</summary>
    internal static class Utility
    {
        /// <summary>Reports game-owned menus/chat that should suspend aim and movement input.</summary>
        public static bool CursorIsVisible()
        {
            foreach (var system in RoR2.UI.MPEventSystem.readOnlyInstancesList)
                if (system && system.isCursorVisible) return true;
            return false;
        }

        /// <summary>Discovers currently loaded spawn cards and sorts them for repeatable preset matching.</summary>
        public static List<SpawnCard> GetSpawnCards()
        {
            return Resources.FindObjectsOfTypeAll<SpawnCard>().Where(card => card).OrderBy(card => card.name).ToList();
        }
    }
}
