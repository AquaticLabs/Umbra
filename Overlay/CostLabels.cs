using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Readable costs without exposing internal enum names to the overlay.</summary>
    internal static class CostLabels
    {
        internal static string Format(CostTypeIndex type, int cost)
        {
            switch (type)
            {
                case CostTypeIndex.None: return "";
                case CostTypeIndex.Money: return "($" + cost + ")";
                case CostTypeIndex.PercentHealth: return "-%" + cost + " Health";
                case CostTypeIndex.SoulCost: return "-%" + cost + " Soul";
                case CostTypeIndex.LunarCoin: return cost + " Lunar coin" + (cost == 1 ? "" : "s");
                case CostTypeIndex.VoidCoin: return cost + " Void coin" + (cost == 1 ? "" : "s");
                case CostTypeIndex.WhiteItem: return cost + " Common item(s)";
                case CostTypeIndex.GreenItem: return cost + " Uncommon item(s)";
                case CostTypeIndex.RedItem: return cost + " Legendary item(s)";
                case CostTypeIndex.BossItem: return cost + " Boss item(s)";
                case CostTypeIndex.Equipment: return cost + " Equipment";
                case CostTypeIndex.LunarItemOrEquipment: return cost + " Lunar item / equipment";
                case CostTypeIndex.VolatileBattery: return "Fuel array";
                case CostTypeIndex.TreasureCacheItem: return cost + " Rusted key(s)";
                case CostTypeIndex.TreasureCacheVoidItem: return cost + " Encrusted key(s)";
                case CostTypeIndex.ArtifactShellKillerItem: return cost + " Artifact key(s)";
                default: return cost + " Required resource(s)";
            }
        }

        internal static Color ColorFor(CostTypeIndex type, Color fallback)
        {
            if (type == CostTypeIndex.PercentHealth) return VisualSettings.Current.HealthCostColor;
            if (type == CostTypeIndex.SoulCost) return VisualSettings.Current.SoulCostColor;
            return fallback;
        }
    }
}
