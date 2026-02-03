using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Wrapper classes for JSON serialization
    /// Unity's JsonUtility requires specific wrapper formats
    /// </summary>
    public static class GameDataTypes
    {
        // =============================================================================
        // JSON WRAPPER CLASSES
        // =============================================================================
        
        [Serializable]
        public class HeroListWrapper
        {
            public List<HeroData> heroes;
        }
        
        [Serializable]
        public class MonsterListWrapper
        {
            public List<MonsterData> monsters;
        }
        
        [Serializable]
        public class SkillListWrapper
        {
            public List<SkillData> skills;
        }
        
        [Serializable]
        public class ItemListWrapper
        {
            public List<ItemData> items;
        }
        
        // =============================================================================
        // SKILL DATA
        // =============================================================================
        
        [Serializable]
        public class SkillData
        {
            public string skill_id;
            public string skill_name;
            public string description;
            public int skill_type;
            public int damage_type;
            public int target_type;
            public int power;
            public int accuracy;
            public int mp_cost;
            public float cooldown;
            public string icon_path;
            public string[] tags;
        }
        
        // =============================================================================
        // ITEM DATA
        // =============================================================================
        
        [Serializable]
        public class ItemData
        {
            public string item_id;
            public string display_name;
            public string description;
            public int category;
            public int rarity;
            public int value;
            public string icon_path;
            public string[] tags;
        }
    }
}
