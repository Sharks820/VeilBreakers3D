using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.UI.Controls;

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

        // =============================================================================
        // UI ELEMENTS
        // =============================================================================

        private VisualElement _root;
        private VisualElement _settingsPanel;
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

            // Graphics
            public int resolutionIndex = 0;
            public int fullscreenMode = 0;
            public int qualityLevel = 2;
            public bool vsync = true;
            public bool showFPS = false;

            // Controls
            public float mouseSensitivity = 50f;
            public bool invertY = false;

            public SettingsData Clone()
            {
                return new SettingsData
                {
                    masterVolume = this.masterVolume,
                    musicVolume = this.musicVolume,
                    sfxVolume = this.sfxVolume,
                    voiceVolume = this.voiceVolume,
                    muteAll = this.muteAll,
                    resolutionIndex = this.resolutionIndex,
                    fullscreenMode = this.fullscreenMode,
                    qualityLevel = this.qualityLevel,
                    vsync = this.vsync,
                    showFPS = this.showFPS,
                    mouseSensitivity = this.mouseSensitivity,
                    invertY = this.invertY
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

            _currentSettings = new SettingsData();
            _pendingSettings = new SettingsData();
        }

        private void OnEnable()
        {
            // Skip auto-initialization if we're being initialized externally via Initialize()
            // MainMenuBootstrap calls Initialize() right after AddComponent, which would cause double-init
            if (_initialized) return;

            // Only auto-initialize if we have our own UIDocument assigned
            if (_uiDocument != null || GetComponent<UIDocument>() != null)
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

            _root = _uiDocument.rootVisualElement;
            _settingsPanel = _root.Q<VisualElement>("settings-panel");

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

            ErrorLogger.UI("SettingsPanel initialized");
        }

        private void PopulateResolutions()
        {
            _availableResolutions = new List<Resolution>();
            var choices = new List<string>();

            foreach (var res in Screen.resolutions)
            {
                // Filter out very low resolutions and duplicates
                if (res.width >= 1280 && res.height >= 720)
                {
                    string resString = $"{res.width} x {res.height}";
                    if (!choices.Contains(resString))
                    {
                        choices.Add(resString);
                        _availableResolutions.Add(res);
                    }
                }
            }

            // Fallback if no resolutions were added (unusual displays)
            if (choices.Count == 0)
            {
                var fallback = new Resolution { width = 1920, height = 1080, refreshRate = 60 };
                choices.Add("1920 x 1080");
                _availableResolutions.Add(fallback);
            }

            if (_dropdownResolution != null)
            {
                _dropdownResolution.choices = choices;
            }
        }

        private void BindEvents()
        {
            // Close button
            _btnClose?.RegisterCallback<ClickEvent>(evt => Close());
            _btnReset?.RegisterCallback<ClickEvent>(evt => ResetToDefaults());
            _btnApply?.RegisterCallback<ClickEvent>(evt => ApplySettings());

            // Tab buttons
            _tabAudio?.RegisterCallback<ClickEvent>(evt => ShowTab(0));
            _tabGraphics?.RegisterCallback<ClickEvent>(evt => ShowTab(1));
            _tabControls?.RegisterCallback<ClickEvent>(evt => ShowTab(2));
            _tabGameplay?.RegisterCallback<ClickEvent>(evt => ShowTab(3));

            // Audio sliders
            _sliderMaster?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.masterVolume = evt.newValue / 100f;
                UpdateVolumeLabel(_labelMasterValue, evt.newValue);
            });
            _sliderMusic?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.musicVolume = evt.newValue / 100f;
                UpdateVolumeLabel(_labelMusicValue, evt.newValue);
            });
            _sliderSFX?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.sfxVolume = evt.newValue / 100f;
                UpdateVolumeLabel(_labelSFXValue, evt.newValue);
            });
            _sliderVoice?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.voiceVolume = evt.newValue / 100f;
                UpdateVolumeLabel(_labelVoiceValue, evt.newValue);
            });
            _toggleMute?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.muteAll = evt.newValue;
            });

            // Graphics
            _dropdownResolution?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.resolutionIndex = _dropdownResolution.index;
            });
            _dropdownFullscreen?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.fullscreenMode = _dropdownFullscreen.index;
            });
            _dropdownQuality?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.qualityLevel = _dropdownQuality.index;
            });
            _toggleVSync?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.vsync = evt.newValue;
            });
            _toggleFPS?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.showFPS = evt.newValue;
            });

            // Controls
            _sliderSensitivity?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.mouseSensitivity = evt.newValue;
                if (_labelSensitivityValue != null)
                    _labelSensitivityValue.text = $"{evt.newValue:F0}";
            });
            _toggleInvertY?.RegisterValueChangedCallback(evt =>
            {
                _pendingSettings.invertY = evt.newValue;
            });

            // Keyboard
            _root?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                    Close();
            });
        }

        private void UnbindEvents()
        {
            // UI Toolkit handles most cleanup automatically
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

        // =============================================================================
        // SETTINGS MANAGEMENT
        // =============================================================================

        private void LoadSettings()
        {
            _currentSettings.masterVolume = PlayerPrefs.GetFloat(kPrefMasterVolume, 0.8f);
            _currentSettings.musicVolume = PlayerPrefs.GetFloat(kPrefMusicVolume, 0.7f);
            _currentSettings.sfxVolume = PlayerPrefs.GetFloat(kPrefSFXVolume, 0.8f);
            _currentSettings.voiceVolume = PlayerPrefs.GetFloat(kPrefVoiceVolume, 1.0f);
            _currentSettings.muteAll = PlayerPrefs.GetInt(kPrefMuteAll, 0) == 1;

            _currentSettings.resolutionIndex = PlayerPrefs.GetInt(kPrefResolution, GetCurrentResolutionIndex());
            _currentSettings.fullscreenMode = PlayerPrefs.GetInt(kPrefFullscreen, (int)Screen.fullScreenMode);
            _currentSettings.qualityLevel = PlayerPrefs.GetInt(kPrefQuality, QualitySettings.GetQualityLevel());
            _currentSettings.vsync = PlayerPrefs.GetInt(kPrefVSync, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            _currentSettings.showFPS = PlayerPrefs.GetInt(kPrefShowFPS, 0) == 1;

            _currentSettings.mouseSensitivity = PlayerPrefs.GetFloat(kPrefSensitivity, 50f);
            _currentSettings.invertY = PlayerPrefs.GetInt(kPrefInvertY, 0) == 1;

            _pendingSettings = _currentSettings.Clone();
        }

        private int GetCurrentResolutionIndex()
        {
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

            // Graphics
            if (_dropdownResolution != null && _availableResolutions.Count > 0)
            {
                _dropdownResolution.index = Mathf.Clamp(_currentSettings.resolutionIndex, 0, _availableResolutions.Count - 1);
                _dropdownResolution.SetValueWithoutNotify($"{_availableResolutions[_dropdownResolution.index].width} x {_availableResolutions[_dropdownResolution.index].height}");
            }
            _dropdownFullscreen?.SetValueWithoutNotify(GetFullscreenModeString(_currentSettings.fullscreenMode));
            if (_dropdownFullscreen != null)
                _dropdownFullscreen.index = Mathf.Clamp(_currentSettings.fullscreenMode, 0, Math.Max(0, _dropdownFullscreen.choices?.Count - 1 ?? 0));

            _dropdownQuality?.SetValueWithoutNotify(GetQualityString(_currentSettings.qualityLevel));
            if (_dropdownQuality != null)
                _dropdownQuality.index = Mathf.Clamp(_currentSettings.qualityLevel, 0, Math.Max(0, _dropdownQuality.choices?.Count - 1 ?? 0));
            _toggleVSync?.SetValueWithoutNotify(_currentSettings.vsync);
            _toggleFPS?.SetValueWithoutNotify(_currentSettings.showFPS);

            // Controls
            _sliderSensitivity?.SetValueWithoutNotify(_currentSettings.mouseSensitivity);
            if (_labelSensitivityValue != null)
                _labelSensitivityValue.text = $"{_currentSettings.mouseSensitivity:F0}";
            _toggleInvertY?.SetValueWithoutNotify(_currentSettings.invertY);
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

            // Save to PlayerPrefs
            SaveSettings();

            OnSettingsApplied?.Invoke(_currentSettings);
            ErrorLogger.UI("Settings applied");
        }

        private void ApplyAudioSettings()
        {
            // TODO: Integrate with AudioManager
            // AudioManager.Instance?.SetMasterVolume(_currentSettings.muteAll ? 0 : _currentSettings.masterVolume);
            // AudioManager.Instance?.SetMusicVolume(_currentSettings.musicVolume);
            // AudioManager.Instance?.SetSFXVolume(_currentSettings.sfxVolume);
            // AudioManager.Instance?.SetVoiceVolume(_currentSettings.voiceVolume);
        }

        private void ApplyGraphicsSettings()
        {
            if (_availableResolutions == null || _availableResolutions.Count == 0)
                return;

            // Resolution
            if (_currentSettings.resolutionIndex < _availableResolutions.Count)
            {
                var res = _availableResolutions[_currentSettings.resolutionIndex];
                var mode = _currentSettings.fullscreenMode switch
                {
                    0 => FullScreenMode.ExclusiveFullScreen,
                    1 => FullScreenMode.Windowed,
                    2 => FullScreenMode.FullScreenWindow,
                    _ => FullScreenMode.FullScreenWindow
                };
                Screen.SetResolution(res.width, res.height, mode);
            }

            // Quality
            QualitySettings.SetQualityLevel(_currentSettings.qualityLevel);

            // VSync
            QualitySettings.vSyncCount = _currentSettings.vsync ? 1 : 0;
        }

        private void ResetToDefaults()
        {
            _currentSettings = new SettingsData();
            _pendingSettings = _currentSettings.Clone();
            UpdateUIFromSettings();
            ApplySettings();
            ErrorLogger.UI("Settings reset to defaults");
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        public void Show()
        {
            gameObject.SetActive(true);
            LoadSettings();
            UpdateUIFromSettings();
        }

        public void Close()
        {
            // Close any open dropdowns first
            CloseAllDropdowns();

            // Check for unsaved changes
            if (HasUnsavedChanges())
            {
                ShowConfirmationDialog();
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
            gameObject.SetActive(false);
            OnSettingsClosed?.Invoke();
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

            return false;
        }

        /// <summary>
        /// Show a confirmation dialog for unsaved changes.
        /// </summary>
        private void ShowConfirmationDialog()
        {
            if (_confirmationDialog != null)
            {
                _confirmationDialog.style.display = DisplayStyle.Flex;
                return;
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
            dialogBox.style.borderTopColor = new Color(0.7f, 0.16f, 0.24f, 1f);
            dialogBox.style.borderBottomColor = new Color(0.7f, 0.16f, 0.24f, 1f);
            dialogBox.style.borderLeftColor = new Color(0.7f, 0.16f, 0.24f, 1f);
            dialogBox.style.borderRightColor = new Color(0.7f, 0.16f, 0.24f, 1f);
            dialogBox.style.borderTopLeftRadius = 8;
            dialogBox.style.borderTopRightRadius = 8;
            dialogBox.style.borderBottomLeftRadius = 8;
            dialogBox.style.borderBottomRightRadius = 8;
            dialogBox.style.paddingTop = 24;
            dialogBox.style.paddingBottom = 24;
            dialogBox.style.paddingLeft = 32;
            dialogBox.style.paddingRight = 32;
            dialogBox.style.minWidth = 350;

            // Title
            var title = new Label("UNSAVED CHANGES");
            title.style.fontSize = 18;
            title.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 3;
            title.style.marginBottom = 16;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            dialogBox.Add(title);

            // Message
            var message = new Label("You have unsaved changes.\nWould you like to apply them before closing?");
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

            // Save button
            var btnSave = new Button(() => {
                ApplySettings();
                ForceClose();
            });
            btnSave.text = "SAVE";
            btnSave.style.backgroundColor = new Color(0.7f, 0.16f, 0.24f, 1f);
            btnSave.style.color = new Color(0.92f, 0.88f, 0.84f, 1f);
            btnSave.style.borderTopWidth = 2;
            btnSave.style.borderBottomWidth = 2;
            btnSave.style.borderLeftWidth = 2;
            btnSave.style.borderRightWidth = 2;
            btnSave.style.borderTopColor = new Color(0.86f, 0.24f, 0.31f, 1f);
            btnSave.style.borderBottomColor = new Color(0.86f, 0.24f, 0.31f, 1f);
            btnSave.style.borderLeftColor = new Color(0.86f, 0.24f, 0.31f, 1f);
            btnSave.style.borderRightColor = new Color(0.86f, 0.24f, 0.31f, 1f);
            btnSave.style.borderTopLeftRadius = 4;
            btnSave.style.borderTopRightRadius = 4;
            btnSave.style.borderBottomLeftRadius = 4;
            btnSave.style.borderBottomRightRadius = 4;
            btnSave.style.paddingTop = 10;
            btnSave.style.paddingBottom = 10;
            btnSave.style.paddingLeft = 24;
            btnSave.style.paddingRight = 24;
            btnSave.style.marginRight = 12;
            btnSave.style.fontSize = 13;
            btnSave.style.letterSpacing = 1;
            btnSave.style.unityFontStyleAndWeight = FontStyle.Bold;
            buttonContainer.Add(btnSave);

            // Discard button
            var btnDiscard = new Button(() => {
                _pendingSettings = _currentSettings.Clone();
                ForceClose();
            });
            btnDiscard.text = "DISCARD";
            btnDiscard.style.backgroundColor = Color.clear;
            btnDiscard.style.color = new Color(0.47f, 0.43f, 0.39f, 1f);
            btnDiscard.style.borderTopWidth = 1;
            btnDiscard.style.borderBottomWidth = 1;
            btnDiscard.style.borderLeftWidth = 1;
            btnDiscard.style.borderRightWidth = 1;
            btnDiscard.style.borderTopColor = new Color(0.7f, 0.16f, 0.24f, 0.3f);
            btnDiscard.style.borderBottomColor = new Color(0.7f, 0.16f, 0.24f, 0.3f);
            btnDiscard.style.borderLeftColor = new Color(0.7f, 0.16f, 0.24f, 0.3f);
            btnDiscard.style.borderRightColor = new Color(0.7f, 0.16f, 0.24f, 0.3f);
            btnDiscard.style.borderTopLeftRadius = 4;
            btnDiscard.style.borderTopRightRadius = 4;
            btnDiscard.style.borderBottomLeftRadius = 4;
            btnDiscard.style.borderBottomRightRadius = 4;
            btnDiscard.style.paddingTop = 10;
            btnDiscard.style.paddingBottom = 10;
            btnDiscard.style.paddingLeft = 24;
            btnDiscard.style.paddingRight = 24;
            btnDiscard.style.marginRight = 12;
            btnDiscard.style.fontSize = 13;
            btnDiscard.style.letterSpacing = 1;
            buttonContainer.Add(btnDiscard);

            // Cancel button
            var btnCancel = new Button(() => {
                HideConfirmationDialog();
            });
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

            dialogBox.Add(buttonContainer);
            _confirmationDialog.Add(dialogBox);

            // Add to root
            _root.Add(_confirmationDialog);
            _confirmationDialog.BringToFront();
        }

        /// <summary>
        /// Hide the confirmation dialog.
        /// </summary>
        private void HideConfirmationDialog()
        {
            if (_confirmationDialog != null)
            {
                _confirmationDialog.style.display = DisplayStyle.None;
            }
        }

        public void Hide()
        {
            // Close any open dropdowns first
            CloseAllDropdowns();
            gameObject.SetActive(false);
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

            // Query settings panel
            _settingsPanel = _root.Q<VisualElement>("settings-panel");
            if (_settingsPanel == null)
            {
                _settingsPanel = _root;
            }

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
