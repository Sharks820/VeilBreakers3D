using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.UI.Controls;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controller for the settings panel UI.
    /// Handles Audio, Graphics, and Controls settings.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kPrefMasterVolume = "Audio_Master";
        private const string kPrefMusicVolume = "Audio_Music";
        private const string kPrefSFXVolume = "Audio_SFX";
        private const string kPrefVoiceVolume = "Audio_Voice";
        private const string kPrefMuteAll = "Audio_Mute";
        private const string kPrefResolution = "Graphics_Resolution";
        private const string kPrefFullscreen = "Graphics_Fullscreen";
        private const string kPrefQuality = "Graphics_Quality";
        private const string kPrefVSync = "Graphics_VSync";
        private const string kPrefShowFPS = "Graphics_ShowFPS";
        private const string kPrefSensitivity = "Controls_Sensitivity";
        private const string kPrefInvertY = "Controls_InvertY";
        private const string kPrefSubtitles = "Audio_Subtitles";
        private const string kPrefDifficulty = "Gameplay_Difficulty";
        private const string kPrefDamageNumbers = "Gameplay_DamageNumbers";
        private const string kPrefHealthBars = "Gameplay_HealthBars";
        private const string kPrefTutorials = "Gameplay_Tutorials";

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        private VisualElement _root;
        private VisualElement _settingsPanel;
        private ScrollView _settingsScrollView;
        private Button _btnClose;
        private Button _btnReset;
        private Button _btnApply;

        // Tabs
        private Button _tabAudio;
        private Button _tabGraphics;
        private Button _tabControls;
        private Button _tabGameplay;
        private VisualElement _audioSettings;
        private VisualElement _graphicsSettings;
        private VisualElement _controlsSettings;
        private VisualElement _gameplaySettings;

        // Audio
        private Slider _sliderMaster;
        private Slider _sliderMusic;
        private Slider _sliderSFX;
        private Slider _sliderVoice;
        private Label _labelMasterValue;
        private Label _labelMusicValue;
        private Label _labelSFXValue;
        private Label _labelVoiceValue;
        private Toggle _toggleMute;
        private Toggle _toggleSubtitles;

        // Graphics
        private VBDropdownField _dropdownResolution;
        private VBDropdownField _dropdownFullscreen;
        private VBDropdownField _dropdownQuality;
        private Toggle _toggleVSync;
        private Toggle _toggleFPS;

        // Gameplay
        private VBDropdownField _dropdownDifficulty;
        private Toggle _toggleDamageNumbers;
        private Toggle _toggleHealthBars;
        private Toggle _toggleTutorials;

        // Controls
        private Slider _sliderSensitivity;
        private Label _labelSensitivityValue;
        private Toggle _toggleInvertY;

        // =============================================================================
        // STATE
        // =============================================================================

        private SettingsData _currentSettings;
        private SettingsData _pendingSettings;
        private List<Resolution> _availableResolutions;
        private int _currentTab = 0;
        private bool _initialized = false;
        private VisualElement _confirmationDialog;

        private enum ConfirmationDialogMode
        {
            None,
            CloseUnsaved,
            ApplyAndClose
        }

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnSettingsClosed;
        public event Action<SettingsData> OnSettingsApplied;

        /// <summary>
        /// Alias for OnSettingsClosed (used by MainMenuBootstrap).
        /// </summary>
        public event Action OnClose
        {
            add => OnSettingsClosed += value;
            remove => OnSettingsClosed -= value;
        }

        // =============================================================================
        // DATA STRUCTURE
        // =============================================================================

        [Serializable]
        public class SettingsData
        {
            // Audio
            public float masterVolume = 0.8f;
            public float musicVolume = 0.7f;
            public float sfxVolume = 0.8f;
            public float voiceVolume = 1.0f;
            public bool muteAll = false;
            public bool subtitles = true;

            // Graphics
            public int resolutionIndex = 0;
            public int fullscreenMode = 0;
            public int qualityLevel = 2;
            public bool vsync = true;
            public bool showFPS = false;

            // Controls
            public float mouseSensitivity = 50f;
            public bool invertY = false;

            // Gameplay
            public int difficulty = 1;  // 0=Story, 1=Normal, 2=Hard, 3=Nightmare
            public bool damageNumbers = true;
            public bool healthBars = true;
            public bool tutorials = true;

            public SettingsData Clone()
            {
                return new SettingsData
                {
                    masterVolume = this.masterVolume,
                    musicVolume = this.musicVolume,
                    sfxVolume = this.sfxVolume,
                    voiceVolume = this.voiceVolume,
                    muteAll = this.muteAll,
                    subtitles = this.subtitles,
                    resolutionIndex = this.resolutionIndex,
                    fullscreenMode = this.fullscreenMode,
                    qualityLevel = this.qualityLevel,
                    vsync = this.vsync,
                    showFPS = this.showFPS,
                    mouseSensitivity = this.mouseSensitivity,
                    invertY = this.invertY,
                    difficulty = this.difficulty,
                    damageNumbers = this.damageNumbers,
                    healthBars = this.healthBars,
                    tutorials = this.tutorials
                };
            }
        }

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            // Only rebind events if already initialized by Bootstrap
            if (_initialized)
            {
                BindEvents();
                return;
            }

            // Auto-initialize only when the settings panel is already present in the document.
            // MainMenuBootstrap adds this component before/around overlay injection.
            // If we initialize too early here, tab queries fail and callbacks are never bound.
            var doc = _uiDocument ?? GetComponent<UIDocument>();
            var root = doc?.rootVisualElement;
            bool hasSettingsPanel = root?.Q<VisualElement>("settings-panel") != null;

            if (hasSettingsPanel)
            {
                InitializeUI();
                LoadSettings();
                UpdateUIFromSettings();
                ShowTab(0);
            }
        }

        private void OnDisable()
        {
            UnbindEvents();
            // Clean up confirmation dialog if it exists
            if (_confirmationDialog != null)
            {
                _confirmationDialog.RemoveFromHierarchy();
                _confirmationDialog = null;
            }
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                ErrorLogger.Error("SettingsPanelController: UIDocument is null!");
                return;
            }

            // Ensure settings data exists (may not have been created if Awake was skipped)
            _currentSettings ??= new SettingsData();
            _pendingSettings ??= new SettingsData();

            _root = _uiDocument.rootVisualElement;
            _settingsPanel = _root.Q<VisualElement>("settings-panel");
            _settingsScrollView = _root.Q<ScrollView>("settings-content");

            // Buttons
            _btnClose = _root.Q<Button>("btn-close");
            _btnReset = _root.Q<Button>("btn-reset");
            _btnApply = _root.Q<Button>("btn-apply");

            // Tabs
            _tabAudio = _root.Q<Button>("tab-audio");
            _tabGraphics = _root.Q<Button>("tab-graphics");
            _tabControls = _root.Q<Button>("tab-controls");
            _tabGameplay = _root.Q<Button>("tab-gameplay");
            _audioSettings = _root.Q<VisualElement>("audio-settings");
            _graphicsSettings = _root.Q<VisualElement>("graphics-settings");
            _controlsSettings = _root.Q<VisualElement>("controls-settings");
            _gameplaySettings = _root.Q<VisualElement>("gameplay-settings");

            // Audio controls
            _sliderMaster = _root.Q<Slider>("slider-master");
            _sliderMusic = _root.Q<Slider>("slider-music");
            _sliderSFX = _root.Q<Slider>("slider-sfx");
            _sliderVoice = _root.Q<Slider>("slider-voice");
            _labelMasterValue = _root.Q<Label>("label-master-value");
            _labelMusicValue = _root.Q<Label>("label-music-value");
            _labelSFXValue = _root.Q<Label>("label-sfx-value");
            _labelVoiceValue = _root.Q<Label>("label-voice-value");
            _toggleMute = _root.Q<Toggle>("toggle-mute");
            _toggleSubtitles = _root.Q<Toggle>("toggle-subtitles");

            // Graphics controls
            _dropdownResolution = _root.Q<VBDropdownField>("dropdown-resolution");
            _dropdownFullscreen = _root.Q<VBDropdownField>("dropdown-fullscreen");
            _dropdownQuality = _root.Q<VBDropdownField>("dropdown-quality");
            _toggleVSync = _root.Q<Toggle>("toggle-vsync");
            _toggleFPS = _root.Q<Toggle>("toggle-fps");

            // Controls
            _sliderSensitivity = _root.Q<Slider>("slider-sensitivity");
            _labelSensitivityValue = _root.Q<Label>("label-sensitivity-value");
            _toggleInvertY = _root.Q<Toggle>("toggle-invert-y");

            // Gameplay
            _dropdownDifficulty = _root.Q<VBDropdownField>("dropdown-difficulty");
            _toggleDamageNumbers = _root.Q<Toggle>("toggle-damage-numbers");
            _toggleHealthBars = _root.Q<Toggle>("toggle-health-bars");
            _toggleTutorials = _root.Q<Toggle>("toggle-tutorials");

            // Populate resolution dropdown
            PopulateResolutions();

            // Bind events
            BindEvents();

            _initialized = true;
            ErrorLogger.UI("SettingsPanel initialized");
        }

        private void PopulateResolutions()
        {
            _availableResolutions = new List<Resolution>();
            var choices = new List<string>();
            var seen = new HashSet<string>();

            foreach (var res in Screen.resolutions)
            {
                // Filter out very low resolutions and duplicates
                if (res.width >= 1280 && res.height >= 720)
                {
                    string resString = $"{res.width} x {res.height}";
                    if (seen.Add(resString))
                    {
                        choices.Add(resString);
                        _availableResolutions.Add(res);
                    }
                }
            }

            // Fallback if no resolutions were added (unusual displays)
            if (choices.Count == 0)
            {
                var fallback = new Resolution
                {
                    width = 1920,
                    height = 1080,
                    refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 }
                };
                choices.Add("1920 x 1080");
                _availableResolutions.Add(fallback);
            }

            if (_dropdownResolution != null)
            {
                _dropdownResolution.choices = choices;
            }
        }

        private bool _eventsBound;

        private void BindEvents()
        {
            if (_eventsBound) return;
            _eventsBound = true;

            _btnClose?.RegisterCallback<ClickEvent>(OnCloseClicked);
            _btnReset?.RegisterCallback<ClickEvent>(OnResetClicked);
            _btnApply?.RegisterCallback<ClickEvent>(OnApplyClicked);

            _tabAudio?.RegisterCallback<ClickEvent>(OnTabAudioClicked);
            _tabGraphics?.RegisterCallback<ClickEvent>(OnTabGraphicsClicked);
            _tabControls?.RegisterCallback<ClickEvent>(OnTabControlsClicked);
            _tabGameplay?.RegisterCallback<ClickEvent>(OnTabGameplayClicked);

            _sliderMaster?.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _sliderMusic?.RegisterValueChangedCallback(OnMusicVolumeChanged);
            _sliderSFX?.RegisterValueChangedCallback(OnSFXVolumeChanged);
            _sliderVoice?.RegisterValueChangedCallback(OnVoiceVolumeChanged);
            _toggleMute?.RegisterValueChangedCallback(OnMuteChanged);
            _toggleSubtitles?.RegisterValueChangedCallback(OnSubtitlesChanged);

            _dropdownResolution?.RegisterValueChangedCallback(OnResolutionChanged);
            _dropdownFullscreen?.RegisterValueChangedCallback(OnFullscreenChanged);
            _dropdownQuality?.RegisterValueChangedCallback(OnQualityChanged);
            _toggleVSync?.RegisterValueChangedCallback(OnVSyncChanged);
            _toggleFPS?.RegisterValueChangedCallback(OnFPSChanged);

            _sliderSensitivity?.RegisterValueChangedCallback(OnSensitivityChanged);
            _toggleInvertY?.RegisterValueChangedCallback(OnInvertYChanged);

            _dropdownDifficulty?.RegisterValueChangedCallback(OnDifficultyChanged);
            _toggleDamageNumbers?.RegisterValueChangedCallback(OnDamageNumbersChanged);
            _toggleHealthBars?.RegisterValueChangedCallback(OnHealthBarsChanged);
            _toggleTutorials?.RegisterValueChangedCallback(OnTutorialsChanged);

            _root?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void UnbindEvents()
        {
            if (!_eventsBound) return;
            _eventsBound = false;

            _btnClose?.UnregisterCallback<ClickEvent>(OnCloseClicked);
            _btnReset?.UnregisterCallback<ClickEvent>(OnResetClicked);
            _btnApply?.UnregisterCallback<ClickEvent>(OnApplyClicked);

            _tabAudio?.UnregisterCallback<ClickEvent>(OnTabAudioClicked);
            _tabGraphics?.UnregisterCallback<ClickEvent>(OnTabGraphicsClicked);
            _tabControls?.UnregisterCallback<ClickEvent>(OnTabControlsClicked);
            _tabGameplay?.UnregisterCallback<ClickEvent>(OnTabGameplayClicked);

            _sliderMaster?.UnregisterValueChangedCallback(OnMasterVolumeChanged);
            _sliderMusic?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            _sliderSFX?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
            _sliderVoice?.UnregisterValueChangedCallback(OnVoiceVolumeChanged);
            _toggleMute?.UnregisterValueChangedCallback(OnMuteChanged);
            _toggleSubtitles?.UnregisterValueChangedCallback(OnSubtitlesChanged);

            _dropdownResolution?.UnregisterValueChangedCallback(OnResolutionChanged);
            _dropdownFullscreen?.UnregisterValueChangedCallback(OnFullscreenChanged);
            _dropdownQuality?.UnregisterValueChangedCallback(OnQualityChanged);
            _toggleVSync?.UnregisterValueChangedCallback(OnVSyncChanged);
            _toggleFPS?.UnregisterValueChangedCallback(OnFPSChanged);

            _sliderSensitivity?.UnregisterValueChangedCallback(OnSensitivityChanged);
            _toggleInvertY?.UnregisterValueChangedCallback(OnInvertYChanged);

            _dropdownDifficulty?.UnregisterValueChangedCallback(OnDifficultyChanged);
            _toggleDamageNumbers?.UnregisterValueChangedCallback(OnDamageNumbersChanged);
            _toggleHealthBars?.UnregisterValueChangedCallback(OnHealthBarsChanged);
            _toggleTutorials?.UnregisterValueChangedCallback(OnTutorialsChanged);

            _root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnCloseClicked(ClickEvent evt) => Close();
        private void OnResetClicked(ClickEvent evt) => ResetToDefaults();
        private void OnApplyClicked(ClickEvent evt)
        {
            if (!HasUnsavedChanges())
            {
                ForceClose();
                return;
            }

            ShowConfirmationDialog(ConfirmationDialogMode.ApplyAndClose);
        }
        private void OnTabAudioClicked(ClickEvent evt) => ShowTab(0);
        private void OnTabGraphicsClicked(ClickEvent evt) => ShowTab(1);
        private void OnTabControlsClicked(ClickEvent evt) => ShowTab(2);
        private void OnTabGameplayClicked(ClickEvent evt) => ShowTab(3);

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            _pendingSettings.masterVolume = evt.newValue / 100f;
            UpdateVolumeLabel(_labelMasterValue, evt.newValue);
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            _pendingSettings.musicVolume = evt.newValue / 100f;
            UpdateVolumeLabel(_labelMusicValue, evt.newValue);
        }

        private void OnSFXVolumeChanged(ChangeEvent<float> evt)
        {
            _pendingSettings.sfxVolume = evt.newValue / 100f;
            UpdateVolumeLabel(_labelSFXValue, evt.newValue);
        }

        private void OnVoiceVolumeChanged(ChangeEvent<float> evt)
        {
            _pendingSettings.voiceVolume = evt.newValue / 100f;
            UpdateVolumeLabel(_labelVoiceValue, evt.newValue);
        }

        private void OnMuteChanged(ChangeEvent<bool> evt) => _pendingSettings.muteAll = evt.newValue;
        private void OnSubtitlesChanged(ChangeEvent<bool> evt) => _pendingSettings.subtitles = evt.newValue;

        private void OnResolutionChanged(ChangeEvent<string> evt) => _pendingSettings.resolutionIndex = _dropdownResolution.index;
        private void OnFullscreenChanged(ChangeEvent<string> evt) => _pendingSettings.fullscreenMode = _dropdownFullscreen.index;
        private void OnQualityChanged(ChangeEvent<string> evt) => _pendingSettings.qualityLevel = _dropdownQuality.index;
        private void OnVSyncChanged(ChangeEvent<bool> evt) => _pendingSettings.vsync = evt.newValue;
        private void OnFPSChanged(ChangeEvent<bool> evt) => _pendingSettings.showFPS = evt.newValue;

        private void OnSensitivityChanged(ChangeEvent<float> evt)
        {
            _pendingSettings.mouseSensitivity = evt.newValue;
            if (_labelSensitivityValue != null) _labelSensitivityValue.text = $"{evt.newValue:F0}";
        }

        private void OnInvertYChanged(ChangeEvent<bool> evt) => _pendingSettings.invertY = evt.newValue;

        private void OnDifficultyChanged(ChangeEvent<string> evt) => _pendingSettings.difficulty = _dropdownDifficulty.index;
        private void OnDamageNumbersChanged(ChangeEvent<bool> evt) => _pendingSettings.damageNumbers = evt.newValue;
        private void OnHealthBarsChanged(ChangeEvent<bool> evt) => _pendingSettings.healthBars = evt.newValue;
        private void OnTutorialsChanged(ChangeEvent<bool> evt) => _pendingSettings.tutorials = evt.newValue;

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape) Close();
        }

        // =============================================================================
        // TAB MANAGEMENT
        // =============================================================================

        private void ShowTab(int tabIndex)
        {
            _currentTab = tabIndex;

            // Close any open dropdowns before switching tabs to prevent glitches
            CloseAllDropdowns();

            // Update tab button styles
            SetTabActive(_tabAudio, tabIndex == 0);
            SetTabActive(_tabGraphics, tabIndex == 1);
            SetTabActive(_tabControls, tabIndex == 2);
            SetTabActive(_tabGameplay, tabIndex == 3);

            // Show/hide content
            _audioSettings?.style.SetDisplay(tabIndex == 0);
            _graphicsSettings?.style.SetDisplay(tabIndex == 1);
            _controlsSettings?.style.SetDisplay(tabIndex == 2);
            _gameplaySettings?.style.SetDisplay(tabIndex == 3);

            // Reset scroll position to top when switching tabs
            ResetScrollToTop();
        }

        /// <summary>
        /// Close all VBDropdownField popups in the settings panel.
        /// </summary>
        private void CloseAllDropdowns()
        {
            _dropdownResolution?.ForceCloseAndCleanup();
            _dropdownFullscreen?.ForceCloseAndCleanup();
            _dropdownQuality?.ForceCloseAndCleanup();
            _dropdownDifficulty?.ForceCloseAndCleanup();
        }

        private void SetTabActive(Button tab, bool active)
        {
            if (tab == null) return;

            tab.EnableInClassList("settings-tab-active", active);
            tab.EnableInClassList("settings-tab-inactive", !active);
        }

        /// <summary>
        /// Resets the settings content scroll view to the top position.
        /// </summary>
        private void ResetScrollToTop()
        {
            if (_settingsScrollView == null) return;

            // Immediate reset
            _settingsScrollView.scrollOffset = Vector2.zero;
            if (_settingsScrollView.verticalScroller != null)
            {
                _settingsScrollView.verticalScroller.value = _settingsScrollView.verticalScroller.lowValue;
            }

            // Also schedule a reset for next frame to handle layout timing issues
            _settingsScrollView.schedule.Execute(() =>
            {
                _settingsScrollView.scrollOffset = Vector2.zero;
                if (_settingsScrollView.verticalScroller != null)
                {
                    _settingsScrollView.verticalScroller.value = _settingsScrollView.verticalScroller.lowValue;
                }
            }).ExecuteLater(1);
        }

        // =============================================================================
        // SETTINGS MANAGEMENT
        // =============================================================================

        private void LoadSettings()
        {
            if (_availableResolutions == null || _availableResolutions.Count == 0)
            {
                PopulateResolutions();
            }

            _currentSettings.masterVolume = PlayerPrefs.GetFloat(kPrefMasterVolume, 0.8f);
            _currentSettings.musicVolume = PlayerPrefs.GetFloat(kPrefMusicVolume, 0.7f);
            _currentSettings.sfxVolume = PlayerPrefs.GetFloat(kPrefSFXVolume, 0.8f);
            _currentSettings.voiceVolume = PlayerPrefs.GetFloat(kPrefVoiceVolume, 1.0f);
            _currentSettings.muteAll = PlayerPrefs.GetInt(kPrefMuteAll, 0) == 1;

            _currentSettings.resolutionIndex = PlayerPrefs.GetInt(kPrefResolution, GetCurrentResolutionIndex());
            int fullscreenPref = PlayerPrefs.GetInt(kPrefFullscreen, GetDefaultFullscreenModeIndex());
            _currentSettings.fullscreenMode = Mathf.Clamp(fullscreenPref, 0, 2);
            _currentSettings.qualityLevel = PlayerPrefs.GetInt(kPrefQuality, QualitySettings.GetQualityLevel());
            _currentSettings.vsync = PlayerPrefs.GetInt(kPrefVSync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            _currentSettings.showFPS = PlayerPrefs.GetInt(kPrefShowFPS, 0) == 1;

            _currentSettings.mouseSensitivity = PlayerPrefs.GetFloat(kPrefSensitivity, 50f);
            _currentSettings.invertY = PlayerPrefs.GetInt(kPrefInvertY, 0) == 1;

            _currentSettings.subtitles = PlayerPrefs.GetInt(kPrefSubtitles, 1) == 1;
            _currentSettings.difficulty = PlayerPrefs.GetInt(kPrefDifficulty, 1);
            _currentSettings.damageNumbers = PlayerPrefs.GetInt(kPrefDamageNumbers, 1) == 1;
            _currentSettings.healthBars = PlayerPrefs.GetInt(kPrefHealthBars, 1) == 1;
            _currentSettings.tutorials = PlayerPrefs.GetInt(kPrefTutorials, 1) == 1;

            _pendingSettings = _currentSettings.Clone();
        }

        private int GetCurrentResolutionIndex()
        {
            if (_availableResolutions == null || _availableResolutions.Count == 0)
            {
                return 0;
            }

            var current = Screen.currentResolution;
            for (int i = 0; i < _availableResolutions.Count; i++)
            {
                if (_availableResolutions[i].width == current.width &&
                    _availableResolutions[i].height == current.height)
                {
                    return i;
                }
            }
            return _availableResolutions.Count - 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(kPrefMasterVolume, _currentSettings.masterVolume);
            PlayerPrefs.SetFloat(kPrefMusicVolume, _currentSettings.musicVolume);
            PlayerPrefs.SetFloat(kPrefSFXVolume, _currentSettings.sfxVolume);
            PlayerPrefs.SetFloat(kPrefVoiceVolume, _currentSettings.voiceVolume);
            PlayerPrefs.SetInt(kPrefMuteAll, _currentSettings.muteAll ? 1 : 0);

            PlayerPrefs.SetInt(kPrefResolution, _currentSettings.resolutionIndex);
            PlayerPrefs.SetInt(kPrefFullscreen, _currentSettings.fullscreenMode);
            PlayerPrefs.SetInt(kPrefQuality, _currentSettings.qualityLevel);
            PlayerPrefs.SetInt(kPrefVSync, _currentSettings.vsync ? 1 : 0);
            PlayerPrefs.SetInt(kPrefShowFPS, _currentSettings.showFPS ? 1 : 0);

            PlayerPrefs.SetFloat(kPrefSensitivity, _currentSettings.mouseSensitivity);
            PlayerPrefs.SetInt(kPrefInvertY, _currentSettings.invertY ? 1 : 0);

            PlayerPrefs.SetInt(kPrefSubtitles, _currentSettings.subtitles ? 1 : 0);
            PlayerPrefs.SetInt(kPrefDifficulty, _currentSettings.difficulty);
            PlayerPrefs.SetInt(kPrefDamageNumbers, _currentSettings.damageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(kPrefHealthBars, _currentSettings.healthBars ? 1 : 0);
            PlayerPrefs.SetInt(kPrefTutorials, _currentSettings.tutorials ? 1 : 0);

            PlayerPrefs.Save();
            ErrorLogger.UI("Settings saved");
        }

        private void UpdateUIFromSettings()
        {
            // Audio
            _sliderMaster?.SetValueWithoutNotify(_currentSettings.masterVolume * 100f);
            _sliderMusic?.SetValueWithoutNotify(_currentSettings.musicVolume * 100f);
            _sliderSFX?.SetValueWithoutNotify(_currentSettings.sfxVolume * 100f);
            _sliderVoice?.SetValueWithoutNotify(_currentSettings.voiceVolume * 100f);
            UpdateVolumeLabel(_labelMasterValue, _currentSettings.masterVolume * 100f);
            UpdateVolumeLabel(_labelMusicValue, _currentSettings.musicVolume * 100f);
            UpdateVolumeLabel(_labelSFXValue, _currentSettings.sfxVolume * 100f);
            UpdateVolumeLabel(_labelVoiceValue, _currentSettings.voiceVolume * 100f);
            _toggleMute?.SetValueWithoutNotify(_currentSettings.muteAll);
            _toggleSubtitles?.SetValueWithoutNotify(_currentSettings.subtitles);

            // Graphics
            if (_dropdownResolution != null && _availableResolutions.Count > 0)
            {
                _dropdownResolution.index = Mathf.Clamp(_currentSettings.resolutionIndex, 0, _availableResolutions.Count - 1);
                _dropdownResolution.SetValueWithoutNotify($"{_availableResolutions[_dropdownResolution.index].width} x {_availableResolutions[_dropdownResolution.index].height}");
            }
            _dropdownFullscreen?.SetValueWithoutNotify(GetFullscreenModeString(_currentSettings.fullscreenMode));
            if (_dropdownFullscreen != null)
            {
                int maxFullscreenIndex = (_dropdownFullscreen.choices?.Count ?? 1) - 1;
                _dropdownFullscreen.index = Mathf.Clamp(_currentSettings.fullscreenMode, 0, Mathf.Max(0, maxFullscreenIndex));
            }

            _dropdownQuality?.SetValueWithoutNotify(GetQualityString(_currentSettings.qualityLevel));
            if (_dropdownQuality != null)
            {
                int maxQualityIndex = (_dropdownQuality.choices?.Count ?? 1) - 1;
                _dropdownQuality.index = Mathf.Clamp(_currentSettings.qualityLevel, 0, Mathf.Max(0, maxQualityIndex));
            }
            _toggleVSync?.SetValueWithoutNotify(_currentSettings.vsync);
            _toggleFPS?.SetValueWithoutNotify(_currentSettings.showFPS);

            // Controls
            _sliderSensitivity?.SetValueWithoutNotify(_currentSettings.mouseSensitivity);
            if (_labelSensitivityValue != null)
                _labelSensitivityValue.text = $"{_currentSettings.mouseSensitivity:F0}";
            _toggleInvertY?.SetValueWithoutNotify(_currentSettings.invertY);

            // Gameplay
            if (_dropdownDifficulty != null)
            {
                _dropdownDifficulty.index = Mathf.Clamp(_currentSettings.difficulty, 0, 3);
                _dropdownDifficulty.SetValueWithoutNotify(GetDifficultyString(_currentSettings.difficulty));
            }
            _toggleDamageNumbers?.SetValueWithoutNotify(_currentSettings.damageNumbers);
            _toggleHealthBars?.SetValueWithoutNotify(_currentSettings.healthBars);
            _toggleTutorials?.SetValueWithoutNotify(_currentSettings.tutorials);
        }

        private void UpdateVolumeLabel(Label label, float value)
        {
            if (label != null)
                label.text = $"{value:F0}%";
        }

        private string GetFullscreenModeString(int mode)
        {
            return mode switch
            {
                0 => "Fullscreen",
                1 => "Windowed",
                2 => "Borderless",
                _ => "Fullscreen"
            };
        }

        private static int GetDefaultFullscreenModeIndex()
        {
            return Screen.fullScreenMode switch
            {
                FullScreenMode.ExclusiveFullScreen => 0,
                FullScreenMode.Windowed => 1,
                FullScreenMode.FullScreenWindow => 2,
                FullScreenMode.MaximizedWindow => 2,
                _ => 0
            };
        }

        private static FullScreenMode ResolveFullscreenMode(int modeIndex)
        {
            return modeIndex switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.FullScreenWindow
            };
        }

        private string GetQualityString(int level)
        {
            return level switch
            {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                3 => "Ultra",
                _ => "High"
            };
        }

        private string GetDifficultyString(int level)
        {
            return level switch
            {
                0 => "Story",
                1 => "Normal",
                2 => "Hard",
                3 => "Nightmare",
                _ => "Normal"
            };
        }

        // =============================================================================
        // ACTIONS
        // =============================================================================

        private void ApplySettings()
        {
            _currentSettings = _pendingSettings.Clone();

            // Apply audio
            ApplyAudioSettings();

            // Apply graphics
            ApplyGraphicsSettings();

            // Apply FPS counter
            ApplyFPSCounter();

            // Sync to SettingsManager singleton (bridges both settings systems)
            SyncToSettingsManager();

            // Save to PlayerPrefs
            SaveSettings();

            OnSettingsApplied?.Invoke(_currentSettings);
            ErrorLogger.UI("Settings applied");
        }

        private void ApplyAudioSettings()
        {
            if (Audio.AudioManager.HasInstance)
            {
                var audioManager = Audio.AudioManager.Instance;
                audioManager.SetMasterVolume(_currentSettings.muteAll ? 0f : _currentSettings.masterVolume);
                audioManager.SetMusicVolume(_currentSettings.musicVolume);
                audioManager.SetSFXVolume(_currentSettings.sfxVolume);
                audioManager.SetVoiceVolume(_currentSettings.voiceVolume);
            }
        }

        private void ApplyGraphicsSettings()
        {
            if (_availableResolutions == null || _availableResolutions.Count == 0)
                return;

            // Resolution
            if (_currentSettings.resolutionIndex >= 0 && _currentSettings.resolutionIndex < _availableResolutions.Count)
            {
                var res = _availableResolutions[_currentSettings.resolutionIndex];
                var mode = ResolveFullscreenMode(_currentSettings.fullscreenMode);
                Screen.SetResolution(res.width, res.height, mode);
            }

            // Quality
            QualitySettings.SetQualityLevel(_currentSettings.qualityLevel);

            // VSync
            QualitySettings.vSyncCount = _currentSettings.vsync ? 1 : 0;
        }

        private void ApplyFPSCounter()
        {
            var fpsCounter = FPSCounter.Instance;
            if (fpsCounter != null)
            {
                fpsCounter.SetVisible(_currentSettings.showFPS);
            }
        }

        private void SyncToSettingsManager()
        {
            if (!Managers.SettingsManager.HasInstance) return;
            var sm = Managers.SettingsManager.Instance;

            // Audio
            sm.SetMasterVolume(_currentSettings.masterVolume);
            sm.SetMusicVolume(_currentSettings.musicVolume);
            sm.SetSFXVolume(_currentSettings.sfxVolume);
            sm.SetVoiceVolume(_currentSettings.voiceVolume);
            sm.SetMuteAll(_currentSettings.muteAll);
            sm.SetSubtitles(_currentSettings.subtitles);

            // Controls - normalize slider range (1-100) to sensitivity range (0.02-2.0)
            sm.SetCameraSensitivity(_currentSettings.mouseSensitivity / 50f);
            sm.SetInvertY(_currentSettings.invertY);

            // Gameplay
            sm.SetDamageNumbers(_currentSettings.damageNumbers);
            sm.SetHealthBars(_currentSettings.healthBars);
            sm.SetTutorialTips(_currentSettings.tutorials);
            sm.SetDifficulty(_currentSettings.difficulty);

            sm.SaveSettings();
        }

        private void ResetToDefaults()
        {
            // Show a lightweight confirmation before resetting
            if (_confirmationDialog != null)
            {
                _confirmationDialog.RemoveFromHierarchy();
                _confirmationDialog = null;
            }

            _confirmationDialog = new VisualElement();
            _confirmationDialog.name = "confirmation-dialog-overlay";
            _confirmationDialog.style.position = Position.Absolute;
            _confirmationDialog.style.left = 0;
            _confirmationDialog.style.top = 0;
            _confirmationDialog.style.right = 0;
            _confirmationDialog.style.bottom = 0;
            _confirmationDialog.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _confirmationDialog.style.justifyContent = Justify.Center;
            _confirmationDialog.style.alignItems = Align.Center;

            var dialogBox = new VisualElement();
            dialogBox.style.backgroundColor = new Color(0.08f, 0.06f, 0.1f, 1f);
            dialogBox.style.borderTopWidth = 2;
            dialogBox.style.borderBottomWidth = 2;
            dialogBox.style.borderLeftWidth = 2;
            dialogBox.style.borderRightWidth = 2;
            dialogBox.style.borderTopColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderBottomColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderLeftColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderRightColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderTopLeftRadius = 8;
            dialogBox.style.borderTopRightRadius = 8;
            dialogBox.style.borderBottomLeftRadius = 8;
            dialogBox.style.borderBottomRightRadius = 8;
            dialogBox.style.paddingTop = 24;
            dialogBox.style.paddingBottom = 24;
            dialogBox.style.paddingLeft = 32;
            dialogBox.style.paddingRight = 32;
            dialogBox.style.minWidth = 350;

            var title = new Label("RESET TO DEFAULTS");
            title.style.fontSize = 18;
            title.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 3;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 16;
            dialogBox.Add(title);

            var message = new Label("Are you sure you want to reset all settings to their defaults?");
            message.style.fontSize = 14;
            message.style.color = new Color(0.65f, 0.6f, 0.56f, 1f);
            message.style.marginBottom = 24;
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.whiteSpace = WhiteSpace.Normal;
            dialogBox.Add(message);

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;

            var btnCancel = new Button(HideConfirmationDialog);
            btnCancel.text = "CANCEL";
            btnCancel.style.backgroundColor = Color.clear;
            btnCancel.style.color = new Color(0.47f, 0.43f, 0.39f, 1f);
            btnCancel.style.borderTopWidth = 1;
            btnCancel.style.borderBottomWidth = 1;
            btnCancel.style.borderLeftWidth = 1;
            btnCancel.style.borderRightWidth = 1;
            btnCancel.style.borderTopColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderBottomColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderLeftColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderRightColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderTopLeftRadius = 4;
            btnCancel.style.borderTopRightRadius = 4;
            btnCancel.style.borderBottomLeftRadius = 4;
            btnCancel.style.borderBottomRightRadius = 4;
            btnCancel.style.paddingTop = 10;
            btnCancel.style.paddingBottom = 10;
            btnCancel.style.paddingLeft = 24;
            btnCancel.style.paddingRight = 24;
            btnCancel.style.fontSize = 13;
            btnCancel.style.letterSpacing = 1;
            buttonContainer.Add(btnCancel);

            var btnConfirm = new Button(() =>
            {
                HideConfirmationDialog();
                _currentSettings = new SettingsData();
                _pendingSettings = _currentSettings.Clone();
                UpdateUIFromSettings();
                ApplySettings();
                ErrorLogger.UI("Settings reset to defaults");
            });
            btnConfirm.text = "RESET";
            btnConfirm.style.backgroundColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            btnConfirm.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            btnConfirm.style.borderTopWidth = 2;
            btnConfirm.style.borderBottomWidth = 2;
            btnConfirm.style.borderLeftWidth = 2;
            btnConfirm.style.borderRightWidth = 2;
            btnConfirm.style.borderTopColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnConfirm.style.borderBottomColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnConfirm.style.borderLeftColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnConfirm.style.borderRightColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnConfirm.style.borderTopLeftRadius = 4;
            btnConfirm.style.borderTopRightRadius = 4;
            btnConfirm.style.borderBottomLeftRadius = 4;
            btnConfirm.style.borderBottomRightRadius = 4;
            btnConfirm.style.paddingTop = 10;
            btnConfirm.style.paddingBottom = 10;
            btnConfirm.style.paddingLeft = 24;
            btnConfirm.style.paddingRight = 24;
            btnConfirm.style.marginLeft = 12;
            btnConfirm.style.fontSize = 13;
            btnConfirm.style.letterSpacing = 1;
            btnConfirm.style.unityFontStyleAndWeight = FontStyle.Bold;
            buttonContainer.Add(btnConfirm);

            dialogBox.Add(buttonContainer);
            _confirmationDialog.Add(dialogBox);
            _root.Add(_confirmationDialog);
            _confirmationDialog.BringToFront();
            _settingsPanel?.SetEnabled(false);
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        public void Show()
        {
            if (!_initialized)
            {
                InitializeUI();
                if (!_initialized)
                {
                    return;
                }
            }

            // When used as overlay (via Initialize), don't touch gameObject
            // MainMenuBootstrap handles overlay visibility
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
            }
            else
            {
                gameObject.SetActive(true);
            }

            LoadSettings();
            UpdateUIFromSettings();

            // Reset scroll to top when showing panel
            ResetScrollToTop();
        }

        public void Close()
        {
            // Close any open dropdowns first
            CloseAllDropdowns();

            // Check for unsaved changes
            if (HasUnsavedChanges())
            {
                ShowConfirmationDialog(ConfirmationDialogMode.CloseUnsaved);
                return;
            }

            // No changes, just close
            ForceClose();
        }

        /// <summary>
        /// Force close without checking for unsaved changes.
        /// </summary>
        private void ForceClose()
        {
            CloseAllDropdowns();
            HideConfirmationDialog();
            _pendingSettings = _currentSettings.Clone();

            // Re-enable the settings panel in case it was disabled by confirmation dialog
            _settingsPanel?.SetEnabled(true);

            // Fire the close event - MainMenuBootstrap handles the visual transition
            // Don't deactivate the GameObject or hide elements here, as that would
            // bypass the animation and potentially hide the entire main menu
            if (OnSettingsClosed != null)
            {
                OnSettingsClosed.Invoke();
            }
            else
            {
                Hide();
            }
        }

        private void SaveAndCloseFromDialog()
        {
            ApplySettings();
            ForceClose();
        }

        private void DiscardAndCloseFromDialog()
        {
            _pendingSettings = _currentSettings.Clone();
            UpdateUIFromSettings();
            ForceClose();
        }

        /// <summary>
        /// Check if there are any unsaved changes between pending and current settings.
        /// </summary>
        private bool HasUnsavedChanges()
        {
            if (_pendingSettings == null || _currentSettings == null) return false;

            // Audio
            if (!Mathf.Approximately(_pendingSettings.masterVolume, _currentSettings.masterVolume)) return true;
            if (!Mathf.Approximately(_pendingSettings.musicVolume, _currentSettings.musicVolume)) return true;
            if (!Mathf.Approximately(_pendingSettings.sfxVolume, _currentSettings.sfxVolume)) return true;
            if (!Mathf.Approximately(_pendingSettings.voiceVolume, _currentSettings.voiceVolume)) return true;
            if (_pendingSettings.muteAll != _currentSettings.muteAll) return true;

            // Graphics
            if (_pendingSettings.resolutionIndex != _currentSettings.resolutionIndex) return true;
            if (_pendingSettings.fullscreenMode != _currentSettings.fullscreenMode) return true;
            if (_pendingSettings.qualityLevel != _currentSettings.qualityLevel) return true;
            if (_pendingSettings.vsync != _currentSettings.vsync) return true;
            if (_pendingSettings.showFPS != _currentSettings.showFPS) return true;

            // Controls
            if (!Mathf.Approximately(_pendingSettings.mouseSensitivity, _currentSettings.mouseSensitivity)) return true;
            if (_pendingSettings.invertY != _currentSettings.invertY) return true;

            // Audio extras
            if (_pendingSettings.subtitles != _currentSettings.subtitles) return true;

            // Gameplay
            if (_pendingSettings.difficulty != _currentSettings.difficulty) return true;
            if (_pendingSettings.damageNumbers != _currentSettings.damageNumbers) return true;
            if (_pendingSettings.healthBars != _currentSettings.healthBars) return true;
            if (_pendingSettings.tutorials != _currentSettings.tutorials) return true;

            return false;
        }

        /// <summary>
        /// Show a confirmation dialog for unsaved changes.
        /// </summary>
        private void ShowConfirmationDialog(ConfirmationDialogMode mode)
        {
            if (_confirmationDialog != null)
            {
                _confirmationDialog.RemoveFromHierarchy();
                _confirmationDialog = null;
            }

            // Create the confirmation dialog
            _confirmationDialog = new VisualElement();
            _confirmationDialog.name = "confirmation-dialog-overlay";
            _confirmationDialog.style.position = Position.Absolute;
            _confirmationDialog.style.left = 0;
            _confirmationDialog.style.top = 0;
            _confirmationDialog.style.right = 0;
            _confirmationDialog.style.bottom = 0;
            _confirmationDialog.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            _confirmationDialog.style.justifyContent = Justify.Center;
            _confirmationDialog.style.alignItems = Align.Center;

            // Dialog box
            var dialogBox = new VisualElement();
            dialogBox.style.backgroundColor = new Color(0.08f, 0.06f, 0.1f, 1f);
            dialogBox.style.borderTopWidth = 2;
            dialogBox.style.borderBottomWidth = 2;
            dialogBox.style.borderLeftWidth = 2;
            dialogBox.style.borderRightWidth = 2;
            dialogBox.style.borderTopColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderBottomColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderLeftColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderRightColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            dialogBox.style.borderTopLeftRadius = 8;
            dialogBox.style.borderTopRightRadius = 8;
            dialogBox.style.borderBottomLeftRadius = 8;
            dialogBox.style.borderBottomRightRadius = 8;
            dialogBox.style.paddingTop = 24;
            dialogBox.style.paddingBottom = 24;
            dialogBox.style.paddingLeft = 32;
            dialogBox.style.paddingRight = 32;
            dialogBox.style.minWidth = 350;

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.Center;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 16;
            headerRow.style.position = Position.Relative;
            headerRow.style.width = Length.Percent(100);

            // Title
            var title = new Label(mode == ConfirmationDialogMode.ApplyAndClose ? "CONFIRM CHANGES" : "UNSAVED CHANGES");
            title.style.fontSize = 18;
            title.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 3;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            headerRow.Add(title);

            var btnDismiss = new Button(HideConfirmationDialog);
            btnDismiss.text = "X";
            btnDismiss.style.width = 30;
            btnDismiss.style.height = 30;
            btnDismiss.style.minWidth = 30;
            btnDismiss.style.backgroundColor = Color.clear;
            btnDismiss.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            btnDismiss.style.borderTopWidth = 1;
            btnDismiss.style.borderBottomWidth = 1;
            btnDismiss.style.borderLeftWidth = 1;
            btnDismiss.style.borderRightWidth = 1;
            btnDismiss.style.borderTopColor = new Color(0.24f, 0.2f, 0.29f, 0.6f);
            btnDismiss.style.borderBottomColor = new Color(0.24f, 0.2f, 0.29f, 0.6f);
            btnDismiss.style.borderLeftColor = new Color(0.24f, 0.2f, 0.29f, 0.6f);
            btnDismiss.style.borderRightColor = new Color(0.24f, 0.2f, 0.29f, 0.6f);
            btnDismiss.style.borderTopLeftRadius = 4;
            btnDismiss.style.borderTopRightRadius = 4;
            btnDismiss.style.borderBottomLeftRadius = 4;
            btnDismiss.style.borderBottomRightRadius = 4;
            btnDismiss.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnDismiss.style.unityTextAlign = TextAnchor.MiddleCenter;
            btnDismiss.style.paddingLeft = 0;
            btnDismiss.style.paddingRight = 0;
            btnDismiss.style.paddingTop = 0;
            btnDismiss.style.paddingBottom = 0;
            btnDismiss.style.marginLeft = 0;
            btnDismiss.style.marginRight = 0;
            btnDismiss.style.marginTop = 0;
            btnDismiss.style.marginBottom = 0;
            btnDismiss.style.letterSpacing = 0;
            btnDismiss.style.justifyContent = Justify.Center;
            btnDismiss.style.alignItems = Align.Center;
            btnDismiss.style.position = Position.Absolute;
            btnDismiss.style.right = 0;
            btnDismiss.style.top = 0;
            headerRow.Add(btnDismiss);

            dialogBox.Add(headerRow);

            // Message
            var messageText = mode == ConfirmationDialogMode.ApplyAndClose
                ? "Apply these settings now?\nYou can cancel or close this dialog."
                : "You have unsaved changes.\nIf you close without saving, your changes will be reverted.";
            var message = new Label(messageText);
            message.style.fontSize = 14;
            message.style.color = new Color(0.65f, 0.6f, 0.56f, 1f);
            message.style.marginBottom = 24;
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.whiteSpace = WhiteSpace.Normal;
            dialogBox.Add(message);

            // Button container
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.Center;

            // Save and close button
            var btnSaveAndClose = new Button(SaveAndCloseFromDialog);
            btnSaveAndClose.text = "SAVE & CLOSE";
            btnSaveAndClose.style.backgroundColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            btnSaveAndClose.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            btnSaveAndClose.style.borderTopWidth = 2;
            btnSaveAndClose.style.borderBottomWidth = 2;
            btnSaveAndClose.style.borderLeftWidth = 2;
            btnSaveAndClose.style.borderRightWidth = 2;
            btnSaveAndClose.style.borderTopColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnSaveAndClose.style.borderBottomColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnSaveAndClose.style.borderLeftColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnSaveAndClose.style.borderRightColor = new Color(1.0f, 0.55f, 0.2f, 1f);
            btnSaveAndClose.style.borderTopLeftRadius = 4;
            btnSaveAndClose.style.borderTopRightRadius = 4;
            btnSaveAndClose.style.borderBottomLeftRadius = 4;
            btnSaveAndClose.style.borderBottomRightRadius = 4;
            btnSaveAndClose.style.paddingTop = 10;
            btnSaveAndClose.style.paddingBottom = 10;
            btnSaveAndClose.style.paddingLeft = 24;
            btnSaveAndClose.style.paddingRight = 24;
            btnSaveAndClose.style.marginLeft = 12;
            btnSaveAndClose.style.fontSize = 13;
            btnSaveAndClose.style.letterSpacing = 1;
            btnSaveAndClose.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Cancel button
            var btnCancel = new Button(HideConfirmationDialog);
            btnCancel.text = "CANCEL";
            btnCancel.style.backgroundColor = Color.clear;
            btnCancel.style.color = new Color(0.47f, 0.43f, 0.39f, 1f);
            btnCancel.style.borderTopWidth = 1;
            btnCancel.style.borderBottomWidth = 1;
            btnCancel.style.borderLeftWidth = 1;
            btnCancel.style.borderRightWidth = 1;
            btnCancel.style.borderTopColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderBottomColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderLeftColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderRightColor = new Color(0.24f, 0.2f, 0.29f, 0.3f);
            btnCancel.style.borderTopLeftRadius = 4;
            btnCancel.style.borderTopRightRadius = 4;
            btnCancel.style.borderBottomLeftRadius = 4;
            btnCancel.style.borderBottomRightRadius = 4;
            btnCancel.style.paddingTop = 10;
            btnCancel.style.paddingBottom = 10;
            btnCancel.style.paddingLeft = 24;
            btnCancel.style.paddingRight = 24;
            btnCancel.style.fontSize = 13;
            btnCancel.style.letterSpacing = 1;
            buttonContainer.Add(btnCancel);

            if (mode == ConfirmationDialogMode.CloseUnsaved)
            {
                var btnDiscard = new Button(DiscardAndCloseFromDialog);
                btnDiscard.text = "DISCARD";
                btnDiscard.style.backgroundColor = Color.clear;
                btnDiscard.style.color = new Color(0.47f, 0.43f, 0.39f, 1f);
                btnDiscard.style.borderTopWidth = 1;
                btnDiscard.style.borderBottomWidth = 1;
                btnDiscard.style.borderLeftWidth = 1;
                btnDiscard.style.borderRightWidth = 1;
                btnDiscard.style.borderTopColor = new Color(0.85f, 0.4f, 0.12f, 0.3f);
                btnDiscard.style.borderBottomColor = new Color(0.85f, 0.4f, 0.12f, 0.3f);
                btnDiscard.style.borderLeftColor = new Color(0.85f, 0.4f, 0.12f, 0.3f);
                btnDiscard.style.borderRightColor = new Color(0.85f, 0.4f, 0.12f, 0.3f);
                btnDiscard.style.borderTopLeftRadius = 4;
                btnDiscard.style.borderTopRightRadius = 4;
                btnDiscard.style.borderBottomLeftRadius = 4;
                btnDiscard.style.borderBottomRightRadius = 4;
                btnDiscard.style.paddingTop = 10;
                btnDiscard.style.paddingBottom = 10;
                btnDiscard.style.paddingLeft = 24;
                btnDiscard.style.paddingRight = 24;
                btnDiscard.style.marginLeft = 12;
                btnDiscard.style.fontSize = 13;
                btnDiscard.style.letterSpacing = 1;
                buttonContainer.Add(btnDiscard);
            }

            buttonContainer.Add(btnSaveAndClose);
            dialogBox.Add(buttonContainer);
            _confirmationDialog.Add(dialogBox);

            // Add to root
            _root.Add(_confirmationDialog);
            _confirmationDialog.BringToFront();
            _settingsPanel?.SetEnabled(false);
        }

        /// <summary>
        /// Hide the confirmation dialog.
        /// </summary>
        private void HideConfirmationDialog()
        {
            if (_confirmationDialog != null)
            {
                _confirmationDialog.RemoveFromHierarchy();
                _confirmationDialog = null;
                _settingsPanel?.SetEnabled(true);
            }
        }

        public void Hide()
        {
            // Close any open dropdowns first
            CloseAllDropdowns();

            // When used as overlay (via Initialize), don't touch gameObject
            // MainMenuBootstrap handles overlay visibility
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Initialize with an external root element (called by MainMenuBootstrap).
        /// </summary>
        public void Initialize(VisualElement root)
        {
            // Prevent double initialization
            if (_initialized)
            {
                ErrorLogger.Warn("SettingsPanel already initialized, skipping");
                return;
            }

            _root = root;
            _initialized = true;

            // Query settings panel and scroll view
            _settingsPanel = _root.Q<VisualElement>("settings-panel");
            if (_settingsPanel == null)
            {
                _settingsPanel = _root;
            }
            _settingsScrollView = _root.Q<ScrollView>("settings-content");

            // Query tabs
            _tabAudio = _root.Q<Button>("tab-audio");
            _tabGraphics = _root.Q<Button>("tab-graphics");
            _tabControls = _root.Q<Button>("tab-controls");
            _tabGameplay = _root.Q<Button>("tab-gameplay");

            // Query tab content panels
            _audioSettings = _root.Q<VisualElement>("audio-settings");
            _graphicsSettings = _root.Q<VisualElement>("graphics-settings");
            _controlsSettings = _root.Q<VisualElement>("controls-settings");
            _gameplaySettings = _root.Q<VisualElement>("gameplay-settings");

            // Query audio controls
            _sliderMaster = _root.Q<Slider>("slider-master");
            _sliderMusic = _root.Q<Slider>("slider-music");
            _sliderSFX = _root.Q<Slider>("slider-sfx");
            _sliderVoice = _root.Q<Slider>("slider-voice");
            _toggleMute = _root.Q<Toggle>("toggle-mute");
            _toggleSubtitles = _root.Q<Toggle>("toggle-subtitles");

            _labelMasterValue = _root.Q<Label>("label-master-value");
            _labelMusicValue = _root.Q<Label>("label-music-value");
            _labelSFXValue = _root.Q<Label>("label-sfx-value");
            _labelVoiceValue = _root.Q<Label>("label-voice-value");

            // Query graphics controls
            _dropdownResolution = _root.Q<VBDropdownField>("dropdown-resolution");
            _dropdownFullscreen = _root.Q<VBDropdownField>("dropdown-fullscreen");
            _dropdownQuality = _root.Q<VBDropdownField>("dropdown-quality");
            _toggleVSync = _root.Q<Toggle>("toggle-vsync");
            _toggleFPS = _root.Q<Toggle>("toggle-fps");

            // Query controls controls
            _sliderSensitivity = _root.Q<Slider>("slider-sensitivity");
            _toggleInvertY = _root.Q<Toggle>("toggle-invert-y");
            _labelSensitivityValue = _root.Q<Label>("label-sensitivity-value");

            // Query gameplay controls
            _dropdownDifficulty = _root.Q<VBDropdownField>("dropdown-difficulty");
            _toggleDamageNumbers = _root.Q<Toggle>("toggle-damage-numbers");
            _toggleHealthBars = _root.Q<Toggle>("toggle-health-bars");
            _toggleTutorials = _root.Q<Toggle>("toggle-tutorials");

            // Query buttons
            _btnClose = _root.Q<Button>("btn-close");
            _btnReset = _root.Q<Button>("btn-reset");
            _btnApply = _root.Q<Button>("btn-apply");

            // Initialize data
            _currentSettings = new SettingsData();
            _pendingSettings = _currentSettings.Clone();
            _availableResolutions = new List<Resolution>();

            // Setup
            PopulateResolutions();
            BindEvents();
            LoadSettings();
            UpdateUIFromSettings();
            ShowTab(0);

            ErrorLogger.UI("SettingsPanel initialized via external root");
        }

        public SettingsData GetCurrentSettings() => _currentSettings.Clone();

        // Custom dropdowns handle their own popup behavior.

    }

}

namespace VeilBreakers.UI.Core
{
    // =============================================================================
    // EXTENSION METHODS
    // =============================================================================

    public static class StyleExtensions
    {
        public static void SetDisplay(this IStyle style, bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
