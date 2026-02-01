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

        // Count properties for convenience
        public int MonsterCount => _monsters.Count;
        public int SkillCount => _skills.Count;
        public int HeroCount => _heroes.Count;
        public int ItemCount => _items.Count;

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
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.MonstersJson : null;
                if (jsonAsset == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] MonstersJson not assigned in GameDataAssets!");
#else
                    Debug.LogError("[GameDatabase] MonstersJson not assigned in GameDataAssets!");
                    return;
#endif
                }

                var wrapper = JsonUtility.FromJson<MonsterDataWrapper>("{\"monsters\":" + jsonAsset.text + "}");
                if (wrapper?.monsters == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] monsters.json invalid format");
#else
                    Debug.LogError("[GameDatabase] monsters.json invalid format");
                    return;
#endif
                }

                foreach (var monster in wrapper.monsters)
                {
                    if (!string.IsNullOrEmpty(monster.monster_id))
                    {
                        _monsters[monster.monster_id] = monster;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load monsters: {e.Message}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw;
#endif
            }
        }

        private void LoadSkills()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.SkillsJson : null;
                if (jsonAsset == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] SkillsJson not assigned in GameDataAssets!");
#else
                    Debug.LogError("[GameDatabase] SkillsJson not assigned in GameDataAssets!");
                    return;
#endif
                }

                var wrapper = JsonUtility.FromJson<SkillDataWrapper>("{\"skills\":" + jsonAsset.text + "}");
                if (wrapper?.skills == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] skills.json invalid format");
#else
                    Debug.LogError("[GameDatabase] skills.json invalid format");
                    return;
#endif
                }

                foreach (var skill in wrapper.skills)
                {
                    if (!string.IsNullOrEmpty(skill.skill_id))
                    {
                        _skills[skill.skill_id] = skill;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load skills: {e.Message}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw;
#endif
            }
        }

        private void LoadHeroes()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.HeroesJson : null;
                if (jsonAsset == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] HeroesJson not assigned in GameDataAssets!");
#else
                    Debug.LogError("[GameDatabase] HeroesJson not assigned in GameDataAssets!");
                    return;
#endif
                }

                var wrapper = JsonUtility.FromJson<HeroDataWrapper>("{\"heroes\":" + jsonAsset.text + "}");
                if (wrapper?.heroes == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] heroes.json invalid format");
#else
                    Debug.LogError("[GameDatabase] heroes.json invalid format");
                    return;
#endif
                }

                foreach (var hero in wrapper.heroes)
                {
                    if (!string.IsNullOrEmpty(hero.hero_id))
                    {
                        _heroes[hero.hero_id] = hero;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load heroes: {e.Message}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw;
#endif
            }
        }

        private void LoadItems()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.ItemsJson : null;
                if (jsonAsset == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] ItemsJson not assigned in GameDataAssets!");
#else
                    Debug.LogError("[GameDatabase] ItemsJson not assigned in GameDataAssets!");
                    return;
#endif
                }

                var wrapper = JsonUtility.FromJson<ItemDataWrapper>("{\"items\":" + jsonAsset.text + "}");
                if (wrapper?.items == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    throw new InvalidOperationException("[GameDatabase] items.json invalid format");
#else
                    Debug.LogError("[GameDatabase] items.json invalid format");
                    return;
#endif
                }

                foreach (var item in wrapper.items)
                {
                    if (!string.IsNullOrEmpty(item.item_id))
                    {
                        _items[item.item_id] = item;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameDatabase] Failed to load items: {e.Message}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw;
#endif
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
        /// Get all monsters as a list
        /// </summary>
        public List<MonsterData> GetAllMonsters()
        {
            return new List<MonsterData>(_monsters.Values);
        }

        /// <summary>
        /// Get all heroes as a list
        /// </summary>
        public List<HeroData> GetAllHeroes()
        {
            return new List<HeroData>(_heroes.Values);
        }

        /// <summary>
        /// Get all items as a list
        /// </summary>
        public List<ItemData> GetAllItems()
        {
            return new List<ItemData>(_items.Values);
        }

        /// <summary>
        /// Get all skills as a list
        /// </summary>
        public List<SkillData> GetAllSkills()
        {
            return new List<SkillData>(_skills.Values);
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
