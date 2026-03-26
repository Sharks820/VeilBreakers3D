using UnityEngine;
using System.Collections;

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

    private void TriggerScreenEffect(float magnitude)
    {
        if (!screenEffectsEnabled) return;

        // Brief white flash
        StartCoroutine(ScreenFlash(magnitude));
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
            float originalIntensity = ca.intensity.value;
            ca.intensity.value = chromaticAberration * magnitude;
            yield return new WaitForSeconds(flashDuration * magnitude);
            ca.intensity.value = originalIntensity;
        }
        else
        {
            yield return new WaitForSeconds(flashDuration);
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
        StartCoroutine(SpawnHitEffect(hitPoint, hitRotation, magnitude));
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

        yield return new WaitForSeconds(effectDuration * magnitude);

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
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", glowColor);
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
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", brandColor);
    }
}
