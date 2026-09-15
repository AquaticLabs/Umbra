using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Catalog-backed item/equipment actions, with authoritative validation and permanent stack semantics.</summary>
    internal static class ItemService
    {
        internal sealed class Entry
        {
            public ItemDef Item;
            public EquipmentDef Equipment;
            public string Name, Id, Group;
            public Texture Icon;
            public PickupIndex Pickup;
        }
        private static readonly List<Entry> catalog = new List<Entry>();
        // Current builds expose a public getter but private setter; keep this version-specific adapter isolated.
        private static readonly System.Reflection.MethodInfo setChestPickup = typeof(ChestBehavior).GetProperty("currentPickup")?.GetSetMethod(true);
        public static IList<Entry> Catalog { get { if (catalog.Count == 0) Refresh(); return catalog; } }
        /// <summary>Releases catalog references so a later injection starts with the current loaded content.</summary>
        public static void Clear() { catalog.Clear(); }
        /// <summary>Builds a sorted browseable catalog, excluding hidden/system-only definitions.</summary>
        public static void Refresh()
        {
            catalog.Clear();
            if (!RoR2Application.loadFinished) return;
            foreach (var index in ItemCatalog.allItems)
            {
                var item = ItemCatalog.GetItemDef(index);
                if (!item || item.hidden) continue;
                var tier = ItemTierCatalog.GetItemTierDef(item.tier);
                if (!tier || !tier.isDroppable) continue;
                catalog.Add(new Entry { Item = item, Id = item.name, Name = Language.GetString(item.nameToken), Group = TierName(item.tier), Icon = item.pickupIconTexture, Pickup = PickupCatalog.FindPickupIndex(index) });
            }
            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                var equipment = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
                if (!equipment || !equipment.canDrop) continue;
                catalog.Add(new Entry { Equipment = equipment, Id = equipment.name, Name = Language.GetString(equipment.nameToken), Group = "Equipment", Icon = equipment.pickupIconTexture, Pickup = PickupCatalog.FindPickupIndex((EquipmentIndex)i) });
            }
            catalog.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>Maps exact tiers into concise browser categories.</summary>
        private static string TierName(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return "Common";
                case ItemTier.Tier2: return "Uncommon";
                case ItemTier.Tier3: return "Legendary";
                case ItemTier.Lunar: return "Lunar";
                case ItemTier.Boss: return "Boss";
                case ItemTier.VoidTier1: case ItemTier.VoidTier2: case ItemTier.VoidTier3: case ItemTier.VoidBoss: return "Void";
                default: return "Other";
            }
        }
        /// <summary>Returns permanent owned stacks, or possession of the selected equipment.</summary>
        public static int Owned(Entry entry, Inventory inventory)
        { return entry == null || !inventory ? 0 : entry.Item ? inventory.GetItemCountPermanent(entry.Item.itemIndex) : inventory.HasEquipment(entry.Equipment.equipmentIndex) ? 1 : 0; }
        /// <summary>Rejects unavailable DLC items and invalid counts before touching an inventory.</summary>
        private static void Validate(Entry entry, int count)
        {
            if (!MenuController.HasHost) throw new InvalidOperationException("This action requires the host.");
            if (entry == null) throw new InvalidOperationException("Choose an item or equipment first.");
            if (count < 1 || count > 100) throw new InvalidOperationException("Choose a quantity from 1 to 100.");
            if (!Run.instance) throw new InvalidOperationException("Start a run first.");
            if ((entry.Item && !Run.instance.IsItemAvailable(entry.Item.itemIndex)) || (entry.Equipment && !Run.instance.IsEquipmentAvailable(entry.Equipment.equipmentIndex)))
                throw new InvalidOperationException("This pickup is not available in the current run/DLC selection.");
        }
        /// <summary>Gives permanent stacks; equipment deliberately replaces the active slot and accepts quantity one only.</summary>
        public static void Give(Entry entry, CharacterMaster master, int count)
        {
            Validate(entry, count);
            if (!master || !master.inventory) throw new InvalidOperationException("The recipient does not have an inventory yet.");
            if (entry.Item)
            {
                if (master.inventory.GetItemCountPermanent(entry.Item.itemIndex) > int.MaxValue - count) throw new InvalidOperationException("That stack would overflow.");
                master.inventory.GiveItemPermanent(entry.Item.itemIndex, count);
            }
            else
            {
                if (count != 1) throw new InvalidOperationException("Giving equipment requires quantity 1; it replaces the active equipment.");
                master.inventory.SetEquipmentIndex(entry.Equipment.equipmentIndex, false);
            }
        }
        /// <summary>Creates networked ground pickups; inventory transfer removes each stack only after its pickup exists.</summary>
        public static void Drop(Entry entry, int count, bool fromInventory)
        {
            Validate(entry, count);
            var body = UmbraRuntime.LocalPlayerBody;
            var inventory = UmbraRuntime.LocalPlayerInv;
            if (!UmbraRuntime.characterCollected || !body) throw new InvalidOperationException("A living character is required.");
            if (fromInventory && (!inventory || Owned(entry, inventory) < count)) throw new InvalidOperationException("Not enough permanent copies in your inventory.");
            if (fromInventory && entry.Item && !entry.Item.canRemove) throw new InvalidOperationException("This item cannot be removed safely.");
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.399963f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle)) * (2f + i * 0.03f);
                var info = new GenericPickupController.CreatePickupInfo { pickup = new UniquePickup(entry.Pickup), position = body.footPosition + offset, rotation = Quaternion.identity };
                var pickup = GenericPickupController.CreatePickup(in info);
                if (!pickup) throw new InvalidOperationException("Pickup creation failed; remaining inventory was left untouched.");
                State.Spawn.spawnedObjects.Add(pickup.gameObject);
                if (fromInventory)
                {
                    if (entry.Item) inventory.RemoveItemPermanent(entry.Item.itemIndex, 1);
                    else inventory.RemoveEquipment(entry.Equipment.equipmentIndex);
                }
            }
        }
        /// <summary>Rolls uniformly from eligible items in the chosen category, never equipment or internal items.</summary>
        public static void Roll(int count, string group)
        {
            if (!MenuController.HasHost || !Run.instance || !UmbraRuntime.LocalPlayerInv) throw new InvalidOperationException("A host run and inventory are required.");
            if (count < 1 || count > 100) throw new InvalidOperationException("Roll between 1 and 100 items.");
            var pool = Catalog.Where(e => e.Item && (group == "All" || e.Group == group) && Run.instance.IsItemAvailable(e.Item.itemIndex)).ToArray();
            if (pool.Length == 0) throw new InvalidOperationException("No eligible items in that category. Select All or an item tier.");
            for (int i = 0; i < count; i++) Give(pool[UnityEngine.Random.Range(0, pool.Length)], UmbraRuntime.LocalPlayer, 1);
        }
        /// <summary>Gives one eligible permanent copy of every item; equipment is excluded.</summary>
        public static void GiveAll()
        {
            if (!MenuController.HasHost || !Run.instance || !UmbraRuntime.LocalPlayerInv) throw new InvalidOperationException("A host run and inventory are required.");
            foreach (var entry in Catalog) if (entry.Item && Run.instance.IsItemAvailable(entry.Item.itemIndex)) Give(entry, UmbraRuntime.LocalPlayer, 1);
        }
        /// <summary>Clears removable permanent items only, preserving hidden/internal and temporary state.</summary>
        public static void ClearInventory()
        {
            if (!MenuController.HasHost || !UmbraRuntime.LocalPlayerInv) throw new InvalidOperationException("A host inventory is required.");
            foreach (var entry in Catalog) if (entry.Item && entry.Item.canRemove) UmbraRuntime.LocalPlayerInv.ResetItemPermanent(entry.Item.itemIndex);
        }
        /// <summary>Applies the game's Shrine of Order inventory restacking behavior.</summary>
        public static void Restack()
        {
            if (!MenuController.HasHost || !UmbraRuntime.LocalPlayerInv || !Run.instance) throw new InvalidOperationException("A host run and inventory are required.");
            UmbraRuntime.LocalPlayerInv.ShrineRestackInventory(RoR2Application.rng);
        }
        /// <summary>Replaces only the nearest available chest's selected pickup within interaction-scale range.</summary>
        public static void ReplaceNearestChest(Entry entry)
        {
            Validate(entry, 1);
            if (!UmbraRuntime.characterCollected) throw new InvalidOperationException("A living character is required.");
            var chest = UnityEngine.Object.FindObjectsOfType<ChestBehavior>().Where(c => c && c.GetComponent<PurchaseInteraction>() && c.GetComponent<PurchaseInteraction>().available)
                .OrderBy(c => Vector3.SqrMagnitude(c.transform.position - UmbraRuntime.LocalPlayerBody.corePosition)).FirstOrDefault();
            if (!chest || Vector3.Distance(chest.transform.position, UmbraRuntime.LocalPlayerBody.corePosition) > 25) throw new InvalidOperationException("No unopened chest within 25 m.");
            if (setChestPickup == null) throw new InvalidOperationException("Chest replacement is unavailable on this game build.");
            setChestPickup.Invoke(chest, new object[] { new UniquePickup(entry.Pickup) });
        }
    }
}
