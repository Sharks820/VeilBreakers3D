using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// Centralized reference holder for game data assets.
    /// Eliminates Resources.Load calls by using direct references.
    /// Mark this asset as Addressable with key "GameDataAssets" for async loading.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataAssets", menuName = "VeilBreakers/Data/GameDataAssets")]
    public class GameDataAssets : ScriptableObject
    {
        private const string kDataAssetResourcePath = "Data/GameDataAssets";
        private static readonly string[] kMonsterJsonCandidates = { "Data/monsters", "monsters" };
        private static readonly string[] kSkillsJsonCandidates = { "Data/skills", "skills" };
        private static readonly string[] kHeroesJsonCandidates = { "Data/heroes", "heroes" };
        private static readonly string[] kItemsJsonCandidates = { "Data/items", "items" };

        private static GameDataAssets _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        /// <summary>
        /// Singleton instance. Must be initialized via Initialize() before use.
        /// For Addressables, load this asset first then call Initialize().
        /// </summary>
        public static GameDataAssets Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreateInstance();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize the singleton with a loaded instance.
        /// Call this after loading via Addressables.
        /// </summary>
        public static void Initialize(GameDataAssets instance)
        {
            _instance = instance;
            EnsureRequiredJsonAssets(_instance);
        }

        private static GameDataAssets LoadOrCreateInstance()
        {
            var assets = Resources.Load<GameDataAssets>(kDataAssetResourcePath);
            if (assets == null)
            {
                assets = CreateInstance<GameDataAssets>();
                assets.hideFlags = HideFlags.DontUnloadUnusedAsset;
                Debug.LogWarning("[GameDataAssets] Data/GameDataAssets not found. Using runtime fallback from Resources/Data/*.json.");
            }

            EnsureRequiredJsonAssets(assets);
            return assets;
        }

        private static void EnsureRequiredJsonAssets(GameDataAssets assets)
        {
            if (assets == null)
            {
                return;
            }

            bool resolvedAny = false;
            assets._monstersJson = ResolveJsonAsset(assets._monstersJson, "monsters", kMonsterJsonCandidates, ref resolvedAny);
            assets._skillsJson = ResolveJsonAsset(assets._skillsJson, "skills", kSkillsJsonCandidates, ref resolvedAny);
            assets._heroesJson = ResolveJsonAsset(assets._heroesJson, "heroes", kHeroesJsonCandidates, ref resolvedAny);
            assets._itemsJson = ResolveJsonAsset(assets._itemsJson, "items", kItemsJsonCandidates, ref resolvedAny);

            if (HasAllRequiredJsonAssets(assets))
            {
                if (resolvedAny)
                {
                    Debug.LogWarning("[GameDataAssets] Filled missing JSON references from Resources.");
                }
                return;
            }

            Debug.LogError("[GameDataAssets] Required JSON assets are missing. Expected Resources/Data/monsters, skills, heroes, and items.");
        }

        private static bool HasAllRequiredJsonAssets(GameDataAssets assets)
        {
            return assets._monstersJson != null &&
                   assets._skillsJson != null &&
                   assets._heroesJson != null &&
                   assets._itemsJson != null;
        }

        private static TextAsset ResolveJsonAsset(TextAsset current, string label, string[] candidatePaths, ref bool resolvedAny)
        {
            if (current != null)
            {
                return current;
            }

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                var asset = Resources.Load<TextAsset>(candidatePaths[i]);
                if (asset != null)
                {
                    resolvedAny = true;
                    return asset;
                }
            }

            Debug.LogError($"[GameDataAssets] Could not find {label}.json in Resources.");
            return null;
        }

        // =============================================================================
        // JSON DATA FILES
        // =============================================================================

        [Header("JSON Data Files")]
        [SerializeField] private TextAsset _monstersJson;
        [SerializeField] private TextAsset _skillsJson;
        [SerializeField] private TextAsset _heroesJson;
        [SerializeField] private TextAsset _itemsJson;

        public TextAsset MonstersJson => _monstersJson;
        public TextAsset SkillsJson => _skillsJson;
        public TextAsset HeroesJson => _heroesJson;
        public TextAsset ItemsJson => _itemsJson;

        // =============================================================================
        // STATUS EFFECTS
        // =============================================================================

        [Header("Status Effects")]
        [SerializeField] private StatusEffectData[] _statusEffects;

        public IReadOnlyList<StatusEffectData> StatusEffects => _statusEffects;
    }
}
