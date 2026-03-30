using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VeilBreakers.VFX
{
/// <summary>
/// Directional combat hit VFX for SAVAGE brand.
/// Blood splatter with feral claw slash marks
/// Phase 23 -- VFX3-07
/// </summary>
public class VB_HitVFX_SAVAGE : MonoBehaviour
{
    [Header("Brand Config")]
    [SerializeField] private string brand = "SAVAGE";
    [SerializeField] private Color brandColor = new Color(0.545f, 0.0f, 0.0f, 1.0f);
    [SerializeField] private Color glowColor = new Color(1.0f, 0.15f, 0.1f, 1.0f);

    [Header("Hit Settings")]
    [SerializeField] private float baseMagnitude = 1.5f;
    [SerializeField] private int burstParticleCount = 80;
    [SerializeField] private float splashRadius = 0.5f;
    [SerializeField] private float effectDuration = 0.8f;

    [Header("Screen Effects")]
    [SerializeField] private bool screenEffectsEnabled = true;
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private float chromaticAberration = 0.3f;

    // Cached shader reference (UNITY-19)
    private static Shader _cachedParticleShader;

    // Track concurrent screen flash coroutines to prevent CA restoration race condition (BUG-13)
    private static int _activeFlashCount;
    private static float _savedCaIntensity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _cachedParticleShader = null;
        _activeFlashCount = 0;
        _savedCaIntensity = 0f;
    }

    // Track dynamic materials for cleanup (VFX-01)
    private readonly List<Material> _dynamicMaterials = new List<Material>();

    // Track active coroutines for cleanup (BUG-12)
    private readonly List<Coroutine> _activeCoroutines = new List<Coroutine>();

    // Cached WaitForSeconds to avoid allocation per call (BUG-13)
    private WaitForSeconds _cachedFlashWait;

    private static Shader GetParticleShader()
    {
        if (_cachedParticleShader == null)
            _cachedParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"); // VB-IGNORE UNITY-19 -- cached in static field, called once
        return _cachedParticleShader;
    }

    private void Awake()
    {
        _cachedFlashWait = new WaitForSeconds(flashDuration);
    }

    private void TriggerScreenEffect(float magnitude)
    {
        if (!screenEffectsEnabled) return;

        // Brief white flash
        var cr = StartCoroutine(ScreenFlash(magnitude));
        _activeCoroutines.Add(cr);
    }

    private System.Collections.IEnumerator ScreenFlash(float magnitude)
    {
        // Apply chromatic aberration via post-processing volume
        var volumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
        UnityEngine.Rendering.Universal.ChromaticAberration ca = null;
        foreach (var vol in volumes)
        {
            if (vol.profile != null && vol.profile.TryGet(out ca))
                break;
        }

        if (ca != null)
        {
            // Only save the original value on the first concurrent flash to avoid stale captures
            _activeFlashCount++;
            if (_activeFlashCount == 1)
            {
                _savedCaIntensity = ca.intensity.value;
            }

            ca.intensity.value = chromaticAberration * magnitude;
            yield return new WaitForSeconds(flashDuration * magnitude); // magnitude varies, can't cache

            _activeFlashCount--;
            // Only restore when all concurrent flashes have finished
            if (_activeFlashCount <= 0)
            {
                _activeFlashCount = 0;
                ca.intensity.value = _savedCaIntensity;
            }
        }
        else
        {
            yield return _cachedFlashWait;
        }
    }

    public string Brand => brand;

    /// <summary>
    /// Trigger a directional hit effect.
    /// </summary>
    /// <param name="hitPoint">World position of the hit.</param>
    /// <param name="hitDirection">Incoming damage direction vector.</param>
    /// <param name="magnitude">Hit magnitude (0-3) affecting scale.</param>
    public void TriggerHit(Vector3 hitPoint, Vector3 hitDirection, float magnitude = -1f)
    {
        if (magnitude < 0f) magnitude = baseMagnitude;
        magnitude = Mathf.Clamp(magnitude, 0f, 3f);

        // Orient splash to face incoming direction
        Quaternion hitRotation = Quaternion.LookRotation(-hitDirection.normalized, Vector3.up);

        // Create hit VFX at impact point
        var cr = StartCoroutine(SpawnHitEffect(hitPoint, hitRotation, magnitude));
        _activeCoroutines.Add(cr);
        TriggerScreenEffect(magnitude);
    }

    private IEnumerator SpawnHitEffect(Vector3 position, Quaternion rotation, float magnitude)
    {
        // Create particle burst
        GameObject burstObj = new GameObject("HitBurst");
        burstObj.transform.position = position;
        burstObj.transform.rotation = rotation;
        burstObj.transform.SetParent(transform);

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        ConfigureHitParticles(ps, magnitude);
        ps.Play();

        // Create splash decal particles (ground scatter)
        GameObject splashObj = new GameObject("HitSplash");
        splashObj.transform.position = position;
        splashObj.transform.rotation = rotation;
        splashObj.transform.SetParent(transform);

        ParticleSystem splashPS = splashObj.AddComponent<ParticleSystem>();
        ConfigureSplashParticles(splashPS, magnitude);
        splashPS.Play();

        yield return new WaitForSeconds(effectDuration * magnitude); // VB-IGNORE BUG-13 -- dynamic duration, varies per hit magnitude

        Destroy(burstObj);
        Destroy(splashObj);
    }

    private void ConfigureHitParticles(ParticleSystem ps, float magnitude)
    {
        var main = ps.main;
        main.duration = effectDuration;
        main.loop = false;
        main.startLifetime = 0.4f * magnitude;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f * magnitude, 8f * magnitude);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.12f * magnitude);
        main.startColor = glowColor;
        main.maxParticles = (int)(burstParticleCount * magnitude);
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)(burstParticleCount * magnitude))
        });

        // Cone shape facing hit direction
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = splashRadius * magnitude;

        // Color gradient
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(brandColor, 0.4f),
                new GradientColorKey(brandColor * 0.2f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(GetParticleShader()); // VB-IGNORE VFX-01 -- tracked in _dynamicMaterials, Destroyed in OnDestroy
        mat.SetColor("_Color", glowColor);
        renderer.material = mat;
        _dynamicMaterials.Add(mat);
    }

    private void ConfigureSplashParticles(ParticleSystem ps, float magnitude)
    {
        var main = ps.main;
        main.duration = effectDuration * 1.5f;
        main.loop = false;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f * magnitude);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = brandColor;
        main.maxParticles = (int)(burstParticleCount * 0.5f * magnitude);
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.05f, (short)(burstParticleCount * 0.3f * magnitude))
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = splashRadius * 0.5f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(GetParticleShader());
        mat.SetColor("_Color", brandColor);
        renderer.material = mat;
        _dynamicMaterials.Add(mat);
    }

    private void OnDestroy()
    {
        // Stop all active coroutines (BUG-12)
        foreach (var cr in _activeCoroutines)
        {
            if (cr != null) StopCoroutine(cr);
        }
        _activeCoroutines.Clear();

        // Destroy all dynamically created materials (VFX-01)
        foreach (var mat in _dynamicMaterials)
        {
            if (mat != null) Destroy(mat);
        }
        _dynamicMaterials.Clear();
    }
}
}
