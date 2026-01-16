using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.Data
{
    /// <summary>
    /// Item data structure - loaded from JSON
    /// Consumables, equipment, key items, materials
    /// </summary>
    [Serializable]
    public class ItemData
    {
        // =============================================================================
        // IDENTITY
        // =============================================================================

        public string item_id;
        public string display_name;
        public string description;
        public string icon_path;
        public string item_category;

        // =============================================================================
        // ITEM TYPE
        // =============================================================================

        public int item_type;
        public int equipment_slot;
        public int rarity;

        // =============================================================================
        // VALUE
        // =============================================================================

        public int buy_price;
        public int sell_price;
        public int max_stack;

        // =============================================================================
        // CONSUMABLE EFFECTS
        // =============================================================================

        public int heal_amount;
        public int mp_restore;
        public string[] remove_status;
        public string[] apply_status;
        public int status_duration;

        // =============================================================================
        // EQUIPMENT STATS
        // =============================================================================

        public int attack_bonus;
        public int defense_bonus;
        public int magic_bonus;
        public int resistance_bonus;
        public int speed_bonus;
        public int hp_bonus;
        public int mp_bonus;

        // =============================================================================
        // SPECIAL PROPERTIES
        // =============================================================================

        public int brand_affinity;
        public string[] special_effects;
        public float capture_rate_bonus;

        // =============================================================================
        // HELPER METHODS
        // =============================================================================

        public ItemCategory GetCategory()
        {
            return item_category?.ToLower() switch
            {
                "consumables" => ItemCategory.CONSUMABLE,
                "consumable" => ItemCategory.CONSUMABLE,
                "equipment" => ItemCategory.EQUIPMENT,
                "key_item" => ItemCategory.KEY_ITEM,
                "material" => ItemCategory.MATERIAL,
                _ => ItemCategory.CONSUMABLE
            };
        }

        public EquipmentSlot GetEquipmentSlot()
        {
            return (EquipmentSlot)equipment_slot;
        }

        public Rarity GetRarity()
        {
            return (Rarity)rarity;
        }

        public Brand GetBrandAffinity()
        {
            if (brand_affinity <= 0) return Brand.NONE;
            return (Brand)brand_affinity;
        }

        /// <summary>
        /// Check if this is a capture item (Capture Orb, etc.)
        /// </summary>
        public bool IsCaptureItem()
        {
            return capture_rate_bonus > 0 ||
                   (item_id?.Contains("capture") ?? false) ||
                   (item_id?.Contains("orb") ?? false);
        }

        /// <summary>
        /// Check if this is a healing item
        /// </summary>
        public bool IsHealingItem()
        {
            return heal_amount > 0 || mp_restore > 0;
        }

        /// <summary>
        /// Check if this is equipment
        /// </summary>
        public bool IsEquipment()
        {
            return GetCategory() == ItemCategory.EQUIPMENT;
        }

        /// <summary>
        /// Get total stat bonus value (for sorting/comparison)
        /// </summary>
        public int GetTotalStatValue()
        {
            return attack_bonus + defense_bonus + magic_bonus +
                   resistance_bonus + speed_bonus + hp_bonus + mp_bonus;
        }
    }
}
