using UnityEngine;

namespace VeilBreakers.VFX
{
/// <summary>
/// Status effect VFX for SURGE brand: Shocked.
/// Lightning arcs jumping between random points
/// Phase 23 -- VFX3-05
/// </summary>
public class VB_StatusVFX_SURGE : MonoBehaviour
{
    [Header("Brand")]
    [SerializeField] private string brand = "SURGE";
    [SerializeField] private string effectName = "Shocked";

    [Header("Colors")]
    [SerializeField] private Color brandColor = new Color(0.0f, 0.75f, 1.0f, 1.0f);
    [SerializeField] private Color glowColor = new Color(0.3f, 0.85f, 1.0f, 1.0f);

    [Header("Effect Settings")]
    [SerializeField] private float intensity = 1.0f;
    [SerializeField] private float orbitSpeed = 300.0f;
    [SerializeField] private float orbitRadius = 0.5f;
    [SerializeField] private float glowPulseSpeed = 6.0f;

    // Runtime
    private ParticleSystem mainPS;
    private ParticleSystem secondaryPS;
    private LineRenderer lineRenderer;
    private float pulsePhase = 0f;
    private Renderer[] targetRenderers;
    private MaterialPropertyBlock _mpb;

    public string Brand => brand;
    public string EffectName => effectName;
    public float Intensity => intensity;

    public void SetIntensity(float newIntensity)
    {
        intensity = Mathf.Clamp01(newIntensity);
        UpdateIntensity();
    }

    private void Start()
    {
        targetRenderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        CreateMainParticleSystem();
        CreateSecondaryParticleSystem();
        // Lightning arc line renderer
        GameObject lrObj = new GameObject("LightningArc");
        lrObj.transform.SetParent(transform);
        lineRenderer = lrObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        lineRenderer.material.SetColor("_Color", glowColor);
    }

    private void Update()
    {
        // Glow pulse
        pulsePhase += Time.deltaTime * glowPulseSpeed;
        float pulse = (Mathf.Sin(pulsePhase) * 0.5f + 0.5f) * intensity;

        // Apply glow to renderers via MaterialPropertyBlock
        if (targetRenderers != null)
        {
            foreach (var rend in targetRenderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", glowColor * pulse * 2f);
                rend.SetPropertyBlock(_mpb);
            }
        }

        // Orbit main particles around target
        if (orbitSpeed > 0f && mainPS != null)
        {
            float angle = Time.time * orbitSpeed * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
            mainPS.transform.localPosition = offset;
        }

        UpdateSecondaryEffect();
    }

    private void UpdateIntensity()
    {
        if (mainPS != null)
        {
            var emission = mainPS.emission;
            emission.rateOverTime = 30f * intensity;
        }
        if (secondaryPS != null)
        {
            var emission = secondaryPS.emission;
            emission.rateOverTime = 20f * intensity;
        }
    }

    private void CreateMainParticleSystem()
    {
        GameObject psObj = new GameObject("StatusVFX_Main");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;

        mainPS = psObj.AddComponent<ParticleSystem>();
        var main = mainPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 8.0f;
        main.startSize = 0.1f * intensity;
        main.startColor = brandColor;
        main.maxParticles = 100;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = mainPS.emission;
        emission.rateOverTime = 30f * intensity;

        var shape = mainPS.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = 0.3f;

        // Color over lifetime
        var col = mainPS.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(brandColor, 0.5f),
                new GradientColorKey(brandColor * 0.3f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(intensity, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = mainPS.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", glowColor);
    }

    private void CreateSecondaryParticleSystem()
    {
        GameObject psObj = new GameObject("StatusVFX_Secondary");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;

        secondaryPS = psObj.AddComponent<ParticleSystem>();
        var main = secondaryPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = 1.0f;
        main.startSpeed = 1f;
        main.startSize = 0.08f;
        main.startColor = glowColor;
        main.maxParticles = 50;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = secondaryPS.emission;
        emission.rateOverTime = 0; // Emit manually in UpdateSecondaryEffect

        var renderer = secondaryPS.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", brandColor);
    }

    private void UpdateSecondaryEffect()
    {
        // Lightning arcs: line renderer between random points
        arcTimer -= Time.deltaTime;
        if (arcTimer <= 0f && lineRenderer != null)
        {
            arcTimer = Random.Range(0.05f, 0.2f) / intensity;
            Vector3 start = transform.position + Random.insideUnitSphere * 0.5f;
            Vector3 end = transform.position + Random.insideUnitSphere * 1.5f;
            lineRenderer.positionCount = 4;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, Vector3.Lerp(start, end, 0.33f) + Random.insideUnitSphere * 0.3f);
            lineRenderer.SetPosition(2, Vector3.Lerp(start, end, 0.66f) + Random.insideUnitSphere * 0.3f);
            lineRenderer.SetPosition(3, end);
            lineRenderer.startColor = glowColor;
            lineRenderer.endColor = brandColor;
            lineRenderer.startWidth = 0.05f * intensity;
            lineRenderer.endWidth = 0.02f * intensity;
        }
    }
    private float arcTimer = 0f;

    private void OnDestroy()
    {
        // Clean up dynamic materials
        if (mainPS != null)
        {
            var r = mainPS.GetComponent<ParticleSystemRenderer>();
            if (r != null && r.material != null) Destroy(r.material);
        }
        if (secondaryPS != null)
        {
            var r = secondaryPS.GetComponent<ParticleSystemRenderer>();
            if (r != null && r.material != null) Destroy(r.material);
        }
    }
}
}
