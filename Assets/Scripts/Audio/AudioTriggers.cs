using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Trigger zone for audio zone changes and preloading.
    /// </summary>
    public class AudioTriggerZone : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Zone Settings")]
        [SerializeField] private string _zoneName;
        [SerializeField] private bool _isBoundary = false;  // If true, preloads; if false, enters zone

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public string ZoneName => _zoneName;
        public bool IsBoundary => _isBoundary;

        // =============================================================================
        // TRIGGER
        // =============================================================================

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (AudioManager.Instance == null)
            {
                if (_debugLog) Debug.LogWarning($"[AudioTriggerZone] AudioManager not found");
                return;
            }

            if (_isBoundary)
            {
                // Preload the zone audio
                AudioManager.Instance.OnZoneBoundaryApproach(_zoneName);

                if (_debugLog) Debug.Log($"[AudioTriggerZone] Preloading zone: {_zoneName}");
            }
            else
            {
                // Actually enter the zone
                AudioManager.Instance.OnZoneEnter(_zoneName);

                if (_debugLog) Debug.Log($"[AudioTriggerZone] Entering zone: {_zoneName}");
            }
        }

        // =============================================================================
        // EDITOR
        // =============================================================================

        private void OnDrawGizmos()
        {
            var collider = GetComponent<Collider>();
            if (collider == null) return;

            Gizmos.color = _isBoundary
                ? new Color(1f, 1f, 0f, 0.3f)  // Yellow for boundary
                : new Color(0f, 1f, 0f, 0.3f); // Green for zone entry

            if (collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var collider = GetComponent<Collider>();
            if (collider == null) return;

            Gizmos.color = _isBoundary ? Color.yellow : Color.green;

            if (collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Zone: {_zoneName}\n{(_isBoundary ? "Boundary" : "Entry")}");
#endif
        }
    }

    /// <summary>
    /// Trigger for preloading NPC voice banks on approach.
    /// </summary>
    public class AudioTriggerNPC : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("NPC Settings")]
        [SerializeField] private string _npcId;
        [SerializeField] private float _preloadDistance = 20f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // =============================================================================
        // STATE
        // =============================================================================

        private Transform _playerTransform;
        private bool _preloaded = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public string NPCId => _npcId;
        public float PreloadDistance => _preloadDistance;
        public bool IsPreloaded => _preloaded;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            // Find player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (_preloaded) return;
            if (_playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            if (distance < _preloadDistance)
            {
                _preloaded = true;
                AudioManager.Instance?.OnNPCApproach(_npcId);

                if (_debugLog) Debug.Log($"[AudioTriggerNPC] Preloading NPC voice: {_npcId}");
            }
        }

        /// <summary>
        /// Reset the preload state (call when player leaves area).
        /// </summary>
        public void ResetPreload()
        {
            _preloaded = false;
        }

        // =============================================================================
        // EDITOR
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _preloadDistance);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"NPC: {_npcId}\nPreload: {_preloadDistance}m");
#endif
        }
    }

    /// <summary>
    /// Trigger for Veil proximity audio effects.
    /// </summary>
    public class AudioTriggerVeil : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Veil Settings")]
        [SerializeField] private float _maxEffectDistance = 50f;  // Distance where effects start
        [SerializeField] private float _fullEffectDistance = 10f; // Distance for full effect

        [Header("Effect Parameters")]
        [SerializeField] private float _maxReverb = 0.8f;
        [SerializeField] private float _maxDistortion = 0.5f;
        [SerializeField] private float _maxWhisperVolume = 0.7f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // =============================================================================
        // STATE
        // =============================================================================

        private Transform _playerTransform;
        private bool _isPlayerInRange = false;
        private float _currentProximity = 0f;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public float CurrentProximity => _currentProximity;
        public bool IsPlayerInRange => _isPlayerInRange;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, _playerTransform.position);

            if (distance <= _maxEffectDistance)
            {
                if (!_isPlayerInRange)
                {
                    _isPlayerInRange = true;
                    OnEnterVeilProximity();
                }

                // Calculate proximity (0 = at max distance, 1 = at full effect distance)
                _currentProximity = 1f - Mathf.InverseLerp(_fullEffectDistance, _maxEffectDistance, distance);
                _currentProximity = Mathf.Clamp01(_currentProximity);

                UpdateVeilEffects();
            }
            else if (_isPlayerInRange)
            {
                _isPlayerInRange = false;
                _currentProximity = 0f;
                OnExitVeilProximity();
            }
        }

        // =============================================================================
        // EFFECTS
        // =============================================================================

        private void OnEnterVeilProximity()
        {
            // FMOD Integration: Start ambient Veil sounds
            // _veilAmbience = FMODUnity.RuntimeManager.CreateInstance("event:/Ambient/Veil/Proximity");
            // _veilAmbience.start();

            if (_debugLog) Debug.Log("[AudioTriggerVeil] Player entered Veil proximity");
        }

        private void OnExitVeilProximity()
        {
            // FMOD Integration: Stop Veil sounds
            // _veilAmbience.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            // _veilAmbience.release();

            // Reset audio effects
            ResetVeilEffects();

            if (_debugLog) Debug.Log("[AudioTriggerVeil] Player exited Veil proximity");
        }

        private void UpdateVeilEffects()
        {
            float reverb = _currentProximity * _maxReverb;
            float distortion = _currentProximity * _maxDistortion;
            float whispers = _currentProximity * _maxWhisperVolume;

            // FMOD Integration:
            // _veilAmbience.setParameterByName("Proximity", _currentProximity);
            // _veilAmbience.setParameterByName("WhisperVolume", whispers);
            // Global bus effects for reverb/distortion

            if (_debugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AudioTriggerVeil] Proximity: {_currentProximity:F2}, Reverb: {reverb:F2}");
            }
        }

        private void ResetVeilEffects()
        {
            // FMOD Integration: Reset global effects
        }

        // =============================================================================
        // EDITOR
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            // Max effect distance
            Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _maxEffectDistance);

            // Full effect distance
            Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _fullEffectDistance);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Veil Proximity\nMax: {_maxEffectDistance}m\nFull: {_fullEffectDistance}m");
