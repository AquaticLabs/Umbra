using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UmbraMenu
{
    /// <summary>Asynchronously loads exact addressable spawn cards, including stage/DLC assets not already in memory.</summary>
    internal static class SpawnCatalog
    {
        internal sealed class Entry { public SpawnCard Card; public string Name, Category; public Texture Icon; public Sprite IconSprite; }
        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly Queue<string> pending = new Queue<string>();
        private static readonly HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<AsyncOperationHandle<SpawnCard>> handles = new List<AsyncOperationHandle<SpawnCard>>();
        private static bool requested, scanned;
        private static int inFlight, failures, generation;
        public static IList<Entry> Entries { get { return entries; } }
        public static string Status { get { return !requested ? "Not loaded" : entries.Count + " loaded / " + (pending.Count + inFlight) + " pending" + (failures > 0 ? " / " + failures + " unavailable" : ""); } }
        /// <summary>Schedules discovery; expensive loading is processed outside OnGUI.</summary>
        public static void Request() { requested = true; }
        /// <summary>Loads at most four concurrent operations, without blocking Unity on WaitForCompletion.</summary>
        public static void Update()
        {
            if (!requested || !RoR2Application.loadFinished) return;
            if (!scanned)
            {
                // Never freeze an empty early catalog as the completed result.
                if (!Addressables.ResourceLocators.Any()) return;
                scanned = true;
                foreach (var card in Resources.FindObjectsOfTypeAll<SpawnCard>()) Add(card);
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var locator in Addressables.ResourceLocators)
                    foreach (var key in locator.Keys)
                    {
                        var path = key as string;
                        if (path == null || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                        string file = System.IO.Path.GetFileName(path);
                        if (!file.StartsWith("csc", StringComparison.OrdinalIgnoreCase) && !file.StartsWith("isc", StringComparison.OrdinalIgnoreCase)) continue;
                        IList<IResourceLocation> locations;
                        if (locator.Locate(key, typeof(SpawnCard), out locations) && paths.Add(path)) pending.Enqueue(path);
                    }
            }
            while (pending.Count > 0 && inFlight < 4)
            {
                var handle = Addressables.LoadAssetAsync<SpawnCard>(pending.Dequeue());
                handles.Add(handle); inFlight++;
                int ticket = generation;
                handle.Completed += result =>
                {
                    if (ticket != generation) return;
                    inFlight--;
                    try { if (result.Status == AsyncOperationStatus.Succeeded) Add(result.Result); else failures++; }
                    catch (Exception error) { failures++; Debug.LogWarning("Umbra spawn entry: " + error.Message); }
                };
            }
        }
        /// <summary>Deduplicates exact card names and classifies monsters using the game's boss eligibility flag.</summary>
        private static void Add(SpawnCard card)
        {
            if (!card || !card.prefab || !names.Add(card.name)) return;
            string category, name = card.name;
            var character = card as CharacterSpawnCard;
            if (character) category = character.forbiddenAsBoss ? "Common" : "Boss";
            else if (name.IndexOf("portal", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("battery", StringComparison.OrdinalIgnoreCase) < 0) category = "Portal";
            else if (name.IndexOf("chest", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0) category = "Chest";
            else if (name.IndexOf("shrine", StringComparison.OrdinalIgnoreCase) >= 0) category = "Shrine";
            else if (name.IndexOf("drone", StringComparison.OrdinalIgnoreCase) >= 0) category = "Drone";
            else if (name.IndexOf("duplicator", StringComparison.OrdinalIgnoreCase) >= 0) category = "Printer";
            else category = "Other";
            Texture icon = null; Sprite sprite = null;
            var identity = card.prefab.GetComponent<CharacterMaster>();
            if (identity && identity.bodyPrefab)
            {
                var body = identity.bodyPrefab.GetComponent<CharacterBody>();
                if (body) { name = Language.GetString(body.baseNameToken); category = body.isChampion ? "Boss" : "Common"; icon = body.portraitIcon; }
            }
            else name = PortalName(card.name);
            if (!icon)
            {
                // Not every interactable has an inspect image. Prefab providers may be uninitialized: fail locally.
                foreach (var component in card.prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var provider = component as IInspectInfoProvider;
                    if (provider == null) continue;
                    try
                    {
                        var info = provider.GetInfo();
                        if (info != null && info.Visual) { sprite = info.Visual; icon = sprite.texture; break; }
                    }
                    catch (Exception) { /* Missing prefab-only state uses the category fallback in the browser. */ }
                }
            }
            entries.Add(new Entry { Card = card, Name = name, Category = category, Icon = icon, IconSprite = sprite });
            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>Names known portals while leaving every additional catalog variant independently selectable.</summary>
        private static string PortalName(string id)
        {
            switch (id)
            {
                case "iscShopPortal": return "Blue • Bazaar";
                case "iscGoldshoresPortal": return "Gold • Gilded Coast";
                case "iscMSPortal": return "Celestial • Obliterate";
                case "iscColossusPortal": return "Green • Colossus";
                case "iscVoidPortal": return "Void • Fields";
                case "iscDeepVoidPortal": return "Deep Void";
                case "iscVoidOutroPortal": return "Void exit";
                case "iscDestinationPortal": return "Destination portal";
                case "iscHardwareProgPortal": return "Tech • Hardware progression";
                case "iscHardwareProgPortal_Haunt": return "Tech • Haunt";
                case "iscEyePortal": return "Eye portal";
                case "iscSolusShopPortal": return "Solus • Exchange";
                case "iscSolusPortalBackout": return "Solus • Return";
                case "iscInfiniteTowerPortal": return "Infinite Tower";
                default: return System.Text.RegularExpressions.Regex.Replace(id.Length > 3 ? id.Substring(3) : id, "(?<=[a-z])(?=[A-Z0-9])", " ");
            }
        }
        /// <summary>Spawns the exact selected card with host/range/team validation and no shared-asset mutations.</summary>
        public static void Spawn(Entry entry)
        {
            if (!MenuController.HasHost || !UmbraRuntime.characterCollected || !Run.instance) throw new InvalidOperationException("A living host in a run is required.");
            if (entry == null || !entry.Card || !DirectorCore.instance) throw new InvalidOperationException("Choose a loaded spawn card.");
            foreach (var requirement in entry.Card.prefab.GetComponentsInChildren<RoR2.ExpansionManagement.ExpansionRequirementComponent>())
                if (requirement.requiredExpansion && !Run.instance.IsExpansionEnabled(requirement.requiredExpansion)) throw new InvalidOperationException("This spawn requires an expansion enabled in the run.");
            var copy = UnityEngine.Object.Instantiate(entry.Card);
            try
            {
                copy.sendOverNetwork = true;
                var request = new DirectorSpawnRequest(copy, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    minDistance = State.Spawn.minDistance,
                    maxDistance = Mathf.Max(State.Spawn.minDistance, State.Spawn.maxDistance),
                    position = UmbraRuntime.LocalPlayerBody.footPosition
                }, RoR2Application.rng) { ignoreTeamMemberLimit = true };
                if (copy is CharacterSpawnCard) request.teamIndexOverride = State.Spawn.team[State.Spawn.teamIndex];
                var spawned = DirectorCore.instance.TrySpawnObject(request);
                if (!spawned) throw new InvalidOperationException("No valid director placement found. Try open ground or increase range.");
                State.Spawn.spawnedObjects.Add(spawned);
            }
            finally { UnityEngine.Object.Destroy(copy); }
        }
        /// <summary>Releases only loaded addressable references; old callbacks cannot repopulate an unloaded catalog.</summary>
        public static void Dispose()
        {
            generation++;
            foreach (var handle in handles) if (handle.IsValid()) Addressables.Release(handle);
            handles.Clear(); entries.Clear(); pending.Clear(); names.Clear(); inFlight = failures = 0; requested = scanned = false;
        }
    }
}
