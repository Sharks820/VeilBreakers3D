using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// GameDatabase - Central data repository
    /// Loads all JSON data files and provides access to game data
    /// Singleton pattern for global access
    /// </summary>
    public class GameDatabase : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static GameDatabase _instance;
        public static GameDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameDatabase");
                    _instance = go.AddComponent<GameDatabase>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // =============================================================================
        // DATA CONTAINERS
        // =============================================================================

        private Dictionary<string, MonsterData> _monsters = new Dictionary<string, MonsterData>();
        private Dictionary<string, SkillData> _skills = new Dictionary<string, SkillData>();
        private Dictionary<string, HeroData> _heroes = new Dictionary<string, HeroData>();
        private Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>();

        public bool IsLoaded { get; private set; } = false;

        // =============================================================================
        // DATA ACCESS
        // =============================================================================

        public IReadOnlyDictionary<string, MonsterData> Monsters => _monsters;
        public IReadOnlyDictionary<string, SkillData> Skills => _skills;
        public IReadOnlyDictionary<string, HeroData> Heroes => _heroes;
        public IReadOnlyDictionary<string, ItemData> Items => _items;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllData();
        }

        /// <summary>
        /// Load all game data from JSON files
        /// </summary>
        public void LoadAllData()
        {
            Debug.Log("[GameDatabase] Loading all game data...");

            LoadMonsters();
            LoadSkills();
            LoadHeroes();
            LoadItems();

            IsLoaded = true;

            Debug.Log($"[GameDatabase] Data loaded successfully!");
            Debug.Log($"  - Monsters: {_monsters.Count}");
            Debug.Log($"  - Skills: {_skills.Count}");
            Debug.Log($"  - Heroes: {_heroes.Count}");
            Debug.Log($"  - Items: {_items.Count}");
        }

        // =============================================================================
        // DATA LOADERS
        // =============================================================================

        private void LoadMonsters()
        {
            try
            {
                var jsonAsset = Resources.Load<TextAsset>("Data/monsters");
                if (jsonAsset != null)
                {
                    var wrapper = JsonUtility.FromJson<MonsterDataWrapper>("{\"monsters\":" + jsonAsset.text + "}");
                    foreach (var monster in wrapper.monsters)
                    {
                        if (!string.IsNullOrEmpty(monster.monster_id))
                        {
                            _monsters[monster.monster_id] = monster;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GameDatabase] monsters.json not found in Resources/Data/");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load monsters: {e.Message}");
            }
        }

        private void LoadSkills()
        {
            try
            {
                var jsonAsset = Resources.Load<TextAsset>("Data/skills");
                if (jsonAsset != null)
                {
                    var wrapper = JsonUtility.FromJson<SkillDataWrapper>("{\"skills\":" + jsonAsset.text + "}");
                    foreach (var skill in wrapper.skills)
                    {
                        if (!string.IsNullOrEmpty(skill.skill_id))
                        {
                            _skills[skill.skill_id] = skill;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GameDatabase] skills.json not found in Resources/Data/");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load skills: {e.Message}");
            }
        }

        private void LoadHeroes()
        {
            try
            {
                var jsonAsset = Resources.Load<TextAsset>("Data/heroes");
                if (jsonAsset != null)
                {
                    var wrapper = JsonUtility.FromJson<HeroDataWrapper>("{\"heroes\":" + jsonAsset.text + "}");
                    foreach (var hero in wrapper.heroes)
                    {
                        if (!string.IsNullOrEmpty(hero.hero_id))
                        {
                            _heroes[hero.hero_id] = hero;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GameDatabase] heroes.json not found in Resources/Data/");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load heroes: {e.Message}");
            }
        }

        private void LoadItems()
        {
            try
            {
                var jsonAsset = Resources.Load<TextAsset>("Data/items");
                if (jsonAsset != null)
                {
                    var wrapper = JsonUtility.FromJson<ItemDataWrapper>("{\"items\":" + jsonAsset.text + "}");
                    foreach (var item in wrapper.items)
                    {
                        if (!string.IsNullOrEmpty(item.item_id))
                        {
                            _items[item.item_id] = item;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GameDatabase] items.json not found in Resources/Data/");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load items: {e.Message}");
            }
        }

        // =============================================================================
        // QUERY METHODS
        // =============================================================================

        /// <summary>
        /// Get monster data by ID
        /// </summary>
        public MonsterData GetMonster(string monsterId)
        {
            return _monsters.TryGetValue(monsterId, out var data) ? data : null;
        }

        /// <summary>
        /// Get skill data by ID
        /// </summary>
        public SkillData GetSkill(string skillId)
        {
            return _skills.TryGetValue(skillId, out var data) ? data : null;
        }

        /// <summary>
        /// Get hero data by ID
        /// </summary>
        public HeroData GetHero(string heroId)
        {
            return _heroes.TryGetValue(heroId, out var data) ? data : null;
        }

        /// <summary>
        /// Get item data by ID
        /// </summary>
        public ItemData GetItem(string itemId)
        {
            return _items.TryGetValue(itemId, out var data) ? data : null;
        }

        /// <summary>
        /// Get all monsters of a specific brand
        /// </summary>
        public List<MonsterData> GetMonstersByBrand(Brand brand)
        {
            var result = new List<MonsterData>();
            foreach (var monster in _monsters.Values)
            {
                if (monster.GetPrimaryBrand() == brand)
                {
                    result.Add(monster);
                }
            }
            return result;
        }

        /// <summary>
        /// Get all monsters of a specific rarity
        /// </summary>
        public List<MonsterData> GetMonstersByRarity(Rarity rarity)
        {
            var result = new List<MonsterData>();
            foreach (var monster in _monsters.Values)
            {
                if (monster.GetRarity() == rarity)
                {
                    result.Add(monster);
                }
            }
            return result;
        }

        /// <summary>
        /// Get all skills usable by a specific brand
        /// </summary>
        public List<SkillData> GetSkillsByBrand(Brand brand)
        {
            var result = new List<SkillData>();
            foreach (var skill in _skills.Values)
            {
                if (skill.GetBrandRequirement() == brand || skill.GetBrandRequirement() == Brand.NONE)
                {
                    result.Add(skill);
                }
            }
            return result;
        }

        /// <summary>
        /// Get all items by category
        /// </summary>
        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            var result = new List<ItemData>();
            foreach (var item in _items.Values)
            {
                if (item.GetCategory() == category)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        /// <summary>
        /// Get skills for a monster's innate skill list
        /// </summary>
        public List<SkillData> GetMonsterInnateSkills(MonsterData monster)
        {
            var result = new List<SkillData>();
            if (monster?.innate_skills != null)
            {
                foreach (var skillId in monster.innate_skills)
                {
                    var skill = GetSkill(skillId);
                    if (skill != null)
                    {
                        result.Add(skill);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Get skills for a hero's innate skill list
        /// </summary>
        public List<SkillData> GetHeroInnateSkills(HeroData hero)
        {
            var result = new List<SkillData>();
            if (hero?.innate_skills != null)
            {
                foreach (var skillId in hero.innate_skills)
                {
                    var skill = GetSkill(skillId);
                    if (skill != null)
                    {
                        result.Add(skill);
                    }
                }
            }
            return result;
        }
    }

    // =============================================================================
    // WRAPPER CLASSES FOR JSON DESERIALIZATION
    // =============================================================================

    [Serializable]
    internal class MonsterDataWrapper
    {
        public MonsterData[] monsters;
    }

    [Serializable]
    internal class SkillDataWrapper
    {
        public SkillData[] skills;
    }

    [Serializable]
    internal class HeroDataWrapper
    {
        public HeroData[] heroes;
    }

    [Serializable]
    internal class ItemDataWrapper
    {
        public ItemData[] items;
    }
}
