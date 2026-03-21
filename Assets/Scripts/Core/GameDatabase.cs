using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// GameDatabase - Central data repository
    /// Loads all JSON data files and provides access to game data
    /// Singleton pattern for global access
    /// </summary>
    public class GameDatabase : SingletonMonoBehaviour<GameDatabase>
    {
        // =============================================================================
        // DATA CONTAINERS
        // =============================================================================

        private readonly Dictionary<string, MonsterData> _monsters = new Dictionary<string, MonsterData>();
        private readonly Dictionary<string, SkillData> _skills = new Dictionary<string, SkillData>();
        private readonly Dictionary<string, HeroData> _heroes = new Dictionary<string, HeroData>();
        private readonly Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>();
        private bool _isLoading;

        public bool IsLoaded { get; private set; } = false;
        public bool LoadFailed { get; private set; } = false;
        public string LastLoadError { get; private set; }

        /// <summary>True when data loaded successfully (IsLoaded && !LoadFailed). Use for consumers that need valid data.</summary>
        public bool IsReady => IsLoaded && !LoadFailed;

        /// <summary>Task representing the initialization load. Await this to ensure data is available.</summary>
        public Task InitializationTask { get; private set; }

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

        protected override void OnSingletonAwake()
        {
            // Do not block the main thread here
            InitializationTask = LoadAllDataAsync();
        }

        /// <summary>
        /// Load all game data from JSON files asynchronously.
        /// </summary>
        public async Task LoadAllDataAsync()
        {
            if (IsLoaded || _isLoading) return;
            ErrorLogger.Log("[GameDatabase] Starting asynchronous data load...");
            _isLoading = true;
            LoadFailed = false;
            LastLoadError = null;

            try
            {
                // Each loader writes exclusively to its own dictionary (_monsters, _skills, etc.),
                // so parallel execution is safe with no shared mutable state between tasks.
                var tasks = new List<Task>
                {
                    LoadMonstersAsync(),
                    LoadSkillsAsync(),
                    LoadHeroesAsync(),
                    LoadItemsAsync()
                };

                await Task.WhenAll(tasks);

                IsLoaded = true;

                ErrorLogger.Log("[GameDatabase] Data loaded successfully!");
                ErrorLogger.Log($"  - Monsters: {_monsters.Count}");
                ErrorLogger.Log($"  - Skills: {_skills.Count}");
                ErrorLogger.Log($"  - Heroes: {_heroes.Count}");
                ErrorLogger.Log($"  - Items: {_items.Count}");
            }
            catch (Exception ex)
            {
                LoadFailed = true;
                LastLoadError = ex.Message;
                IsLoaded = true; // Unblock menu flows; consumers can inspect counts/LoadFailed.

                Debug.LogError($"[GameDatabase] Data load failed: {ex.Message}");
                Debug.LogError($"[GameDatabase] Partial load counts - Monsters: {_monsters.Count}, Skills: {_skills.Count}, Heroes: {_heroes.Count}, Items: {_items.Count}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        // =============================================================================
        // JSON PARSING HELPER
        // =============================================================================

        /// <summary>
        /// Safely wraps a JSON array in an object wrapper for JsonUtility parsing.
        /// Validates input format to prevent parsing errors.
        /// </summary>
        private static string WrapJsonArray(string jsonArrayText, string wrapperKey)
        {
            if (string.IsNullOrEmpty(jsonArrayText))
                return null;

            // Trim whitespace
            string trimmed = jsonArrayText.Trim();

            // Validate it's an array (starts with [ and ends with ])
            if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
            {
                ErrorLogger.Warn($"[GameDatabase] JSON text does not appear to be an array. Expected [...], got: {trimmed.Substring(0, System.Math.Min(50, trimmed.Length))}...");
                // Try to parse anyway in case it's already wrapped
                if (trimmed.StartsWith("{"))
                    return trimmed;
            }

            // Use StringBuilder for efficiency and safety
            var sb = new System.Text.StringBuilder(trimmed.Length + wrapperKey.Length + 6);
            sb.Append("{\"");
            sb.Append(wrapperKey);
            sb.Append("\":");
            sb.Append(trimmed);
            sb.Append("}");

            return sb.ToString();
        }

        // =============================================================================
        // DATA LOADERS
        // =============================================================================

        private Task LoadMonstersAsync()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.MonstersJson : null;
                if (jsonAsset == null)
                {
                    throw new InvalidOperationException("[GameDatabase] MonstersJson not assigned in GameDataAssets!");
                }

                string jsonContent = jsonAsset.text;

                // Wrapping is trivial string work - no need for a background thread
                string wrappedJson = WrapJsonArray(jsonContent, "monsters");

                var wrapper = JsonUtility.FromJson<MonsterDataWrapper>(wrappedJson);
                MonsterData[] monsters = wrapper?.monsters;

                if (monsters == null)
                {
                    throw new InvalidDataException("[GameDatabase] monsters.json invalid format");
                }

                foreach (var monster in monsters)
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

            return Task.CompletedTask;
        }

        private Task LoadSkillsAsync()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.SkillsJson : null;
                if (jsonAsset == null)
                {
                    throw new InvalidOperationException("[GameDatabase] SkillsJson not assigned in GameDataAssets!");
                }

                string jsonContent = jsonAsset.text;

                // Wrapping is trivial string work - no need for a background thread
                string wrappedJson = WrapJsonArray(jsonContent, "skills");

                var wrapper = JsonUtility.FromJson<SkillDataWrapper>(wrappedJson);
                SkillData[] skills = wrapper?.skills;

                if (skills == null)
                {
                    throw new InvalidDataException("[GameDatabase] skills.json invalid format");
                }

                foreach (var skill in skills)
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

            return Task.CompletedTask;
        }

        private Task LoadHeroesAsync()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.HeroesJson : null;
                if (jsonAsset == null)
                {
                    throw new InvalidOperationException("[GameDatabase] HeroesJson not assigned in GameDataAssets!");
                }

                string jsonContent = jsonAsset.text;

                // Wrapping is trivial string work - no need for a background thread
                string wrappedJson = WrapJsonArray(jsonContent, "heroes");

                var wrapper = JsonUtility.FromJson<HeroDataWrapper>(wrappedJson);
                HeroData[] heroes = wrapper?.heroes;

                if (heroes == null)
                {
                    throw new InvalidDataException("[GameDatabase] heroes.json invalid format");
                }

                foreach (var hero in heroes)
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

            return Task.CompletedTask;
        }

        private Task LoadItemsAsync()
        {
            try
            {
                var dataAssets = GameDataAssets.Instance;
                var jsonAsset = dataAssets != null ? dataAssets.ItemsJson : null;
                if (jsonAsset == null)
                {
                    throw new InvalidOperationException("[GameDatabase] ItemsJson not assigned in GameDataAssets!");
                }

                string jsonContent = jsonAsset.text;

                // Wrapping is trivial string work - no need for a background thread
                string wrappedJson = WrapJsonArray(jsonContent, "items");

                var wrapper = JsonUtility.FromJson<ItemDataWrapper>(wrappedJson);
                ItemData[] items = wrapper?.items;

                if (items == null)
                {
                    throw new InvalidDataException("[GameDatabase] items.json invalid format");
                }

                foreach (var item in items)
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

            return Task.CompletedTask;
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
        /// Get all monsters of a specific brand (allocates a new list).
        /// </summary>
        public List<MonsterData> GetMonstersByBrand(Brand brand)
        {
            var result = new List<MonsterData>();
            GetMonstersByBrand(brand, result);
            return result;
        }

        /// <summary>
        /// Get all monsters of a specific brand into a pre-allocated list.
        /// </summary>
        public void GetMonstersByBrand(Brand brand, List<MonsterData> results)
        {
            results.Clear();
            foreach (var monster in _monsters.Values)
            {
                if (monster.GetPrimaryBrand() == brand)
                {
                    results.Add(monster);
                }
            }
        }

        /// <summary>
        /// Get all monsters of a specific rarity (allocates a new list).
        /// </summary>
        public List<MonsterData> GetMonstersByRarity(Rarity rarity)
        {
            var result = new List<MonsterData>();
            GetMonstersByRarity(rarity, result);
            return result;
        }

        /// <summary>
        /// Get all monsters of a specific rarity into a pre-allocated list.
        /// </summary>
        public void GetMonstersByRarity(Rarity rarity, List<MonsterData> results)
        {
            results.Clear();
            foreach (var monster in _monsters.Values)
            {
                if (monster.GetRarity() == rarity)
                {
                    results.Add(monster);
                }
            }
        }

        /// <summary>
        /// Get all skills usable by a specific brand (allocates a new list).
        /// </summary>
        public List<SkillData> GetSkillsByBrand(Brand brand)
        {
            var result = new List<SkillData>();
            GetSkillsByBrand(brand, result);
            return result;
        }

        /// <summary>
        /// Get all skills usable by a specific brand into a pre-allocated list.
        /// </summary>
        public void GetSkillsByBrand(Brand brand, List<SkillData> results)
        {
            results.Clear();
            foreach (var skill in _skills.Values)
            {
                if (skill.GetBrandRequirement() == brand || skill.GetBrandRequirement() == Brand.NONE)
                {
                    results.Add(skill);
                }
            }
        }

        /// <summary>
        /// Get all items by category (allocates a new list).
        /// </summary>
        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            var result = new List<ItemData>();
            GetItemsByCategory(category, result);
            return result;
        }

        /// <summary>
        /// Get all items by category into a pre-allocated list.
        /// </summary>
        public void GetItemsByCategory(ItemCategory category, List<ItemData> results)
        {
            results.Clear();
            foreach (var item in _items.Values)
            {
                if (item.GetCategory() == category)
                {
                    results.Add(item);
                }
            }
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

        // =============================================================================
        // WRAPPER CLASSES FOR JSON DESERIALIZATION
        // =============================================================================

        [Serializable]
        private class MonsterDataWrapper
        {
            public MonsterData[] monsters;
        }

        [Serializable]
        private class SkillDataWrapper
        {
            public SkillData[] skills;
        }

        [Serializable]
        private class HeroDataWrapper
        {
            public HeroData[] heroes;
        }

        [Serializable]
        private class ItemDataWrapper
        {
            public ItemData[] items;
        }
    }
}
