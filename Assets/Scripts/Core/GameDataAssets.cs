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
        private static GameDataAssets _instance;

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
                    // Fallback to Resources.Load for backwards compatibility
                    _instance = Resources.Load<GameDataAssets>("Data/GameDataAssets");
                    if (_instance == null)
                    {
                        Debug.LogError("[GameDataAssets] GameDataAssets not found! Create via Assets > Create > VeilBreakers > Data > GameDataAssets");
                    }
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

        public StatusEffectData[] StatusEffects => _statusEffects;
    }
}
