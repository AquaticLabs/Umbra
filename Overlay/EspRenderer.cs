using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>One cached entry per entity, with live world bounds and independent category/item styles.</summary>
    internal static class EspRenderer
    {
        private static readonly List<Entry> entries = new List<Entry>();
        private static readonly HashSet<int> seen = new HashSet<int>();
        private static float nextRefresh;
        private static GUIStyle label;
        private static VisualPreferences Prefs { get { return VisualSettings.Current; } }

        #region Discovery and ownership

        /// <summary>Refreshes object references periodically outside OnGUI; render bounds remain live each frame.</summary>
        public static void Update()
        {
            if (!UmbraRuntime.characterCollected) { if (entries.Count > 0) Clear(); return; }
            if (Time.unscaledTime < nextRefresh) return;
            Refresh();
        }

        /// <summary>Rebuilds the bounded cache, deduplicating components that belong to the same object.</summary>
        public static void Refresh()
        {
            nextRefresh = Time.unscaledTime + 1.5f;
            entries.Clear(); seen.Clear();
            foreach (var body in CharacterBody.readOnlyInstancesList)
                if (body && body != UmbraRuntime.LocalPlayerBody) Add(body, EspCategory.Enemy, body.GetDisplayName());
            foreach (var purchase in UnityEngine.Object.FindObjectsOfType<PurchaseInteraction>())
                Add(purchase, Classify(purchase.gameObject.name), purchase.GetDisplayName());
            foreach (var barrel in UnityEngine.Object.FindObjectsOfType<BarrelInteraction>()) Add(barrel, EspCategory.Barrel, "Barrel");
            foreach (var scrapper in UnityEngine.Object.FindObjectsOfType<ScrapperController>()) Add(scrapper, EspCategory.Scrapper, "Scrapper");
            foreach (var secret in UnityEngine.Object.FindObjectsOfType<PressurePlateController>()) Add(secret, EspCategory.Secret, "Secret switch");
            foreach (var pickup in UnityEngine.Object.FindObjectsOfType<GenericPickupController>()) Add(pickup, EspCategory.Other, "Pickup");
            if (TeleporterInteraction.instance) Add(TeleporterInteraction.instance, EspCategory.Teleporter, "Teleporter");
        }

        /// <summary>Captures renderer/component references once, excluding particle and trail renderers from bounds.</summary>
        private static void Add(Component source, EspCategory category, string name)
        {
            if (!source || !seen.Add(source.gameObject.GetInstanceID())) return;
            var body = source as CharacterBody;
            Transform model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            GameObject root = model ? model.gameObject : source.gameObject;
            var renderers = new List<UnityEngine.Renderer>();
            foreach (var renderer in root.GetComponentsInChildren<UnityEngine.Renderer>())
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) renderers.Add(renderer);
            entries.Add(new Entry
            {
                Source = source, Body = body, Category = category, Name = name,
                Renderers = renderers.ToArray(), Colliders = source.GetComponentsInChildren<Collider>(),
                Purchase = source.GetComponent<PurchaseInteraction>(), Barrel = source.GetComponent<BarrelInteraction>(),
                Pickup = source.GetComponent<GenericPickupController>(), Chest = source.GetComponent<ChestBehavior>()
            });
        }

        /// <summary>Clears object references on scene transitions or unload.</summary>
        public static void Clear() { entries.Clear(); seen.Clear(); nextRefresh = 0; }

        #endregion

        #region Overlay rendering

        /// <summary>Paints the full overlay once per repaint using the same local camera as aim selection.</summary>
        public static void Draw()
        {
            if (Event.current.type != EventType.Repaint || !UmbraRuntime.characterCollected) return;
            Camera camera = OverlayGeometry.GetCamera();
            if (!camera) return;
            if (label == null) label = new GUIStyle { alignment = TextAnchor.UpperCenter, richText = false, wordWrap = false, clipping = TextClipping.Clip };
            label.fontSize = Prefs.FontSize;
            foreach (Entry entry in entries)
            {
                if (!entry.Source || !entry.Source.gameObject.activeInHierarchy) continue;
                EspCategory category;
                if (!Visible(entry, out category)) continue;
                float distance = Vector3.Distance(camera.transform.position, entry.Source.transform.position);
                if (distance > Prefs.MaxDistance) continue;
                EspStyle style = VisualSettings.For(category);
                string name = entry.Name;
                if (entry.Pickup)
                {
                    ResolvePickup(entry.Pickup.pickup.pickupIndex, out name, out style);
                }
                if (!style.Enabled) continue;
                Bounds bounds;
                Rect rectangle;
                if (!GetBounds(entry, out bounds) || !OverlayGeometry.ProjectBounds(camera, bounds, out rectangle)) continue;
                if (style.Boxes) ESPHelper.Box(rectangle, style.Color, style.Thickness, Prefs.CornerBoxes);
                float textBottom = rectangle.center.y;
                if (style.Labels)
                {
                    string detail = Prefs.Distances ? Mathf.RoundToInt(distance) + " m" : "";
                    if (entry.Category == EspCategory.Teleporter && TeleporterInteraction.instance)
                        detail = (TeleporterInteraction.instance.isCharged ? "Charged" : TeleporterInteraction.instance.isCharging ? "Charging" : "Idle") + (detail.Length > 0 ? " / " + detail : "");
                    if (entry.Purchase) detail += (detail.Length > 0 ? " / " : "") + entry.Purchase.cost + " " + entry.Purchase.costType;
                    string itemName = "";
                    EspStyle itemStyle = null;
                    if (entry.Chest)
                    {
                        ResolvePickup(entry.Chest.currentPickup.pickupIndex, out itemName, out itemStyle);
                        if (itemName == "Pickup" || !itemStyle.Enabled || !itemStyle.Labels) itemName = "";
                    }
                    if (!style.Boxes)
                    {
                        // Without a box, center one compact text block on the object's projected center.
                        int lines = 1 + (detail.Length > 0 ? 1 : 0) + (itemName.Length > 0 ? 1 : 0);
                        float step = Prefs.FontSize + 3;
                        float y = rectangle.center.y - lines * step * 0.5f;
                        DrawLabel(rectangle.center.x, y, name, style.Color); y += step;
                        if (detail.Length > 0) { DrawLabel(rectangle.center.x, y, detail, style.Color); y += step; }
                        if (itemName.Length > 0) { DrawLabel(rectangle.center.x, y, itemName, itemStyle.Color); y += step; }
                        textBottom = y;
                    }
                    else
                    {
                        DrawLabel(rectangle.center.x, Mathf.Max(0, rectangle.yMin - Prefs.FontSize - 5), name, style.Color);
                        if (detail.Length > 0) DrawLabel(rectangle.center.x, rectangle.yMax + 3, detail, style.Color);
                        if (itemName.Length > 0) DrawLabel(rectangle.center.x, rectangle.yMax + (detail.Length > 0 ? Prefs.FontSize + 6 : 3), itemName, itemStyle.Color);
                    }
                }
                if (entry.Body && Prefs.HealthBars && entry.Body.healthComponent)
                {
                    var health = entry.Body.healthComponent;
                    float fraction = Mathf.Clamp01(health.health / Mathf.Max(1, health.fullHealth));
                    if (style.Boxes)
                    {
                        ESPHelper.Fill(new Rect(rectangle.xMin - 6, rectangle.yMin, 3, rectangle.height), new Color(0, 0, 0, 0.7f));
                        ESPHelper.Fill(new Rect(rectangle.xMin - 6, rectangle.yMax - rectangle.height * fraction, 3, rectangle.height * fraction), style.Color);
                    }
                    else
                    {
                        ESPHelper.Fill(new Rect(rectangle.center.x - 32, textBottom + 2, 64, 3), new Color(0, 0, 0, 0.7f));
                        ESPHelper.Fill(new Rect(rectangle.center.x - 32, textBottom + 2, 64 * fraction, 3), style.Color);
                    }
                }
            }
            DrawReticle(camera);
            if (State.Render.renderMods) DrawActiveMods();
        }

        /// <summary>Checks master switches, category switches, team, life state, and consumed objects independently.</summary>
        private static bool Visible(Entry entry, out EspCategory category)
        {
            category = entry.Category;
            if (entry.Body)
            {
                if (!entry.Body.healthComponent || !entry.Body.healthComponent.alive || !entry.Body.teamComponent || !UmbraRuntime.LocalPlayerBody.teamComponent) return false;
                TeamIndex team = entry.Body.teamComponent.teamIndex;
                bool ally = team == UmbraRuntime.LocalPlayerBody.teamComponent.teamIndex;
                if (team == TeamIndex.Neutral || team == TeamIndex.None) return false;
                category = ally ? EspCategory.Ally : entry.Body.isBoss ? EspCategory.Boss : EspCategory.Enemy;
                return ally ? Prefs.Allies : State.Render.renderMobs;
            }
            if (entry.Category == EspCategory.Teleporter) return Prefs.Teleporter;
            if (entry.Pickup) return Prefs.Pickups;
            if (!State.Render.renderInteractables) return false;
            if (entry.Purchase && !entry.Purchase.available) return false;
            if (entry.Barrel && entry.Barrel.Networkopened) return false;
            return true;
        }

        /// <summary>Unions current mesh bounds; collider/body dimensions provide a world-space fallback.</summary>
        private static bool GetBounds(Entry entry, out Bounds bounds)
        {
            bounds = default(Bounds);
            bool initialized = false;
            foreach (var renderer in entry.Renderers)
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!initialized) { bounds = renderer.bounds; initialized = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (initialized && bounds.size.sqrMagnitude > 0.001f) return true;
            foreach (var collider in entry.Colliders)
            {
                if (!collider || !collider.enabled || collider.isTrigger) continue;
                if (!initialized) { bounds = collider.bounds; initialized = true; }
                else bounds.Encapsulate(collider.bounds);
            }
            if (initialized) return true;
            if (entry.Body)
            {
                float radius = Mathf.Max(0.25f, entry.Body.radius);
                bounds = new Bounds(entry.Body.corePosition, Vector3.one * radius * 2f);
                return true;
            }
            bounds = new Bounds(entry.Source.transform.position, Vector3.one * 0.7f);
            return true;
        }

        /// <summary>Draws text with a subtle shadow while retaining the user's exact color.</summary>
        private static void DrawLabel(float x, float y, string text, Color color)
        {
            float width = Mathf.Min(Screen.width, Mathf.Max(70, label.CalcSize(new GUIContent(text)).x + 8));
            Rect rect = new Rect(Mathf.Clamp(x - width * 0.5f, 0, Mathf.Max(0, Screen.width - width)), Mathf.Clamp(y, 0, Screen.height - Prefs.FontSize - 5), width, Prefs.FontSize + 5);
            label.normal.textColor = new Color(0, 0, 0, color.a);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, label);
            label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }

        /// <summary>Draws the user-styled crosshair and mathematically shared FOV boundary.</summary>
        private static void DrawReticle(Camera camera)
        {
            Vector2 center = OverlayGeometry.Center(camera);
            if (Prefs.Crosshair)
            {
                float thickness = Prefs.CrosshairThickness;
                ESPHelper.DrawLine(center + Vector2.left * 12, center + Vector2.left * 5, Prefs.CrosshairColor, thickness);
                ESPHelper.DrawLine(center + Vector2.right * 5, center + Vector2.right * 12, Prefs.CrosshairColor, thickness);
                ESPHelper.DrawLine(center + Vector2.up * 5, center + Vector2.up * 12, Prefs.CrosshairColor, thickness);
                ESPHelper.DrawLine(center + Vector2.down * 5, center + Vector2.down * 12, Prefs.CrosshairColor, thickness);
            }
            if (Prefs.ShowFov && ModernAimbot.Enabled)
                ESPHelper.Circle(center, OverlayGeometry.FovRadius(camera), Prefs.FovColor, Prefs.FovThickness);
            Vector2 endpoint;
            if (Prefs.TargetLine && ModernAimbot.HasTarget && OverlayGeometry.ProjectPoint(camera, ModernAimbot.TargetPoint, out endpoint))
            {
                Vector3 mouse = Input.mousePosition;
                Vector2 start = Cursor.lockState == CursorLockMode.Locked ? center : new Vector2(mouse.x, Screen.height - mouse.y);
                ESPHelper.DrawLine(start, endpoint, Prefs.TargetLineColor, Prefs.TargetLineThickness);
            }
        }

        /// <summary>Shows only the active gameplay modules in a compact status line.</summary>
        private static void DrawActiveMods()
        {
            var active = new List<string>();
            if (ModernAimbot.Enabled) active.Add("Aim: " + ModernAimbot.ModeName);
            if (State.Player.GodToggle) active.Add("God mode");
            if (State.Player.SkillToggle) active.Add("Skills");
            if (State.Items.noEquipmentCD) active.Add("Equipment");
            if (State.Movement.flightToggle) active.Add("Flight");
            if (State.Movement.alwaysSprintToggle) active.Add("Sprint");
            if (State.Movement.jumpPackToggle) active.Add("Jump pack");
            if (State.StatsMod.damageToggle || State.StatsMod.critToggle || State.StatsMod.armorToggle || State.StatsMod.moveSpeedToggle || State.StatsMod.attackSpeedToggle) active.Add("Stats");
            if (active.Count > 0) DrawLabel(Screen.width * 0.5f, Screen.height - Prefs.FontSize - 12, string.Join("  /  ", active.ToArray()), new Color(0.6f, 0.8f, 1f));
        }

        #endregion

        #region Classification and palette resolution

        /// <summary>Maps stable prefab names to categories with specific matches before general ones.</summary>
        private static EspCategory Classify(string prefab)
        {
            string name = prefab.ToLowerInvariant();
            if (name.Contains("newt")) return EspCategory.Newt;
            if (name.Contains("teleporter")) return EspCategory.Teleporter;
            if (name.Contains("scrapper")) return EspCategory.Scrapper;
            if (name.Contains("barrel")) return EspCategory.Barrel;
            if (name.Contains("duplicator") || name.Contains("printer")) return EspCategory.Printer;
            if (name.Contains("void")) return EspCategory.Void;
            if (name.Contains("lunar") || name.Contains("order")) return EspCategory.Lunar;
            if (name.Contains("equipment")) return EspCategory.Equipment;
            if (name.Contains("chance") || name.Contains("goldshores")) return EspCategory.Chance;
            if (name.Contains("blood")) return EspCategory.Blood;
            if (name.Contains("combat")) return EspCategory.Combat;
            if (name.Contains("boss") || name.Contains("mountain")) return EspCategory.Mountain;
            if (name.Contains("healing") || name.Contains("woods")) return EspCategory.Woods;
            if (name.Contains("chest") || name.Contains("multishop")) return EspCategory.Chest;
            return EspCategory.Other;
        }

        /// <summary>Maps every item tier, including void tiers, to a customizable palette entry.</summary>
        public static EspCategory ItemCategory(ItemDef item)
        {
            if (!item) return EspCategory.Other;
            switch (item.tier)
            {
                case ItemTier.Tier1: return EspCategory.CommonItem;
                case ItemTier.Tier2: return EspCategory.UncommonItem;
                case ItemTier.Tier3: return EspCategory.LegendaryItem;
                case ItemTier.Boss: return EspCategory.BossItem;
                case ItemTier.Lunar: return EspCategory.LunarItem;
                case ItemTier.VoidTier1: case ItemTier.VoidTier2: case ItemTier.VoidTier3: case ItemTier.VoidBoss: return EspCategory.VoidItem;
                default: return EspCategory.Other;
            }
        }

        /// <summary>Resolves dropped items and revealed chest contents through the same override hierarchy.</summary>
        private static void ResolvePickup(PickupIndex index, out string name, out EspStyle style)
        {
            name = "Pickup"; style = VisualSettings.For(EspCategory.Other);
            var definition = PickupCatalog.GetPickupDef(index);
            if (definition == null) return;
            name = Language.GetString(definition.nameToken);
            var item = ItemCatalog.GetItemDef(definition.itemIndex);
            if (item) style = VisualSettings.ForItem(item.name, ItemCategory(item));
            else if (definition.equipmentIndex != EquipmentIndex.None) style = VisualSettings.For(EspCategory.Equipment);
        }

        /// <summary>Cached scene entry; component references are always checked before use.</summary>
        private sealed class Entry
        {
            public Component Source;
            public CharacterBody Body;
            public PurchaseInteraction Purchase;
            public BarrelInteraction Barrel;
            public GenericPickupController Pickup;
            public ChestBehavior Chest;
            public UnityEngine.Renderer[] Renderers;
            public Collider[] Colliders;
            public EspCategory Category;
            public string Name;
        }

        #endregion
    }
}
