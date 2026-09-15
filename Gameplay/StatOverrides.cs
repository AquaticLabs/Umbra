using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Reversible base-stat overrides; snapshots are captured on enable for each body.</summary>
    internal static class StatOverrides
    {
        private static CharacterBody owner;
        private static readonly ValueOverride Damage = new ValueOverride(), Crit = new ValueOverride(),
            Attack = new ValueOverride(), Armor = new ValueOverride(), Speed = new ValueOverride();

        /// <summary>Applies only changed values and restores a field immediately when its toggle is disabled.</summary>
        public static void Update(CharacterBody body)
        {
            if (owner != body) { Restore(); owner = body; }
            if (!body) return;
            bool host = UnityEngine.Networking.NetworkServer.active;
            bool changed = Damage.Apply(ref body.levelDamage, host && State.StatsMod.damageToggle, State.StatsMod.damagePerLvl);
            changed |= Crit.Apply(ref body.levelCrit, host && State.StatsMod.critToggle, State.StatsMod.critPerLvl);
            changed |= Attack.Apply(ref body.baseAttackSpeed, host && State.StatsMod.attackSpeedToggle, State.StatsMod.attackSpeed);
            changed |= Armor.Apply(ref body.baseArmor, host && State.StatsMod.armorToggle, State.StatsMod.armor);
            changed |= Speed.Apply(ref body.baseMoveSpeed, host && State.StatsMod.moveSpeedToggle, State.StatsMod.moveSpeed);
            if (changed) body.RecalculateStats();
        }

        /// <summary>Returns all owned fields to their pre-override values during disable, body change, or unload.</summary>
        public static void Restore()
        {
            if (owner)
            {
                bool changed = Damage.Apply(ref owner.levelDamage, false, 0);
                changed |= Crit.Apply(ref owner.levelCrit, false, 0);
                changed |= Attack.Apply(ref owner.baseAttackSpeed, false, 0);
                changed |= Armor.Apply(ref owner.baseArmor, false, 0);
                changed |= Speed.Apply(ref owner.baseMoveSpeed, false, 0);
                if (changed) owner.RecalculateStats();
            }
            Damage.Active = Crit.Active = Attack.Active = Armor.Active = Speed.Active = false;
            owner = null;
        }

        /// <summary>Tracks ownership of one modified field independently of other toggles.</summary>
        private sealed class ValueOverride
        {
            public bool Active;
            private float original;

            /// <summary>Captures once, changes only when needed, and restores once on release.</summary>
            public bool Apply(ref float field, bool enabled, float value)
            {
                if (!enabled && !Active) return false;
                if (enabled && !Active) original = field;
                float next = enabled ? value : original;
                Active = enabled;
                if (Mathf.Approximately(field, next)) return false;
                field = next;
                return true;
            }
        }
    }
}
