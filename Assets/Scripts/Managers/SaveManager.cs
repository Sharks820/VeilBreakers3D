using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages all save/load operations for VeilBreakers.
    /// Singleton MonoBehaviour with async operations and bulletproof corruption prevention.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[SaveManager] Instance not found! Ensure SaveManager exists in scene.");
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONSTANTS
        // =============================================================================

        public const int SLOT_COUNT = 3;           // Manual save slots (0, 1, 2)
        public const int AUTO_SLOT = -1;           // Auto-save slot identifier
        private const string SAVES_FOLDER = "saves";
        private const string SLOT_PREFIX = "slot_";
        private const string AUTO_FILENAME = "auto";
        private const string SAVE_EXTENSION = ".sav";

        // =============================================================================
        // STATE
        // =============================================================================

        private SaveData _currentSave;
        private int _currentSlot = -1;
        private bool _isSaving;
        private bool _isLoading;
        private float _sessionStartTime;
        private readonly SemaphoreSlim _saveMutex = new SemaphoreSlim(1, 1);
        private MigrationRunner _migrationRunner;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Currently loaded save data (null if no save loaded)</summary>
        public SaveData CurrentSave => _currentSave;

        /// <summary>Currently loaded slot (-1 = auto, 0-2 = manual, -2 = none)</summary>
        public int CurrentSlot => _currentSlot;

        /// <summary>True if a save operation is in progress</summary>
        public bool IsSaving => _isSaving;

        /// <summary>True if a load operation is in progress</summary>
        public bool IsLoading => _isLoading;

        /// <summary>True if any save data is loaded</summary>
        public bool HasActiveSave => _currentSave != null;

        /// <summary>Path to saves directory</summary>
        public string SavesDirectory => Path.Combine(Application.persistentDataPath, SAVES_FOLDER);

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SaveManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            _saveMutex?.Dispose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Auto-save when app is paused (mobile)
            if (pauseStatus && HasActiveSave)
            {
                _ = AutoSaveAsync("app_pause");
            }
        }

        private void OnApplicationQuit()
        {
            // Update playtime before quit
            UpdatePlaytime();
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            _migrationRunner = MigrationFactory.Create();
            _sessionStartTime = Time.realtimeSinceStartup;

            // Ensure saves directory exists
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
                Debug.Log($"[SaveManager] Created saves directory: {SavesDirectory}");
            }

            // Cleanup orphaned temp files
            SaveFileHandler.CleanupOrphanedTempFiles(SavesDirectory);

            Debug.Log($"[SaveManager] Initialized. Saves directory: {SavesDirectory}");
        }

        // =============================================================================
        // PUBLIC API - SAVE OPERATIONS
        // =============================================================================

        /// <summary>
        /// Saves current game state to specified slot.
        /// </summary>
        /// <param name="slot">Slot index (0-2 for manual)</param>
        /// <returns>True if save succeeded</returns>
        public async Task<bool> SaveAsync(int slot)
        {
            if (slot < 0 || slot >= SLOT_COUNT)
            {
                Debug.LogError($"[SaveManager] Invalid slot: {slot}");
                return false;
            }

            return await SaveInternalAsync(slot, GetSlotPath(slot));
        }

        /// <summary>
        /// Saves current game state to auto-save slot.
        /// </summary>
        /// <param name="reason">Reason for auto-save (for logging)</param>
        /// <returns>True if save succeeded</returns>
        public async Task<bool> AutoSaveAsync(string reason = "checkpoint")
        {
            Debug.Log($"[SaveManager] Auto-save triggered: {reason}");
            EventBus.AutoSaveTriggered(reason);
            return await SaveInternalAsync(AUTO_SLOT, GetAutoSavePath());
        }

        /// <summary>
        /// Creates a new save from initial game state.
        /// </summary>
        public async Task<bool> CreateNewSaveAsync(int slot, string heroId, string heroName, Path heroPath)
        {
            if (slot < 0 || slot >= SLOT_COUNT)
            {
                Debug.LogError($"[SaveManager] Invalid slot: {slot}");
                return false;
            }

            _currentSave = SaveData.CreateNew(heroId, heroName, heroPath);
            _currentSlot = slot;
            _sessionStartTime = Time.realtimeSinceStartup;

            return await SaveAsync(slot);
        }

        // =============================================================================
        // PUBLIC API - LOAD OPERATIONS
        // =============================================================================

        /// <summary>
        /// Loads save data from specified slot.
        /// </summary>
        /// <param name="slot">Slot index (0-2 for manual, -1 for auto)</param>
        /// <returns>True if load succeeded</returns>
        public async Task<bool> LoadAsync(int slot)
        {
            string path = slot == AUTO_SLOT ? GetAutoSavePath() : GetSlotPath(slot);

            if (!File.Exists(path))
            {
                Debug.LogError($"[SaveManager] Save file not found: {path}");
                return false;
            }

            if (!await _saveMutex.WaitAsync(0))
            {
                Debug.LogWarning("[SaveManager] Save/Load already in progress");
                return false;
            }

            _isLoading = true;
            EventBus.LoadStarted(slot);

            try
            {
                Debug.Log($"[SaveManager] Loading slot {slot}...");

                // Read file
                byte[] fileData = await SaveFileHandler.ReadFileAsync(path);
                if (fileData == null)
                {
                    // Try backup recovery
                    Debug.LogWarning("[SaveManager] Primary file failed, attempting backup recovery...");
                    fileData = await SaveFileHandler.TryRecoverFromBackup(path);

                    if (fileData == null)
                    {
                        throw new IOException("Failed to read save file and no valid backup found");
                    }
                }

                // Deserialize
                SaveData data = SaveFileHandler.DeserializeFromBytes(fileData);
                if (data == null)
                {
                    throw new InvalidDataException("Failed to deserialize save data");
                }

                // Migrate if needed
                if (data.version < SaveVersion.CURRENT)
                {
                    Debug.Log($"[SaveManager] Migrating save from v{data.version} to v{SaveVersion.CURRENT}...");
                    data = _migrationRunner.MigrateToLatest(data);

                    if (data == null)
                    {
                        throw new InvalidDataException("Migration failed");
                    }

                    // Assign migrated data BEFORE saving (fixes race condition)
                    _currentSave = data;

                    // Save migrated data
                    Debug.Log("[SaveManager] Saving migrated data...");
                    await SaveInternalAsync(slot, path);
                }
                else
                {
                    _currentSave = data;
                }
                _currentSlot = slot;
                _sessionStartTime = Time.realtimeSinceStartup;

                Debug.Log($"[SaveManager] Loaded slot {slot} successfully. Hero: {data.heroName} Lv{data.heroLevel}");
                EventBus.LoadCompleted(slot);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}");
                EventBus.LoadFailed(slot, ex.Message);
                return false;
            }
            finally
            {
                _isLoading = false;
                _saveMutex.Release();
            }
        }

        // =============================================================================
        // PUBLIC API - SLOT MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Deletes save data from specified slot.
        /// </summary>
        public bool DeleteSlot(int slot)
        {
            string path = slot == AUTO_SLOT ? GetAutoSavePath() : GetSlotPath(slot);

            try
            {
                // Delete main file
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                // Delete backups
                string bak1 = path.Replace(SAVE_EXTENSION, ".bak1");
                string bak2 = path.Replace(SAVE_EXTENSION, ".bak2");

                if (File.Exists(bak1)) File.Delete(bak1);
                if (File.Exists(bak2)) File.Delete(bak2);

                // Clear current if deleting active slot
                if (_currentSlot == slot)
                {
                    _currentSave = null;
                    _currentSlot = -2;
                }

                Debug.Log($"[SaveManager] Deleted slot {slot}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Delete failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a slot has save data.
        /// </summary>
        public bool SlotExists(int slot)
        {
            string path = slot == AUTO_SLOT ? GetAutoSavePath() : GetSlotPath(slot);
            return File.Exists(path);
        }

        /// <summary>
        /// Gets metadata for a single slot without loading full data.
        /// </summary>
        public async Task<SaveSlotMetadata> GetSlotMetadataAsync(int slot)
        {
            string path = slot == AUTO_SLOT ? GetAutoSavePath() : GetSlotPath(slot);

            if (!File.Exists(path))
            {
                return SaveSlotMetadata.Empty(slot);
            }

            try
            {
                byte[] fileData = await SaveFileHandler.ReadFileAsync(path);
                return SaveFileHandler.ExtractMetadata(fileData, slot);
            }
            catch (Exception ex)
            {
                return SaveSlotMetadata.Corrupted(slot, ex.Message);
            }
        }

        /// <summary>
        /// Gets metadata for all slots (for load screen).
        /// </summary>
        public async Task<SaveSlotMetadata[]> GetAllSlotsMetadataAsync()
        {
            var results = new SaveSlotMetadata[SLOT_COUNT + 1]; // +1 for auto slot

            // Manual slots
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                results[i] = await GetSlotMetadataAsync(i);
            }

            // Auto slot
            results[SLOT_COUNT] = await GetSlotMetadataAsync(AUTO_SLOT);

            return results;
        }

        // =============================================================================
        // PUBLIC API - CURRENT SAVE OPERATIONS
        // =============================================================================

        /// <summary>
        /// Updates playtime in current save.
        /// </summary>
        public void UpdatePlaytime()
        {
            if (_currentSave == null) return;

            float sessionTime = Time.realtimeSinceStartup - _sessionStartTime;
            _currentSave.playtimeSeconds += sessionTime;
            _sessionStartTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Updates current location in save data.
        /// </summary>
        public void SetCurrentLocation(string location)
        {
            if (_currentSave != null)
            {
                _currentSave.currentLocation = location;
            }
        }

        /// <summary>
        /// Adds a discovered shrine to save data.
        /// </summary>
        public void AddDiscoveredShrine(string shrineId)
        {
            if (_currentSave != null && !_currentSave.discoveredShrines.Contains(shrineId))
            {
                _currentSave.discoveredShrines.Add(shrineId);
            }
        }

        /// <summary>
        /// Checks if a shrine has been discovered.
        /// </summary>
        public bool IsShrineDiscovered(string shrineId)
        {
            return _currentSave?.discoveredShrines.Contains(shrineId) ?? false;
        }

        // =============================================================================
        // PRIVATE HELPERS
        // =============================================================================

        private async Task<bool> SaveInternalAsync(int slot, string path)
        {
            if (_currentSave == null)
            {
                Debug.LogError("[SaveManager] No save data to save");
                return false;
            }

            if (!await _saveMutex.WaitAsync(0))
            {
                Debug.LogWarning("[SaveManager] Save already in progress");
                return false;
            }

            _isSaving = true;
            EventBus.SaveStarted(slot);

            try
            {
                Debug.Log($"[SaveManager] Saving to slot {slot}...");

                // Update metadata
                UpdatePlaytime();
                _currentSave.UpdateTimestamp();

                // Rotate backups before save
                SaveFileHandler.RotateBackups(path);

                // Serialize
                byte[] data = SaveFileHandler.SerializeToBytes(_currentSave);

                // Write atomically
                bool success = await SaveFileHandler.WriteFileAtomicAsync(path, data);

                if (success)
                {
                    Debug.Log($"[SaveManager] Saved slot {slot} successfully. Size: {data.Length} bytes");
                    EventBus.SaveCompleted(slot);
                    return true;
                }
                else
                {
                    throw new IOException("Atomic write failed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}");
                EventBus.SaveFailed(slot, ex.Message);
                return false;
            }
            finally
            {
                _isSaving = false;
                _saveMutex.Release();
            }
        }

        private string GetSlotPath(int slot)
        {
            return Path.Combine(SavesDirectory, $"{SLOT_PREFIX}{slot}{SAVE_EXTENSION}");
        }

        private string GetAutoSavePath()
        {
            return Path.Combine(SavesDirectory, $"{AUTO_FILENAME}{SAVE_EXTENSION}");
        }
    }
}
