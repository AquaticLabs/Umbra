using RoR2;

namespace UmbraMenu
{
    /// <summary>Nullable local-player snapshot, independent of menu state and safe between bodies and scenes.</summary>
    internal static class RuntimeContext
    {
        public static NetworkUser User;
        public static CharacterMaster Master;
        public static CharacterBody Body;
        public static Inventory Inventory;
        public static HealthComponent Health;
        public static SkillLocator Skills;
        public static CharacterMotor Motor;
        public static bool Alive { get { return Body && Health && Health.alive; } }

        /// <summary>Refreshes only after game content is ready; no cached objects survive a disconnect.</summary>
        public static void Refresh()
        {
            User = null;
            foreach (var user in NetworkUser.readOnlyLocalPlayersList)
                if (user && user.isLocalPlayer) { User = user; break; }
            Master = User ? User.master : null;
            Body = Master ? Master.GetBody() : null;
            Inventory = Master ? Master.inventory : null;
            Health = Body ? Body.healthComponent : null;
            Skills = Body ? Body.skillLocator : null;
            Motor = Body ? Body.characterMotor : null;
        }

        /// <summary>Releases all scene-owned references during shutdown.</summary>
        public static void Clear() { User = null; Master = null; Body = null; Inventory = null; Health = null; Skills = null; Motor = null; }
    }
}
