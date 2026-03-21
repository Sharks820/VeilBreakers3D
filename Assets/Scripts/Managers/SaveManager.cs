using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using IOPath = System.IO.Path;  // Alias to avoid conflict with VeilBreakers.Data.Path

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages all save/load operations for VeilBreakers.
    /// Singleton MonoBehaviour with async operations and bulletproof corruption prevention.
    /// </summary>
    public class SaveManager : SingletonMonoBehaviour<SaveManager>
    {

        // =============================================================================
        // CONSTANTS
        // =============================================================================

        public const int kSlotCount = 3;                    // Manual save slots (0, 1, 2)
        public const int kAutoSlot = -1;                   // Primary auto-save slot
        public const int kAutoSlotCheckpoint = -2;         // Secondary rolling auto-save slot
        public const int kAutoSlotCount = 2;               // Total auto-save slots
        public const int kNoneSlot = -999;                 // No active slot loaded
        private const string kSavesFolder = "saves";
        private const string SLOT_PREFIX = "slot_";
        private const string AUTO_FILENAME = "auto";
        private const string AUTO_CHECKPOINT_FILENAME = "auto_checkpoint";
        private const string SAVE_EXTENSION = ".sav";

        // =============================================================================
        // STATE
        // =============================================================================

        private SaveData _currentSave;
        private int _currentSlot = kNoneSlot;
        private bool _isSaving;
        private bool _isLoading;
        private float _sessionStartTime;
        private int _nextAutoSlot = kAutoSlot;
        private readonly SemaphoreSlim _saveMutex = new SemaphoreSlim(1, 1);
        private MigrationRunner _migrationRunner;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>Currently loaded save data (null if no save loaded)</summary>
        public SaveData CurrentSave => _currentSave;

        /// <summary>Currently loaded slot (0-2 = manual, -1/-2 = auto, -999 = none)</summary>
        public int CurrentSlot => _currentSlot;

        /// <summary>True if a save operation is in progress</summary>
        public bool IsSaving => _isSaving;

        /// <summary>True if a load operation is in progress</summary>
        public bool IsLoading => _isLoading;

        /// <summary>True if any save data is loaded</summary>
        public bool HasActiveSave => _currentSave != null;

        /// <summary>Path to saves directory</summary>
        public string SavesDirectory => IOPath.Combine(Application.persistentDataPath, kSavesFolder);

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            Initialize();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // SemaphoreSlim is GC-safe - don't Dispose while operations might be in flight.
            // Disposing during an active save/load race causes ObjectDisposedException.
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && HasActiveSave)
            {
                if (_isSaving || _isLoading) return;

                // Non-blocking: check mutex availability before attempting save
                if (_saveMutex.CurrentCount > 0)
                {
                    _ = AutoSaveAsync("app_pause"); // Fire and forget; method has internal try/catch
                }
                else
                {
                    Debug.LogWarning("[SaveManager] Save mutex contended during pause, skipping auto-save.");
                }
            }
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
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
            if (slot < 0 || slot >= kSlotCount)
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

            // Keep two rolling auto-save checkpoints so recovery doesn't depend on a single file.
            int targetAutoSlot = _nextAutoSlot;
            _nextAutoSlot = targetAutoSlot == kAutoSlot ? kAutoSlotCheckpoint : kAutoSlot;
            return await SaveInternalAsync(targetAutoSlot, GetAutoSavePath(targetAutoSlot));
        }

        /// <summary>
        /// Creates a new save from initial game state.
        /// </summary>
        public async Task<bool> CreateNewSaveAsync(int slot, string heroId, string heroName, Data.Path heroPath)
        {
            if (slot < 0 || slot >= kSlotCount)
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
        /// <param name="slot">Slot index (0-2 for manual, -1/-2 for auto)</param>
        /// <returns>True if load succeeded</returns>
        public async Task<bool> LoadAsync(int slot)
        {
            if (!TryResolveSlotPath(slot, out string path))
            {
                Debug.LogError($"[SaveManager] Invalid slot: {slot}");
                return false;
            }

            if (!File.Exists(path))
            {
                Debug.LogError($"[SaveManager] Save file not found: {path}");
                return false;
            }

            if (!await _saveMutex.WaitAsync(5000)) // 5 second timeout
            {
                Debug.LogError("[SaveManager] Save/Load operation timeout - took longer than 5 seconds");
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

                    // Save migrated data (skip mutex since we already hold it)
                    Debug.Log("[SaveManager] Saving migrated data...");
                    await SaveInternalCoreAsync(slot, path);
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
            if (!TryResolveSlotPath(slot, out string path))
            {
                Debug.LogError($"[SaveManager] Invalid slot: {slot}");
                return false;
            }

            try
            {
                // Delete main file
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                // Delete backups
                string bak1 = IOPath.ChangeExtension(path, ".bak1");
                string bak2 = IOPath.ChangeExtension(path, ".bak2");

                if (File.Exists(bak1)) File.Delete(bak1);
                if (File.Exists(bak2)) File.Delete(bak2);

                // Clear current if deleting active slot
                if (_currentSlot == slot)
                {
                    _currentSave = null;
                    _currentSlot = kNoneSlot;
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
            if (!TryResolveSlotPath(slot, out string path))
            {
                return false;
            }

            return File.Exists(path);
        }

        /// <summary>
        /// Gets metadata for a single slot without loading full data.
        /// </summary>
        public async Task<SaveSlotMetadata> GetSlotMetadataAsync(int slot)
        {
            if (!TryResolveSlotPath(slot, out string path))
            {
                return SaveSlotMetadata.Empty(slot);
            }

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
            var results = new SaveSlotMetadata[kSlotCount + kAutoSlotCount];

            // Load all slots in parallel - each reads a separate file
            var tasks = new Task<SaveSlotMetadata>[kSlotCount + kAutoSlotCount];
            for (int i = 0; i < kSlotCount; i++)
                tasks[i] = GetSlotMetadataAsync(i);
            tasks[kSlotCount] = GetSlotMetadataAsync(kAutoSlot);
            tasks[kSlotCount + 1] = GetSlotMetadataAsync(kAutoSlotCheckpoint);

            await Task.WhenAll(tasks);

            for (int i = 0; i < tasks.Length; i++)
                results[i] = tasks[i].Result;

            return results;
        }

        /// <summary>
        /// Returns the most recent valid slot across manual slots and, optionally, auto-save slots.
        /// Returns kNoneSlot when no valid save exists.
        /// </summary>
        public async Task<int> GetMostRecentSlotAsync(bool includeAutoSlots = true)
        {
            var all = await GetAllSlotsMetadataAsync();
            return SelectMostRecentSlot(all, includeAutoSlots);
        }

        /// <summary>
        /// Returns the manual slot to use for a new character.
        /// Prefers first empty slot; if all are occupied, returns the oldest manual slot.
        /// </summary>
        public async Task<int> GetBestNewGameSlotAsync()
        {
            SaveSlotMetadata[] manual = new SaveSlotMetadata[kSlotCount];

            for (int i = 0; i < kSlotCount; i++)
            {
                manual[i] = await GetSlotMetadataAsync(i);
            }

            return SelectBestNewGameSlot(manual);
        }

        public static int SelectMostRecentSlot(SaveSlotMetadata[] metadata, bool includeAutoSlots)
        {
            if (metadata == null || metadata.Length == 0)
            {
                return kNoneSlot;
            }

            int bestSlot = kNoneSlot;
            DateTime bestDate = DateTime.MinValue;
            bool foundValid = false;

            for (int i = 0; i < metadata.Length; i++)
            {
                var meta = metadata[i];
                if (meta == null)
                {
                    continue;
                }

                bool isAutoSlot = meta.slotIndex == kAutoSlot || meta.slotIndex == kAutoSlotCheckpoint;
                if (!includeAutoSlots && isAutoSlot)
                {
                    continue;
                }

                if (!TryGetMetadataTimestamp(meta, out DateTime timestamp))
                {
                    continue;
                }

                if (!foundValid || timestamp > bestDate)
                {
                    foundValid = true;
                    bestDate = timestamp;
                    bestSlot = meta.slotIndex;
                }
            }

            return bestSlot;
        }

        public static int SelectBestNewGameSlot(SaveSlotMetadata[] manualMetadata)
        {
            if (manualMetadata == null || manualMetadata.Length == 0)
            {
                return 0;
            }

            int count = Math.Min(kSlotCount, manualMetadata.Length);

            for (int i = 0; i < count; i++)
            {
                var meta = manualMetadata[i];
                if (meta == null || !meta.hasData || meta.isCorrupted)
                {
                    return i;
                }
            }

            int oldestSlot = 0;
            DateTime oldestDate = DateTime.MaxValue;
            bool foundTimestamp = false;

            for (int i = 0; i < count; i++)
            {
                var meta = manualMetadata[i];
                if (!TryGetMetadataTimestamp(meta, out DateTime timestamp))
                {
                    continue;
                }

                if (!foundTimestamp || timestamp < oldestDate)
                {
                    foundTimestamp = true;
                    oldestDate = timestamp;
                    oldestSlot = meta.slotIndex >= 0 ? meta.slotIndex : i;
                }
            }

            return oldestSlot;
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

            if (!await _saveMutex.WaitAsync(5000)) // 5 second timeout
            {
                Debug.LogError("[SaveManager] Save/Load operation timeout - took longer than 5 seconds");
                return false;
            }

            try
            {
                return await SaveInternalCoreAsync(slot, path);
            }
            finally
            {
                _saveMutex.Release();
            }
        }

        /// <summary>
        /// Core save logic without mutex acquisition (for use when mutex is already held).
        /// </summary>
        private async Task<bool> SaveInternalCoreAsync(int slot, string path)
        {
            if (_currentSave == null)
            {
                Debug.LogError("[SaveManager] No save data to save");
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
            }
        }

        private string GetSlotPath(int slot)
        {
            return IOPath.Combine(SavesDirectory, $"{SLOT_PREFIX}{slot}{SAVE_EXTENSION}");
        }

        private string GetAutoSavePath(int slot = kAutoSlot)
        {
            if (slot == kAutoSlotCheckpoint)
            {
                return IOPath.Combine(SavesDirectory, $"{AUTO_CHECKPOINT_FILENAME}{SAVE_EXTENSION}");
            }

            return IOPath.Combine(SavesDirectory, $"{AUTO_FILENAME}{SAVE_EXTENSION}");
        }

        private bool TryResolveSlotPath(int slot, out string path)
        {
            if (slot >= 0 && slot < kSlotCount)
            {
                path = GetSlotPath(slot);
                return true;
            }

            if (slot == kAutoSlot || slot == kAutoSlotCheckpoint)
            {
                path = GetAutoSavePath(slot);
                return true;
            }

            path = null;
            return false;
        }

        private static bool TryGetMetadataTimestamp(SaveSlotMetadata metadata, out DateTime timestamp)
        {
            timestamp = DateTime.MinValue;
            if (metadata == null || !metadata.hasData || metadata.isCorrupted)
            {
                return false;
            }

            if (!DateTime.TryParse(
                metadata.saveDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out timestamp))
            {
                timestamp = DateTime.MinValue;
                return false;
            }

            return true;
        }
    }
}
