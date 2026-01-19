using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages shrine discovery and save zone checking.
    /// Works with SaveManager to control when manual saves are allowed.
    /// </summary>
    public class ShrineManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static ShrineManager _instance;
        public static ShrineManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[ShrineManager] Instance not found! Ensure ShrineManager exists in scene.");
                }
                return _instance;
            }
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Shrine Configuration")]
        [SerializeField]
        [Tooltip("All shrines in the game (loaded from Resources or assigned)")]
        private List<ShrineData> _allShrines = new List<ShrineData>();

        [Header("Debug")]
        [SerializeField]
        private bool _debugDrawRadius = true;

        // =============================================================================
        // STATE
        // =============================================================================

        private HashSet<string> _discoveredShrines = new HashSet<string>();
        private ShrineData _currentShrine;
        private bool _isInShrineZone;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        /// <summary>All registered shrines</summary>
        public IReadOnlyList<ShrineData> AllShrines => _allShrines;

        /// <summary>IDs of discovered shrines</summary>
        public IReadOnlyCollection<string> DiscoveredShrines => _discoveredShrines;

        /// <summary>Currently active shrine (if player is in range)</summary>
        public ShrineData CurrentShrine => _currentShrine;

        /// <summary>True if player can save (in discovered shrine zone)</summary>
        public bool CanSave => _isInShrineZone && _currentShrine != null;

        /// <summary>Name of current shrine zone (or null)</summary>
        public string CurrentShrineName => _currentShrine?.shrineName;

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

            LoadDiscoveredShrines();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_debugDrawRadius) return;

            foreach (var shrine in _allShrines)
            {
                if (shrine == null) continue;

                // Draw shrine position
                Gizmos.color = _discoveredShrines.Contains(shrine.shrineId) ? Color.green : Color.gray;
                Gizmos.DrawWireSphere(shrine.shrinePosition, shrine.saveRadius);

                // Draw icon at center
                Gizmos.DrawIcon(shrine.shrinePosition, "sv_icon_dot0_sml", true);
            }
        }
#endif

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Registers a new shrine (call during scene setup or dynamically).
        /// </summary>
        public void RegisterShrine(ShrineData shrine)
        {
            if (shrine == null || string.IsNullOrEmpty(shrine.shrineId)) return;

            if (!_allShrines.Contains(shrine))
            {
                _allShrines.Add(shrine);
                Debug.Log($"[ShrineManager] Registered shrine: {shrine.shrineName}");
            }
        }

        /// <summary>
        /// Discovers a shrine (permanently unlocks for saving).
        /// </summary>
        public void DiscoverShrine(string shrineId)
        {
            if (string.IsNullOrEmpty(shrineId)) return;
            if (_discoveredShrines.Contains(shrineId)) return;

            _discoveredShrines.Add(shrineId);

            // Persist to SaveManager
            SaveManager.Instance?.AddDiscoveredShrine(shrineId);

            // Fire event
            EventBus.ShrineDiscovered(shrineId);

            var shrine = GetShrineById(shrineId);
            Debug.Log($"[ShrineManager] Discovered shrine: {shrine?.shrineName ?? shrineId}");
        }

        /// <summary>
        /// Checks if a specific shrine has been discovered.
        /// </summary>
        public bool IsShrineDiscovered(string shrineId)
        {
            return _discoveredShrines.Contains(shrineId);
        }

        /// <summary>
        /// Updates player position and checks shrine zones.
        /// Call this from player controller or GameManager.
        /// </summary>
        public void UpdatePlayerPosition(Vector3 playerPosition)
        {
            ShrineData nearestShrine = null;
            float nearestDistance = float.MaxValue;

            foreach (var shrine in _allShrines)
            {
                if (shrine == null) continue;
                if (!_discoveredShrines.Contains(shrine.shrineId)) continue;

                float distance = shrine.GetDistanceFrom(playerPosition);
                if (distance <= shrine.saveRadius && distance < nearestDistance)
                {
                    nearestShrine = shrine;
                    nearestDistance = distance;
                }
            }

            // State change detection
            bool wasInZone = _isInShrineZone;
            ShrineData previousShrine = _currentShrine;

            _currentShrine = nearestShrine;
            _isInShrineZone = nearestShrine != null;

            // Fire events on state change
            if (_isInShrineZone && !wasInZone)
            {
                EventBus.ShrineEntered(_currentShrine.shrineId);
                Debug.Log($"[ShrineManager] Entered shrine zone: {_currentShrine.shrineName}");
            }
            else if (!_isInShrineZone && wasInZone && previousShrine != null)
            {
                EventBus.ShrineExited(previousShrine.shrineId);
                Debug.Log($"[ShrineManager] Exited shrine zone: {previousShrine.shrineName}");
            }
        }

        /// <summary>
        /// Checks if saving is allowed at a specific position.
        /// </summary>
        public bool CanSaveAtPosition(Vector3 position)
        {
            foreach (var shrine in _allShrines)
            {
                if (shrine == null) continue;
                if (!_discoveredShrines.Contains(shrine.shrineId)) continue;

                if (shrine.IsPositionInRange(position))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets shrine data by ID.
        /// </summary>
        public ShrineData GetShrineById(string shrineId)
        {
            foreach (var shrine in _allShrines)
            {
                if (shrine != null && shrine.shrineId == shrineId)
                {
                    return shrine;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all shrines in a specific area.
        /// </summary>
        public List<ShrineData> GetShrinesInArea(string areaName)
        {
            var result = new List<ShrineData>();
            foreach (var shrine in _allShrines)
            {
                if (shrine != null && shrine.areaName == areaName)
                {
                    result.Add(shrine);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the nearest shrine to a position (discovered or not).
        /// </summary>
        public ShrineData GetNearestShrine(Vector3 position, bool discoveredOnly = true)
        {
            ShrineData nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var shrine in _allShrines)
            {
                if (shrine == null) continue;
                if (discoveredOnly && !_discoveredShrines.Contains(shrine.shrineId)) continue;

                float distance = shrine.GetDistanceFrom(position);
                if (distance < nearestDistance)
                {
                    nearest = shrine;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        // =============================================================================
        // PERSISTENCE
        // =============================================================================

        /// <summary>
        /// Loads discovered shrines from SaveManager.
        /// </summary>
        public void LoadDiscoveredShrines()
        {
            _discoveredShrines.Clear();

            var saveData = SaveManager.Instance?.CurrentSave;
            if (saveData?.discoveredShrines != null)
            {
                foreach (var shrineId in saveData.discoveredShrines)
                {
                    _discoveredShrines.Add(shrineId);
                }
                Debug.Log($"[ShrineManager] Loaded {_discoveredShrines.Count} discovered shrines");
            }
        }

        /// <summary>
        /// Syncs discovered shrines to SaveManager (called before save).
        /// </summary>
        public void SyncToSaveData()
        {
            var saveData = SaveManager.Instance?.CurrentSave;
            if (saveData != null)
            {
                saveData.discoveredShrines.Clear();
                foreach (var shrineId in _discoveredShrines)
                {
                    saveData.discoveredShrines.Add(shrineId);
                }
            }
        }

        /// <summary>
        /// Clears all discovered shrines (for new game).
        /// </summary>
        public void ResetDiscoveredShrines()
        {
            _discoveredShrines.Clear();
            _currentShrine = null;
            _isInShrineZone = false;
        }
    }
}
