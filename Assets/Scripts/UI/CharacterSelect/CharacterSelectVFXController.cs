using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// VFX Controller for Character Select Screen
    /// Manages particles, atmospheric effects, and visual polish
    /// 
    /// OPTIMIZED VERSION - Memory-efficient, pooled resources
    /// </summary>
    public class CharacterSelectVFXController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem heroAuraParticles;
        [SerializeField] private ParticleSystem monsterAuraParticles;
        [SerializeField] private ParticleSystem environmentParticles;
        [SerializeField] private ParticleSystem synergyTetherParticles;
        
        [Header("Particle Prefabs by Hero")]
        [SerializeField] private ParticleSystem vexParticles;      // Iron filings, sparks
        [SerializeField] private ParticleSystem seraphinaParticles; // Poison mist, petals
        [SerializeField] private ParticleSystem orionParticles;     // Lightning, static
        [SerializeField] private ParticleSystem nyxParticles;       // Shadow wisps, fragments
        
        [Header("Lighting")]
        [SerializeField] private Light heroRimLight;
        [SerializeField] private Light fillLight;
        [SerializeField] private float lightTransitionDuration = 0.8f;
        
        [Header("Camera")]
        [SerializeField] private Camera renderCamera;
        [SerializeField] private float cameraBobAmount = 0.05f;
        [SerializeField] private float cameraBobSpeed = 0.5f;
        
        #endregion
        
        #region Private Fields
        
        // Runtime
        private ParticleSystem currentHeroParticles;
        private ParticleSystem currentEnvParticles;
        private Dictionary<string, ParticleSystem> heroParticleMap;
        private Coroutine lightCoroutine;
        private Vector3 cameraBasePosition;
        
        // Pooled resources to avoid GC
        private static readonly Gradient SharedGradient = new();
        private static readonly GradientColorKey[] SharedColorKeys = new GradientColorKey[2];
        private static readonly GradientAlphaKey[] SharedAlphaKeys = new GradientAlphaKey[2];
        
        // Pre-allocated arrays for emission rate caching
        private readonly Dictionary<ParticleSystem, float> baseEmissionRates = new(8);
        
        // Hero Colors - readonly
        private static readonly Dictionary<string, Color> HeroColors = new(4)
        {
            { "vex", new Color(0.55f, 0.59f, 0.65f) },
            { "seraphina", new Color(0.31f, 0.71f, 0.24f) },
            { "orion", new Color(0.24f, 0.55f, 0.86f) },
            { "nyx", new Color(0.63f, 0.16f, 0.27f) }
        };
        
        // Seeded random for deterministic camera shake
        private System.Random random;
        private bool isDestroyed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeParticleMap();
            CacheBaseEmissionRates();
            
            if (renderCamera != null)
            {
                cameraBasePosition = renderCamera.transform.position;
            }
            
            // Seeded random for consistent behavior
            random = new System.Random(12345);
        }
        
        private void Start()
        {
            DisableAllParticles();
        }
        
        private void Update()
        {
            UpdateCameraBob();
        }
        
        private void OnDestroy()
        {
            isDestroyed = true;
            
            if (lightCoroutine != null)
            {
                StopCoroutine(lightCoroutine);
                lightCoroutine = null;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeParticleMap()
        {
            heroParticleMap = new Dictionary<string, ParticleSystem>(4)
            {
                { "vex", vexParticles },
                { "seraphina", seraphinaParticles },
                { "orion", orionParticles },
                { "nyx", nyxParticles }
            };
        }
        
        private void CacheBaseEmissionRates()
        {
            baseEmissionRates.Clear();
            
            if (environmentParticles != null)
            {
                baseEmissionRates[environmentParticles] = environmentParticles.emission.rateOverTime.constant;
            }
            
            if (synergyTetherParticles != null)
            {
                baseEmissionRates[synergyTetherParticles] = synergyTetherParticles.emission.rateOverTime.constant;
            }
            
            // Cache hero particle rates
            foreach (var kvp in heroParticleMap)
            {
                if (kvp.Value != null)
                {
                    baseEmissionRates[kvp.Value] = kvp.Value.emission.rateOverTime.constant;
                }
            }
        }
        
        private void DisableAllParticles()
        {
            foreach (var particles in heroParticleMap.Values)
            {
                if (particles != null && particles.gameObject != null)
                {
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particles.gameObject.SetActive(false);
                }
            }
            
            if (environmentParticles != null)
            {
                environmentParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            
            if (synergyTetherParticles != null)
            {
                synergyTetherParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        #endregion
        
        #region Hero VFX
        
        public void OnHeroChanged(string heroId)
        {
            if (isDestroyed) return;
            
            // Stop current hero particles
            if (currentHeroParticles != null)
            {
                StartCoroutine(FadeOutParticles(currentHeroParticles));
            }
            
            // Start new hero particles
            if (heroParticleMap.TryGetValue(heroId, out var newParticles) && newParticles != null)
            {
                newParticles.gameObject.SetActive(true);
                newParticles.Play(true);
                currentHeroParticles = newParticles;
            }
            
            // Update lighting
            if (HeroColors.TryGetValue(heroId, out var color))
            {
                UpdateLighting(color);
                UpdateEnvironmentEffects(heroId, color);
            }
        }
        
        private IEnumerator FadeOutParticles(ParticleSystem particles)
        {
            if (particles == null || isDestroyed) yield break;
            
            var emission = particles.emission;
            float originalRate = baseEmissionRates.GetValueOrDefault(particles, emission.rateOverTime.constant);
            
            // Gradually reduce emission
            float elapsed = 0;
            const float duration = 0.5f;
            
            while (elapsed < duration && !isDestroyed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                emission.rateOverTime = Mathf.Lerp(originalRate, 0, t);
                yield return null;
            }
            
            if (!isDestroyed && particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (particles.gameObject != null)
                {
                    particles.gameObject.SetActive(false);
                }
            }
            
            // Reset emission rate for next use
            emission.rateOverTime = originalRate;
        }
        
        private void UpdateLighting(Color heroColor)
        {
            if (lightCoroutine != null)
            {
                StopCoroutine(lightCoroutine);
            }
            
            lightCoroutine = StartCoroutine(AnimateLighting(heroColor));
        }
        
        private IEnumerator AnimateLighting(Color targetColor)
        {
            if (heroRimLight == null || isDestroyed) yield break;
            
            Color startColor = heroRimLight.color;
            float startIntensity = heroRimLight.intensity;
            const float targetIntensity = 1.5f;
            
            float elapsed = 0;
            while (elapsed < lightTransitionDuration && !isDestroyed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lightTransitionDuration;
                float eased = EaseOutExpo(t);
                
                heroRimLight.color = Color.Lerp(startColor, targetColor, eased);
                heroRimLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, eased);
                
                yield return null;
            }
            
            if (!isDestroyed && heroRimLight != null)
            {
                heroRimLight.color = targetColor;
                heroRimLight.intensity = targetIntensity;
            }
        }
        
        private void UpdateEnvironmentEffects(string heroId, Color color)
        {
            if (environmentParticles == null) return;
            
            var main = environmentParticles.main;
            var colorOverLifetime = environmentParticles.colorOverLifetime;
            
            // Reuse shared gradient to avoid allocation
            SharedColorKeys[0] = new GradientColorKey(color, 0f);
            SharedColorKeys[1] = new GradientColorKey(color * 0.5f, 1f);
            
            SharedAlphaKeys[0] = new GradientAlphaKey(0.3f, 0f);
            SharedAlphaKeys[1] = new GradientAlphaKey(0f, 1f);
            
            SharedGradient.SetKeys(SharedColorKeys, SharedAlphaKeys);
            colorOverLifetime.color = SharedGradient;
            
            // Adjust emission based on hero using cached values
            var emission = environmentParticles.emission;
            float baseRate = baseEmissionRates.GetValueOrDefault(environmentParticles, 10f);
            
            switch (heroId)
            {
                case "vex":
                    emission.rateOverTime = baseRate;
                    main.startSpeed = 0.5f;
                    break;
                case "seraphina":
                    emission.rateOverTime = baseRate * 2.5f;
                    main.startSpeed = 1f;
                    break;
                case "orion":
                    emission.rateOverTime = baseRate * 5f;
                    main.startSpeed = 3f;
                    break;
                case "nyx":
                    emission.rateOverTime = baseRate * 1.5f;
                    main.startSpeed = 0.3f;
                    break;
            }
            
            environmentParticles.Play(true);
        }
        
        #endregion
        
        #region Effects
        
        public void TriggerSynergyBurst(int synergyRating)
        {
            if (synergyTetherParticles == null || isDestroyed) return;
            
            var main = synergyTetherParticles.main;
            var emission = synergyTetherParticles.emission;
            
            float baseRate = baseEmissionRates.GetValueOrDefault(synergyTetherParticles, 10f);
            emission.rateOverTime = synergyRating * baseRate;
            
            // Different colors based on rating
            switch (synergyRating)
            {
                case 5: // Perfect
                    main.startColor = new Color(1f, 0.8f, 0.2f); // Gold
                    break;
                case 4: // Strong
                    main.startColor = new Color(0.4f, 0.8f, 1f); // Cyan
                    break;
                default: // Normal
                    main.startColor = new Color(0.7f, 0.7f, 0.7f); // White
                    break;
            }
            
            synergyTetherParticles.Play(true);
            StartCoroutine(SynergyBurstPulse(baseRate));
        }
        
        private IEnumerator SynergyBurstPulse(float baseRate)
        {
            if (synergyTetherParticles == null || isDestroyed) yield break;
            
            var emission = synergyTetherParticles.emission;
            emission.rateOverTime = baseRate * 3;
            
            yield return new WaitForSeconds(0.3f);
            
            if (!isDestroyed && synergyTetherParticles != null)
            {
                emission.rateOverTime = baseRate;
            }
        }
        
        public void TriggerLightningFlash()
        {
            if (isDestroyed) return;
            StartCoroutine(LightningFlashSequence());
        }
        
        private IEnumerator LightningFlashSequence()
        {
            if (renderCamera == null || isDestroyed) yield break;
            
            Color originalColor = renderCamera.backgroundColor;
            renderCamera.backgroundColor = new Color(0.9f, 0.95f, 1f);
            
            yield return new WaitForSeconds(0.05f);
            
            if (isDestroyed) yield break;
            
            float elapsed = 0;
            const float duration = 0.3f;
            
            while (elapsed < duration && !isDestroyed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                renderCamera.backgroundColor = Color.Lerp(
                    new Color(0.9f, 0.95f, 1f), 
                    originalColor, 
                    t
                );
                yield return null;
            }
            
            if (!isDestroyed && renderCamera != null)
            {
                renderCamera.backgroundColor = originalColor;
            }
        }
        
        public void TriggerCorruptionEffect()
        {
            if (isDestroyed) return;
            StartCoroutine(CorruptionSequence());
        }
        
        private IEnumerator CorruptionSequence()
        {
            if (renderCamera == null || isDestroyed) yield break;
            
            Vector3 originalPos = cameraBasePosition;
            
            float elapsed = 0;
            const float duration = 0.8f;
            
            while (elapsed < duration && !isDestroyed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Intense shake that fades out
                float shake = (1 - t) * 0.1f;
                Vector3 shakeOffset = new(
                    (float)(random.NextDouble() * 2 - 1) * shake,
                    (float)(random.NextDouble() * 2 - 1) * shake,
                    0
                );
                
                renderCamera.transform.position = originalPos + shakeOffset;
                
                yield return null;
            }
            
            if (!isDestroyed && renderCamera != null)
            {
                renderCamera.transform.position = originalPos;
            }
        }
        
        public void TriggerConfirmEffect()
        {
            if (isDestroyed) return;
            StartCoroutine(ConfirmSequence());
        }
        
        private IEnumerator ConfirmSequence()
        {
            // Burst all particles
            if (currentHeroParticles != null && !isDestroyed)
            {
                var emission = currentHeroParticles.emission;
                float originalRate = baseEmissionRates.GetValueOrDefault(currentHeroParticles, 10f);
                
                emission.rateOverTime = originalRate * 5;
                
                yield return new WaitForSeconds(0.5f);
                
                if (!isDestroyed && currentHeroParticles != null)
                {
                    emission.rateOverTime = originalRate;
                }
            }
            
            // Screen flash
            if (renderCamera != null && !isDestroyed)
            {
                Color originalColor = renderCamera.backgroundColor;
                renderCamera.backgroundColor = Color.white;
                
                yield return new WaitForSeconds(0.1f);
                
                if (!isDestroyed && renderCamera != null)
                {
                    renderCamera.backgroundColor = originalColor;
                }
            }
        }
        
        #endregion
        
        #region Camera
        
        private void UpdateCameraBob()
        {
            if (renderCamera == null) return;
            
            float time = Time.time * cameraBobSpeed;
            Vector3 offset = new(
                Mathf.Sin(time) * cameraBobAmount,
                Mathf.Cos(time * 0.7f) * cameraBobAmount * 0.5f,
                0
            );
            renderCamera.transform.position = cameraBasePosition + offset;
        }
        
        #endregion
        
        #region Utility
        
        private static float EaseOutExpo(float t)
        {
            return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }
        
        #endregion
    }
}