#endif
        }
    }

    /// <summary>
    /// Trigger for combat tension music.
    /// </summary>
    public class AudioTriggerCombatTension : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Tension Settings")]
        [SerializeField] private float _detectionRadius = 30f;
        [SerializeField] private float _fullTensionRadius = 10f;
        [SerializeField] private string _enemyTag = "Enemy";

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // =============================================================================
        // STATE
        // =============================================================================

        private float _currentTension = 0f;
        private int _enemiesInRange = 0;

        // Pre-allocated for overlap check
        private Collider[] _overlapResults = new Collider[32];

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public float CurrentTension => _currentTension;
        public int EnemiesInRange => _enemiesInRange;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Update()
        {
            UpdateTension();
        }

        // =============================================================================
        // TENSION CALCULATION
        // =============================================================================

        private void UpdateTension()
        {
            int enemyCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _detectionRadius,
                _overlapResults,
                LayerMask.GetMask("Enemy")
            );

            _enemiesInRange = 0;
            float closestDistance = _detectionRadius;

            for (int i = 0; i < enemyCount; i++)
            {
                if (_overlapResults[i].CompareTag(_enemyTag))
                {
                    _enemiesInRange++;
                    float dist = Vector3.Distance(transform.position, _overlapResults[i].transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                    }
                }
            }

            // Calculate tension based on closest enemy and count
            if (_enemiesInRange > 0)
            {
                // Distance factor (0 = at detection edge, 1 = at full tension radius)
                float distanceFactor = 1f - Mathf.InverseLerp(_fullTensionRadius, _detectionRadius, closestDistance);

                // Count factor (more enemies = more tension)
                float countFactor = Mathf.Min(1f, _enemiesInRange / 3f);

                _currentTension = Mathf.Max(distanceFactor, countFactor * 0.5f);
            }
            else
            {
                _currentTension = Mathf.MoveTowards(_currentTension, 0f, Time.deltaTime * 0.5f);
            }

            // Apply to music
            MusicManager.Instance?.SetTension(_currentTension);
        }

        // =============================================================================
        // EDITOR
        // =============================================================================

        private void OnDrawGizmosSelected()
        {
            // Detection radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Full tension radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _fullTensionRadius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Combat Tension\nDetect: {_detectionRadius}m\nFull: {_fullTensionRadius}m\n" +
                $"Tension: {_currentTension:F2}");
#endif
        }
    }
}
