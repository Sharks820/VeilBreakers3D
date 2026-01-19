using System;
using System.Collections;
using UnityEngine;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Controls VERA's dynamic voice system based on Veil Integrity.
    /// Handles voice processing, glitches, and dual-voice effects.
    /// </summary>
    public class VERAVoiceController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static VERAVoiceController _instance;
        public static VERAVoiceController Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[VERAVoiceController] Instance is null.");
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

        [Header("Voice Settings")]
        [SerializeField] private float _currentVeilIntegrity = 100f;
        [SerializeField] private float _integrityLerpSpeed = 5f;

        [Header("Glitch Settings")]
        [SerializeField] private float _glitchMinInterval = 3f;
        [SerializeField] private float _glitchMaxInterval = 8f;
        [SerializeField] private float _glitchDuration = 0.1f;

        // =============================================================================
        // STATE
        // =============================================================================

        private float _targetVeilIntegrity = 100f;
        private float _corruptionLevel = 0f;
        private bool _isGlitching = false;
        private float _nextGlitchTime = 0f;
        private bool _isSpeaking = false;
        private string _currentDialogueId;

        // Voice processing parameters
        private float _pitchShift = 0f;
        private float _reverb = 0f;
        private float _distortion = 0f;
        private float _dualVoiceBlend = 0f;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public float VeilIntegrity => _currentVeilIntegrity;
        public float CorruptionLevel => _corruptionLevel;
        public bool IsSpeaking => _isSpeaking;
        public bool IsGlitching => _isGlitching;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<float> OnVeilIntegrityChanged;
        public event Action<string> OnDialogueStarted;
        public event Action<string> OnDialogueEnded;
        public event Action OnGlitchTriggered;

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

        private void Update()
        {
            UpdateVeilIntegrity();
            UpdateGlitches();
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
            OnVeilIntegrityChanged = null;
            OnDialogueStarted = null;
            OnDialogueEnded = null;
            OnGlitchTriggered = null;

            StopAllCoroutines();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set VERA's Veil Integrity (0-100).
        /// </summary>
        public void SetVeilIntegrity(float integrity)
        {
            _targetVeilIntegrity = Mathf.Clamp(integrity, 0f, 100f);
        }

        /// <summary>
        /// Set Veil Integrity immediately (no interpolation).
        /// </summary>
        public void SetVeilIntegrityImmediate(float integrity)
        {
            _currentVeilIntegrity = _targetVeilIntegrity = Mathf.Clamp(integrity, 0f, 100f);
            UpdateVoiceProcessing();
            OnVeilIntegrityChanged?.Invoke(_currentVeilIntegrity);
        }

        /// <summary>
        /// Play VERA dialogue line.
        /// </summary>
        public void PlayDialogue(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId)) return;
            if (_isSpeaking)
            {
                StopCurrentDialogue();
            }

            _currentDialogueId = dialogueId;
            _isSpeaking = true;

            // FMOD Integration:
            // string eventPath = $"{_config.veraDialoguePath}{dialogueId}";
            // _veraVoice = FMODUnity.RuntimeManager.CreateInstance(eventPath);
            // _veraVoice.setParameterByName("VeilIntegrity", _currentVeilIntegrity);
            // _veraVoice.start();

            OnDialogueStarted?.Invoke(dialogueId);
            Debug.Log($"[VERAVoice] Playing dialogue: {dialogueId} (VI: {_currentVeilIntegrity:F0}%)");
        }

        /// <summary>
        /// Play a combat callout.
        /// </summary>
        public void PlayCombatCallout(string calloutType)
        {
            string eventPath = $"event:/Voice/VERA/Combat_{calloutType}";

            // FMOD Integration:
            // FMODUnity.RuntimeManager.PlayOneShot(eventPath);

            Debug.Log($"[VERAVoice] Combat callout: {calloutType}");
        }

        /// <summary>
        /// Play a reaction line.
        /// </summary>
        public void PlayReaction(string reactionType)
        {
            string eventPath = $"event:/Voice/VERA/Reaction_{reactionType}";

            // FMOD Integration:
            // var instance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
            // instance.setParameterByName("VeilIntegrity", _currentVeilIntegrity);
            // instance.start();
            // instance.release();

            Debug.Log($"[VERAVoice] Reaction: {reactionType}");
        }

        /// <summary>
        /// Stop current dialogue.
        /// </summary>
        public void StopCurrentDialogue()
        {
            if (!_isSpeaking) return;

            // FMOD Integration:
            // _veraVoice.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            // _veraVoice.release();

            string endedDialogue = _currentDialogueId;
            _isSpeaking = false;
            _currentDialogueId = null;

            OnDialogueEnded?.Invoke(endedDialogue);
        }

        /// <summary>
        /// Trigger a manual glitch effect.
        /// </summary>
        public void TriggerGlitch()
        {
            if (_isGlitching) return;
            StartCoroutine(GlitchCoroutine());
        }

        // =============================================================================
        // VEIL INTEGRITY UPDATES
        // =============================================================================

        private void UpdateVeilIntegrity()
        {
            if (Mathf.Approximately(_currentVeilIntegrity, _targetVeilIntegrity)) return;

            float oldIntegrity = _currentVeilIntegrity;
            _currentVeilIntegrity = Mathf.MoveTowards(
                _currentVeilIntegrity,
                _targetVeilIntegrity,
                Time.deltaTime * _integrityLerpSpeed
            );

            if (!Mathf.Approximately(oldIntegrity, _currentVeilIntegrity))
            {
                UpdateVoiceProcessing();
                OnVeilIntegrityChanged?.Invoke(_currentVeilIntegrity);
            }
        }

        private void UpdateVoiceProcessing()
        {
            // Calculate corruption level
            _corruptionLevel = _config != null
                ? _config.GetVERACorruptionLevel(_currentVeilIntegrity)
                : 1f - (_currentVeilIntegrity / 100f);

            // Calculate voice processing parameters
            CalculateVoiceParameters();

            // Apply to FMOD
            ApplyVoiceParameters();
        }

        private void CalculateVoiceParameters()
        {
            if (_config == null)
            {
                // Default calculations
                _pitchShift = _corruptionLevel * 0.2f;
                _reverb = _corruptionLevel * 0.5f;
                _distortion = _corruptionLevel * 0.3f;
                _dualVoiceBlend = Mathf.Max(0f, (_corruptionLevel - 0.5f) * 2f);
                return;
            }

            // Based on config thresholds
            if (_currentVeilIntegrity >= _config.veraCleanThreshold)
            {
                // Clean voice
                _pitchShift = 0f;
                _reverb = 0.1f;
                _distortion = 0f;
                _dualVoiceBlend = 0f;
            }
            else if (_currentVeilIntegrity >= _config.veraMildGlitchThreshold)
            {
                // Mild glitches
                _pitchShift = 0.02f;
                _reverb = 0.15f;
                _distortion = 0.05f;
                _dualVoiceBlend = 0f;
            }
            else if (_currentVeilIntegrity >= _config.veraDistortionThreshold)
            {
                // Noticeable distortion
                _pitchShift = 0.05f;
                _reverb = 0.25f;
                _distortion = 0.15f;
                _dualVoiceBlend = 0.1f;
            }
            else if (_currentVeilIntegrity >= _config.veraDualVoiceThreshold)
            {
                // Dual voice bleeding through
                _pitchShift = 0.1f;
                _reverb = 0.4f;
                _distortion = 0.25f;
                _dualVoiceBlend = 0.4f;
            }
            else
            {
                // Full corruption / reveal
                _pitchShift = 0.15f;
                _reverb = 0.6f;
                _distortion = 0.35f;
                _dualVoiceBlend = 0.8f;
            }
        }

        private void ApplyVoiceParameters()
        {
            // FMOD Integration:
            // _veraVoice.setParameterByName("PitchShift", _pitchShift);
            // _veraVoice.setParameterByName("Reverb", _reverb);
            // _veraVoice.setParameterByName("Distortion", _distortion);
            // _veraVoice.setParameterByName("DualVoice", _dualVoiceBlend);
        }

        // =============================================================================
        // GLITCH SYSTEM
        // =============================================================================

        private void UpdateGlitches()
        {
            // Only glitch at lower integrity levels
            if (_currentVeilIntegrity >= (_config?.veraMildGlitchThreshold ?? 60f)) return;
            if (_isGlitching) return;
            if (!_isSpeaking) return;

            if (Time.time >= _nextGlitchTime)
            {
                // Chance increases with corruption
                float glitchChance = _corruptionLevel * 0.3f;
                if (UnityEngine.Random.value < glitchChance)
                {
                    StartCoroutine(GlitchCoroutine());
                }

                // Schedule next check
                float interval = Mathf.Lerp(_glitchMaxInterval, _glitchMinInterval, _corruptionLevel);
                _nextGlitchTime = Time.time + interval;
            }
        }

        private IEnumerator GlitchCoroutine()
        {
            _isGlitching = true;
            OnGlitchTriggered?.Invoke();

            // Store current values
            float originalPitch = _pitchShift;
            float originalDistortion = _distortion;

            // Apply glitch
            _pitchShift = UnityEngine.Random.Range(-0.3f, 0.3f);
            _distortion = UnityEngine.Random.Range(0.5f, 1f);
            ApplyVoiceParameters();

            // FMOD Integration: Could also trigger a glitch sound effect
            // FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/VERA/Glitch");

            yield return new WaitForSeconds(_glitchDuration);

            // Restore
            _pitchShift = originalPitch;
            _distortion = originalDistortion;
            ApplyVoiceParameters();

            _isGlitching = false;
        }

        // =============================================================================
        // DIALOGUE CALLBACK (For FMOD)
        // =============================================================================

        /// <summary>
        /// Called when dialogue playback completes (callback from FMOD).
        /// </summary>
        public void OnDialoguePlaybackComplete()
        {
            string endedDialogue = _currentDialogueId;
            _isSpeaking = false;
            _currentDialogueId = null;

            OnDialogueEnded?.Invoke(endedDialogue);
        }

        // =============================================================================
        // DEBUG
        // =============================================================================

        /// <summary>
        /// Get voice state as string for debugging.
        /// </summary>
        public string GetStateString()
        {
            return $"VI: {_currentVeilIntegrity:F0}% | Corrupt: {_corruptionLevel:F2} | " +
                   $"Pitch: {_pitchShift:F2} | Dual: {_dualVoiceBlend:F2} | " +
                   $"Speaking: {_isSpeaking}";
        }
    }
}
