using System;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages persistent game settings using PlayerPrefs.
    /// Handles audio, graphics, controls, and accessibility settings.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static SettingsManager _instance;
        public static SettingsManager Instance => _instance;

        // =============================================================================
        // SETTINGS DATA
        // =============================================================================

        private GameSettings _settings;
        public GameSettings Settings => _settings;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<GameSettings> OnSettingsChanged;
        public event Action OnSettingsLoaded;
        public event Action OnSettingsSaved;

        // =============================================================================
        // PLAYERPREFS KEYS
        // =============================================================================

        private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
        private const string KEY_VOICE_VOLUME = "Settings_VoiceVolume";
        private const string KEY_MUTE_ALL = "Settings_MuteAll";

        private const string KEY_RESOLUTION_WIDTH = "Settings_ResolutionWidth";
        private const string KEY_RESOLUTION_HEIGHT = "Settings_ResolutionHeight";
        private const string KEY_FULLSCREEN_MODE = "Settings_FullscreenMode";
        private const string KEY_VSYNC = "Settings_VSync";
        private const string KEY_TARGET_FRAMERATE = "Settings_TargetFramerate";
        private const string KEY_QUALITY_LEVEL = "Settings_QualityLevel";
        private const string KEY_BRIGHTNESS = "Settings_Brightness";

        private const string KEY_CAMERA_SENSITIVITY = "Settings_CameraSensitivity";
        private const string KEY_INVERT_Y = "Settings_InvertY";
        private const string KEY_VIBRATION = "Settings_Vibration";
        private const string KEY_AUTO_AIM = "Settings_AutoAim";

        private const string KEY_SCREEN_SHAKE = "Settings_ScreenShake";
        private const string KEY_SUBTITLES = "Settings_Subtitles";
        private const string KEY_COLORBLIND_MODE = "Settings_ColorblindMode";
        private const string KEY_UI_SCALE = "Settings_UIScale";
        private const string KEY_TEXT_SIZE = "Settings_TextSize";
        private const string KEY_DAMAGE_NUMBERS = "Settings_DamageNumbers";

        private const string KEY_LANGUAGE = "Settings_Language";
        private const string KEY_FIRST_RUN = "Settings_FirstRun";

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

            _settings = new GameSettings();
            LoadSettings();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnApplicationQuit()
        {
            SaveSettings();
        }

        // =============================================================================
        // LOAD SETTINGS
        // =============================================================================

        public void LoadSettings()
        {
            // Audio
            _settings.MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 1f);
            _settings.MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.8f);
            _settings.SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1f);
            _settings.VoiceVolume = PlayerPrefs.GetFloat(KEY_VOICE_VOLUME, 1f);
            _settings.MuteAll = PlayerPrefs.GetInt(KEY_MUTE_ALL, 0) == 1;

            // Graphics
            _settings.ResolutionWidth = PlayerPrefs.GetInt(KEY_RESOLUTION_WIDTH, Screen.currentResolution.width);
            _settings.ResolutionHeight = PlayerPrefs.GetInt(KEY_RESOLUTION_HEIGHT, Screen.currentResolution.height);
            _settings.FullscreenMode = (FullScreenMode)PlayerPrefs.GetInt(KEY_FULLSCREEN_MODE, (int)FullScreenMode.FullScreenWindow);
            _settings.VSync = PlayerPrefs.GetInt(KEY_VSYNC, 1) == 1;
            _settings.TargetFramerate = PlayerPrefs.GetInt(KEY_TARGET_FRAMERATE, 60);
            _settings.QualityLevel = PlayerPrefs.GetInt(KEY_QUALITY_LEVEL, QualitySettings.GetQualityLevel());
            _settings.Brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 1f);

            // Controls
            _settings.CameraSensitivity = PlayerPrefs.GetFloat(KEY_CAMERA_SENSITIVITY, 1f);
            _settings.InvertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
            _settings.Vibration = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;
            _settings.AutoAim = PlayerPrefs.GetInt(KEY_AUTO_AIM, 1) == 1;

            // Accessibility
            _settings.ScreenShake = PlayerPrefs.GetFloat(KEY_SCREEN_SHAKE, 1f);
            _settings.Subtitles = PlayerPrefs.GetInt(KEY_SUBTITLES, 1) == 1;
            _settings.ColorblindMode = (ColorblindMode)PlayerPrefs.GetInt(KEY_COLORBLIND_MODE, 0);
            _settings.UIScale = PlayerPrefs.GetFloat(KEY_UI_SCALE, 1f);
            _settings.TextSize = PlayerPrefs.GetFloat(KEY_TEXT_SIZE, 1f);
            _settings.DamageNumbers = PlayerPrefs.GetInt(KEY_DAMAGE_NUMBERS, 1) == 1;

            // General
            _settings.Language = PlayerPrefs.GetString(KEY_LANGUAGE, "en");
            _settings.FirstRun = PlayerPrefs.GetInt(KEY_FIRST_RUN, 1) == 1;

            ApplySettings();
            OnSettingsLoaded?.Invoke();
            ErrorLogger.Settings("Settings loaded from PlayerPrefs");
        }

        // =============================================================================
        // SAVE SETTINGS
        // =============================================================================

        public void SaveSettings()
        {
            // Audio
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, _settings.MasterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, _settings.MusicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, _settings.SFXVolume);
            PlayerPrefs.SetFloat(KEY_VOICE_VOLUME, _settings.VoiceVolume);
            PlayerPrefs.SetInt(KEY_MUTE_ALL, _settings.MuteAll ? 1 : 0);

            // Graphics
            PlayerPrefs.SetInt(KEY_RESOLUTION_WIDTH, _settings.ResolutionWidth);
            PlayerPrefs.SetInt(KEY_RESOLUTION_HEIGHT, _settings.ResolutionHeight);
            PlayerPrefs.SetInt(KEY_FULLSCREEN_MODE, (int)_settings.FullscreenMode);
            PlayerPrefs.SetInt(KEY_VSYNC, _settings.VSync ? 1 : 0);
            PlayerPrefs.SetInt(KEY_TARGET_FRAMERATE, _settings.TargetFramerate);
            PlayerPrefs.SetInt(KEY_QUALITY_LEVEL, _settings.QualityLevel);
            PlayerPrefs.SetFloat(KEY_BRIGHTNESS, _settings.Brightness);

            // Controls
            PlayerPrefs.SetFloat(KEY_CAMERA_SENSITIVITY, _settings.CameraSensitivity);
            PlayerPrefs.SetInt(KEY_INVERT_Y, _settings.InvertY ? 1 : 0);
            PlayerPrefs.SetInt(KEY_VIBRATION, _settings.Vibration ? 1 : 0);
            PlayerPrefs.SetInt(KEY_AUTO_AIM, _settings.AutoAim ? 1 : 0);

            // Accessibility
            PlayerPrefs.SetFloat(KEY_SCREEN_SHAKE, _settings.ScreenShake);
            PlayerPrefs.SetInt(KEY_SUBTITLES, _settings.Subtitles ? 1 : 0);
            PlayerPrefs.SetInt(KEY_COLORBLIND_MODE, (int)_settings.ColorblindMode);
            PlayerPrefs.SetFloat(KEY_UI_SCALE, _settings.UIScale);
            PlayerPrefs.SetFloat(KEY_TEXT_SIZE, _settings.TextSize);
            PlayerPrefs.SetInt(KEY_DAMAGE_NUMBERS, _settings.DamageNumbers ? 1 : 0);

            // General
            PlayerPrefs.SetString(KEY_LANGUAGE, _settings.Language);
            PlayerPrefs.SetInt(KEY_FIRST_RUN, _settings.FirstRun ? 1 : 0);

            PlayerPrefs.Save();
            OnSettingsSaved?.Invoke();
            ErrorLogger.Settings("Settings saved to PlayerPrefs");
        }

        // =============================================================================
        // APPLY SETTINGS
        // =============================================================================

        public void ApplySettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        private void ApplyGraphicsSettings()
        {
            // Resolution and fullscreen
            Screen.SetResolution(
                _settings.ResolutionWidth,
                _settings.ResolutionHeight,
                _settings.FullscreenMode
            );

            // VSync
            QualitySettings.vSyncCount = _settings.VSync ? 1 : 0;

            // Target framerate (only applies when VSync is off)
            Application.targetFrameRate = _settings.VSync ? -1 : _settings.TargetFramerate;

            // Quality level
            QualitySettings.SetQualityLevel(_settings.QualityLevel, true);

            ErrorLogger.Settings($"Graphics applied: {_settings.ResolutionWidth}x{_settings.ResolutionHeight} {_settings.FullscreenMode}");
        }

        private void ApplyAudioSettings()
        {
            // Audio will be applied through AudioManager when it's available
            // For now, just log
            ErrorLogger.Settings($"Audio settings ready: Master={_settings.MasterVolume}, Music={_settings.MusicVolume}");
        }

        // =============================================================================
        // INDIVIDUAL SETTERS
        // =============================================================================

        // Audio
        public void SetMasterVolume(float value)
        {
            _settings.MasterVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetMusicVolume(float value)
        {
            _settings.MusicVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetSFXVolume(float value)
        {
            _settings.SFXVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetVoiceVolume(float value)
        {
            _settings.VoiceVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetMuteAll(bool muted)
        {
            _settings.MuteAll = muted;
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        // Graphics
        public void SetResolution(int width, int height)
        {
            _settings.ResolutionWidth = width;
            _settings.ResolutionHeight = height;
            ApplyGraphicsSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetFullscreenMode(FullScreenMode mode)
        {
            _settings.FullscreenMode = mode;
            ApplyGraphicsSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetVSync(bool enabled)
        {
            _settings.VSync = enabled;
            ApplyGraphicsSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetTargetFramerate(int fps)
        {
            _settings.TargetFramerate = Mathf.Clamp(fps, 30, 240);
            ApplyGraphicsSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetQualityLevel(int level)
        {
            _settings.QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            ApplyGraphicsSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetBrightness(float value)
        {
            _settings.Brightness = Mathf.Clamp(value, 0.5f, 1.5f);
            OnSettingsChanged?.Invoke(_settings);
        }

        // Controls
        public void SetCameraSensitivity(float value)
        {
            _settings.CameraSensitivity = Mathf.Clamp(value, 0.1f, 3f);
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetInvertY(bool inverted)
        {
            _settings.InvertY = inverted;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetVibration(bool enabled)
        {
            _settings.Vibration = enabled;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetAutoAim(bool enabled)
        {
            _settings.AutoAim = enabled;
            OnSettingsChanged?.Invoke(_settings);
        }

        // Accessibility
        public void SetScreenShake(float intensity)
        {
            _settings.ScreenShake = Mathf.Clamp01(intensity);
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetSubtitles(bool enabled)
        {
            _settings.Subtitles = enabled;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetColorblindMode(ColorblindMode mode)
        {
            _settings.ColorblindMode = mode;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetUIScale(float scale)
        {
            _settings.UIScale = Mathf.Clamp(scale, 0.75f, 1.5f);
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetTextSize(float size)
        {
            _settings.TextSize = Mathf.Clamp(size, 0.75f, 1.5f);
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetDamageNumbers(bool enabled)
        {
            _settings.DamageNumbers = enabled;
            OnSettingsChanged?.Invoke(_settings);
        }

        // General
        public void SetLanguage(string languageCode)
        {
            _settings.Language = languageCode;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetFirstRun(bool isFirstRun)
        {
            _settings.FirstRun = isFirstRun;
            SaveSettings();
        }

        // =============================================================================
        // RESET
        // =============================================================================

        public void ResetToDefaults()
        {
            _settings = new GameSettings();
            ApplySettings();
            SaveSettings();
            ErrorLogger.Settings("Settings reset to defaults");
        }

        public void ResetAudioToDefaults()
        {
            _settings.MasterVolume = 1f;
            _settings.MusicVolume = 0.8f;
            _settings.SFXVolume = 1f;
            _settings.VoiceVolume = 1f;
            _settings.MuteAll = false;
            ApplyAudioSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void ResetGraphicsToDefaults()
        {
            _settings.ResolutionWidth = Screen.currentResolution.width;
            _settings.ResolutionHeight = Screen.currentResolution.height;
            _settings.FullscreenMode = FullScreenMode.FullScreenWindow;
            _settings.VSync = true;
            _settings.TargetFramerate = 60;
            _settings.QualityLevel = QualitySettings.GetQualityLevel();
            _settings.Brightness = 1f;
            ApplyGraphicsSettings();
            OnSettingsChanged?.Invoke(_settings);
        }

        public void ResetControlsToDefaults()
        {
            _settings.CameraSensitivity = 1f;
            _settings.InvertY = false;
            _settings.Vibration = true;
            _settings.AutoAim = true;
            OnSettingsChanged?.Invoke(_settings);
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        public Resolution[] GetAvailableResolutions()
        {
            return Screen.resolutions;
        }

        public string[] GetQualityLevelNames()
        {
            return QualitySettings.names;
        }
    }

    // =============================================================================
    // SETTINGS DATA CLASS
    // =============================================================================

    [Serializable]
    public class GameSettings
    {
        // Audio
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SFXVolume = 1f;
        public float VoiceVolume = 1f;
        public bool MuteAll = false;

        // Graphics
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public FullScreenMode FullscreenMode = FullScreenMode.FullScreenWindow;
        public bool VSync = true;
        public int TargetFramerate = 60;
        public int QualityLevel = 2;
        public float Brightness = 1f;

        // Controls
        public float CameraSensitivity = 1f;
        public bool InvertY = false;
        public bool Vibration = true;
        public bool AutoAim = true;

        // Accessibility
        public float ScreenShake = 1f;
        public bool Subtitles = true;
        public ColorblindMode ColorblindMode = ColorblindMode.None;
        public float UIScale = 1f;
        public float TextSize = 1f;
        public bool DamageNumbers = true;

        // General
        public string Language = "en";
        public bool FirstRun = true;
    }

    public enum ColorblindMode
    {
        None = 0,
        Protanopia = 1,    // Red-blind
        Deuteranopia = 2,  // Green-blind
        Tritanopia = 3     // Blue-blind
    }
}
