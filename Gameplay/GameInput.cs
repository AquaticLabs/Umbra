namespace UmbraMenu
{
    /// <summary>Game-owned UI focus, distinct from Umbra's pointer capture.</summary>
    internal static class GameInput
    {
        /// <summary>Reports menus/chat that should suspend aim and movement input.</summary>
        public static bool CursorIsVisible()
        {
            foreach (var system in RoR2.UI.MPEventSystem.readOnlyInstancesList)
                if (system && system.isCursorVisible) return true;
            return false;
        }
    }
}
