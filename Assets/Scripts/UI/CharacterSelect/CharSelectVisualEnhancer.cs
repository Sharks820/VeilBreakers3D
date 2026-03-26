using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;
using VeilBreakers.UI.Core;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Applies AAA visual effects to the Character Select screen that USS alone cannot achieve:
    /// gradient backgrounds, glow overlays, depth shadows, and animated effects.
    /// Matches the approved V5 HTML mockup pixel-for-pixel using runtime Texture2D generation.
    ///
    /// Attach to the same GameObject as CharacterSelectManager's UIDocument.
    /// </summary>
    public class CharSelectVisualEnhancer : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        // Generated textures (destroyed on cleanup)
        private Texture2D _panelGradient;
        private Texture2D _bottomLayerGradient;
        private Texture2D _embarkGradient;
        private Texture2D _embarkHoverGradient;
        private Texture2D _vignetteGradient;
        private Texture2D _statBarHpGradient;
        private Texture2D _statBarStaGradient;
        private Texture2D _statBarAtkGradient;
        private Texture2D _statBarDefGradient;
        private Texture2D _champAreaGradient;
        private Texture2D _champModelGradient;
        private Texture2D _panelTopHighlight;

        // Hero card gradient textures
        private Texture2D _heroCardBaseGradient;
        private readonly Dictionary<string, Texture2D> _heroCardSelectedGradients = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> _heroCardGlowTextures = new Dictionary<string, Texture2D>();

        private VisualElement _root;
        private bool _applied;

        private void OnEnable()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null) return;

            // Defer to let UXML instantiate first
            _uiDocument.rootVisualElement?.schedule.Execute(ApplyVisualPass).ExecuteLater(50);

            // Subscribe to hero changes for dynamic card gradient updates
            CharSelectEvents.OnHeroChanged += HandleHeroChangedForCards;
            // Subscribe to screen ready in case carousel builds after our initial visual pass
            CharSelectEvents.OnScreenReady += HandleScreenReadyForCards;
        }

        private void OnDisable()
        {
            CharSelectEvents.OnHeroChanged -= HandleHeroChangedForCards;
            CharSelectEvents.OnScreenReady -= HandleScreenReadyForCards;
            CleanupTextures();
            _applied = false;
        }

        private void ApplyVisualPass()
        {
            if (_applied) return;
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            ApplyInfoPanelGradient();
            ApplyBottomLayerGradient();
            ApplyEmbarkGradient();
            ApplyStatBarGradients();
            ApplyChampionAreaGradients();
            ApplyPanelDepth();
            ApplyVignette();
            ApplyEmbarkBreathing();
            ApplyEmbarkShineSweep();
            ApplyHeroCardGradients();
            ApplyNavArrowGradients();

            _applied = true;
        }

        // =====================================================================
        // INFO PANEL — gradient background matching mockup
        // =====================================================================

        private void ApplyInfoPanelGradient()
        {
            var panel = _root.Q<VisualElement>("info-panel-container");
            if (panel == null) return;

            // Mockup: linear-gradient(180deg, rgba(18,14,26,0.96) 0%, rgba(10,8,16,0.97) 40%, rgba(6,4,12,0.98) 100%)
            _panelGradient = UIGradientHelper.CreateVerticalGradient3(
                new Color(18f/255f, 14f/255f, 26f/255f, 0.96f),
                new Color(10f/255f, 8f/255f, 16f/255f, 0.97f),
                new Color(6f/255f, 4f/255f, 12f/255f, 0.98f)
            );
            UIGradientHelper.ApplyGradient(panel, _panelGradient);

            // Top highlight line (mockup: golden glow at top edge)
            int hlW = 256, hlH = 1;
            _panelTopHighlight = new Texture2D(hlW, hlH, TextureFormat.RGBA32, false);
            _panelTopHighlight.wrapMode = TextureWrapMode.Clamp;
            _panelTopHighlight.filterMode = FilterMode.Bilinear;
            var hlPixels = new Color[hlW];
            for (int x = 0; x < hlW; x++)
            {
                float t = (float)x / (hlW - 1);
                float fade = 1f - Mathf.Abs(t * 2f - 1f);
                fade *= fade; // Quadratic falloff from center
                hlPixels[x] = new Color(200f/255f, 160f/255f, 60f/255f, 0.5f * fade);
            }
            _panelTopHighlight.SetPixels(hlPixels);
            _panelTopHighlight.Apply(false, false);

            var highlight = new VisualElement();
            highlight.name = "panel-top-highlight";
            highlight.pickingMode = PickingMode.Ignore;
            highlight.style.position = Position.Absolute;
            highlight.style.top = -1;
            highlight.style.left = 20;
            highlight.style.right = 20;
            highlight.style.height = 1;
            highlight.style.backgroundImage = new StyleBackground(_panelTopHighlight);
            highlight.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            panel.Insert(0, highlight);
        }

        // =====================================================================
        // BOTTOM LAYER — transparent-to-dark gradient
        // =====================================================================

        private void ApplyBottomLayerGradient()
        {
            var bottomLayer = _root.Q<VisualElement>("bottom-layer");
            if (bottomLayer == null) return;

            // Mockup: linear-gradient(180deg, transparent 0%, rgba(0,0,0,0.6) 30%, rgba(0,0,0,0.85) 100%)
            _bottomLayerGradient = UIGradientHelper.CreateVerticalGradient3(
                new Color(0f, 0f, 0f, 0f),       // Top: transparent
                new Color(0f, 0f, 0f, 0.6f),      // Mid: semi-dark
                new Color(0f, 0f, 0f, 0.85f)       // Bottom: dark
            );
            UIGradientHelper.ApplyGradient(bottomLayer, _bottomLayerGradient);
        }

        // =====================================================================
        // EMBARK BUTTON — gold gradient
        // =====================================================================

        private void ApplyEmbarkGradient()
        {
            var embark = _root.Q<Button>("btn-embark");
            if (embark == null) return;

            // Mockup: linear-gradient(180deg, rgba(220,175,70,0.95) 0%, rgba(180,135,40,0.98) 50%, rgba(150,110,25,0.99) 100%)
            _embarkGradient = UIGradientHelper.CreateVerticalGradient3(
                new Color(220f/255f, 175f/255f, 70f/255f, 0.95f),
                new Color(180f/255f, 135f/255f, 40f/255f, 0.98f),
                new Color(150f/255f, 110f/255f, 25f/255f, 0.99f)
            );
            UIGradientHelper.ApplyGradient(embark, _embarkGradient);

            // Hover: brighter gold
            _embarkHoverGradient = UIGradientHelper.CreateVerticalGradient3(
                new Color(255f/255f, 220f/255f, 100f/255f, 1f),
                new Color(220f/255f, 180f/255f, 60f/255f, 1f),
                new Color(190f/255f, 150f/255f, 40f/255f, 1f)
            );

            embark.RegisterCallback<MouseEnterEvent>(evt =>
            {
                UIGradientHelper.ApplyGradient(embark, _embarkHoverGradient);
            });
            embark.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                UIGradientHelper.ApplyGradient(embark, _embarkGradient);
            });

            // Top highlight shine on embark button
            var embarkShine = new VisualElement();
            embarkShine.name = "embark-shine";
            embarkShine.pickingMode = PickingMode.Ignore;
            embarkShine.style.position = Position.Absolute;
            embarkShine.style.top = 3;
            embarkShine.style.left = Length.Percent(10);
            embarkShine.style.right = Length.Percent(10);
            embarkShine.style.height = 1;
            embarkShine.style.backgroundColor = new Color(1f, 1f, 1f, 0.25f);
            embark.Add(embarkShine);
        }

        // =====================================================================
        // STAT BARS — gradient fills (left-to-right, color → bright color)
        // =====================================================================

        private void ApplyStatBarGradients()
        {
            // HP: #cc3838 → #ff5555
            _statBarHpGradient = CreateHorizontalGradient(
                new Color(204f/255f, 56f/255f, 56f/255f, 1f),
                new Color(255f/255f, 85f/255f, 85f/255f, 1f)
            );
            ApplyToStatFill("stat-hp-fill", _statBarHpGradient);

            // STAMINA: #38a855 → #55dd70
            _statBarStaGradient = CreateHorizontalGradient(
                new Color(56f/255f, 168f/255f, 85f/255f, 1f),
                new Color(85f/255f, 221f/255f, 112f/255f, 1f)
            );
            ApplyToStatFill("stat-stamina-fill", _statBarStaGradient);

            // ATK: #cc8820 → #ffaa33
            _statBarAtkGradient = CreateHorizontalGradient(
                new Color(204f/255f, 136f/255f, 32f/255f, 1f),
                new Color(255f/255f, 170f/255f, 51f/255f, 1f)
            );
            ApplyToStatFill("stat-atk-fill", _statBarAtkGradient);

            // DEF: #3878cc → #55aaff
            _statBarDefGradient = CreateHorizontalGradient(
                new Color(56f/255f, 120f/255f, 204f/255f, 1f),
                new Color(85f/255f, 170f/255f, 255f/255f, 1f)
            );
            ApplyToStatFill("stat-def-fill", _statBarDefGradient);
        }

        private void ApplyToStatFill(string elementName, Texture2D gradient)
        {
            var fill = _root.Q<VisualElement>(elementName);
            if (fill == null) return;
            UIGradientHelper.ApplyGradient(fill, gradient);
        }

        /// <summary>Creates a horizontal gradient (left → right).</summary>
        private static Texture2D CreateHorizontalGradient(Color left, Color right, int width = 64, int height = 4)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                Color c = Color.Lerp(left, right, t);
                for (int y = 0; y < height; y++)
                    pixels[y * width + x] = c;
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        // =====================================================================
        // CHAMPION AREA — subtle gradient backgrounds
        // =====================================================================

        private void ApplyChampionAreaGradients()
        {
            // Champion section: diagonal-like gradient
            var champSection = _root.Q<VisualElement>("champion-section");
            if (champSection == null) champSection = _root.Q<VisualElement>(className: "champion-section-v2");
            if (champSection != null)
            {
                _champAreaGradient = UIGradientHelper.CreateVerticalGradient(
                    new Color(200f/255f, 160f/255f, 60f/255f, 0.02f),  // Top: faint gold
                    new Color(0f, 0f, 0f, 0.3f)                         // Bottom: dark
                );
                UIGradientHelper.ApplyGradient(champSection, _champAreaGradient);
            }

            // Champion model viewer: subtle gradient
            var champModel = _root.Q<VisualElement>("champion-model-viewer");
            if (champModel != null)
            {
                _champModelGradient = UIGradientHelper.CreateVerticalGradient(
                    new Color(200f/255f, 160f/255f, 60f/255f, 0.03f),
                    new Color(0f, 0f, 0f, 0.2f)
                );
                UIGradientHelper.ApplyGradient(champModel, _champModelGradient);
            }
        }

        // =====================================================================
        // PANEL DEPTH — shadow layers behind panel for AAA depth illusion
        // =====================================================================

        private void ApplyPanelDepth()
        {
            var panel = _root.Q<VisualElement>("info-panel-container");
            if (panel == null) return;

            // --- OUTER SHADOW: Large dark glow behind panel (simulates box-shadow: 0 0 60px) ---
            var outerShadow = new VisualElement();
            outerShadow.name = "panel-outer-shadow";
            outerShadow.pickingMode = PickingMode.Ignore;
            outerShadow.style.position = Position.Absolute;
            outerShadow.style.left = -30;
            outerShadow.style.top = -20;
            outerShadow.style.right = -30;
            outerShadow.style.bottom = -30;
            var shadowTex = UIGradientHelper.CreateRadialGradient(
                new Color(0f, 0f, 0f, 0.4f),
                new Color(0f, 0f, 0f, 0f),
                128
            );
            UIGradientHelper.ApplyGradient(outerShadow, shadowTex);
            // Insert shadow BEFORE the panel in the parent
            var panelParent = panel.parent;
            if (panelParent != null)
            {
                int panelIndex = panelParent.IndexOf(panel);
                panelParent.Insert(panelIndex, outerShadow);
            }

            // --- HERO COLOR TOP GLOW: Subtle gold/hero-color radiance at panel top ---
            var topGlow = new VisualElement();
            topGlow.name = "panel-top-glow";
            topGlow.pickingMode = PickingMode.Ignore;
            topGlow.style.position = Position.Absolute;
            topGlow.style.top = -2;
            topGlow.style.left = Length.Percent(10);
            topGlow.style.right = Length.Percent(10);
            topGlow.style.height = 40;
            var topGlowTex = UIGradientHelper.CreateVerticalGradient(
                new Color(200f/255f, 160f/255f, 60f/255f, 0.05f),
                new Color(0f, 0f, 0f, 0f)
            );
            UIGradientHelper.ApplyGradient(topGlow, topGlowTex);
            panel.Insert(0, topGlow);

            // --- INNER GLOW: Faint white at top (simulates inset 0 1px 0 rgba(255,255,255,0.04)) ---
            var innerGlow = new VisualElement();
            innerGlow.name = "panel-inner-glow";
            innerGlow.pickingMode = PickingMode.Ignore;
            innerGlow.style.position = Position.Absolute;
            innerGlow.style.top = 0;
            innerGlow.style.left = 0;
            innerGlow.style.right = 0;
            innerGlow.style.height = 1;
            innerGlow.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);
            panel.Insert(0, innerGlow);

            // --- INNER EDGE DARKENING: Simulates inset 0 0 30px rgba(0,0,0,0.2) ---
            var innerDark = new VisualElement();
            innerDark.name = "panel-inner-dark";
            innerDark.pickingMode = PickingMode.Ignore;
            innerDark.style.position = Position.Absolute;
            innerDark.style.left = 0;
            innerDark.style.top = 0;
            innerDark.style.right = 0;
            innerDark.style.bottom = 0;
            innerDark.style.borderTopWidth = 20;
            innerDark.style.borderBottomWidth = 20;
            innerDark.style.borderLeftWidth = 15;
            innerDark.style.borderRightWidth = 15;
            innerDark.style.borderTopColor = new Color(0f, 0f, 0f, 0.12f);
            innerDark.style.borderBottomColor = new Color(0f, 0f, 0f, 0.15f);
            innerDark.style.borderLeftColor = new Color(0f, 0f, 0f, 0.08f);
            innerDark.style.borderRightColor = new Color(0f, 0f, 0f, 0.08f);
            panel.Insert(0, innerDark);
        }

        // =====================================================================
        // EMBARK SHINE SWEEP — animated white line sweeping across (mockup ::after)
        // =====================================================================

        private void ApplyEmbarkShineSweep()
        {
            var embark = _root.Q<Button>("btn-embark");
            if (embark == null) return;
            embark.style.overflow = Overflow.Hidden;

            var sweep = new VisualElement();
            sweep.name = "embark-shine-sweep";
            sweep.pickingMode = PickingMode.Ignore;
            sweep.style.position = Position.Absolute;
            sweep.style.top = 0;
            sweep.style.bottom = 0;
            sweep.style.width = Length.Percent(25);
            sweep.style.left = Length.Percent(-30);
            sweep.style.backgroundColor = new Color(1f, 1f, 1f, 0.1f);
            sweep.style.transitionProperty = new System.Collections.Generic.List<StylePropertyName>
            {
                new StylePropertyName("left")
            };
            sweep.style.transitionDuration = new System.Collections.Generic.List<TimeValue>
            {
                new TimeValue(1.2f, TimeUnit.Second)
            };
            sweep.style.transitionTimingFunction = new System.Collections.Generic.List<EasingFunction>
            {
                new EasingFunction(EasingMode.EaseInOut)
            };
            embark.Add(sweep);

            // Auto-sweep every 3.5 seconds (matching mockup @keyframes esw)
            float sweepTimer = 0f;
            sweep.schedule.Execute(() =>
            {
                sweepTimer += 0.05f;
                if (sweepTimer >= 3.5f)
                {
                    sweepTimer = 0f;
                    // Reset position instantly
                    sweep.style.transitionDuration = new System.Collections.Generic.List<TimeValue>
                    {
                        new TimeValue(0f, TimeUnit.Second)
                    };
                    sweep.style.left = Length.Percent(-30);
                    sweep.schedule.Execute(() =>
                    {
                        // Animate to right
                        sweep.style.transitionDuration = new System.Collections.Generic.List<TimeValue>
                        {
                            new TimeValue(1.2f, TimeUnit.Second)
                        };
                        sweep.style.left = Length.Percent(110);
                    }).ExecuteLater(20);
                }
            }).Every(50);
        }

        // =====================================================================
        // VIGNETTE — dark edges on CharSelect
        // =====================================================================

        private void ApplyVignette()
        {
            // The UXML already has overlay-vignette but it's just a flat color
            // Replace it with a proper radial gradient
            var vignette = _root.Q<VisualElement>("vignette-outer");
            if (vignette == null) return;

            _vignetteGradient = UIGradientHelper.CreateRadialGradient(
                new Color(0f, 0f, 0f, 0f),       // Center: transparent
                new Color(0f, 0f, 0f, 0.5f),      // Edges: dark
                256
            );
            UIGradientHelper.ApplyGradient(vignette, _vignetteGradient);
            // Clear the flat background-color that was there before
            vignette.style.borderTopWidth = 0;
            vignette.style.borderBottomWidth = 0;
            vignette.style.borderLeftWidth = 0;
            vignette.style.borderRightWidth = 0;
        }

        // =====================================================================
        // EMBARK BREATHING — golden glow pulse animation
        // =====================================================================

        private void ApplyEmbarkBreathing()
        {
            var embarkGlow = _root.Q<VisualElement>("embark-glow");
            if (embarkGlow == null) return;

            float breathPhase = 0f;
            embarkGlow.schedule.Execute(() =>
            {
                breathPhase += 0.05f * 2.5f; // ~2.5s full cycle
                float pulse = Mathf.Sin(breathPhase) * 0.5f + 0.5f;
                float opacity = Mathf.Lerp(0.08f, 0.25f, pulse);
                embarkGlow.style.opacity = opacity;
            }).Every(50);

            // "HOLD TO CONFIRM" subtitle text pulse (spec: opacity 0.7→1.0, 2s cycle)
            var embarkSubtitle = _root.Q<Label>("embark-subtitle");
            if (embarkSubtitle != null)
            {
                float subtitlePhase = 0f;
                embarkSubtitle.schedule.Execute(() =>
                {
                    subtitlePhase += 0.05f * 3.14f; // ~2s full cycle
                    float sp = Mathf.Sin(subtitlePhase) * 0.5f + 0.5f;
                    embarkSubtitle.style.opacity = Mathf.Lerp(0.7f, 1f, sp);
                }).Every(50);
            }
        }

        // =====================================================================
        // HERO CARDS — per-hero gradient backgrounds (V5 mockup spec)
        // USS cannot do linear-gradient, so we generate textures at runtime.
        // =====================================================================

        /// <summary>
        /// Per-hero color palette from the V5 HTML mockup.
        /// --cc = border-color, --ct = gradient top, --cb = gradient bottom, --cx = accent text, --cg = glow
        /// </summary>
        private struct HeroCardColors
        {
            public Color BorderColor;   // --cc
            public Color GradTop;       // --ct
            public Color GradBottom;    // --cb
            public Color AccentText;    // --cx
            public Color GlowColor;    // --cg (same as border for box-shadow)
        }

        private static readonly Dictionary<string, HeroCardColors> kHeroCardPalette = new Dictionary<string, HeroCardColors>
        {
            ["vex"] = new HeroCardColors
            {
                BorderColor = new Color(200f/255f, 160f/255f, 60f/255f, 0.65f),
                GradTop     = new Color(60f/255f, 50f/255f, 30f/255f, 0.85f),
                GradBottom  = new Color(30f/255f, 25f/255f, 15f/255f, 0.95f),
                AccentText  = new Color(255f/255f, 210f/255f, 112f/255f, 1f),   // #ffd270
                GlowColor   = new Color(200f/255f, 160f/255f, 60f/255f, 0.45f),
            },
            ["seraphina"] = new HeroCardColors
            {
                BorderColor = new Color(150f/255f, 90f/255f, 210f/255f, 0.65f),
                GradTop     = new Color(45f/255f, 30f/255f, 65f/255f, 0.85f),
                GradBottom  = new Color(22f/255f, 15f/255f, 32f/255f, 0.95f),
                AccentText  = new Color(210f/255f, 170f/255f, 255f/255f, 1f),   // #d2aaff
                GlowColor   = new Color(150f/255f, 90f/255f, 210f/255f, 0.45f),
            },
            ["orion"] = new HeroCardColors
            {
                BorderColor = new Color(200f/255f, 60f/255f, 60f/255f, 0.65f),
                GradTop     = new Color(60f/255f, 25f/255f, 25f/255f, 0.85f),
                GradBottom  = new Color(30f/255f, 12f/255f, 12f/255f, 0.95f),
                AccentText  = new Color(255f/255f, 136f/255f, 102f/255f, 1f),   // #ff8866
                GlowColor   = new Color(200f/255f, 60f/255f, 60f/255f, 0.45f),
            },
            ["nyx"] = new HeroCardColors
            {
                BorderColor = new Color(70f/255f, 190f/255f, 210f/255f, 0.65f),
                GradTop     = new Color(25f/255f, 55f/255f, 60f/255f, 0.85f),
                GradBottom  = new Color(12f/255f, 27f/255f, 30f/255f, 0.95f),
                AccentText  = new Color(130f/255f, 230f/255f, 250f/255f, 1f),   // #82e6fa
                GlowColor   = new Color(70f/255f, 190f/255f, 210f/255f, 0.45f),
            },
        };

        // =====================================================================
        // NAV ARROWS — gradient background (spec: linear-gradient dark purple)
        // =====================================================================

        private void ApplyNavArrowGradients()
        {
            // Spec: background: linear-gradient(180deg, rgba(30,24,40,0.9), rgba(14,10,20,0.95))
            var navGradient = UIGradientHelper.CreateVerticalGradient(
                new Color(30f/255f, 24f/255f, 40f/255f, 0.9f),
                new Color(14f/255f, 10f/255f, 20f/255f, 0.95f)
            );

            var arrows = _root.Query<Button>(className: "nav-arrow").ToList();
            foreach (var arrow in arrows)
            {
                UIGradientHelper.ApplyGradient(arrow, navGradient);
                arrow.style.backgroundColor = Color.clear;
            }
        }

        private void ApplyHeroCardGradients()
        {
            // Base gradient for unselected cards
            // Mockup: linear-gradient(180deg, rgba(30,24,44,0.85), rgba(15,12,22,0.95))
            _heroCardBaseGradient = UIGradientHelper.CreateVerticalGradient(
                new Color(30f/255f, 24f/255f, 44f/255f, 0.85f),
                new Color(15f/255f, 12f/255f, 22f/255f, 0.95f)
            );

            // Pre-generate per-hero selected gradients and glow textures
            foreach (var kvp in kHeroCardPalette)
            {
                var colors = kvp.Value;
                _heroCardSelectedGradients[kvp.Key] = UIGradientHelper.CreateVerticalGradient(
                    colors.GradTop,
                    colors.GradBottom
                );
                _heroCardGlowTextures[kvp.Key] = UIGradientHelper.CreateRadialGradient(
                    new Color(colors.GlowColor.r, colors.GlowColor.g, colors.GlowColor.b, 0.5f),
                    new Color(colors.GlowColor.r, colors.GlowColor.g, colors.GlowColor.b, 0f),
                    64
                );
            }

            // Apply base gradient to all hero cards
            var allCards = _root.Query<VisualElement>(className: "hero-card").ToList();
            foreach (var card in allCards)
            {
                if (card.ClassListContains("teaser")) continue;

                // Apply base gradient to all cards initially
                UIGradientHelper.ApplyGradient(card, _heroCardBaseGradient);

                // Clear USS background-color so the gradient texture shows through
                card.style.backgroundColor = Color.clear;

                // If card is already selected, apply its hero-specific gradient
                if (card.ClassListContains("selected"))
                {
                    ApplySelectedCardVisuals(card);
                }
            }
        }

        /// <summary>
        /// Called when screen is ready (carousel cards are built).
        /// Re-applies card gradients if the visual pass already ran before cards existed.
        /// </summary>
        private void HandleScreenReadyForCards()
        {
            if (_root == null || !_applied) return;
            // Defer slightly so the carousel has finished building its cards
            _root.schedule.Execute(UpdateHeroCardGradients).ExecuteLater(60);
        }

        /// <summary>
        /// Called on hero change events to update card gradient backgrounds dynamically.
        /// </summary>
        private void HandleHeroChangedForCards(int index, HeroData data, HeroDisplayConfig config)
        {
            if (_root == null || !_applied) return;
            UpdateHeroCardGradients();
        }

        /// <summary>
        /// Re-applies gradients to all hero cards based on current selection state.
        /// Selected card gets per-hero gradient + glow; unselected cards get base gradient.
        /// </summary>
        private void UpdateHeroCardGradients()
        {
            var allCards = _root.Query<VisualElement>(className: "hero-card").ToList();
            foreach (var card in allCards)
            {
                if (card.ClassListContains("teaser")) continue;

                // Remove any existing glow overlay from previous selection
                var existingGlow = card.Q<VisualElement>("card-glow-overlay");
                if (existingGlow != null)
                    card.Remove(existingGlow);

                if (card.ClassListContains("selected"))
                {
                    ApplySelectedCardVisuals(card);
                }
                else
                {
                    // Unselected: base gradient, clear any hero-specific overrides
                    UIGradientHelper.ApplyGradient(card, _heroCardBaseGradient);
                    card.style.backgroundColor = Color.clear;
                    // Reset border color to USS default
                    card.style.borderTopColor = StyleKeyword.Null;
                    card.style.borderBottomColor = StyleKeyword.Null;
                    card.style.borderLeftColor = StyleKeyword.Null;
                    card.style.borderRightColor = StyleKeyword.Null;
                    // Reset text colors to USS defaults
                    var initial = card.Q<VisualElement>(className: "hero-card-initial");
                    if (initial != null) initial.style.color = StyleKeyword.Null;
                    var nameLabel = card.Q<VisualElement>(className: "hero-card-name");
                    if (nameLabel != null) nameLabel.style.color = StyleKeyword.Null;
                    var dot = card.Q<VisualElement>(className: "hero-card-dot");
                    if (dot != null) dot.style.backgroundColor = StyleKeyword.Null;
                }
            }
        }

        /// <summary>
        /// Applies the per-hero selected visuals: gradient background, border color, glow overlay.
        /// </summary>
        private void ApplySelectedCardVisuals(VisualElement card)
        {
            string heroId = DetectHeroIdFromCard(card);
            if (heroId == null || !kHeroCardPalette.ContainsKey(heroId)) return;

            var colors = kHeroCardPalette[heroId];

            // Apply per-hero selected gradient
            if (_heroCardSelectedGradients.TryGetValue(heroId, out var selectedGrad))
            {
                UIGradientHelper.ApplyGradient(card, selectedGrad);
            }
            card.style.backgroundColor = Color.clear;

            // Apply per-hero border color
            card.style.borderTopColor = colors.BorderColor;
            card.style.borderBottomColor = colors.BorderColor;
            card.style.borderLeftColor = colors.BorderColor;
            card.style.borderRightColor = colors.BorderColor;

            // Add glow overlay behind card content (simulates box-shadow: 0 0 18px)
            if (_heroCardGlowTextures.TryGetValue(heroId, out var glowTex))
            {
                var glowOverlay = new VisualElement();
                glowOverlay.name = "card-glow-overlay";
                glowOverlay.pickingMode = PickingMode.Ignore;
                glowOverlay.style.position = Position.Absolute;
                glowOverlay.style.left = -10;
                glowOverlay.style.top = -8;
                glowOverlay.style.right = -10;
                glowOverlay.style.bottom = -8;
                glowOverlay.style.backgroundImage = new StyleBackground(glowTex);
                glowOverlay.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                card.Insert(0, glowOverlay);
            }

            // Tint the initial letter and name with hero accent color
            var initial = card.Q<VisualElement>(className: "hero-card-initial");
            if (initial != null)
            {
                initial.style.color = colors.AccentText;
            }

            var nameLabel = card.Q<VisualElement>(className: "hero-card-name");
            if (nameLabel != null)
            {
                nameLabel.style.color = colors.AccentText;
            }

            // Tint the selection dot with hero accent color
            var dot = card.Q<VisualElement>(className: "hero-card-dot");
            if (dot != null)
            {
                dot.style.backgroundColor = colors.AccentText;
            }
        }

        /// <summary>
        /// Detects the hero_id from a card's class list (e.g. "hero-card-vex" -> "vex").
        /// </summary>
        private static string DetectHeroIdFromCard(VisualElement card)
        {
            foreach (var kvp in kHeroCardPalette)
            {
                if (card.ClassListContains($"hero-card-{kvp.Key}"))
                    return kvp.Key;
            }
            return null;
        }

        // =====================================================================
        // CLEANUP
        // =====================================================================

        private void CleanupTextures()
        {
            DestroyTex(ref _panelGradient);
            DestroyTex(ref _bottomLayerGradient);
            DestroyTex(ref _embarkGradient);
            DestroyTex(ref _embarkHoverGradient);
            DestroyTex(ref _vignetteGradient);
            DestroyTex(ref _statBarHpGradient);
            DestroyTex(ref _statBarStaGradient);
            DestroyTex(ref _statBarAtkGradient);
            DestroyTex(ref _statBarDefGradient);
            DestroyTex(ref _champAreaGradient);
            DestroyTex(ref _champModelGradient);
            DestroyTex(ref _panelTopHighlight);
            DestroyTex(ref _heroCardBaseGradient);

            foreach (var tex in _heroCardSelectedGradients.Values)
            {
                if (tex != null) Destroy(tex);
            }
            _heroCardSelectedGradients.Clear();

            foreach (var tex in _heroCardGlowTextures.Values)
            {
                if (tex != null) Destroy(tex);
            }
            _heroCardGlowTextures.Clear();
        }

        private void DestroyTex(ref Texture2D tex)
        {
            if (tex != null) { Destroy(tex); tex = null; }
        }
    }
}
