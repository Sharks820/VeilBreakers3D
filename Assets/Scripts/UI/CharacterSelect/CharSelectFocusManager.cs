using System;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Manages zone-based gamepad navigation for the character select screen.
    /// Divides UI into 4 zones (Back, InfoTabs, Embark, Carousel), handles D-pad
    /// traversal between zones, L1/R1 hero switch + tab cycle, and visible focus ring.
    /// Right stick hero model rotation is handled by HeroStageController independently.
    /// </summary>
    public class CharSelectFocusManager : MonoBehaviour
    {
        // =============================================================================
        // ENUMS
        // =============================================================================

        private enum FocusZone { Back = 0, InfoTabs = 1, Embark = 2, Carousel = 3 }

        // =============================================================================
        // CONSTANTS
        // =============================================================================

        private const string kBtnBack = "btn-back";
        private const string kBtnEmbark = "btn-embark";
        private const string kInfoPanel = "info-panel-container";
        private const string kCarouselContainer = "carousel-container";
        private const string kFocusRingClass = "focus-ring-active";
        private const int kTabCount = 3; // Overview, Abilities, Lore

        // =============================================================================
        // ZONE NAVIGATION GRAPH
        // =============================================================================

        /// <summary>
        /// Static navigation graph: [zone, direction] => target zone.
        /// Directions: 0=Up, 1=Down, 2=Left, 3=Right
        /// </summary>
        private static readonly FocusZone[,] kZoneGraph = new FocusZone[4, 4]
        {
            // From Back:      Up=Back,      Down=InfoTabs, Left=Back,     Right=Back
            { FocusZone.Back,     FocusZone.InfoTabs, FocusZone.Back,     FocusZone.Back     },
            // From InfoTabs:  Up=Back,      Down=Embark,   Left=InfoTabs, Right=InfoTabs
            { FocusZone.Back,     FocusZone.Embark,   FocusZone.InfoTabs, FocusZone.InfoTabs },
            // From Embark:    Up=InfoTabs,  Down=Carousel, Left=Embark,   Right=Embark
            { FocusZone.InfoTabs, FocusZone.Carousel, FocusZone.Embark,   FocusZone.Embark   },
            // From Carousel:  Up=Embark,    Down=Carousel, Left=Carousel, Right=Carousel
            { FocusZone.Embark,   FocusZone.Carousel, FocusZone.Carousel, FocusZone.Carousel },
        };

        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private UIDocument _uiDocument;

        // =============================================================================
        // STATE
        // =============================================================================

        private FocusZone _currentZone = FocusZone.Carousel;
        private VisualElement _root;
        private VisualElement _btnBack;
        private VisualElement _infoPanel;
        private VisualElement _btnEmbark;
        private VisualElement _carouselContainer;
        private VisualElement _currentFocusTarget;
        private bool _isHoldingEmbark;
        private bool _isInitialized;
        private int _currentHeroIndex;
        private int _currentTabIndex;
        private int _heroCount;

        // =============================================================================
        // AUDIO
        // =============================================================================

        private AudioClip _navTickClip;
        private AudioClip _heroSwitchClip;
        private AudioSource _sfxSource;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void OnEnable()
        {
            CharSelectEvents.OnScreenReady += HandleScreenReady;
            CharSelectEvents.OnHeroChanged += HandleHeroChanged;

            // Create a local AudioSource for SFX playback
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f; // 2D sound

            // Generate placeholder audio tones
            _navTickClip = GeneratePlaceholderTone(800f, 0.05f, 0.3f);
            _heroSwitchClip = GeneratePlaceholderTone(440f, 0.15f, 0.4f);
        }

        private void OnDisable()
        {
            CharSelectEvents.OnScreenReady -= HandleScreenReady;
            CharSelectEvents.OnHeroChanged -= HandleHeroChanged;

            if (_root != null)
            {
                _root.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            }

            // Cleanup audio
            if (_navTickClip != null) { Destroy(_navTickClip); _navTickClip = null; }
            if (_heroSwitchClip != null) { Destroy(_heroSwitchClip); _heroSwitchClip = null; }
            if (_sfxSource != null) { Destroy(_sfxSource); _sfxSource = null; }

            _isInitialized = false;
            _currentFocusTarget = null;
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void HandleScreenReady()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            // Cache zone elements
            _btnBack = _root.Q<VisualElement>(kBtnBack);
            _infoPanel = _root.Q<VisualElement>(kInfoPanel);
            _btnEmbark = _root.Q<VisualElement>(kBtnEmbark);
            _carouselContainer = _root.Q<VisualElement>(kCarouselContainer);

            Debug.Assert(_btnBack != null, $"[CharSelectFocusManager] Element '{kBtnBack}' not found in UXML");
            Debug.Assert(_infoPanel != null, $"[CharSelectFocusManager] Element '{kInfoPanel}' not found in UXML");
            Debug.Assert(_btnEmbark != null, $"[CharSelectFocusManager] Element '{kBtnEmbark}' not found in UXML");
            Debug.Assert(_carouselContainer != null, $"[CharSelectFocusManager] Element '{kCarouselContainer}' not found in UXML");

            // Register navigation event handler at root level
            _root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);

            // Start with Carousel zone focused
            _currentZone = FocusZone.Carousel;
            MoveFocusToZone(FocusZone.Carousel);

            _isInitialized = true;
        }

        private void HandleHeroChanged(int index, HeroData data, HeroDisplayConfig config)
        {
            _currentHeroIndex = index;
            PlayHeroSwitch();
        }

        // =============================================================================
        // UPDATE -- SHOULDER BUTTON POLLING
        // =============================================================================

        private void Update()
        {
            if (!_isInitialized || _isHoldingEmbark) return;

            // L1/R1 shoulder buttons for hero switch + tab cycle
            if (InputManager.HasInstance)
            {
                if (InputManager.Instance.GetActionDown(InputManager.GameAction.TargetPrev))
                {
                    HandleShoulderButton(-1);
                }
                else if (InputManager.Instance.GetActionDown(InputManager.GameAction.TargetNext))
                {
                    HandleShoulderButton(1);
                }
            }
        }

        // =============================================================================
        // SHOULDER BUTTON HANDLER
        // =============================================================================

        /// <summary>
        /// Handles L1/R1 shoulder button input.
        /// When InfoTabs zone is focused, cycles tabs. Otherwise, switches hero.
        /// </summary>
        private void HandleShoulderButton(int direction)
        {
            if (_currentZone == FocusZone.InfoTabs)
            {
                // Cycle tabs within the info panel
                int newTab = _currentTabIndex + direction;
                if (newTab < 0) newTab = kTabCount - 1;
                else if (newTab >= kTabCount) newTab = 0;

                _currentTabIndex = newTab;
                CharSelectEvents.RaiseTabChanged(_currentTabIndex);
                PlayNavTick();
            }
            else
            {
                // Switch hero globally
                int newIndex = _currentHeroIndex + direction;
                CharSelectEvents.RaiseNavigationRequested(newIndex);
            }
        }

        // =============================================================================
        // NAVIGATION MOVE HANDLER
        // =============================================================================

        /// <summary>
        /// Intercepts D-pad navigation events at the root level.
        /// Routes to zone traversal or carousel hero switching based on current zone.
        /// </summary>
        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (_isHoldingEmbark)
            {
                evt.StopPropagation();
                evt.PreventDefault();
                return;
            }

            int dirIndex = evt.direction switch
            {
                NavigationMoveEvent.Direction.Up => 0,
                NavigationMoveEvent.Direction.Down => 1,
                NavigationMoveEvent.Direction.Left => 2,
                NavigationMoveEvent.Direction.Right => 3,
                _ => -1
            };

            if (dirIndex < 0) return;

            // In Carousel zone, Left/Right navigates heroes instead of zones
            if (_currentZone == FocusZone.Carousel && (dirIndex == 2 || dirIndex == 3))
            {
                if (dirIndex == 2)
                {
                    CharSelectEvents.RaiseNavigationRequested(_currentHeroIndex - 1);
                }
                else
                {
                    CharSelectEvents.RaiseNavigationRequested(_currentHeroIndex + 1);
                }

                PlayNavTick();
                evt.StopPropagation();
                evt.PreventDefault();
                return;
            }

            FocusZone targetZone = kZoneGraph[(int)_currentZone, dirIndex];
            if (targetZone != _currentZone)
            {
                MoveFocusToZone(targetZone);
                PlayNavTick();
            }

            evt.StopPropagation();
            evt.PreventDefault();
        }

        // =============================================================================
        // FOCUS MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Moves the visible focus ring to the specified zone and updates focus state.
        /// </summary>
        private void MoveFocusToZone(FocusZone zone)
        {
            // Remove focus ring from current target
            if (_currentFocusTarget != null)
            {
                _currentFocusTarget.RemoveFromClassList(kFocusRingClass);
            }

            _currentZone = zone;

            // Resolve target element for the new zone
            VisualElement target = zone switch
            {
                FocusZone.Back => _btnBack,
                FocusZone.InfoTabs => _infoPanel,
                FocusZone.Embark => _btnEmbark,
                FocusZone.Carousel => _carouselContainer,
                _ => null
            };

            if (target != null)
            {
                target.AddToClassList(kFocusRingClass);
                target.Focus();
            }

            _currentFocusTarget = target;
            CharSelectEvents.RaiseFocusZoneChanged((int)zone);
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Sets the embark hold lock state. When true, all zone navigation is blocked.
        /// Called by HoldToEmbarkController during hold-to-confirm interaction.
        /// </summary>
        public void SetHoldLock(bool locked)
        {
            _isHoldingEmbark = locked;
        }

        /// <summary>
        /// Sets the total hero count for wrapping calculations.
        /// Called by CharacterSelectManager after hero data is loaded.
        /// </summary>
        public void SetHeroCount(int count)
        {
            _heroCount = count;
        }

        // =============================================================================
        // AUDIO -- PLACEHOLDER TONE GENERATION
        // =============================================================================

        /// <summary>
        /// Generates a simple sine wave AudioClip for placeholder SFX.
        /// Includes a linear fade-out to prevent click artifacts.
        /// </summary>
        private static AudioClip GeneratePlaceholderTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            var clip = AudioClip.Create("placeholder_tone", sampleCount, 1, sampleRate, false);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - t / duration; // Linear fade-out
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Plays a short navigation tick sound.</summary>
        private void PlayNavTick()
        {
            if (_sfxSource != null && _navTickClip != null)
            {
                _sfxSource.PlayOneShot(_navTickClip);
            }
        }

        /// <summary>Plays the hero switch audio feedback.</summary>
        private void PlayHeroSwitch()
        {
            if (_sfxSource != null && _heroSwitchClip != null)
            {
                _sfxSource.PlayOneShot(_heroSwitchClip);
            }
        }
    }
}
