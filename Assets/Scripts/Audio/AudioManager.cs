using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Main audio manager with smart bank loading and memory management.
    /// Handles FMOD integration for AAA audio experience.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[AudioManager] Instance is null. Ensure AudioManager exists in scene.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Configuration")]
        [SerializeField] private AudioConfig _config;

        [Header("Volume Settings")]
        [SerializeField] private float _masterVolume = 0.8f;
        [SerializeField] private float _musicVolume = 0.7f;
        [SerializeField] private float _sfxVolume = 1.0f;
        [SerializeField] private float _voiceVolume = 1.0f;
        [SerializeField] private float _ambientVolume = 0.6f;

        // =============================================================================
        // STATE
        // =============================================================================

        // Bank tracking
        private HashSet<string> _loadedBanks = new HashSet<string>();
        private Dictionary<string, float> _bankLastUsed = new Dictionary<string, float>();
        private Dictionary<string, long> _bankMemoryUsage = new Dictionary<string, long>();

        // Zone tracking
        private string _currentZone;
        private string _preloadingZone;

        // Party monster banks
        private List<string> _partyMonsterBanks = new List<string>();

        // Memory tracking
        private long _currentMemoryUsage = 0;

        // Initialization
        private bool _isInitialized = false;

        // Pre-allocated lists
        private List<string> _banksToUnload = new List<string>(16);
        private List<KeyValuePair<string, float>> _sortedBanks = new List<KeyValuePair<string, float>>(32);

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public AudioConfig Config => _config;
        public bool IsInitialized => _isInitialized;
        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public float VoiceVolume => _voiceVolume;
        public float AmbientVolume => _ambientVolume;
        public long CurrentMemoryUsage => _currentMemoryUsage;
        public IReadOnlyCollection<string> LoadedBanks => _loadedBanks;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnInitialized;
        public event Action<string> OnBankLoaded;
        public event Action<string> OnBankUnloaded;
        public event Action<string> OnZoneChanged;
        public event Action<long> OnMemoryUsageChanged;

        // =============================================================================
        // UNITY LIFECYCLE
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
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnDisable()
        {
            OnInitialized = null;
            OnBankLoaded = null;
            OnBankUnloaded = null;
            OnZoneChanged = null;
            OnMemoryUsageChanged = null;

            StopAllCoroutines();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_isInitialized) return;

            // Load default volumes from config
            if (_config != null)
            {
                _masterVolume = _config.defaultMaster;
                _musicVolume = _config.defaultMusic;
                _sfxVolume = _config.defaultSFX;
                _voiceVolume = _config.defaultVoice;
                _ambientVolume = _config.defaultAmbient;
            }

            // Load core bank
            StartCoroutine(LoadCoreBankCoroutine());
        }

        private IEnumerator LoadCoreBankCoroutine()
        {
            yield return LoadBankCoroutine(AudioConfig.CORE_BANK);

            _isInitialized = true;
            OnInitialized?.Invoke();

            Debug.Log("[AudioManager] Initialized with core bank loaded");
        }

        // =============================================================================
        // ZONE MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Called when entering a new zone.
        /// </summary>
        public void OnZoneEnter(string zoneName)
        {
            if (_currentZone == zoneName) return;

            string oldZone = _currentZone;
            _currentZone = zoneName;

            StartCoroutine(HandleZoneEnterCoroutine(zoneName, oldZone));
        }

        private IEnumerator HandleZoneEnterCoroutine(string zoneName, string oldZone)
        {
            string newBankName = AudioConfig.GetZoneBankName(zoneName);
            yield return LoadBankCoroutine(newBankName);

            if (!string.IsNullOrEmpty(oldZone))
            {
                string oldBankName = AudioConfig.GetZoneBankName(oldZone);
                StartCoroutine(DelayedUnloadCoroutine(oldBankName, _config?.zoneUnloadDelay ?? 30f));
            }

            OnZoneChanged?.Invoke(zoneName);
            Debug.Log($"[AudioManager] Zone changed to: {zoneName}");
        }

        /// <summary>
        /// Called when approaching a zone boundary for preloading.
        /// </summary>
        public void OnZoneBoundaryApproach(string nextZone)
        {
            if (_preloadingZone == nextZone) return;
            if (_loadedBanks.Contains(AudioConfig.GetZoneBankName(nextZone))) return;

            _preloadingZone = nextZone;
            StartCoroutine(LoadBankCoroutine(AudioConfig.GetZoneBankName(nextZone)));

            Debug.Log($"[AudioManager] Preloading zone: {nextZone}");
        }

        // =============================================================================
        // COMBAT MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Called when combat starts to load enemy sound banks.
        /// </summary>
        public void OnCombatStart(List<string> enemyIds)
        {
            if (enemyIds == null || enemyIds.Count == 0) return;

            StartCoroutine(LoadCombatBanksCoroutine(enemyIds));
        }

        private IEnumerator LoadCombatBanksCoroutine(List<string> enemyIds)
        {
            foreach (var enemyId in enemyIds)
            {
                string bankName = AudioConfig.GetMonsterBankName(enemyId);
                if (!_loadedBanks.Contains(bankName))
                {
                    yield return LoadBankCoroutine(bankName);
                }
            }
        }

        /// <summary>
        /// Called when combat ends to schedule unloading enemy sounds.
        /// </summary>
        public void OnCombatEnd()
        {
            StartCoroutine(UnloadCombatBanksDelayedCoroutine(_config?.combatUnloadDelay ?? 30f));
        }

        private IEnumerator UnloadCombatBanksDelayedCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Unload monster banks that aren't in the party
            _banksToUnload.Clear();
            foreach (var bankName in _loadedBanks)
            {
                if (bankName.StartsWith(AudioConfig.MONSTER_BANK_PREFIX) &&
                    !_partyMonsterBanks.Contains(bankName))
                {
                    _banksToUnload.Add(bankName);
                }
            }

            foreach (var bankName in _banksToUnload)
            {
                UnloadBank(bankName);
                yield return null; // Spread over frames
            }
        }

        // =============================================================================
        // PARTY MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Called when party composition changes.
        /// </summary>
        public void OnPartyChanged(List<string> monsterIds)
        {
            if (monsterIds == null) return;

            StartCoroutine(HandlePartyChangeCoroutine(monsterIds));
        }

        private IEnumerator HandlePartyChangeCoroutine(List<string> monsterIds)
        {
            // Find banks to unload (no longer in party)
            _banksToUnload.Clear();
            foreach (var bank in _partyMonsterBanks)
            {
                bool stillInParty = false;
                foreach (var monsterId in monsterIds)
                {
                    if (AudioConfig.GetMonsterBankName(monsterId) == bank)
                    {
                        stillInParty = true;
                        break;
                    }
                }
                if (!stillInParty)
                {
                    _banksToUnload.Add(bank);
                }
            }

            foreach (var bank in _banksToUnload)
            {
                UnloadBank(bank);
            }

            // Update party banks and load new ones
            _partyMonsterBanks.Clear();
            foreach (var monsterId in monsterIds)
            {
                string bankName = AudioConfig.GetMonsterBankName(monsterId);
                _partyMonsterBanks.Add(bankName);

                if (!_loadedBanks.Contains(bankName))
                {
                    yield return LoadBankCoroutine(bankName);
                }
            }
        }

        // =============================================================================
        // NPC VOICE
        // =============================================================================

        /// <summary>
        /// Called when approaching an NPC to preload their voice bank.
        /// </summary>
        public void OnNPCApproach(string npcId)
        {
            string voiceBank = AudioConfig.GetNPCBankName(npcId);

            if (!_loadedBanks.Contains(voiceBank))
            {
                StartCoroutine(LoadBankCoroutine(voiceBank));
            }

            _bankLastUsed[voiceBank] = Time.time;
        }

        // =============================================================================
        // VOLUME CONTROL
        // =============================================================================

        /// <summary>
        /// Set master volume.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        /// <summary>
        /// Set music volume.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        /// <summary>
        /// Set SFX volume.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        /// <summary>
        /// Set voice volume.
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            _voiceVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        /// <summary>
        /// Set ambient volume.
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            _ambientVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
            // FMOD Integration:
            // FMODUnity.RuntimeManager.GetBus("bus:/Master").setVolume(_masterVolume);
            // FMODUnity.RuntimeManager.GetBus("bus:/Music").setVolume(_musicVolume * _masterVolume);
            // FMODUnity.RuntimeManager.GetBus("bus:/SFX").setVolume(_sfxVolume * _masterVolume);
            // FMODUnity.RuntimeManager.GetBus("bus:/Voice").setVolume(_voiceVolume * _masterVolume);
            // FMODUnity.RuntimeManager.GetBus("bus:/Ambient").setVolume(_ambientVolume * _masterVolume);
        }

        // =============================================================================
        // SOUND PLAYBACK
        // =============================================================================

        /// <summary>
        /// Play a one-shot sound effect.
        /// </summary>
        public void PlayOneShot(string eventPath)
        {
            if (string.IsNullOrEmpty(eventPath)) return;

            // FMOD Integration:
            // FMODUnity.RuntimeManager.PlayOneShot(eventPath);

            Debug.Log($"[AudioManager] PlayOneShot: {eventPath}");
        }

        /// <summary>
        /// Play a one-shot sound at a specific position.
        /// </summary>
        public void PlayOneShotAtPosition(string eventPath, Vector3 position)
        {
            if (string.IsNullOrEmpty(eventPath)) return;

            // FMOD Integration:
            // FMODUnity.RuntimeManager.PlayOneShot(eventPath, position);

            Debug.Log($"[AudioManager] PlayOneShotAtPosition: {eventPath} at {position}");
        }

        /// <summary>
        /// Play a combat hit sound.
        /// </summary>
        public void PlayCombatHit(string intensity = "Medium")
        {
            string path = _config != null ? $"{_config.combatHitPath}{intensity}" : $"event:/SFX/Combat/Hit_{intensity}";
            PlayOneShot(path);
        }

        /// <summary>
        /// Play a critical hit sound.
        /// </summary>
        public void PlayCriticalHit()
        {
            string path = _config?.combatCriticalPath ?? "event:/SFX/Combat/Critical";
            PlayOneShot(path);
        }

        /// <summary>
        /// Play a block sound.
        /// </summary>
        public void PlayBlock()
        {
            string path = _config?.combatBlockPath ?? "event:/SFX/Combat/Block";
            PlayOneShot(path);
        }

        /// <summary>
        /// Play a miss/dodge sound.
        /// </summary>
        public void PlayMiss()
        {
            string path = _config?.combatMissPath ?? "event:/SFX/Combat/Miss";
            PlayOneShot(path);
        }

        /// <summary>
        /// Play UI confirm sound.
        /// </summary>
        public void PlayUIConfirm()
        {
            string path = _config?.uiConfirmPath ?? "event:/SFX/UI/Confirm";
            PlayOneShot(path);
        }

        /// <summary>
        /// Play UI cancel sound.
        /// </summary>
        public void PlayUICancel()
        {
            string path = _config?.uiCancelPath ?? "event:/SFX/UI/Cancel";
            PlayOneShot(path);
        }

        /// <summary>
        /// Play a music stinger.
        /// </summary>
        public void PlayStinger(string stingerName)
        {
            string path = stingerName switch
            {
                "Victory" => _config?.victoryStingerPath ?? "event:/Music/Stingers/Victory",
                "Defeat" => _config?.defeatStingerPath ?? "event:/Music/Stingers/Defeat",
                _ => $"event:/Music/Stingers/{stingerName}"
            };
            PlayOneShot(path);
        }

        // =============================================================================
        // BANK MANAGEMENT
        // =============================================================================

        private IEnumerator LoadBankCoroutine(string bankName)
        {
            if (_loadedBanks.Contains(bankName))
            {
                _bankLastUsed[bankName] = Time.time;
                yield break;
            }

            // Enforce budget before loading
            yield return EnforceBudgetCoroutine();

            // Simulate bank loading (replace with actual FMOD loading)
            // FMOD Integration:
            // FMODUnity.RuntimeManager.LoadBank(bankName);

            yield return null; // Simulate async load

            // Track the bank
            _loadedBanks.Add(bankName);
            _bankLastUsed[bankName] = Time.time;

            // Estimate memory usage (would come from FMOD in real implementation)
            long estimatedSize = EstimateBankSize(bankName);
            _bankMemoryUsage[bankName] = estimatedSize;
            _currentMemoryUsage += estimatedSize;

            OnBankLoaded?.Invoke(bankName);
            OnMemoryUsageChanged?.Invoke(_currentMemoryUsage);

            Debug.Log($"[AudioManager] Bank loaded: {bankName} (Est. {estimatedSize / 1024}KB)");
        }

        private void UnloadBank(string bankName)
        {
            // Never unload core bank
            if (bankName == AudioConfig.CORE_BANK) return;

            if (!_loadedBanks.Contains(bankName)) return;

            // FMOD Integration:
            // FMODUnity.RuntimeManager.UnloadBank(bankName);

            _loadedBanks.Remove(bankName);
            _bankLastUsed.Remove(bankName);

            if (_bankMemoryUsage.TryGetValue(bankName, out long size))
            {
                _currentMemoryUsage -= size;
                _bankMemoryUsage.Remove(bankName);
            }

            OnBankUnloaded?.Invoke(bankName);
            OnMemoryUsageChanged?.Invoke(_currentMemoryUsage);

            Debug.Log($"[AudioManager] Bank unloaded: {bankName}");
        }

        private IEnumerator DelayedUnloadCoroutine(string bankName, float delay)
        {
            yield return new WaitForSeconds(delay);
            UnloadBank(bankName);
        }

        // =============================================================================
        // MEMORY MANAGEMENT
        // =============================================================================

        private IEnumerator EnforceBudgetCoroutine()
        {
            if (_config == null) yield break;

            long totalBudget = _config.TotalBudgetBytes;
            float budgetThreshold = 0.9f; // Start evicting at 90%

            while (_currentMemoryUsage > totalBudget * budgetThreshold)
            {
                // Find LRU bank to evict (excluding core and party)
                string lruBank = FindLRUBank();

                if (lruBank != null)
                {
                    UnloadBank(lruBank);
                    yield return null; // Spread over frames
                }
                else
                {
                    // No banks can be evicted
                    break;
                }
            }
        }

        private string FindLRUBank()
        {
            _sortedBanks.Clear();
            foreach (var kvp in _bankLastUsed)
            {
                // Skip protected banks
                if (kvp.Key == AudioConfig.CORE_BANK) continue;
                if (_partyMonsterBanks.Contains(kvp.Key)) continue;
                if (kvp.Key == AudioConfig.GetZoneBankName(_currentZone)) continue;

                _sortedBanks.Add(kvp);
            }

            if (_sortedBanks.Count == 0) return null;

            // Sort by last used time (oldest first)
            _sortedBanks.Sort((a, b) => a.Value.CompareTo(b.Value));

            return _sortedBanks[0].Key;
        }

        private long EstimateBankSize(string bankName)
        {
            // Estimate sizes based on bank type
            // In real implementation, FMOD provides actual sizes
            if (bankName == AudioConfig.CORE_BANK)
                return _config?.budgetCore * 1024L * 1024L ?? 50L * 1024L * 1024L;

            if (bankName.StartsWith(AudioConfig.ZONE_BANK_PREFIX))
                return 25L * 1024L * 1024L; // ~25MB per zone

            if (bankName.StartsWith(AudioConfig.MONSTER_BANK_PREFIX))
                return 5L * 1024L * 1024L; // ~5MB per monster

            if (bankName.StartsWith(AudioConfig.NPC_BANK_PREFIX))
                return 2L * 1024L * 1024L; // ~2MB per NPC

            return 10L * 1024L * 1024L; // Default estimate
        }

        // =============================================================================
        // DEBUG
        // =============================================================================

        /// <summary>
        /// Get current memory usage as a formatted string.
        /// </summary>
        public string GetMemoryUsageString()
        {
            long mb = _currentMemoryUsage / (1024 * 1024);
            long budget = _config?.TotalBudgetBytes / (1024 * 1024) ?? 250;
            return $"{mb}MB / {budget}MB ({_loadedBanks.Count} banks)";
        }
    }
}
