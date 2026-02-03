using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static VeilBreakers.Core.GameDataTypes;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// AAA Character Select Screen Controller
    /// Cinematic, immersive hero selection with parallax backgrounds,
    /// animated stat pillars, and dramatic transitions.
    /// 
    /// OPTIMIZED VERSION - Memory-efficient, null-safe, high performance
    /// </summary>
    public class CharacterSelectControllerAAA : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Data Sources")]
        [SerializeField] private TextAsset heroesJson;
        [SerializeField] private TextAsset monstersJson;
        
        [Header("Visual Settings")]
        [SerializeField] private float heroSwitchDuration = 1.2f;
        [SerializeField] private float statAnimationDuration = 0.8f;
        [SerializeField] private float quoteDelay = 1.0f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip ambientLoop;
        [SerializeField] private AudioClip heroSelectSFX;
        [SerializeField] private AudioClip confirmSFX;
        
        #endregion
        
        #region Private Fields
        
        // UI Document
        private UIDocument uiDocument;
        private VisualElement rootElement;
        
        // Cached Camera reference (avoid repeated Camera.main calls)
        private Camera mainCamera;
        
        // Data
        private readonly List<HeroData> heroes = new(4); // Pre-allocate for 4 heroes
        private readonly List<MonsterData> monsters = new(32); // Pre-allocate
        private int currentHeroIndex;
        
        // Hero Color Definitions - readonly for thread safety and performance
        private static readonly Dictionary<string, Color> HeroColors = new(4)
        {
            { "vex", new Color(0.55f, 0.59f, 0.65f) },        // Steel
            { "seraphina", new Color(0.31f, 0.71f, 0.24f) },  // Venom Green
            { "orion", new Color(0.24f, 0.55f, 0.86f) },      // Lightning Blue
            { "nyx", new Color(0.63f, 0.16f, 0.27f) }         // Void Crimson
        };
        
        // Environment Backgrounds per Hero
        private static readonly Dictionary<string, string> HeroEnvironments = new(4)
        {
            { "vex", "prison_ruins" },
            { "seraphina", "poison_garden" },
            { "orion", "storm_summit" },
            { "nyx", "shadow_vault" }
        };
        
        // String builder for efficient string concatenation
        private readonly StringBuilder stringBuilder = new(128);
        
        #endregion
        
        #region UI Element References
        
        // Background
        private VisualElement bgLayerVoid;
        private VisualElement bgLayerFog;
        private VisualElement bgLayerMid;
        private VisualElement bgLayerNear;
        private VisualElement vignetteOverlay;
        private VisualElement transitionOverlay;
        
        // Hero Display
        private VisualElement heroDisplay;
        private VisualElement heroSilhouette;
        private VisualElement heroAuraContainer;
        private VisualElement heroEyes;
        private VisualElement monsterOrbit;
        private VisualElement monsterModel;
        private VisualElement synergyTether;
        private VisualElement envEffects;
        
        // Right Panel
        private VisualElement rightPanel;
        private Label pathName;
        private VisualElement pathIcon;
        private readonly VisualElement[] statPillars = new VisualElement[6];
        private readonly Label[] statValues = new Label[6];
        private readonly VisualElement[] statFills = new VisualElement[6];
        private readonly VisualElement[] abilityCards = new VisualElement[3];
        private VisualElement monsterThumbnail;
        private Label monsterName;
        private Label monsterType;
        private readonly VisualElement[] synergyStars = new VisualElement[5];
        
        // Hero Info
        private Label heroNameDisplay;
        private Label heroTitleDisplay;
        private Label heroQuote;
        
        // Carousel
        private VisualElement carouselTrack;
        private readonly VisualElement[] heroCards = new VisualElement[4];
        private Button carouselPrev;
        private Button carouselNext;
        
        // Navigation
        private Button btnBack;
        private Button btnSettings;
        private Button btnEmbark;
        private Label embarkText;
        
        // VFX
        private VisualElement vfxOverlay;
        private VisualElement lightningFlash;
        private VisualElement corruptionFx;
        private VisualElement screenFade;
        
        #endregion
        
        #region State
        
        private bool isAnimating;
        private Coroutine quoteCoroutine;
        private bool isDestroyed;
        
        // Pre-allocated stat array to avoid GC
        private readonly int[] statValuesArray = new int[6];
        private static readonly string[] StatNames = { "str", "dex", "con", "int", "wis", "cha" };
        private static readonly string[] HeroIds = { "vex", "seraphina", "orion", "nyx" };
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Cache Camera.main once
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CharacterSelectControllerAAA] Camera.main not found. Audio spatialization may not work.");
            }
            
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("[CharacterSelectControllerAAA] UIDocument component required!");
                enabled = false;
                return;
            }
            
            rootElement = uiDocument.rootVisualElement;
            if (rootElement == null)
            {
                Debug.LogError("[CharacterSelectControllerAAA] Root visual element is null!");
                enabled = false;
                return;
            }
            
            CacheUIElements();
            LoadData();
        }
        
        private void Start()
        {
            if (!enabled) return;
            
            SetupEventHandlers();
            StartCoroutine(InitialRevealSequence());
        }
        
        private void OnDestroy()
        {
            isDestroyed = true;
            CleanupEventHandlers();
            
            // Stop all coroutines
            if (quoteCoroutine != null)
            {
                StopCoroutine(quoteCoroutine);
                quoteCoroutine = null;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void CacheUIElements()
        {
            if (rootElement == null) return;
            
            try
            {
                // Background
                bgLayerVoid = rootElement.Q<VisualElement>("bg-layer-void");
                bgLayerFog = rootElement.Q<VisualElement>("bg-layer-fog");
                bgLayerMid = rootElement.Q<VisualElement>("bg-layer-mid");
                bgLayerNear = rootElement.Q<VisualElement>("bg-layer-near");
                vignetteOverlay = rootElement.Q<VisualElement>("vignette-overlay");
                transitionOverlay = rootElement.Q<VisualElement>("transition-overlay");
                
                // Hero Display
                heroDisplay = rootElement.Q<VisualElement>("hero-display");
                heroSilhouette = rootElement.Q<VisualElement>("hero-silhouette");
                heroAuraContainer = rootElement.Q<VisualElement>("hero-aura-container");
                heroEyes = rootElement.Q<VisualElement>("hero-eyes");
                monsterOrbit = rootElement.Q<VisualElement>("monster-orbit");
                monsterModel = rootElement.Q<VisualElement>("monster-model");
                synergyTether = rootElement.Q<VisualElement>("synergy-tether");
                envEffects = rootElement.Q<VisualElement>("env-effects");
                
                // Right Panel
                rightPanel = rootElement.Q<VisualElement>("right-panel");
                pathName = rootElement.Q<Label>("path-name");
                pathIcon = rootElement.Q<VisualElement>("path-icon");
                monsterThumbnail = rootElement.Q<VisualElement>("monster-thumbnail");
                monsterName = rootElement.Q<Label>("monster-name");
                monsterType = rootElement.Q<Label>("monster-type");
                
                // Stat Pillars - use array for O(1) access
                for (int i = 0; i < 6; i++)
                {
                    string stat = StatNames[i];
                    statPillars[i] = rootElement.Q<VisualElement>($"stat-{stat}");
                    statValues[i] = rootElement.Q<Label>($"{stat}-value");
                    statFills[i] = rootElement.Q<VisualElement>($"{stat}-fill");
                }
                
                // Synergy Stars
                for (int i = 0; i < 5; i++)
                {
                    synergyStars[i] = rootElement.Q<VisualElement>($"star-{i + 1}");
                }
                
                // Ability Cards
                for (int i = 0; i < 3; i++)
                {
                    abilityCards[i] = rootElement.Q<VisualElement>($"ability-{i + 1}");
                }
                
                // Hero Info
                heroNameDisplay = rootElement.Q<Label>("hero-name-display");
                heroTitleDisplay = rootElement.Q<Label>("hero-title-display");
                heroQuote = rootElement.Q<Label>("hero-quote");
                
                // Carousel
                carouselTrack = rootElement.Q<VisualElement>("carousel-track");
                carouselPrev = rootElement.Q<Button>("carousel-prev");
                carouselNext = rootElement.Q<Button>("carousel-next");
                
                // Hero Cards
                for (int i = 0; i < 4; i++)
                {
                    heroCards[i] = rootElement.Q<VisualElement>($"hero-card-{HeroIds[i]}");
                }
                
                // Navigation
                btnBack = rootElement.Q<Button>("btn-back");
                btnSettings = rootElement.Q<Button>("btn-settings");
                btnEmbark = rootElement.Q<Button>("btn-embark");
                embarkText = rootElement.Q<Label>("embark-text");
                
                // VFX
                vfxOverlay = rootElement.Q<VisualElement>("vfx-overlay");
                lightningFlash = rootElement.Q<VisualElement>("lightning-flash");
                corruptionFx = rootElement.Q<VisualElement>("corruption-fx");
                screenFade = rootElement.Q<VisualElement>("screen-fade");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CharacterSelectControllerAAA] Error caching UI elements: {ex.Message}");
            }
        }
        
        private void LoadData()
        {
            // Load heroes with error handling
            if (heroesJson != null && !string.IsNullOrEmpty(heroesJson.text))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<HeroListWrapper>(WrapJsonArray(heroesJson.text, "heroes"));
                    if (wrapper?.heroes != null)
                    {
                        heroes.Clear();
                        heroes.AddRange(wrapper.heroes);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CharacterSelectControllerAAA] Failed to load heroes: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError("[CharacterSelectControllerAAA] heroesJson is null or empty!");
            }
            
            // Load monsters with error handling
            if (monstersJson != null && !string.IsNullOrEmpty(monstersJson.text))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<MonsterListWrapper>(WrapJsonArray(monstersJson.text, "monsters"));
                    if (wrapper?.monsters != null)
                    {
                        monsters.Clear();
                        monsters.AddRange(wrapper.monsters);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CharacterSelectControllerAAA] Failed to load monsters: {ex.Message}");
                }
            }
            
            // Validate heroes exist
            if (heroes.Count == 0)
            {
                Debug.LogError("[CharacterSelectControllerAAA] No heroes loaded! Check heroes.json");
                enabled = false;
                return;
            }
            
            // Verify all expected heroes exist (non-blocking warning)
            for (int i = 0; i < HeroIds.Length; i++)
            {
                string heroId = HeroIds[i];
                bool found = false;
                for (int j = 0; j < heroes.Count; j++)
                {
                    if (heroes[j]?.hero_id == heroId)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogWarning($"[CharacterSelectControllerAAA] Hero '{heroId}' not found in heroes data!");
                }
            }
        }
        
        /// <summary>
        /// Unity's JsonUtility requires a wrapper object for arrays
        /// </summary>
        private static string WrapJsonArray(string jsonArray, string propertyName)
        {
            return $"{{\"{propertyName}\":{jsonArray}}}";
        }
        
        private void SetupEventHandlers()
        {
            // Carousel navigation
            carouselPrev?.RegisterCallback<ClickEvent>(OnCarouselPrevClicked);
            carouselNext?.RegisterCallback<ClickEvent>(OnCarouselNextClicked);
            
            // Hero card clicks - pre-capture indices
            for (int i = 0; i < heroCards.Length; i++)
            {
                if (heroCards[i] != null)
                {
                    int index = i; // Capture for closure
                    heroCards[i].RegisterCallback<ClickEvent>(evt => OnHeroCardClicked(index));
                }
            }
            
            // Navigation buttons
            btnBack?.RegisterCallback<ClickEvent>(OnBackClicked);
            btnSettings?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            btnEmbark?.RegisterCallback<ClickEvent>(OnEmbarkClicked);
            
            // Keyboard navigation
            rootElement?.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }
        
        private void CleanupEventHandlers()
        {
            if (rootElement == null) return;
            
            try
            {
                carouselPrev?.UnregisterCallback<ClickEvent>(OnCarouselPrevClicked);
                carouselNext?.UnregisterCallback<ClickEvent>(OnCarouselNextClicked);
                
                for (int i = 0; i < heroCards.Length; i++)
                {
                    heroCards[i]?.UnregisterCallback<ClickEvent>(OnHeroCardClicked);
                }
                
                btnBack?.UnregisterCallback<ClickEvent>(OnBackClicked);
                btnSettings?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
                btnEmbark?.UnregisterCallback<ClickEvent>(OnEmbarkClicked);
                
                rootElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterSelectControllerAAA] Error during cleanup: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnCarouselPrevClicked(ClickEvent evt)
        {
            SelectPreviousHero();
        }
        
        private void OnCarouselNextClicked(ClickEvent evt)
        {
            SelectNextHero();
        }
        
        private void OnHeroCardClicked(int index)
        {
            SelectHero(index);
        }
        
        private void OnBackClicked(ClickEvent evt)
        {
            StartCoroutine(FadeAndNavigate("TitleScreen"));
        }
        
        private void OnSettingsClicked(ClickEvent evt)
        {
            Debug.Log("[CharacterSelectControllerAAA] Settings clicked - implement settings panel");
        }
        
        private void OnEmbarkClicked(ClickEvent evt)
        {
            OnEmbarkClicked();
        }
        
        #endregion
        
        #region Hero Selection
        
        private void SelectPreviousHero()
        {
            if (heroes.Count == 0) return;
            int newIndex = (currentHeroIndex - 1 + heroes.Count) % heroes.Count;
            SelectHero(newIndex);
        }
        
        private void SelectNextHero()
        {
            if (heroes.Count == 0) return;
            int newIndex = (currentHeroIndex + 1) % heroes.Count;
            SelectHero(newIndex);
        }
        
        private void SelectHero(int index)
        {
            if (isDestroyed) return;
            if (isAnimating || index == currentHeroIndex) return;
            if (index < 0 || index >= heroes.Count)
            {
                Debug.LogWarning($"[CharacterSelectControllerAAA] Invalid hero index: {index}");
                return;
            }
            
            currentHeroIndex = index;
            StartCoroutine(HeroSwitchSequence());
            
            // Play select sound
            if (heroSelectSFX != null && mainCamera != null)
            {
                AudioSource.PlayClipAtPoint(heroSelectSFX, mainCamera.transform.position, 0.5f);
            }
        }
        
        private IEnumerator HeroSwitchSequence()
        {
            if (isDestroyed) yield break;
            isAnimating = true;
            
            var hero = heroes[currentHeroIndex];
            if (hero == null)
            {
                Debug.LogError($"[CharacterSelectControllerAAA] Hero at index {currentHeroIndex} is null!");
                isAnimating = false;
                yield break;
            }
            
            if (!HeroColors.TryGetValue(hero.hero_id, out Color color))
            {
                color = Color.white;
            }
            
            // Cancel quote animation
            if (quoteCoroutine != null)
            {
                StopCoroutine(quoteCoroutine);
                quoteCoroutine = null;
            }
            heroQuote?.RemoveFromClassList("visible");
            
            // 1. Start transition effects
            transitionOverlay?.RemoveFromClassList("hidden");
            transitionOverlay?.AddToClassList("active");
            corruptionFx?.RemoveFromClassList("hidden");
            corruptionFx?.AddToClassList("active");
            
            yield return new WaitForSeconds(0.1f);
            
            // 2. Hero-specific effects
            if (hero.hero_id == "orion")
            {
                lightningFlash?.RemoveFromClassList("hidden");
                lightningFlash?.AddToClassList("active");
                yield return new WaitForSeconds(0.1f);
                lightningFlash?.RemoveFromClassList("active");
                lightningFlash?.AddToClassList("hidden");
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // 3. Update all visual elements
            UpdateCarouselState();
            UpdateHeroDisplay(hero, color);
            UpdateRightPanel(hero, color);
            UpdateHeroInfo(hero, color);
            UpdateEmbarkButton(hero);
            UpdateBackgroundEffects(hero, color);
            
            // 4. End transition
            yield return new WaitForSeconds(0.3f);
            
            transitionOverlay?.RemoveFromClassList("active");
            transitionOverlay?.AddToClassList("hidden");
            corruptionFx?.RemoveFromClassList("active");
            corruptionFx?.AddToClassList("hidden");
            
            // 5. Animate stats
            yield return StartCoroutine(AnimateStats(hero));
            
            // 6. Show quote after delay
            if (!isDestroyed)
            {
                quoteCoroutine = StartCoroutine(ShowQuoteAfterDelay());
            }
            
            isAnimating = false;
        }
        
        private void UpdateCarouselState()
        {
            for (int i = 0; i < heroCards.Length; i++)
            {
                if (heroCards[i] == null) continue;
                
                if (i == currentHeroIndex)
                {
                    heroCards[i].AddToClassList("selected");
                }
                else
                {
                    heroCards[i].RemoveFromClassList("selected");
                }
            }
        }
        
        private void UpdateHeroDisplay(HeroData hero, Color color)
        {
            if (hero == null) return;
            
            // Update aura color using USS classes instead of CSS variables
            UpdateHeroAuraColor(color);
            
            // Find starter monster - O(n) but with small n (32 monsters max)
            MonsterData starterMonster = null;
            if (!string.IsNullOrEmpty(hero.starter_monster_id))
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i]?.monster_id == hero.starter_monster_id)
                    {
                        starterMonster = monsters[i];
                        break;
                    }
                }
            }
            
            if (starterMonster != null && monsterModel != null)
            {
                // TODO: Load monster portrait
            }
            
            heroEyes?.RemoveFromClassList("hidden");
        }
        
        /// <summary>
        /// Update aura color by directly modifying style properties
        /// Unity UI Toolkit doesn't support setting CSS custom properties at runtime
        /// </summary>
        private void UpdateHeroAuraColor(Color color)
        {
            if (heroAuraContainer == null) return;
            
            // Set background color directly on aura elements
            var auraBase = heroAuraContainer.Q<VisualElement>("aura-base");
            var auraParticles = heroAuraContainer.Q<VisualElement>("aura-particles");
            var groundGlow = heroAuraContainer.Q<VisualElement>("ground-glow");
            
            Color rgbaColor = new Color(color.r, color.g, color.b, 0.6f);
            if (auraBase != null) auraBase.style.backgroundColor = rgbaColor;
            if (auraParticles != null) auraParticles.style.unityBackgroundImageTintColor = color;
            if (groundGlow != null) groundGlow.style.backgroundColor = new Color(color.r, color.g, color.b, 0.3f);
        }
        
        private void UpdateRightPanel(HeroData hero, Color color)
        {
            if (hero == null) return;
            
            // Update panel theming
            UpdatePanelColors(color);
            
            // Path info
            if (pathName != null)
            {
                pathName.text = ((Path)hero.primary_path).ToString().ToUpperInvariant();
            }
            
            // Find starter monster - O(n) search
            MonsterData starterMonster = null;
            if (!string.IsNullOrEmpty(hero.starter_monster_id))
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i]?.monster_id == hero.starter_monster_id)
                    {
                        starterMonster = monsters[i];
                        break;
                    }
                }
            }
            
            if (starterMonster != null)
            {
                if (monsterName != null) monsterName.text = starterMonster.display_name?.ToUpperInvariant() ?? "UNKNOWN";
                if (monsterType != null) 
                {
                    // Derive type from brand since monster_type field doesn't exist
                    string typeName = ((Brand)starterMonster.brand).ToString();
                    monsterType.text = typeName;
                }
            }
            else
            {
                if (monsterName != null) monsterName.text = "UNKNOWN";
                if (monsterType != null) monsterType.text = "";
            }
            
            // Calculate synergy
            int synergyRating = CalculateSynergyRating(hero);
            for (int i = 0; i < synergyStars.Length; i++)
            {
                if (synergyStars[i] == null) continue;
                
                if (i < synergyRating)
                {
                    synergyStars[i].AddToClassList("filled");
                }
                else
                {
                    synergyStars[i].RemoveFromClassList("filled");
                }
            }
            
            // Update ability cards
            UpdateAbilityCards(hero);
        }
        
        private void UpdatePanelColors(Color color)
        {
            if (rightPanel == null) return;
            
            // Update path badge background
            var pathBadge = rightPanel.Q<VisualElement>("path-badge");
            if (pathBadge != null)
            {
                pathBadge.style.backgroundColor = new Color(color.r, color.g, color.b, 0.15f);
                pathBadge.style.borderLeftColor = new Color(color.r, color.g, color.b, 0.3f);
            }
            
            // Update path name color
            if (pathName != null) pathName.style.color = color;
            
            // Update stat fills will be done in AnimateStats
        }
        
        private void UpdateAbilityCards(HeroData hero)
        {
            if (hero?.innate_skills == null) return;
            
            for (int i = 0; i < abilityCards.Length; i++)
            {
                if (abilityCards[i] == null) continue;
                if (i >= hero.innate_skills.Length) continue;
                
                string skillId = hero.innate_skills[i];
                var nameLabel = abilityCards[i].Q<Label>(className: "ability-name");
                var descLabel = abilityCards[i].Q<Label>(className: "ability-desc");
                
                string skillName = FormatSkillName(skillId);
                
                if (nameLabel != null) nameLabel.text = skillName.ToUpperInvariant();
                if (descLabel != null)
                {
                    descLabel.text = GetSkillDescription(skillId);
                    
                    // Mark ultimate ability on last card
                    if (i == abilityCards.Length - 1 && !string.IsNullOrEmpty(hero.ultimate_skill))
                    {
                        descLabel.AddToClassList("ultimate");
                    }
                }
            }
        }
        
        private void UpdateHeroInfo(HeroData hero, Color color)
        {
            if (hero == null) return;
            
            if (heroNameDisplay != null)
            {
                heroNameDisplay.text = hero.display_name?.ToUpperInvariant() ?? "";
                heroNameDisplay.style.color = color;
            }
            
            if (heroTitleDisplay != null)
            {
                heroTitleDisplay.text = hero.title?.ToUpperInvariant() ?? "";
            }
            
            if (heroQuote != null && !string.IsNullOrEmpty(hero.quote))
            {
                stringBuilder.Clear();
                stringBuilder.Append('"');
                stringBuilder.Append(hero.quote);
                stringBuilder.Append('"');
                heroQuote.text = stringBuilder.ToString();
            }
        }
        
        private void UpdateEmbarkButton(HeroData hero)
        {
            if (embarkText == null || hero == null) return;
            
            stringBuilder.Clear();
            stringBuilder.Append("EMBARK AS ");
            stringBuilder.Append(hero.display_name?.ToUpperInvariant() ?? "HERO");
            embarkText.text = stringBuilder.ToString();
        }
        
        private void UpdateBackgroundEffects(HeroData hero, Color color)
        {
            if (hero == null) return;
            
            // Update ambient glow
            UpdateAmbientGlow(color);
            
            // Environment would be loaded here
            if (HeroEnvironments.TryGetValue(hero.hero_id, out string environment))
            {
                // TODO: Load environment background
            }
        }
        
        private void UpdateAmbientGlow(Color color)
        {
            if (bgLayerNear == null) return;
            
            var ambientGlow = bgLayerNear.Q<VisualElement>("ambient-glow");
            if (ambientGlow != null)
            {
                ambientGlow.style.backgroundColor = new Color(color.r, color.g, color.b, 0.15f);
            }
        }
        
        private IEnumerator AnimateStats(HeroData hero)
        {
            if (hero?.base_stats == null)
            {
                Debug.LogWarning($"[CharacterSelectControllerAAA] Hero or base_stats is null for {hero?.hero_id}");
                yield break;
            }
            
            // Populate stat values
            statValuesArray[0] = hero.base_stats.strength;
            statValuesArray[1] = hero.base_stats.dexterity;
            statValuesArray[2] = hero.base_stats.constitution;
            statValuesArray[3] = hero.base_stats.intelligence;
            statValuesArray[4] = hero.base_stats.wisdom;
            statValuesArray[5] = hero.base_stats.charisma;
            
            // Find highest stat
            int maxStat = statValuesArray[0];
            int highestIndex = 0;
            for (int i = 1; i < statValuesArray.Length; i++)
            {
                if (statValuesArray[i] > maxStat)
                {
                    maxStat = statValuesArray[i];
                    highestIndex = i;
                }
            }
            
            // Get current hero color for theming
            if (!HeroColors.TryGetValue(hero.hero_id, out Color heroColor))
            {
                heroColor = Color.white;
            }
            
            // Reset pillars
            for (int i = 0; i < statPillars.Length; i++)
            {
                statPillars[i]?.RemoveFromClassList("highest");
            }
            statPillars[highestIndex]?.AddToClassList("highest");
            
            // Update stat bar colors
            for (int i = 0; i < statFills.Length; i++)
            {
                if (statFills[i] != null)
                {
                    statFills[i].style.backgroundColor = new Color(heroColor.r, heroColor.g, heroColor.b, 0.9f);
                }
            }
            
            // Animate each stat
            float elapsed = 0;
            while (elapsed < statAnimationDuration)
            {
                if (isDestroyed) yield break;
                
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / statAnimationDuration);
                float eased = EaseOutBack(t);
                
                for (int i = 0; i < statFills.Length && i < statValuesArray.Length; i++)
                {
                    if (statFills[i] == null || statValues[i] == null) continue;
                    
                    float targetHeight = (statValuesArray[i] / 20f) * 100f; // Max stat assumed 20
                    float currentHeight = targetHeight * eased;
                    statFills[i].style.height = new Length(currentHeight, LengthUnit.Percent);
                    statValues[i].text = Mathf.RoundToInt(statValuesArray[i] * eased).ToString();
                }
                
                yield return null;
            }
            
            // Ensure final values are set
            for (int i = 0; i < statFills.Length && i < statValuesArray.Length; i++)
            {
                if (statFills[i] == null || statValues[i] == null) continue;
                
                float targetHeight = (statValuesArray[i] / 20f) * 100f;
                statFills[i].style.height = new Length(targetHeight, LengthUnit.Percent);
                statValues[i].text = statValuesArray[i].ToString();
            }
        }
        
        private IEnumerator ShowQuoteAfterDelay()
        {
            heroQuote?.RemoveFromClassList("visible");
            yield return new WaitForSeconds(quoteDelay);
            
            if (!isDestroyed)
            {
                heroQuote?.AddToClassList("visible");
            }
        }
        
        private int CalculateSynergyRating(HeroData hero)
        {
            if (hero == null) return 3;
            
            int rating = 3; // Base rating
            
            if (string.IsNullOrEmpty(hero.starter_monster_id)) return rating;
            
            // Find starter monster
            MonsterData starterMonster = null;
            for (int i = 0; i < monsters.Count; i++)
            {
                if (monsters[i]?.monster_id == hero.starter_monster_id)
                {
                    starterMonster = monsters[i];
                    break;
                }
            }
            
            if (starterMonster == null) return rating;
            
            // Check brand match - supports both single brand and brands array
            bool hasBrandMatch = false;
            
            // First check brands array if it exists
            if (starterMonster.brands != null && starterMonster.brands.Length > 0)
            {
                for (int i = 0; i < starterMonster.brands.Length; i++)
                {
                    if (starterMonster.brands[i] == hero.primary_brand)
                    {
                        hasBrandMatch = true;
                        break;
                    }
                }
            }
            // Fall back to single brand field
            else if (starterMonster.brand == hero.primary_brand)
            {
                hasBrandMatch = true;
            }
            
            // Path-based adjustments
            rating = ((Path)hero.primary_path) switch
            {
                Path.IRONBOUND => hasBrandMatch ? 5 : 4,
                Path.FANGBORN => hasBrandMatch ? 5 : 3,
                Path.VOIDTOUCHED => hasBrandMatch ? 4 : 3,
                Path.UNCHAINED => hasBrandMatch ? 5 : 4,
                _ => 3
            };
            
            return Mathf.Clamp(rating, 1, 5);
        }
        
        #endregion
        
        #region Initial Reveal
        
        private IEnumerator InitialRevealSequence()
        {
            isAnimating = true;
            
            // 1. Background fades in
            bgLayerVoid?.RemoveFromClassList("hidden");
            yield return new WaitForSeconds(0.3f);
            
            // 2. Carousel slides up
            carouselTrack?.RemoveFromClassList("hidden");
            yield return new WaitForSeconds(0.2f);
            
            // 3. Auto-select first hero (Vex preferred)
            HeroData hero = null;
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i]?.hero_id == "vex")
                {
                    hero = heroes[i];
                    currentHeroIndex = i;
                    break;
                }
            }
            
            // Fallback to first hero
            if (hero == null && heroes.Count > 0)
            {
                hero = heroes[0];
                currentHeroIndex = 0;
            }
            
            if (hero != null)
            {
                if (!HeroColors.TryGetValue(hero.hero_id, out Color color))
                {
                    color = Color.white;
                }
                
                UpdateCarouselState();
                UpdateHeroDisplay(hero, color);
                UpdateRightPanel(hero, color);
                UpdateHeroInfo(hero, color);
                UpdateEmbarkButton(hero);
                UpdateBackgroundEffects(hero, color);
            }
            
            yield return new WaitForSeconds(0.3f);
            
            // 4. Hero display fades in
            heroDisplay?.RemoveFromClassList("hidden");
            yield return new WaitForSeconds(0.2f);
            
            // 5. Right panel animates in
            rightPanel?.RemoveFromClassList("hidden");
            yield return new WaitForSeconds(0.2f);
            
            // 6. Stats animate
            if (hero != null)
            {
                yield return StartCoroutine(AnimateStats(hero));
            }
            
            // 7. Hero info appears
            yield return new WaitForSeconds(0.2f);
            
            // 8. Monster fades in
            monsterOrbit?.RemoveFromClassList("hidden");
            yield return new WaitForSeconds(0.2f);
            
            // 9. Embark button activates
            btnEmbark?.SetEnabled(true);
            
            // 10. Quote appears
            if (!isDestroyed)
            {
                quoteCoroutine = StartCoroutine(ShowQuoteAfterDelay());
            }
            
            isAnimating = false;
        }
        
        #endregion
        
        #region Input Handling
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (isAnimating || isDestroyed) return;
            
            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow:
                case KeyCode.A:
                    SelectPreviousHero();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.RightArrow:
                case KeyCode.D:
                    SelectNextHero();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnEmbarkClicked();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.Escape:
                    StartCoroutine(FadeAndNavigate("TitleScreen"));
                    evt.StopPropagation();
                    break;
            }
        }
        
        #endregion
        
        #region Navigation
        
        private void OnEmbarkClicked()
        {
            if (isAnimating || isDestroyed) return;
            if (heroes.Count == 0 || currentHeroIndex < 0 || currentHeroIndex >= heroes.Count) return;
            
            var selectedHero = heroes[currentHeroIndex];
            if (selectedHero == null) return;
            
            // Play confirm sound
            if (confirmSFX != null && mainCamera != null)
            {
                AudioSource.PlayClipAtPoint(confirmSFX, mainCamera.transform.position, 0.7f);
            }
            
            // Save hero selection
            PlayerPrefs.SetString("SelectedHero", selectedHero.hero_id);
            PlayerPrefs.Save();
            
            // Navigate to game
            StartCoroutine(FadeAndNavigate("MainGame"));
        }
        
        private IEnumerator FadeAndNavigate(string sceneName)
        {
            screenFade?.RemoveFromClassList("hidden");
            screenFade?.AddToClassList("active");
            
            yield return new WaitForSeconds(1.0f);
            
            if (!isDestroyed && !string.IsNullOrEmpty(sceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }
        
        #endregion
        
        #region Utility
        
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        private static string FormatSkillName(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return "Unknown";
            
            // Replace underscores with spaces and title case
            var words = skillId.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            return string.Join(" ", words);
        }

        private static string GetSkillDescription(string skillId)
        {
            return skillId?.ToLowerInvariant() switch
            {
                "attack_basic" => "Standard melee attack",
                "defend" => "Reduce incoming damage",
                "shackle_strike" => "Bind enemy with chains",
                "wardens_gaze" => "Intimidate nearby foes",
                "venom_strike" => "Apply deadly poison",
                "thorn_lash" => "Whip enemies with thorns",
                "arcing_bolt" => "Lightning jumps to nearby foes",
                "static_field" => "Damage enemies that approach",
                "memory_drain" => "Steal enemy's last action",
                "shadow_step" => "Teleport through darkness",
                _ => "Special ability"
            };
        }
        
        #endregion
    }
}
