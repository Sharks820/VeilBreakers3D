using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Replaces the sequential VERAInteractions() coroutine in TitleScreenAudio
    /// with weighted random selection, per-pattern cooldowns, and history exclusion.
    /// VERA interactions now play in randomized order so the title screen feels
    /// organic rather than predictable.
    /// </summary>
    public class VERATitleAudio : MonoBehaviour
    {
        // =============================================================================
        // INTERACTION DEFINITION
        // =============================================================================

        [System.Serializable]
        public class InteractionDef
        {
            public string id;
            public AudioClip veraClip;   // VERA's voice clip (may be null if demon-only)
            public AudioClip demonClip;  // Demon voice clip (may be null if VERA-only)
            public float weight = 1f;    // Selection weight
            public float cooldown = 30f; // Minimum seconds before this interaction can repeat

            [System.NonSerialized] public float lastPlayedTime = -999f;
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Interaction Pool")]
        [SerializeField] private InteractionDef[] _interactions;

        [Header("Timing")]
        [SerializeField] private float _globalCooldown = 12f;  // Min gap between ANY interaction
        [SerializeField] private float _initialDelayMin = 8f;
        [SerializeField] private float _initialDelayMax = 14f;
        [SerializeField] private float _gapMin = 8f;
        [SerializeField] private float _gapMax = 16f;
        [SerializeField] private float _retryDelay = 5f;       // Wait before retry when all on cooldown

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _veraVolume = 0.60f;
        [SerializeField, Range(0f, 1f)] private float _demonVolume = 0.70f;

        [Header("History")]
        [SerializeField] private int _historySize = 2; // How many recent interactions to exclude

        // =============================================================================
        // STATE
        // =============================================================================

        private readonly List<string> _recentHistory = new List<string>();
        private float _lastPlayTime = -999f;
        private Coroutine _interactionLoop;
        private AudioSource _voiceSource;
        private AudioSource _sfxSource;
        private bool _isRunning;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _voiceSource = CreateAudioSource(96, false);
            _sfxSource = CreateAudioSource(128, false);
        }

        private void OnDestroy()
        {
            StopInteractions();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Set the audio sources from the parent TitleScreenAudio (optional, uses own sources if not called).
        /// </summary>
        public void SetAudioSources(AudioSource voiceSource, AudioSource sfxSource)
        {
            if (voiceSource != null) _voiceSource = voiceSource;
            if (sfxSource != null) _sfxSource = sfxSource;
        }

        /// <summary>
        /// Start the randomized VERA interaction loop.
        /// </summary>
        public void StartInteractions()
        {
            if (_isRunning) return;
            _isRunning = true;
            _interactionLoop = StartCoroutine(InteractionLoop());
        }

        /// <summary>
        /// Stop the VERA interaction loop.
        /// </summary>
        public void StopInteractions()
        {
            _isRunning = false;
            if (_interactionLoop != null)
            {
                StopCoroutine(_interactionLoop);
                _interactionLoop = null;
            }
        }

        /// <summary>
        /// Select the next interaction using weighted random selection with cooldown and history exclusion.
        /// Returns null if all interactions are on cooldown.
        /// </summary>
        public InteractionDef SelectNext()
        {
            if (_interactions == null || _interactions.Length == 0)
                return null;

            float now = Time.unscaledTime;

            // Build candidate list: exclude items on cooldown or in recent history
            var candidates = new List<int>();
            var weights = new List<float>();

            for (int i = 0; i < _interactions.Length; i++)
            {
                var def = _interactions[i];

                // Check per-pattern cooldown
                if (now - def.lastPlayedTime < def.cooldown)
                    continue;

                // Check global cooldown
                if (now - _lastPlayTime < _globalCooldown)
                    continue;

                // Check history exclusion
                if (_recentHistory.Contains(def.id))
                    continue;

                candidates.Add(i);
                weights.Add(def.weight);
            }

            if (candidates.Count == 0)
                return null;

            int selectedIndex = WeightedRandom(weights);
            int interactionIndex = candidates[selectedIndex];
            var selected = _interactions[interactionIndex];

            // Update state
            selected.lastPlayedTime = now;
            _lastPlayTime = now;

            // Update history
            _recentHistory.Add(selected.id);
            while (_recentHistory.Count > _historySize)
            {
                _recentHistory.RemoveAt(0);
            }

            return selected;
        }

        /// <summary>
        /// Populate the interaction pool at runtime from procedurally generated clips.
        /// Called by TitleScreenAudio after clip generation.
        /// Only populates if _interactions is empty (allows Inspector override).
        ///
        /// Interaction mappings (from original VERAInteractions switch):
        ///   - "whisper_comment": VERA cry + demon "QUIET!" (weight=1.0, cooldown=25s)
        ///   - "demon_cackle": demon-only laugh (weight=0.7, cooldown=35s -- demon should feel rare)
        ///   - "mysterious_hint": VERA plea + demon growl (weight=0.5, cooldown=45s -- sparse)
        ///   - "vera_reacts": VERA "help me" only (weight=1.0, cooldown=20s)
        /// </summary>
        public void PopulateInteractions(
            AudioClip veraCry, AudioClip demonQuiet,
            AudioClip veraHelpMe,
            AudioClip veraPlease, AudioClip demonSilence)
        {
            // Only populate if not already configured in the Inspector
            if (_interactions != null && _interactions.Length > 0)
                return;

            _interactions = new InteractionDef[]
            {
                new InteractionDef
                {
                    id = "whisper_comment",
                    veraClip = veraCry,
                    demonClip = demonQuiet,
                    weight = 1.0f,
                    cooldown = 25f
                },
                new InteractionDef
                {
                    id = "demon_cackle",
                    veraClip = null,        // demon-only
                    demonClip = demonQuiet, // reuses demon bark
                    weight = 0.7f,
                    cooldown = 35f
                },
                new InteractionDef
                {
                    id = "mysterious_hint",
                    veraClip = veraPlease,
                    demonClip = demonSilence,
                    weight = 0.5f,
                    cooldown = 45f
                },
                new InteractionDef
                {
                    id = "vera_reacts",
                    veraClip = veraHelpMe,
                    demonClip = null,       // VERA-only
                    weight = 1.0f,
                    cooldown = 20f
                }
            };
        }

        // =============================================================================
        // INTERACTION LOOP
        // =============================================================================

        private IEnumerator InteractionLoop()
        {
            // Initial delay before first interaction
            yield return new WaitForSecondsRealtime(
                UnityEngine.Random.Range(_initialDelayMin, _initialDelayMax));

            while (_isRunning && enabled)
            {
                float master = GetMasterVolume();
                if (master < 0.01f)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }

                var interaction = SelectNext();

                if (interaction == null)
                {
                    // All on cooldown -- wait and retry
                    yield return new WaitForSecondsRealtime(_retryDelay);
                    continue;
                }

                // Play VERA clip
                if (interaction.veraClip != null)
                {
                    yield return PlayClip(_voiceSource, interaction.veraClip, _veraVolume * master);

                    // Brief pause between VERA and demon responses
                    if (interaction.demonClip != null)
                    {
                        yield return new WaitForSecondsRealtime(
                            UnityEngine.Random.Range(0.5f, 1.2f));
                    }
                }

                // Play demon clip
                if (interaction.demonClip != null)
                {
                    PlayClipOneShot(_sfxSource, interaction.demonClip, _demonVolume * master);
                }

                // Wait for gap before next interaction
                yield return new WaitForSecondsRealtime(
                    UnityEngine.Random.Range(_gapMin, _gapMax));
            }
        }

        // =============================================================================
        // AUDIO HELPERS
        // =============================================================================

        private IEnumerator PlayClip(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null) yield break;

            source.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            source.PlayOneShot(clip, volume);
            yield return new WaitForSecondsRealtime(clip.length);
            source.pitch = 1f;
        }

        private void PlayClipOneShot(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;

            source.pitch = UnityEngine.Random.Range(0.7f, 0.85f);
            source.PlayOneShot(clip, volume);
        }

        private AudioSource CreateAudioSource(int priority, bool loop, float volume = 1f)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.priority = priority;
            return src;
        }

        private static float GetMasterVolume()
        {
            if (Managers.SettingsManager.HasInstance)
            {
                var settings = Managers.SettingsManager.Instance.Settings;
                if (settings != null)
                {
                    if (settings.MuteAll) return 0f;
                    return Mathf.Clamp01(settings.MasterVolume);
                }
            }
            return 1f;
        }

        // =============================================================================
        // WEIGHTED RANDOM
        // =============================================================================

        private static int WeightedRandom(List<float> weights)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i];
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i];
                if (randomValue <= cumulative)
                    return i;
            }

            // Fallback to last item (shouldn't normally reach here)
            return weights.Count - 1;
        }
    }
}
