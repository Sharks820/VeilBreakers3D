using System;
using System.Collections;
using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Manages adaptive music with horizontal and vertical layering.
    /// Handles music state transitions and parameter-based mixing.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static MusicManager _instance;
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[MusicManager] Instance is null. Ensure MusicManager exists in scene.");
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

        [Header("Transition Settings")]
        [SerializeField] private float _stateTransitionDuration = 2f;
        [SerializeField] private float _parameterLerpSpeed = 3f;

        // =============================================================================
        // STATE
        // =============================================================================

        private MusicState _currentState = MusicState.EXPLORATION;
        private MusicState _targetState = MusicState.EXPLORATION;

        // FMOD Parameters (0-1 range)
        private float _intensityParam = 0f;
        private float _tensionParam = 0f;
        private float _lowHealthParam = 0f;
        private float _bossPhaseParam = 0f;

        // Target values for lerping
        private float _targetIntensity = 0f;
        private float _targetTension = 0f;
        private float _targetLowHealth = 0f;
        private float _targetBossPhase = 0f;

        // Transition tracking
        private Coroutine _transitionCoroutine;
        private bool _isTransitioning = false;
        private bool _isMuted = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public MusicState CurrentState => _currentState;
        public bool IsTransitioning => _isTransitioning;
        public float Intensity => _intensityParam;
        public float Tension => _tensionParam;
        public float LowHealth => _lowHealthParam;
        public bool IsMuted => _isMuted;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<MusicState> OnMusicStateChanged;
        public event Action<string, float> OnParameterChanged;

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
        }

        private void Start()
        {
            StartExplorationMusic();
        }

        private void Update()
        {
            UpdateParameters();
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
            OnMusicStateChanged = null;
            OnParameterChanged = null;

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set the music state.
        /// </summary>
        public void SetMusicState(MusicState state)
        {
            if (_currentState == state && !_isTransitioning) return;

            _targetState = state;

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(TransitionToStateCoroutine(state));
        }

        /// <summary>
        /// Set a parameter value (smoothly interpolated).
        /// </summary>
        public void SetParameter(string paramName, float value)
        {
            value = Mathf.Clamp01(value);

            switch (paramName.ToLower())
            {
                case "intensity":
                    _targetIntensity = value;
                    break;
                case "tension":
                    _targetTension = value;
                    break;
                case "lowhealth":
                    _targetLowHealth = value;
                    break;
                case "bossphase":
                    _targetBossPhase = value;
                    break;
            }
        }

        /// <summary>
        /// Set a parameter value immediately (no interpolation).
        /// </summary>
        public void SetParameterImmediate(string paramName, float value)
        {
            value = Mathf.Clamp01(value);

            switch (paramName.ToLower())
            {
                case "intensity":
                    _intensityParam = _targetIntensity = value;
                    break;
                case "tension":
                    _tensionParam = _targetTension = value;
                    break;
                case "lowhealth":
                    _lowHealthParam = _targetLowHealth = value;
                    break;
                case "bossphase":
                    _bossPhaseParam = _targetBossPhase = value;
                    break;
            }

            ApplyParameter(paramName, value);
        }

        /// <summary>
        /// Start exploration music.
        /// </summary>
        public void StartExplorationMusic()
        {
            SetMusicState(MusicState.EXPLORATION);
            SetParameterImmediate("intensity", 0f);
            SetParameterImmediate("tension", 0f);

            // FMOD Integration:
            // _explorationEvent = FMODUnity.RuntimeManager.CreateInstance(_config.musicExplorationPath);
            // _explorationEvent.start();

            Debug.Log("[MusicManager] Started exploration music");
        }

        /// <summary>
        /// Transition to combat music.
        /// </summary>
        public void StartCombatMusic()
        {
            SetMusicState(MusicState.COMBAT_LOW);
            SetParameter("intensity", 0.5f);

            // FMOD Integration:
            // Stop exploration, start combat
            // _combatEvent = FMODUnity.RuntimeManager.CreateInstance(_config.musicCombatPath);
            // _combatEvent.start();

            Debug.Log("[MusicManager] Started combat music");
        }

        /// <summary>
        /// Increase combat intensity (boss phase, low HP).
        /// </summary>
        public void SetCombatIntensity(float intensity)
        {
            if (_currentState == MusicState.COMBAT_LOW && intensity > 0.7f)
            {
                SetMusicState(MusicState.COMBAT_HIGH);
            }
            else if (_currentState == MusicState.COMBAT_HIGH && intensity < 0.5f)
            {
                SetMusicState(MusicState.COMBAT_LOW);
            }

            SetParameter("intensity", intensity);
        }

        /// <summary>
        /// Set tension level (enemy nearby).
        /// </summary>
        public void SetTension(float tension)
        {
            if (_currentState == MusicState.EXPLORATION && tension > 0.3f)
            {
                SetMusicState(MusicState.TENSION);
            }
            else if (_currentState == MusicState.TENSION && tension < 0.1f)
            {
                SetMusicState(MusicState.EXPLORATION);
            }

            SetParameter("tension", tension);
        }

        /// <summary>
        /// Play victory stinger and transition.
        /// </summary>
        public void PlayVictory()
        {
            SetMusicState(MusicState.VICTORY);
            AudioManager.Instance?.PlayStinger("Victory");

            // After stinger, fade back to exploration
            StartCoroutine(DelayedStateChange(MusicState.EXPLORATION, 5f));
        }

        /// <summary>
        /// Play defeat stinger.
        /// </summary>
        public void PlayDefeat()
        {
            SetMusicState(MusicState.DEFEAT);
            AudioManager.Instance?.PlayStinger("Defeat");
        }

        /// <summary>
        /// Mute/unmute music.
        /// </summary>
        public void SetMuted(bool muted)
        {
            _isMuted = muted;

            // FMOD Integration:
            // Set bus volume to 0 or normal

            Debug.Log($"[MusicManager] Music muted: {muted}");
        }

        // =============================================================================
        // PARAMETER UPDATES
        // =============================================================================

        private void UpdateParameters()
        {
            float dt = Time.deltaTime * _parameterLerpSpeed;

            // Lerp parameters toward targets
            bool intensityChanged = LerpParameter(ref _intensityParam, _targetIntensity, dt);
            bool tensionChanged = LerpParameter(ref _tensionParam, _targetTension, dt);
            bool lowHealthChanged = LerpParameter(ref _lowHealthParam, _targetLowHealth, dt);
            bool bossPhaseChanged = LerpParameter(ref _bossPhaseParam, _targetBossPhase, dt);

            // Apply to FMOD
            if (intensityChanged) ApplyParameter("Intensity", _intensityParam);
            if (tensionChanged) ApplyParameter("Tension", _tensionParam);
            if (lowHealthChanged) ApplyParameter("LowHealth", _lowHealthParam);
            if (bossPhaseChanged) ApplyParameter("BossPhase", _bossPhaseParam);
        }

        private bool LerpParameter(ref float current, float target, float dt)
        {
            if (Mathf.Approximately(current, target)) return false;

            float oldValue = current;
            current = Mathf.MoveTowards(current, target, dt);
            return !Mathf.Approximately(oldValue, current);
        }

        private void ApplyParameter(string paramName, float value)
        {
            // FMOD Integration:
            // _currentEvent.setParameterByName(paramName, value);

            OnParameterChanged?.Invoke(paramName, value);
        }

        // =============================================================================
        // STATE TRANSITIONS
        // =============================================================================

        private IEnumerator TransitionToStateCoroutine(MusicState newState)
        {
            _isTransitioning = true;
            MusicState previousState = _currentState;

            // Configure parameters based on new state
            ConfigureStateParameters(newState);

            // Wait for transition
            yield return new WaitForSeconds(_stateTransitionDuration);

            _currentState = newState;
            _isTransitioning = false;

            OnMusicStateChanged?.Invoke(newState);
            Debug.Log($"[MusicManager] Music state: {previousState} -> {newState}");
        }

        private void ConfigureStateParameters(MusicState state)
        {
            switch (state)
            {
                case MusicState.EXPLORATION:
                    _targetIntensity = 0f;
                    _targetTension = 0f;
                    break;

                case MusicState.TENSION:
                    _targetIntensity = 0.2f;
                    _targetTension = 0.5f;
                    break;

                case MusicState.COMBAT_LOW:
                    _targetIntensity = 0.5f;
                    _targetTension = 1f;
                    break;

                case MusicState.COMBAT_HIGH:
                    _targetIntensity = 1f;
                    _targetTension = 1f;
                    break;

                case MusicState.VICTORY:
                    _targetIntensity = 0f;
                    _targetTension = 0f;
                    break;

                case MusicState.DEFEAT:
                    _targetIntensity = 0f;
                    _targetTension = 0f;
                    break;
            }
        }

        private IEnumerator DelayedStateChange(MusicState state, float delay)
        {
            yield return new WaitForSeconds(delay);
            SetMusicState(state);
        }

        // =============================================================================
        // COMBAT INTEGRATION
        // =============================================================================

        /// <summary>
        /// Update music based on player health.
        /// </summary>
        public void UpdatePlayerHealth(float healthPercent)
        {
            float lowHealthValue = _config != null
                ? _config.GetLowHealthIntensity(healthPercent)
                : (healthPercent <= 0.25f ? 1f - (healthPercent / 0.25f) : 0f);

            SetParameter("lowhealth", lowHealthValue);

            // Increase combat intensity when low health
            if (_currentState == MusicState.COMBAT_LOW || _currentState == MusicState.COMBAT_HIGH)
            {
                float combatIntensity = Mathf.Lerp(_targetIntensity, 1f, lowHealthValue * 0.5f);
                SetCombatIntensity(combatIntensity);
            }
        }

        /// <summary>
        /// Update music for boss phases.
        /// </summary>
        public void SetBossPhase(int phase)
        {
            float phaseValue = Mathf.Clamp01((phase - 1) / 2f); // 1=0, 2=0.5, 3=1
            SetParameter("bossphase", phaseValue);

            if (phase >= 2)
            {
                SetCombatIntensity(0.8f + (phaseValue * 0.2f));
            }
        }

        // =============================================================================
        // DEBUG
        // =============================================================================

        /// <summary>
        /// Get current music state as string for debugging.
        /// </summary>
        public string GetStateString()
        {
            return $"State: {_currentState} | Int: {_intensityParam:F2} | Tens: {_tensionParam:F2} | HP: {_lowHealthParam:F2}";
        }
    }
}
