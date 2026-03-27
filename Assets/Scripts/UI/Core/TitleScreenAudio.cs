using System.Collections;
using UnityEngine;
using VeilBreakers.Managers;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Dark fantasy title screen audio. ALL music is procedurally generated — no external files needed.
    /// Layers: deep sub-bass drone, eerie mid-frequency pad, distant metallic bells,
    /// wind/breath texture, and occasional low rumbles.
    /// Plus: rare demon laughs, VERA whisper interactions.
    /// </summary>
    public class TitleScreenAudio : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private float _musicFadeInDuration = 5f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.40f;

        [Header("Demon Laughs")]
        [SerializeField] private float _laughIntervalMin = 30f;
        [SerializeField] private float _laughIntervalMax = 60f;
        [SerializeField, Range(0f, 1f)] private float _laughVolume = 0.50f;

        [Header("VERA Interactions")]
        [SerializeField] private float _veraIntervalMin = 18f;
        [SerializeField] private float _veraIntervalMax = 40f;
        [SerializeField, Range(0f, 1f)] private float _veraVolume = 0.60f;
        [SerializeField, Range(0f, 1f)] private float _demonBarkVolume = 0.70f;

        // Audio sources
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _voiceSource;
        private AudioSource _bellSource;    // For distant bells layer
        private AudioSource _rumbleSource;  // For low rumbles

        // Generated music clips
        private AudioClip _dronePad;         // Deep sub-bass + eerie mid drone (looping)
        private AudioClip _windTexture;      // Wind/breath texture (looping)
        private AudioClip _distantBell;      // Single distant bell hit
        private AudioClip _lowRumble;        // Ominous low rumble

        // Generated SFX clips
        private AudioClip _demonLaughGen;    // Procedural demon laugh
        private AudioClip _veraHelpMe;
        private AudioClip _veraCry;
        private AudioClip _veraPlease;
        private AudioClip _demonQuiet;
        private AudioClip _demonSilence;

        private bool _initialized;
        private int _interactionIndex;

        private void Start()
        {
            StartCoroutine(InitializeDeferred());
        }

        private IEnumerator InitializeDeferred()
        {
            yield return null;

            GenerateAllAudio();
            SetupAudioSources();

            _initialized = true;

            StartCoroutine(FadeMusicIn());
            StartCoroutine(WindLayer());
            StartCoroutine(BellLayer());
            StartCoroutine(RumbleLayer());
            StartCoroutine(RandomDemonLaughs());
            StartCoroutine(VERAInteractions());
        }

        // =============================================================================
        // AUDIO GENERATION — All music is built from scratch
        // =============================================================================

        private void GenerateAllAudio()
        {
            // --- MUSIC LAYERS ---
            _dronePad = GenerateDarkDrone(30f);           // 30-second looping drone
            _windTexture = GenerateWindTexture(20f);      // 20-second looping wind
            _distantBell = GenerateDistantBell(4f);       // Single 4-second bell hit
            _lowRumble = GenerateLowRumble(6f);           // 6-second rumble

            // --- DEMON ---
            _demonLaughGen = GenerateDemonLaugh(2.5f);

            // --- VERA WHISPERS ---
            _veraHelpMe = GenerateWhisper(0.9f, 380f, 300f, 0.12f);
            _veraCry = GenerateWhisper(1.4f, 260f, 180f, 0.08f);
            _veraPlease = GenerateWhisper(0.5f, 420f, 360f, 0.10f);

            // --- DEMON BARKS ---
            _demonQuiet = GenerateDemonBark(0.35f, 115f, 0.30f);
            _demonSilence = GenerateDemonBark(0.5f, 85f, 0.22f);
        }

        private void SetupAudioSources()
        {
            _musicSource = CreateSource(64, true, 0f);  // Starts silent, FadeMusicIn ramps up
            _sfxSource = CreateSource(128, false);       // Full volume — PlayOneShot controls level
            _voiceSource = CreateSource(96, false);
            _bellSource = CreateSource(110, false);
            _rumbleSource = CreateSource(80, false);
        }

        private AudioSource CreateSource(int priority, bool loop, float volume = 1f)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.priority = priority;
            return src;
        }

        private float GetMasterVolume()
        {
            if (SettingsManager.HasInstance)
            {
                var settings = SettingsManager.Instance.Settings;
                if (settings != null)
                {
                    if (settings.MuteAll) return 0f;
                    return Mathf.Clamp01(settings.MasterVolume);
                }
            }
            return 1f;
        }

        // =============================================================================
        // MUSIC PLAYBACK — Layered ambient
        // =============================================================================

        private IEnumerator FadeMusicIn()
        {
            // Start the main drone pad
            _musicSource.clip = _dronePad;
            _musicSource.volume = 0f;
            _musicSource.Play();

            float elapsed = 0f;
            float target = _musicVolume * GetMasterVolume();
            while (elapsed < _musicFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, target, elapsed / _musicFadeInDuration);
                yield return null;
            }
            _musicSource.volume = target;
        }

        /// <summary>Wind/breath texture fades in after a delay, loops underneath the drone.</summary>
        private IEnumerator WindLayer()
        {
            yield return new WaitForSecondsRealtime(3f);

            var windSrc = gameObject.AddComponent<AudioSource>();
            windSrc.playOnAwake = false;
            windSrc.loop = true;
            windSrc.spatialBlend = 0f;
            windSrc.clip = _windTexture;
            windSrc.volume = 0f;
            windSrc.Play();

            // Slow fade in
            float target = _musicVolume * 0.3f * GetMasterVolume();
            float elapsed = 0f;
            while (elapsed < 6f)
            {
                elapsed += Time.unscaledDeltaTime;
                windSrc.volume = Mathf.Lerp(0f, target, elapsed / 6f);
                yield return null;
            }
        }

        /// <summary>Distant bell hits at random intervals — eerie, reverberant.</summary>
        private IEnumerator BellLayer()
        {
            yield return new WaitForSecondsRealtime(Random.Range(8f, 15f));

            while (enabled)
            {
                float vol = _musicVolume * 0.25f * GetMasterVolume();
                if (vol > 0.005f && _distantBell != null)
                {
                    _bellSource.pitch = Random.Range(0.8f, 1.1f);
                    _bellSource.PlayOneShot(_distantBell, vol);
                }
                yield return new WaitForSecondsRealtime(Random.Range(12f, 28f));
            }
        }

        /// <summary>Low ominous rumbles at long intervals — builds dread.</summary>
        private IEnumerator RumbleLayer()
        {
            yield return new WaitForSecondsRealtime(Random.Range(18f, 30f));

            while (enabled)
            {
                float vol = _musicVolume * 0.35f * GetMasterVolume();
                if (vol > 0.005f && _lowRumble != null)
                {
                    _rumbleSource.pitch = Random.Range(0.6f, 0.85f);
                    _rumbleSource.PlayOneShot(_lowRumble, vol);
                }
                yield return new WaitForSecondsRealtime(Random.Range(25f, 55f));
            }
        }

        // =============================================================================
        // DEMON LAUGHS — Rare, menacing
        // =============================================================================

        private IEnumerator RandomDemonLaughs()
        {
            yield return new WaitForSecondsRealtime(Random.Range(15f, 25f));

            while (enabled)
            {
                float vol = _laughVolume * GetMasterVolume();
                if (vol > 0.01f && _demonLaughGen != null)
                {
                    _sfxSource.pitch = Random.Range(0.65f, 0.8f);
                    _sfxSource.PlayOneShot(_demonLaughGen, vol);
                }
                yield return new WaitForSecondsRealtime(Random.Range(_laughIntervalMin, _laughIntervalMax));
                _sfxSource.pitch = 1f;
            }
        }

        // =============================================================================
        // VERA INTERACTIONS
        // =============================================================================

        private IEnumerator VERAInteractions()
        {
            yield return new WaitForSecondsRealtime(Random.Range(8f, 14f));

            while (enabled)
            {
                float master = GetMasterVolume();
                if (master > 0.01f)
                {
                    int pattern = _interactionIndex % 4;
                    _interactionIndex++;

                    switch (pattern)
                    {
                        case 0: // VERA cries → Demon "QUIET!"
                            yield return PlayVERA(_veraCry, master);
                            yield return new WaitForSecondsRealtime(Random.Range(0.8f, 1.5f));
                            PlayDemon(_demonQuiet, master);
                            break;
                        case 1: // VERA "help me" → silence
                            yield return PlayVERA(_veraHelpMe, master);
                            break;
                        case 2: // VERA plea → Demon growl
                            yield return PlayVERA(_veraPlease, master);
                            yield return new WaitForSecondsRealtime(Random.Range(0.4f, 0.9f));
                            PlayDemon(_demonSilence, master);
                            break;
                        case 3: // Just ambient — VERA too afraid
                            break;
                    }
                }
                yield return new WaitForSecondsRealtime(Random.Range(_veraIntervalMin, _veraIntervalMax));
            }
        }

        private IEnumerator PlayVERA(AudioClip clip, float master)
        {
            if (_voiceSource == null || clip == null) yield break;
            _voiceSource.pitch = Random.Range(0.95f, 1.05f);
            _voiceSource.PlayOneShot(clip, _veraVolume * master);
            yield return new WaitForSecondsRealtime(clip.length);
            _voiceSource.pitch = 1f;
        }

        private void PlayDemon(AudioClip clip, float master)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.pitch = Random.Range(0.7f, 0.85f);
            _sfxSource.PlayOneShot(clip, _demonBarkVolume * master);
        }

        // =============================================================================
        // PROCEDURAL MUSIC GENERATION
        // =============================================================================

        /// <summary>
        /// 30-second looping dark drone. Layers:
        /// - Deep sub-bass (35-45 Hz) with slow LFO
        /// - Eerie mid pad (110-165 Hz) with detuned oscillators
        /// - High ethereal shimmer (880 Hz) very quiet
        /// - Dark fifth interval (power chord feel)
        /// </summary>
        private static AudioClip GenerateDarkDrone(float duration)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            int fadeSamples = sr * 3; // 3-second crossfade for seamless loop
            var clip = AudioClip.Create("dark_drone", count, 2, sr, false); // Stereo
            float[] samples = new float[count * 2];

            // Use time-based phase calculation (no accumulator drift = perfect loop)
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;

                // LFOs: use frequencies that divide evenly into duration for seamless loop
                // 1/duration Hz = exactly 1 cycle per loop, 2/duration = 2 cycles, etc.
                float lfo = Mathf.Sin(2f * Mathf.PI * t / duration);          // 1 cycle per loop
                float lfo2 = Mathf.Sin(2f * Mathf.PI * 2f * t / duration);    // 2 cycles per loop

                // Layer 1: Sub-bass drone (D1 = ~37 Hz) — deep, felt more than heard
                float subFreq = 36.7f;
                float sub = Mathf.Sin(2f * Mathf.PI * subFreq * t + lfo * 0.5f) * 0.10f;

                // Layer 2: Dark fifth (A1 = ~55 Hz) — barely audible power
                float fifth = Mathf.Sin(2f * Mathf.PI * 55f * t + lfo * 0.3f) * 0.05f;

                // Layer 3: Eerie pad (detuned pair — slow, ethereal)
                float pad1 = Mathf.Sin(2f * Mathf.PI * 146.8f * t + lfo * 1.0f) * 0.025f;
                float pad2 = Mathf.Sin(2f * Mathf.PI * 148.2f * t + lfo2 * 0.7f) * 0.025f;
                float padHarm = Mathf.Sin(2f * Mathf.PI * 293.6f * t) * 0.008f;

                // Layer 4: High shimmer — very faint, ghostly
                float shimmerPulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3f * t / duration);
                float shimmer = Mathf.Sin(2f * Mathf.PI * 587f * t + lfo2 * 2f) * 0.006f * shimmerPulse;

                // Combine
                float mono = sub + fifth + pad1 + pad2 + padHarm + shimmer;

                // Stereo spread
                float left = mono + pad1 * 0.03f - pad2 * 0.03f;
                float right = mono - pad1 * 0.03f + pad2 * 0.03f;

                // Crossfade: blend end back into beginning for seamless loop
                if (i > count - fadeSamples)
                {
                    float fadeProgress = (float)(i - (count - fadeSamples)) / fadeSamples;
                    float fadeOut = 1f - fadeProgress;
                    float fadeIn = fadeProgress;

                    // Generate the "beginning" samples for crossfade
                    int mirrorI = (int)(fadeProgress * fadeSamples);
                    float mt = (float)mirrorI / sr;
                    float mLfo = Mathf.Sin(2f * Mathf.PI * mt / duration);
                    float mLfo2 = Mathf.Sin(2f * Mathf.PI * 2f * mt / duration);
                    float mSub = Mathf.Atan(Mathf.Sin(2f * Mathf.PI * 36.7f * mt + mLfo * 0.8f) * 0.25f * 3f) / 2f;
                    float mFifth = Mathf.Sin(2f * Mathf.PI * 55f * mt + mLfo * 0.5f) * 0.12f;
                    float mPad1 = Mathf.Sin(2f * Mathf.PI * 146.8f * mt + mLfo * 1.5f) * 0.06f;
                    float mPad2 = Mathf.Sin(2f * Mathf.PI * 148.2f * mt + mLfo2 * 1.0f) * 0.06f;
                    float mMono = mSub + mFifth + mPad1 + mPad2;
                    float mLeft = mMono + mPad1 * 0.03f - mPad2 * 0.03f;
                    float mRight = mMono - mPad1 * 0.03f + mPad2 * 0.03f;

                    left = left * fadeOut + mLeft * fadeIn;
                    right = right * fadeOut + mRight * fadeIn;
                }

                left = Mathf.Clamp(left, -0.95f, 0.95f);
                right = Mathf.Clamp(right, -0.95f, 0.95f);

                samples[i * 2] = left;
                samples[i * 2 + 1] = right;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Filtered noise that sounds like distant wind through ruined halls.
        /// </summary>
        private static AudioClip GenerateWindTexture(float duration)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            var clip = AudioClip.Create("wind_texture", count, 2, sr, false);
            float[] samples = new float[count * 2];

            // Simple one-pole low-pass filter state
            float filtL = 0f, filtR = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;

                // LFO modulates filter cutoff (breathing wind)
                float breathLfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.12f * t);
                float cutoff = Mathf.Lerp(0.002f, 0.015f, breathLfo); // Very low cutoff = dark wind

                // Volume envelope — swells and fades
                float volLfo = 0.3f + 0.7f * Mathf.Sin(2f * Mathf.PI * 0.07f * t);
                volLfo = Mathf.Max(0f, volLfo);

                // White noise
                float noiseL = (Random.value * 2f - 1f) * 0.15f * volLfo;
                float noiseR = (Random.value * 2f - 1f) * 0.15f * volLfo;

                // One-pole low-pass filter
                filtL += cutoff * (noiseL - filtL);
                filtR += cutoff * (noiseR - filtR);

                samples[i * 2] = filtL;
                samples[i * 2 + 1] = filtR;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Single distant bell strike — like a cathedral bell heard from far away.
        /// Metallic with long reverb tail.
        /// </summary>
        private static AudioClip GenerateDistantBell(float duration)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            var clip = AudioClip.Create("distant_bell", count, 1, sr, false);
            float[] samples = new float[count];

            // Bell frequencies (inharmonic partials — real bells aren't harmonic)
            float[] freqs = { 220f, 277f, 349f, 440f, 554f, 698f };
            float[] amps = { 0.3f, 0.2f, 0.15f, 0.12f, 0.08f, 0.05f };
            float[] decays = { 3.5f, 2.8f, 2.2f, 1.8f, 1.2f, 0.8f };
            float[] phases = new float[6];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;
                float sample = 0f;

                for (int p = 0; p < 6; p++)
                {
                    float env = Mathf.Exp(-t / decays[p]);
                    phases[p] += freqs[p] / sr;
                    sample += Mathf.Sin(2f * Mathf.PI * phases[p]) * amps[p] * env;
                }

                // Distance effect: muffle high frequencies over time
                float distanceMuffle = Mathf.Exp(-t * 0.5f);
                samples[i] = sample * distanceMuffle * 0.5f;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Low ominous rumble — earthquake/distant thunder feel.
        /// </summary>
        private static AudioClip GenerateLowRumble(float duration)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            var clip = AudioClip.Create("low_rumble", count, 1, sr, false);
            float[] samples = new float[count];

            float phase = 0f;
            float filtState = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;
                float progress = t / duration;

                // Envelope: slow attack, long decay
                float env;
                if (progress < 0.15f) env = progress / 0.15f;
                else env = Mathf.Pow(1f - (progress - 0.15f) / 0.85f, 1.5f);

                // Very low frequency oscillation (20-30 Hz)
                float freq = Mathf.Lerp(22f, 30f, Mathf.Sin(2f * Mathf.PI * 0.3f * t) * 0.5f + 0.5f);
                phase += freq / sr;
                float tone = Mathf.Sin(2f * Mathf.PI * phase) * 0.4f;

                // Add rumbling noise
                float noise = (Random.value * 2f - 1f) * 0.2f;
                filtState += 0.003f * (noise - filtState); // Heavy low-pass

                samples[i] = (tone + filtState) * env * 0.6f;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Procedural demon laugh — guttural, low, with pitch wobble.
        /// </summary>
        private static AudioClip GenerateDemonLaugh(float duration)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            var clip = AudioClip.Create("demon_laugh", count, 1, sr, false);
            float[] samples = new float[count];

            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;
                float progress = t / duration;

                // Envelope: pulsing "ha ha ha" pattern
                float pulseRate = 4f; // 4 "ha"s per second
                float pulse = Mathf.Abs(Mathf.Sin(2f * Mathf.PI * pulseRate * t));
                pulse = Mathf.Pow(pulse, 0.5f); // Sharper pulses

                // Overall envelope
                float env;
                if (progress < 0.05f) env = progress / 0.05f;
                else env = Mathf.Pow(1f - progress, 1.5f);

                // Pitch: low with wobble
                float baseFreq = 95f + Mathf.Sin(2f * Mathf.PI * 2f * t) * 15f;
                phase += baseFreq / sr;

                // Distorted wave
                float raw = Mathf.Sin(2f * Mathf.PI * phase);
                float distorted = Mathf.Sign(raw) * Mathf.Pow(Mathf.Abs(raw), 0.4f);

                // Add harmonics for guttural quality
                float harm2 = Mathf.Sin(2f * Mathf.PI * phase * 2f) * 0.3f;
                float harm3 = Mathf.Sin(2f * Mathf.PI * phase * 3f) * 0.15f;

                // Noise for breathiness
                float noise = (Random.value * 2f - 1f) * 0.1f;

                samples[i] = (distorted + harm2 + harm3 + noise) * env * pulse * 0.3f;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        // =============================================================================
        // VOICE GENERATION
        // =============================================================================

        private static AudioClip GenerateWhisper(float duration, float startFreq, float endFreq, float volume)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            var clip = AudioClip.Create($"vera_{startFreq}", count, 1, sr, false);
            float[] samples = new float[count];

            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;
                float progress = t / duration;

                float freq = Mathf.Lerp(startFreq, endFreq, progress);

                // Envelope
                float env;
                if (progress < 0.08f) env = progress / 0.08f;
                else if (progress > 0.7f) env = (1f - progress) / 0.3f;
                else env = 1f;

                // Tremolo (scared shaking)
                float tremolo = 1f + Mathf.Sin(2f * Mathf.PI * 5.5f * t) * 0.3f;

                // Breathy noise
                float noise = (Random.value * 2f - 1f) * 0.18f;

                // Voice formants
                float tone = Mathf.Sin(2f * Mathf.PI * phase);
                float h2 = Mathf.Sin(2f * Mathf.PI * phase * 2f) * 0.35f;
                float h3 = Mathf.Sin(2f * Mathf.PI * phase * 3f) * 0.12f;

                samples[i] = (tone + h2 + h3 + noise) * volume * env * tremolo;
                phase += freq / sr;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateDemonBark(float duration, float baseFreq, float volume)
        {
            int sr = 44100;
            int count = (int)(sr * duration);
            var clip = AudioClip.Create($"demon_{baseFreq}", count, 1, sr, false);
            float[] samples = new float[count];

            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sr;
                float progress = t / duration;

                // Sharp attack, fast decay
                float env;
                if (progress < 0.04f) env = progress / 0.04f;
                else env = Mathf.Pow(1f - progress, 2.5f);

                // Growl FM
                float freqMod = 1f + Mathf.Sin(2f * Mathf.PI * 35f * t) * 0.2f;
                float freq = baseFreq * freqMod;

                // Harsh clipped wave
                float raw = Mathf.Sin(2f * Mathf.PI * phase);
                float distorted = Mathf.Sign(raw) * Mathf.Pow(Mathf.Abs(raw), 0.25f);

                float sub = Mathf.Sin(2f * Mathf.PI * phase * 0.5f) * 0.4f;
                float noise = (Random.value * 2f - 1f) * 0.25f;

                samples[i] = (distorted + sub + noise) * volume * env;
                phase += freq / sr;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_musicSource != null && _musicSource.isPlaying) _musicSource.Stop();
        }

        private void OnDestroy()
        {
            DestroyClip(ref _dronePad);
            DestroyClip(ref _windTexture);
            DestroyClip(ref _distantBell);
            DestroyClip(ref _lowRumble);
            DestroyClip(ref _demonLaughGen);
            DestroyClip(ref _veraHelpMe);
            DestroyClip(ref _veraCry);
            DestroyClip(ref _veraPlease);
            DestroyClip(ref _demonQuiet);
            DestroyClip(ref _demonSilence);
        }

        private static void DestroyClip(ref AudioClip clip)
        {
            if (clip != null) { Destroy(clip); clip = null; }
        }
    }
}
