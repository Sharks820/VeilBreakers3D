using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// AAA molten/lava button effects.
    /// Creates cracks that glow on hover, eruption effects on click,
    /// and lava sweep effects across the button bar.
    /// </summary>
    public class MoltenButtonVFX : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Crack Settings")]
        [Tooltip("Disabled by default - cracks look bad on buttons")]
        [SerializeField] private bool _enableCracks = false;
        [SerializeField] private int _cracksPerButton = 5;
        [SerializeField] private float _crackWidth = 2f;
        [SerializeField] private float _crackLengthMin = 20f;
        [SerializeField] private float _crackLengthMax = 60f;

        [Header("Hover Lava Fill (AAA)")]
        [SerializeField] private bool _enableHoverLavaFill = false;
        [SerializeField] private float _lavaFillDuration = 0.22f;
        [SerializeField, Range(0f, 1f)] private float _lavaFillOpacity = 0.75f;
        [SerializeField] private Color _lavaFillTint = new Color(1f, 0.35f, 0.05f, 1f);
        [SerializeField] private int _lavaBubbleCountPrimary = 10;
        [SerializeField] private int _lavaBubbleCountSecondary = 7;
        [SerializeField] private float _lavaBubbleSpeedMin = 70f;
        [SerializeField] private float _lavaBubbleSpeedMax = 150f;
        [SerializeField] private float _lavaBubbleSizeMin = 6f;
        [SerializeField] private float _lavaBubbleSizeMax = 16f;
        [Tooltip("Optional bubble texture. Defaults to Resources/VFX/ParticleTextures/dirt 2.")]
        [SerializeField] private Texture2D _lavaBubbleTexture;

        [Header("Button Sheet (AAA)")]
        [SerializeField] private bool _enableButtonSheetSkins = false;
        [Tooltip("Optional. Auto-loads from Resources/Art/UI/MainMenu/mainmenu_buttons_sheet when empty.")]
        [SerializeField] private Texture2D _buttonSheetTexture;
        [SerializeField, Range(1, 6)] private int _buttonSheetColumns = 2;
        [SerializeField, Range(1, 6)] private int _buttonSheetRows = 3;
        [SerializeField, Range(0, 64)] private int _buttonSheetAlphaThreshold = 10;
        [SerializeField, Range(0, 32)] private int _buttonSheetCropPadding = 6;

        [Header("Molten Highlight (AAA)")]
        [SerializeField] private bool _enableMoltenHighlight = true;
        [Tooltip("Optional. Defaults to Resources/VFX/ParticleTextures/dirt 2.")]
        [SerializeField] private Texture2D _moltenNoiseTexture;
        [Tooltip("Optional. Defaults to Resources/VFX/ParticleTextures/grunge crack.")]
        [SerializeField] private Texture2D _moltenCrackTexture;
        [SerializeField, Range(0f, 1f)] private float _moltenHighlightOpacity = 0.55f;
        [SerializeField] private float _moltenHighlightSpeed = 220f;
        [SerializeField] private float _moltenHighlightAngle = -18f;

        [Header("Colors")]
        [SerializeField] private Color _crackColorDim = new Color(0.3f, 0.1f, 0.0f, 0.3f);
        [SerializeField] private Color _crackColorGlow = new Color(1f, 0.5f, 0.1f, 0.9f);
        [SerializeField] private Color _crackColorWhiteHot = new Color(1f, 0.9f, 0.7f, 1f);
        [SerializeField] private Color _lavaColor = new Color(1f, 0.4f, 0.05f, 0.8f);
        [SerializeField] private Color _eruptionColor = new Color(1f, 0.6f, 0.2f, 1f);

        [Header("Animation")]
        [SerializeField] private float _hoverGlowSpeed = 4f;
        [SerializeField] private float _clickFlashDuration = 0.15f;
        [SerializeField] private float _lavaSweepDuration = 0.8f;
        [SerializeField] private float _particleLifetime = 0.6f;

        [Header("Button Classes")]
        [SerializeField] private string _primaryButtonClass = "vb-menu-btn-primary";
        [SerializeField] private string _secondaryButtonClass = "vb-menu-btn-secondary";

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _root;
        private VisualElement _lavaSweepElement;
        private readonly Dictionary<Button, ButtonVFXState> _buttonStates = new();
        private readonly List<Texture2D> _generatedButtonSkins = new();
        private bool _isActive;
        private Coroutine _lavaUpdateCoroutine;
        private bool _sheetModeActive;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            // Sheet skins require a sprite sheet asset - disable until art is ready.
            _enableButtonSheetSkins = false;
            // Hover lava fill and molten highlight are too noisy without art tuning - keep disabled.
            _enableHoverLavaFill = false;
            _enableMoltenHighlight = false;
            // Cracks disabled - they create orange scratch lines that obscure button text.
            _enableCracks = false;

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null)
            {
                _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_isActive)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            CleanupCallbacks();

            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
                _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        private void Initialize()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            _isActive = true;

            // The new art button sheet replaces the old bubble-fill concept.
            if (_enableButtonSheetSkins)
            {
                _enableHoverLavaFill = false;
            }

            if (_lavaBubbleTexture == null)
            {
                _lavaBubbleTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dirt 2");
                if (_lavaBubbleTexture == null)
                {
                    _lavaBubbleTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/ink splat");
                }
            }

            if (_buttonSheetTexture == null)
            {
                _buttonSheetTexture = Resources.Load<Texture2D>("Art/UI/MainMenu/mainmenu_buttons_sheet");
            }

            _sheetModeActive = _enableButtonSheetSkins && _buttonSheetTexture != null;
            if (_sheetModeActive)
            {
                // Art-driven buttons: no cracks, no bubble-fill, no flowing sheen.
                _enableCracks = false;
                _enableHoverLavaFill = false;
                _enableMoltenHighlight = false;
            }

            if (_moltenNoiseTexture == null)
            {
                _moltenNoiseTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/dirt 2");
                if (_moltenNoiseTexture == null)
                {
                    _moltenNoiseTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/ink splat");
                }
            }

            if (_moltenCrackTexture == null)
            {
                _moltenCrackTexture = Resources.Load<Texture2D>("VFX/ParticleTextures/grunge crack");
            }

            // Find all buttons
            var primaryButtons = _root.Query<Button>(className: _primaryButtonClass).ToList();
            var secondaryButtons = _root.Query<Button>(className: _secondaryButtonClass).ToList();

            foreach (var button in primaryButtons)
            {
                SetupButton(button, true);
            }

            foreach (var button in secondaryButtons)
            {
                SetupButton(button, false);
            }

            if (_enableButtonSheetSkins)
            {
                TryApplyButtonSheetSkins();
            }

            if (!_sheetModeActive)
            {
                // Create lava sweep element
                CreateLavaSweepElement();

                if ((_enableHoverLavaFill || _enableMoltenHighlight) && _lavaUpdateCoroutine == null)
                {
                    _lavaUpdateCoroutine = StartCoroutine(UpdateLavaOverlays());
                }
            }

            Debug.Log($"[MoltenButtonVFX] Initialized {_buttonStates.Count} buttons");
        }

        private void SetupButton(Button button, bool isPrimary)
        {
            var state = new ButtonVFXState
            {
                Button = button,
                IsPrimary = isPrimary,
                CrackElements = new List<VisualElement>(),
                GlowIntensity = 0f
            };

            // Make button position relative for absolute children
            button.style.overflow = Overflow.Hidden;

            if (_sheetModeActive)
            {
                // Art buttons: no outline overlay, just scale on hover (handled in OnButtonHoverEnter/Exit)
                // Keep state.SheetOutline null - no visual effects needed
            }
            else
            {
                // Create crack overlay container
                var crackContainer = new VisualElement();
                crackContainer.name = "crack-container";
                crackContainer.style.position = Position.Absolute;
                crackContainer.style.left = 0;
                crackContainer.style.top = 0;
                crackContainer.style.right = 0;
                crackContainer.style.bottom = 0;
                crackContainer.pickingMode = PickingMode.Ignore;
                button.Add(crackContainer);
                state.CrackContainer = crackContainer;
            }

            if (_enableMoltenHighlight)
            {
                var highlight = new VisualElement();
                highlight.name = "molten-highlight";
                highlight.style.position = Position.Absolute;
                highlight.style.left = 0;
                highlight.style.top = 0;
                highlight.style.right = 0;
                highlight.style.bottom = 0;
                highlight.style.opacity = 0;
                highlight.pickingMode = PickingMode.Ignore;
                button.Add(highlight);
                state.MoltenHighlight = highlight;

                if (_moltenNoiseTexture != null)
                {
                    // Oversized so we can translate it for a flowing sheen.
                    var sheen = new VisualElement();
                    sheen.name = "molten-sheen";
                    sheen.style.position = Position.Absolute;
                    sheen.style.left = -120;
                    sheen.style.top = -80;
                    sheen.style.width = 640;
                    sheen.style.height = 260;
                    sheen.style.backgroundImage = new StyleBackground(_moltenNoiseTexture);
                    sheen.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    sheen.style.unityBackgroundImageTintColor = new Color(_lavaFillTint.r, _lavaFillTint.g, _lavaFillTint.b, 0.95f);
                    sheen.style.opacity = 0;
                    sheen.style.rotate = new Rotate(_moltenHighlightAngle);
                    sheen.pickingMode = PickingMode.Ignore;
                    highlight.Add(sheen);
                    state.MoltenSheen = sheen;
                }

                if (_moltenCrackTexture != null)
                {
                    var cracks = new VisualElement();
                    cracks.name = "molten-cracks";
                    cracks.style.position = Position.Absolute;
                    cracks.style.left = -40;
                    cracks.style.top = -40;
                    cracks.style.right = -40;
                    cracks.style.bottom = -40;
                    cracks.style.backgroundImage = new StyleBackground(_moltenCrackTexture);
                    cracks.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    cracks.style.unityBackgroundImageTintColor = new Color(1f, 0.45f, 0.15f, 0.85f);
                    cracks.style.opacity = 0;
                    cracks.pickingMode = PickingMode.Ignore;
                    highlight.Add(cracks);
                    state.MoltenCracks = cracks;
                }

                state.MoltenSeed = Random.Range(0f, 1000f);
            }

            // Optional: Lava fill overlay that overtakes the button on hover
            if (_enableHoverLavaFill)
            {
                var lavaOverlay = new VisualElement();
                lavaOverlay.name = "lava-fill";
                lavaOverlay.style.position = Position.Absolute;
                lavaOverlay.style.left = 0;
                lavaOverlay.style.right = 0;
                lavaOverlay.style.bottom = 0;
                lavaOverlay.style.height = Length.Percent(0);
                lavaOverlay.style.opacity = 0;
                lavaOverlay.pickingMode = PickingMode.Ignore;

                // Base molten fill
                lavaOverlay.style.backgroundColor = new Color(0.16f, 0.06f, 0.02f, 1f);

                // Surface glow line
                var surface = new VisualElement();
                surface.name = "lava-surface";
                surface.style.position = Position.Absolute;
                surface.style.left = 0;
                surface.style.right = 0;
                surface.style.top = 0;
                surface.style.height = 6;
                surface.style.backgroundColor = new Color(_lavaFillTint.r, _lavaFillTint.g, _lavaFillTint.b, 0.55f);
                surface.style.opacity = 0;
                surface.pickingMode = PickingMode.Ignore;
                lavaOverlay.Add(surface);

                // Place above the underlay, below crack particles.
                button.Insert(1, lavaOverlay);
                state.LavaOverlay = lavaOverlay;
                state.LavaSeed = Random.Range(0f, 1000f);
                state.LavaSurface = surface;
                state.LavaBubbles = new List<LavaBubble>();

                int bubbleCount = Mathf.Max(0, isPrimary ? _lavaBubbleCountPrimary : _lavaBubbleCountSecondary);
                for (int i = 0; i < bubbleCount; i++)
                {
                    var bubbleEl = new VisualElement();
                    bubbleEl.style.position = Position.Absolute;
                    bubbleEl.pickingMode = PickingMode.Ignore;

                    float size = Random.Range(_lavaBubbleSizeMin, _lavaBubbleSizeMax);
                    bubbleEl.style.width = size;
                    bubbleEl.style.height = size;
                    bubbleEl.style.borderTopLeftRadius = Length.Percent(50);
                    bubbleEl.style.borderTopRightRadius = Length.Percent(50);
                    bubbleEl.style.borderBottomLeftRadius = Length.Percent(50);
                    bubbleEl.style.borderBottomRightRadius = Length.Percent(50);

                    if (_lavaBubbleTexture != null)
                    {
                        bubbleEl.style.backgroundImage = new StyleBackground(_lavaBubbleTexture);
                        bubbleEl.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                        bubbleEl.style.unityBackgroundImageTintColor = new Color(_lavaFillTint.r, _lavaFillTint.g, _lavaFillTint.b, 0.95f);
                    }
                    else
                    {
                        bubbleEl.style.backgroundColor = new Color(_lavaFillTint.r, _lavaFillTint.g, _lavaFillTint.b, 0.95f);
                    }

                    bubbleEl.style.opacity = 0;
                    lavaOverlay.Add(bubbleEl);

                    state.LavaBubbles.Add(new LavaBubble
                    {
                        Element = bubbleEl,
                        Size = size,
                        Speed = Random.Range(_lavaBubbleSpeedMin, _lavaBubbleSpeedMax),
                        X01 = Random.value,
                        Y01 = Random.value,
                        DriftPhase = Random.Range(0f, Mathf.PI * 2f),
                        DriftAmplitude = Random.Range(6f, 16f)
                    });
                }
            }

            // Create cracks (disabled by default - they look ugly)
            if (_enableCracks)
            {
                for (int i = 0; i < _cracksPerButton; i++)
                {
                    var crack = CreateCrack(button);
                    state.CrackContainer.Add(crack);
                    state.CrackElements.Add(crack);
                }
            }

            if (!_sheetModeActive)
            {
                // Create glow underlay
                var glowUnderlay = new VisualElement();
                glowUnderlay.name = "glow-underlay";
                glowUnderlay.style.position = Position.Absolute;
                glowUnderlay.style.left = -10;
                glowUnderlay.style.top = -10;
                glowUnderlay.style.right = -10;
                glowUnderlay.style.bottom = -10;
                glowUnderlay.style.backgroundColor = new Color(_lavaColor.r, _lavaColor.g, _lavaColor.b, 0.1f);
                glowUnderlay.style.borderTopLeftRadius = 10;
                glowUnderlay.style.borderTopRightRadius = 10;
                glowUnderlay.style.borderBottomLeftRadius = 10;
                glowUnderlay.style.borderBottomRightRadius = 10;
                glowUnderlay.style.opacity = 0;
                glowUnderlay.pickingMode = PickingMode.Ignore;
                button.Insert(0, glowUnderlay);
                state.GlowUnderlay = glowUnderlay;
            }

            // Register events
            button.RegisterCallback<MouseEnterEvent>(evt => OnButtonHoverEnter(state));
            button.RegisterCallback<MouseLeaveEvent>(evt => OnButtonHoverExit(state));
            button.RegisterCallback<ClickEvent>(evt => OnButtonClick(state, evt));

            _buttonStates[button] = state;

            // Primary buttons get ambient glow
            if (!_sheetModeActive && isPrimary)
            {
                StartCoroutine(AmbientPulse(state));
            }
        }

        private void TryApplyButtonSheetSkins()
        {
            if (_buttonStates.Count == 0) return;

            // Map button names -> individual image resource paths (preferred approach)
            var imageMapping = new Dictionary<string, string>
            {
                { "btn-new-game", "Art/UI/MainMenu/btn_new_game" },
                { "btn-settings", "Art/UI/MainMenu/btn_settings" },
                { "btn-continue", "Art/UI/MainMenu/btn_continue" },
                { "btn-credits", "Art/UI/MainMenu/btn_credits" },
                { "btn-exit", "Art/UI/MainMenu/btn_exit" }
            };

            bool anyApplied = false;

            foreach (var pair in _buttonStates)
            {
                var button = pair.Key;
                if (button == null) continue;

                if (!imageMapping.TryGetValue(button.name, out var resourcePath)) continue;

                // Try to load individual button image first
                var buttonTexture = Resources.Load<Texture2D>(resourcePath);
                if (buttonTexture != null)
                {
                    ApplyButtonSkin(button, buttonTexture);
                    anyApplied = true;
                    Debug.Log($"[MoltenButtonVFX] Applied individual button skin: {button.name}");
                }
            }

            if (anyApplied)
            {
                Debug.Log("[MoltenButtonVFX] Successfully applied individual button art skins");
                return;
            }

            // Fallback to legacy sheet-cropping if individual images not found
            if (_buttonSheetTexture == null)
            {
                Debug.LogWarning("[MoltenButtonVFX] No individual button images found and no sheet texture available");
                return;
            }

            // Legacy sheet cell mapping for fallback
            var sheetMapping = new Dictionary<string, Vector2Int>
            {
                { "btn-new-game", new Vector2Int(0, 0) },
                { "btn-settings", new Vector2Int(1, 0) },
                { "btn-continue", new Vector2Int(1, 1) },
                { "btn-credits", new Vector2Int(0, 2) },
                { "btn-exit", new Vector2Int(1, 2) }
            };

            foreach (var pair in _buttonStates)
            {
                var button = pair.Key;
                if (button == null) continue;

                if (!sheetMapping.TryGetValue(button.name, out var cell)) continue;

                if (TryCreateCroppedTextureFromCell(_buttonSheetTexture, cell.x, cell.y, out var skin))
                {
                    ApplyButtonSkin(button, skin);
                    _generatedButtonSkins.Add(skin);
                }
            }
        }

        private bool TryCreateCroppedTextureFromCell(Texture2D sheet, int col, int rowTopToBottom, out Texture2D cropped)
        {
            cropped = null;
            if (sheet == null) return false;

            int cols = Mathf.Max(1, _buttonSheetColumns);
            int rows = Mathf.Max(1, _buttonSheetRows);
            int cellW = Mathf.Max(1, sheet.width / cols);
            int cellH = Mathf.Max(1, sheet.height / rows);
            if (cellW <= 2 || cellH <= 2) return false;

            col = Mathf.Clamp(col, 0, cols - 1);
            rowTopToBottom = Mathf.Clamp(rowTopToBottom, 0, rows - 1);

            // Unity texture coords: (0,0) is bottom-left.
            int x0 = col * cellW;
            int y0 = sheet.height - (rowTopToBottom + 1) * cellH;
            y0 = Mathf.Clamp(y0, 0, Mathf.Max(0, sheet.height - cellH));

            int x1 = Mathf.Min(sheet.width, x0 + cellW);
            int y1 = Mathf.Min(sheet.height, y0 + cellH);

            Color32[] pixels;
            try
            {
                pixels = sheet.GetPixels32();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MoltenButtonVFX] Button sheet is not readable. Enable Read/Write on mainmenu_buttons_sheet. {ex.GetType().Name}: {ex.Message}");
                return false;
            }

            byte threshold = (byte)Mathf.Clamp(_buttonSheetAlphaThreshold, 0, 255);
            int minX = x1;
            int minY = y1;
            int maxX = x0 - 1;
            int maxY = y0 - 1;

            for (int y = y0; y < y1; y++)
            {
                int rowIndex = y * sheet.width;
                for (int x = x0; x < x1; x++)
                {
                    byte a = pixels[rowIndex + x].a;
                    if (a <= threshold) continue;

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return false;
            }

            int pad = Mathf.Clamp(_buttonSheetCropPadding, 0, 64);
            minX = Mathf.Max(0, minX - pad);
            minY = Mathf.Max(0, minY - pad);
            maxX = Mathf.Min(sheet.width - 1, maxX + pad);
            maxY = Mathf.Min(sheet.height - 1, maxY + pad);

            int w = Mathf.Max(1, (maxX - minX) + 1);
            int h = Mathf.Max(1, (maxY - minY) + 1);

            Color[] cropPixels = sheet.GetPixels(minX, minY, w, h);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.name = $"btnskin_{col}_{rowTopToBottom}";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(cropPixels);
            tex.Apply(false, true);

            cropped = tex;
            return true;
        }

        private static void ApplyButtonSkin(Button button, Texture2D skin)
        {
            if (button == null || skin == null) return;

            button.text = string.Empty;
            button.AddToClassList("vb-btn-sheet");

            button.style.backgroundImage = new StyleBackground(skin);
            button.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            button.style.unityBackgroundImageTintColor = Color.white;
            button.style.backgroundColor = Color.clear;
            button.style.borderLeftWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderTopWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderTopLeftRadius = 0;
            button.style.borderTopRightRadius = 0;
            button.style.borderBottomLeftRadius = 0;
            button.style.borderBottomRightRadius = 0;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.color = new Color(0, 0, 0, 0);
        }

        private VisualElement CreateCrack(Button button)
        {
            var crack = new VisualElement();
            crack.style.position = Position.Absolute;
            crack.style.backgroundColor = _crackColorDim;
            crack.pickingMode = PickingMode.Ignore;

            // Random crack properties
            float length = Random.Range(_crackLengthMin, _crackLengthMax);
            float angle = Random.Range(-45f, 45f);

            crack.style.width = length;
            crack.style.height = _crackWidth;
            crack.style.borderTopLeftRadius = _crackWidth / 2f;
            crack.style.borderTopRightRadius = _crackWidth / 2f;
            crack.style.borderBottomLeftRadius = _crackWidth / 2f;
            crack.style.borderBottomRightRadius = _crackWidth / 2f;

            // Random position within button
            crack.style.left = Random.Range(10f, 80f);
            crack.style.top = Random.Range(10f, 35f);
            crack.style.rotate = new Rotate(angle);
            crack.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));

            crack.style.opacity = 0.3f;

            return crack;
        }

        private void CreateLavaSweepElement()
        {
            _lavaSweepElement = new VisualElement();
            _lavaSweepElement.name = "lava-sweep";
            _lavaSweepElement.style.position = Position.Absolute;
            _lavaSweepElement.style.left = 0;
            _lavaSweepElement.style.right = 0;
            _lavaSweepElement.style.bottom = 0;
            _lavaSweepElement.style.height = 80;
            _lavaSweepElement.style.backgroundColor = _lavaColor;
            _lavaSweepElement.style.opacity = 0;
            _lavaSweepElement.pickingMode = PickingMode.Ignore;

            // Find button container and insert sweep behind it
            var buttonContainer = _root.Q<VisualElement>("button-container");
            if (buttonContainer != null)
            {
                var parent = buttonContainer.parent;
                int index = parent.IndexOf(buttonContainer);
                parent.Insert(index, _lavaSweepElement);
            }
            else
            {
                _root.Insert(0, _lavaSweepElement);
            }
        }

        // =============================================================================
        // EVENT HANDLERS
        // =============================================================================

        private void OnButtonHoverEnter(ButtonVFXState state)
        {
            if (_sheetModeActive)
            {
                state.IsHovered = true;
                // Simple scale effect for art buttons - no outline or glow
                state.Button.style.scale = new Scale(new Vector2(1.05f, 1.05f));
                return;
            }

            StopCoroutine(nameof(AnimateGlow));
            StartCoroutine(AnimateGlow(state, 1f));
            StartLavaFill(state, 1f);
            state.IsHovered = true;
        }

        private void OnButtonHoverExit(ButtonVFXState state)
        {
            if (_sheetModeActive)
            {
                state.IsHovered = false;
                // Reset scale for art buttons
                state.Button.style.scale = new Scale(Vector2.one);
                return;
            }

            StartCoroutine(AnimateGlow(state, state.IsPrimary ? 0.2f : 0f));
            StartLavaFill(state, 0f);
            state.IsHovered = false;
        }

        private void OnButtonClick(ButtonVFXState state, ClickEvent evt)
        {
            if (_sheetModeActive)
            {
                return;
            }

            StartCoroutine(ClickEruption(state, evt.localPosition));
            StartCoroutine(LavaSweep());
        }

        // =============================================================================
        // ANIMATIONS
        // =============================================================================

        private IEnumerator AnimateGlow(ButtonVFXState state, float targetIntensity)
        {
            float startIntensity = state.GlowIntensity;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                state.GlowIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                ApplyGlowState(state);

                yield return null;
            }

            state.GlowIntensity = targetIntensity;
            ApplyGlowState(state);
        }

        private void StartLavaFill(ButtonVFXState state, float targetFill)
        {
            if (!_enableHoverLavaFill) return;
            if (state?.LavaOverlay == null) return;

            if (state.LavaCoroutine != null)
            {
                StopCoroutine(state.LavaCoroutine);
                state.LavaCoroutine = null;
            }

            state.LavaCoroutine = StartCoroutine(AnimateLavaFill(state, targetFill));
        }

        private IEnumerator AnimateLavaFill(ButtonVFXState state, float targetFill)
        {
            float startFill = state.LavaFill;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, _lavaFillDuration);

            while (elapsed < duration && state.Button != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);

                state.LavaFill = Mathf.Lerp(startFill, targetFill, t);
                ApplyLavaFillState(state);

                yield return null;
            }

            state.LavaFill = targetFill;
            ApplyLavaFillState(state);
            state.LavaCoroutine = null;
        }

        private void ApplyLavaFillState(ButtonVFXState state)
        {
            if (state?.LavaOverlay == null) return;

            float fill = Mathf.Clamp01(state.LavaFill);
            state.LavaOverlay.style.height = Length.Percent(fill * 100f);
            state.LavaOverlay.style.opacity = fill * _lavaFillOpacity;

            if (state.LavaSurface != null)
            {
                state.LavaSurface.style.opacity = fill * 0.9f;
            }
        }

        private IEnumerator UpdateLavaOverlays()
        {
            while (_isActive)
            {
                float dt = Time.unscaledDeltaTime;
                foreach (var state in _buttonStates.Values)
                {
                    UpdateLavaBubbles(state, dt);
                    UpdateMoltenHighlight(state, dt);
                }
                yield return null;
            }
        }

        private void UpdateMoltenHighlight(ButtonVFXState state, float deltaTime)
        {
            if (!_enableMoltenHighlight) return;
            if (state?.MoltenHighlight == null) return;

            float target = state.IsHovered ? 1f : 0f;
            state.MoltenHover = Mathf.MoveTowards(state.MoltenHover, target, deltaTime / 0.18f);
            float a = state.MoltenHover * Mathf.Clamp01(_moltenHighlightOpacity);

            state.MoltenHighlight.style.opacity = a;

            if (state.MoltenSheen != null)
            {
                float w = state.Button.resolvedStyle.width;
                float h = state.Button.resolvedStyle.height;
                if (w <= 1f) w = 240f;
                if (h <= 1f) h = 60f;

                // Flow the sheen sideways; keep it oversized so it always covers the button.
                state.MoltenSheenOffset += deltaTime * Mathf.Max(10f, _moltenHighlightSpeed);
                float sweep = Mathf.Repeat(state.MoltenSheenOffset + state.MoltenSeed, w * 2.2f);

                state.MoltenSheen.style.width = w * 2.2f;
                state.MoltenSheen.style.height = h * 2.0f;
                state.MoltenSheen.style.left = -w * 1.1f + sweep;
                state.MoltenSheen.style.top = -h * 0.55f;

                float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(state.MoltenSeed, Time.unscaledTime * 1.8f);
                state.MoltenSheen.style.opacity = a * flicker;
            }

            if (state.MoltenCracks != null)
            {
                float crackPulse = 0.65f + 0.35f * Mathf.PerlinNoise(state.MoltenSeed + 12.3f, Time.unscaledTime * 1.25f);
                state.MoltenCracks.style.opacity = a * 0.55f * crackPulse;
            }
        }

        private void UpdateLavaBubbles(ButtonVFXState state, float deltaTime)
        {
            if (state?.LavaOverlay == null) return;
            if (state.LavaBubbles == null || state.LavaBubbles.Count == 0) return;

            float fill = Mathf.Clamp01(state.LavaFill);
            if (fill <= 0.001f)
            {
                for (int i = 0; i < state.LavaBubbles.Count; i++)
                {
                    state.LavaBubbles[i].Element.style.opacity = 0;
                }
                return;
            }

            float buttonW = state.Button.resolvedStyle.width;
            float buttonH = state.Button.resolvedStyle.height;
            if (buttonW <= 1f) buttonW = 240f;
            if (buttonH <= 1f) buttonH = 60f;

            float lavaH = Mathf.Max(1f, buttonH * fill);
            float time = Time.unscaledTime;

            for (int i = 0; i < state.LavaBubbles.Count; i++)
            {
                var bubble = state.LavaBubbles[i];

                // Bubble rises within the filled region and wraps.
                bubble.Y01 -= (bubble.Speed / Mathf.Max(1f, lavaH)) * deltaTime;
                if (bubble.Y01 < -0.2f)
                {
                    bubble.Y01 = 1.15f + Random.value * 0.25f;
                    bubble.X01 = Random.value;
                    bubble.Speed = Random.Range(_lavaBubbleSpeedMin, _lavaBubbleSpeedMax);
                }

                float x = bubble.X01 * buttonW;
                float y = bubble.Y01 * lavaH;
                float drift = Mathf.Sin(time * 4.2f + bubble.DriftPhase) * bubble.DriftAmplitude;

                // Overlay is anchored to bottom; y=0 is top of overlay.
                bubble.Element.style.left = x - bubble.Size * 0.5f + drift;
                bubble.Element.style.top = y - bubble.Size * 0.5f;

                float pop = 0.65f + 0.35f * Mathf.PerlinNoise(state.LavaSeed, time * 2.8f + i * 0.21f);
                bubble.Element.style.opacity = fill * pop * 0.9f;
            }
        }

        private void ApplyGlowState(ButtonVFXState state)
        {
            // Update crack colors (if cracks are enabled)
            if (_enableCracks)
            {
                foreach (var crack in state.CrackElements)
                {
                    Color crackColor = Color.Lerp(_crackColorDim, _crackColorGlow, state.GlowIntensity);
                    crack.style.backgroundColor = crackColor;
                    crack.style.opacity = 0.3f + state.GlowIntensity * 0.7f;
                }
            }

            // Update glow underlay - stronger effect when cracks are disabled
            float glowOpacity = _enableCracks ? state.GlowIntensity * 0.4f : state.GlowIntensity * 0.6f;
            state.GlowUnderlay.style.opacity = glowOpacity;

            // Note: Scale is handled by USS :hover transition - don't override it here
        }

        private IEnumerator AmbientPulse(ButtonVFXState state)
        {
            float phase = Random.Range(0f, Mathf.PI * 2f);

            while (_isActive && state.Button != null)
            {
                if (state.GlowIntensity < 0.3f) // Only pulse when not hovered
                {
                    float pulse = 0.1f + 0.1f * Mathf.Sin(Time.unscaledTime * 2f + phase);

                    // Pulse cracks if enabled
                    if (_enableCracks)
                    {
                        foreach (var crack in state.CrackElements)
                        {
                            crack.style.opacity = 0.3f + pulse;
                        }
                    }

                    // Pulse the glow underlay subtly for primary buttons
                    state.GlowUnderlay.style.opacity = pulse * 0.3f;
                }

                yield return null;
            }
        }

        private IEnumerator ClickEruption(ButtonVFXState state, Vector2 localPosition)
        {
            // Flash white-hot (cracks only if enabled)
            if (_enableCracks)
            {
                foreach (var crack in state.CrackElements)
                {
                    crack.style.backgroundColor = _crackColorWhiteHot;
                }
            }

            // Flash the glow underlay bright on click
            state.GlowUnderlay.style.opacity = 0.8f;

            // Create eruption particles
            var particles = new List<VisualElement>();
            int particleCount = 12;

            for (int i = 0; i < particleCount; i++)
            {
                var particle = new VisualElement();
                particle.style.position = Position.Absolute;
                float size = Random.Range(4f, 10f);
                particle.style.width = size;
                particle.style.height = size;
                particle.style.borderTopLeftRadius = size / 2f;
                particle.style.borderTopRightRadius = size / 2f;
                particle.style.borderBottomLeftRadius = size / 2f;
                particle.style.borderBottomRightRadius = size / 2f;
                particle.style.backgroundColor = _eruptionColor;
                particle.style.left = localPosition.x - size / 2f;
                particle.style.top = localPosition.y - size / 2f;
                particle.pickingMode = PickingMode.Ignore;

                state.CrackContainer.Add(particle);
                particles.Add(particle);
            }

            // Animate particles outward
            float elapsed = 0f;
            var velocities = new List<Vector2>();
            foreach (var p in particles)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(100f, 300f);
                velocities.Add(new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed));
            }

            while (elapsed < _particleLifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _particleLifetime;

                for (int i = 0; i < particles.Count; i++)
                {
                    var p = particles[i];
                    var v = velocities[i];

                    float x = localPosition.x + v.x * t;
                    float y = localPosition.y + v.y * t + 50f * t * t; // Gravity

                    p.style.left = x - p.resolvedStyle.width / 2f;
                    p.style.top = y - p.resolvedStyle.height / 2f;
                    p.style.opacity = 1f - t;
                    p.style.scale = new Scale(Vector2.one * (1f - t * 0.5f));
                }

                yield return null;
            }

            // Cleanup particles
            foreach (var p in particles)
            {
                p.RemoveFromHierarchy();
            }

            // Return to normal glow state
            yield return new WaitForSecondsRealtime(_clickFlashDuration);

            if (_enableCracks)
            {
                foreach (var crack in state.CrackElements)
                {
                    crack.style.backgroundColor = Color.Lerp(_crackColorDim, _crackColorGlow, state.GlowIntensity);
                }
            }

            // Fade the underlay back to current intensity
            ApplyGlowState(state);
        }

        private IEnumerator LavaSweep()
        {
            if (_lavaSweepElement == null) yield break;

            float elapsed = 0f;

            // Sweep in
            while (elapsed < _lavaSweepDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / (_lavaSweepDuration / 2f);
                t = t * t; // Ease in

                _lavaSweepElement.style.opacity = t * 0.6f;
                yield return null;
            }

            // Sweep out
            elapsed = 0f;
            while (elapsed < _lavaSweepDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / (_lavaSweepDuration / 2f);
                t = 1f - Mathf.Pow(1f - t, 2f); // Ease out

                _lavaSweepElement.style.opacity = (1f - t) * 0.6f;
                yield return null;
            }

            _lavaSweepElement.style.opacity = 0;
        }

        // =============================================================================
        // CLEANUP
        // =============================================================================

        private void CleanupCallbacks()
        {
            if (_lavaUpdateCoroutine != null)
            {
                StopCoroutine(_lavaUpdateCoroutine);
                _lavaUpdateCoroutine = null;
            }

            for (int i = 0; i < _generatedButtonSkins.Count; i++)
            {
                if (_generatedButtonSkins[i] != null)
                {
                    Destroy(_generatedButtonSkins[i]);
                }
            }
            _generatedButtonSkins.Clear();

            _buttonStates.Clear();
            _isActive = false;
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Trigger a lava sweep effect manually
        /// </summary>
        public void TriggerLavaSweep()
        {
            StartCoroutine(LavaSweep());
        }

        /// <summary>
        /// Intensify all button glows
        /// </summary>
        public void SetGlobalIntensity(float intensity)
        {
            foreach (var state in _buttonStates.Values)
            {
                state.GlowIntensity = Mathf.Clamp01(intensity);
                ApplyGlowState(state);
            }
        }

        // =============================================================================
        // NESTED TYPES
        // =============================================================================

        private class ButtonVFXState
        {
            public Button Button;
            public bool IsPrimary;
            public VisualElement CrackContainer;
            public VisualElement GlowUnderlay;
            public VisualElement SheetOutline;
            public VisualElement LavaOverlay;
            public VisualElement LavaSurface;
            public List<VisualElement> CrackElements;
            public float GlowIntensity;
            public float LavaFill;
            public float LavaSeed;
            public Coroutine LavaCoroutine;
            public bool IsHovered;
            public List<LavaBubble> LavaBubbles;

            public VisualElement MoltenHighlight;
            public VisualElement MoltenSheen;
            public VisualElement MoltenCracks;
            public float MoltenSeed;
            public float MoltenSheenOffset;
            public float MoltenHover;
        }

        private class LavaBubble
        {
            public VisualElement Element;
            public float Size;
            public float Speed;
            public float X01;
            public float Y01;
            public float DriftPhase;
            public float DriftAmplitude;
        }
    }
}
