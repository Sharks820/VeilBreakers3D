using System.Collections;
using UnityEngine;
using VeilBreakers.Managers;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA Title Screen audio controller. Handles:
    /// - Ambient music fade-in
    /// - Random demon laughs at intervals
    /// - Soft girl crying atmospheric loop
    /// - Menu button hover/click sounds
    ///
    /// Attach to the same GameObject as TitleScreenVFX.
    /// Audio clips are loaded from Resources/Audio/ at runtime.
    /// </summary>
    public class TitleScreenAudio : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private float _musicFadeInDuration = 3f;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.4f;

        [Header("Demon Laughs")]
        [SerializeField] private float _laughIntervalMin = 12f;
        [SerializeField] private float _laughIntervalMax = 30f;
        [SerializeField, Range(0f, 1f)] private float _laughVolume = 0.35f;

        [Header("Girl Crying")]
        [SerializeField] private float _cryIntervalMin = 20f;
        [SerializeField] private float _cryIntervalMax = 45f;
        [SerializeField, Range(0f, 1f)] private float _cryVolume = 0.15f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioClip _musicClip;
        private AudioClip _demonLaughClip;
        private AudioClip _girlCryingClip;
        private bool _initialized;

        private void Start()
        {
            StartCoroutine(InitializeDeferred());
        }

        private IEnumerator InitializeDeferred()
        {
            // Wait one frame for other systems to init
            yield return null;

            LoadAudioClips();
            SetupAudioSources();

            _initialized = true;

            // Start music fade-in
            if (_musicClip != null)
            {
                StartCoroutine(FadeMusicIn());
            }

            // Start random demon laughs
            if (_demonLaughClip != null)
            {
                StartCoroutine(RandomDemonLaughs());
            }

            // Start random girl crying
            if (_girlCryingClip != null)
            {
                StartCoroutine(RandomGirlCrying());
            }
        }

        private void LoadAudioClips()
        {
            _musicClip = Resources.Load<AudioClip>("Audio/Music/title_screen_music_dark_fantasy_menu");
            _demonLaughClip = Resources.Load<AudioClip>("Audio/SFX/demon_laugh");
            _girlCryingClip = Resources.Load<AudioClip>("Audio/SFX/girl_crying_soft");

            if (_musicClip == null) Debug.LogWarning("[TitleScreenAudio] Music clip not found at Resources/Audio/Music/title_screen_music_dark_fantasy_menu");
            if (_demonLaughClip == null) Debug.LogWarning("[TitleScreenAudio] Demon laugh clip not found at Resources/Audio/SFX/demon_laugh");
            if (_girlCryingClip == null) Debug.LogWarning("[TitleScreenAudio] Girl crying clip not found at Resources/Audio/SFX/girl_crying_soft");
        }

        private void SetupAudioSources()
        {
            // Music source (looping)
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0f;
            _musicSource.priority = 64;

            // SFX source (one-shots)
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.priority = 128;
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

        private IEnumerator FadeMusicIn()
        {
            _musicSource.clip = _musicClip;
            _musicSource.volume = 0f;
            _musicSource.Play();

            float elapsed = 0f;
            float targetVolume = _musicVolume * GetMasterVolume();
            while (elapsed < _musicFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / _musicFadeInDuration);
                yield return null;
            }
            _musicSource.volume = targetVolume;
        }

        private IEnumerator RandomDemonLaughs()
        {
            // Initial delay before first laugh
            yield return new WaitForSecondsRealtime(Random.Range(8f, 15f));

            while (enabled)
            {
                float vol = _laughVolume * GetMasterVolume();
                if (vol > 0.01f)
                {
                    _sfxSource.PlayOneShot(_demonLaughClip, vol);
                }

                float interval = Random.Range(_laughIntervalMin, _laughIntervalMax);
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private IEnumerator RandomGirlCrying()
        {
            // Initial delay
            yield return new WaitForSecondsRealtime(Random.Range(15f, 25f));

            while (enabled)
            {
                float vol = _cryVolume * GetMasterVolume();
                if (vol > 0.01f)
                {
                    _sfxSource.PlayOneShot(_girlCryingClip, vol);
                }

                float interval = Random.Range(_cryIntervalMin, _cryIntervalMax);
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_musicSource != null && _musicSource.isPlaying)
            {
                _musicSource.Stop();
            }
        }

        private void OnDestroy()
        {
            // AudioSources are auto-destroyed with the GameObject
            _musicClip = null;
            _demonLaughClip = null;
            _girlCryingClip = null;
        }
    }
}
