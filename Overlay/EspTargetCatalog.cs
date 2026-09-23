using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Stable prefab identities shared by scene discovery, spawn assets and ESP overrides.</summary>
    internal static class EspTargetCatalog
    {
        internal sealed class Entry
        {
            public string Key, Name;
            public EspCategory Category;
        }
        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private static int scanned;
        internal static IEnumerable<Entry> Entries { get { return entries.Values; } }

        internal static string Key(GameObject prefab, bool body)
        {
            return (body ? "body:" : "object:") + prefab.name.Replace("(Clone)", "").Trim();
        }

        /// <summary>Stores names only, never retaining scene objects between runs.</summary>
        internal static void Register(GameObject prefab, string name, EspCategory category, bool body)
        {
            if (!prefab || prefab.GetComponent<GenericPickupController>()) return;
            string key = Key(prefab, body);
            entries[key] = new Entry { Key = key, Name = name, Category = category };
        }

        /// <summary>Adds installed spawn assets as they load, so overrides can be edited before spawning.</summary>
        internal static void Refresh()
        {
            SpawnCatalog.Request();
            if (scanned == SpawnCatalog.Entries.Count) return;
            scanned = SpawnCatalog.Entries.Count;
            foreach (var entry in SpawnCatalog.Entries)
            {
                if (!entry.Card || !entry.Card.prefab) continue;
                var prefab = entry.Card.prefab;
                var master = prefab.GetComponent<CharacterMaster>();
                if (master && master.bodyPrefab)
                {
                    var body = master.bodyPrefab.GetComponent<CharacterBody>();
                    Register(master.bodyPrefab, entry.Name, body && body.isChampion ? EspCategory.Boss : EspCategory.Enemy, true);
                }
                else Register(prefab, entry.Name, EspRenderer.Classify(prefab), false);
            }
        }
    }
}
