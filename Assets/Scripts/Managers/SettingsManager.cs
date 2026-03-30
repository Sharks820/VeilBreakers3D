using System;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Manages persistent game settings using PlayerPrefs.
    /// Handles audio, graphics, controls, and accessibility settings.
    /// </summary>
    public class SettingsManager : SingletonMonoBehaviour<SettingsManager>
    {
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

        private const string KEY_GAME_SETTINGS = "GameSettings_JSON";

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        protected override void OnSingletonAwake()
        {
            _settings = new GameSettings();
            LoadSettings();
        }
        
        protected override void OnApplicationQuit()
        {
            SaveSettings();
            base.OnApplicationQuit();
        }

        // =============================================================================
        // LOAD SETTINGS
        // =============================================================================

        public void LoadSettings()
        {
            if (PlayerPrefs.HasKey(KEY_GAME_SETTINGS))
            {
                string json = PlayerPrefs.GetString(KEY_GAME_SETTINGS);
                try
                {
                    _settings = JsonUtility.FromJson<GameSettings>(json); // VB-IGNORE SEC-03 SEC-14 -- validated: try/catch + null check below, PlayerPrefs is trusted local storage
                    // Handle case where new settings were added and aren't in the saved JSON
                    if (_settings == null)
                    {
                        _settings = new GameSettings();
                        Debug.LogWarning("[SettingsManager] Failed to parse settings JSON, resetting to default.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SettingsManager] Error loading settings: {ex.Message}. Resetting to default.");
                    _settings = new GameSettings();
                }
            }
            else
            {
                _settings = new GameSettings();
                Debug.Log("[SettingsManager] No saved settings found, using defaults.");
            }

            ApplySettings();
            OnSettingsLoaded?.Invoke();
            ErrorLogger.Settings("Settings loaded from PlayerPrefs");
        }

        // =============================================================================
        // SAVE SETTINGS
        // =============================================================================

        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(_settings, true);
                PlayerPrefs.SetString(KEY_GAME_SETTINGS, json);
                PlayerPrefs.Save();

                OnSettingsSaved?.Invoke();
                ErrorLogger.Settings("Settings saved to PlayerPrefs");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsManager] Failed to save settings: {ex.Message}");
            }
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
            // Check if resolution is supported before applying
            bool resolutionSupported = false;
            foreach (var res in Screen.resolutions)
            {
                if (res.width == _settings.ResolutionWidth && res.height == _settings.ResolutionHeight)
                {
                    resolutionSupported = true;
                    break;
                }
            }
            if (!resolutionSupported)
            {
                _settings.ResolutionWidth = Screen.currentResolution.width;
                _settings.ResolutionHeight = Screen.currentResolution.height;
            }
            
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
            if (_settings.QualityLevel >= 0 && _settings.QualityLevel < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(_settings.QualityLevel, true);
            }

            ErrorLogger.Settings($"Graphics applied: {_settings.ResolutionWidth}x{_settings.ResolutionHeight} {_settings.FullscreenMode}");
        }

        private void ApplyAudioSettings()
        {
            if (Audio.AudioManager.HasInstance)
            {
                var audioManager = Audio.AudioManager.Instance;
                audioManager.SetMasterVolume(_settings.MuteAll ? 0f : _settings.MasterVolume);
                audioManager.SetMusicVolume(_settings.MusicVolume);
                audioManager.SetSFXVolume(_settings.SFXVolume);
                audioManager.SetVoiceVolume(_settings.VoiceVolume);
                ErrorLogger.Settings($"Audio settings applied: Master={_settings.MasterVolume}, Music={_settings.MusicVolume}");
            }
            else
            {
                ErrorLogger.Settings($"Audio settings ready, waiting for AudioManager.");
            }
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

        public void SetHealthBars(bool enabled)
        {
            _settings.HealthBars = enabled;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetTutorialTips(bool enabled)
        {
            _settings.TutorialTips = enabled;
            OnSettingsChanged?.Invoke(_settings);
        }

        public void SetDifficulty(int level)
        {
            _settings.Difficulty = Mathf.Clamp(level, 0, 3);
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
        public bool HealthBars = true;
        public bool TutorialTips = true;

        // Gameplay
        public int Difficulty = 1;  // 0=Story, 1=Normal, 2=Hard, 3=Nightmare

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
