using UnityEngine;
using System.Collections;

namespace VeilBreakers.VFX
{
/// <summary>
/// Area-of-effect VFX controller (ground_circle) for RUIN brand.
/// Supports ground circles, expanding domes, cone blasts, and ring waves.
/// Phase 23 -- VFX3-04
/// </summary>
public class VB_AoEVFX_ground_circle_RUIN : MonoBehaviour
{
    // Cached shader reference to avoid Shader.Find at runtime (UNITY-19)
    private static Shader _cachedParticleShader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _cachedParticleShader = null;

    // Track dynamic materials for cleanup (VFX-01)
    private readonly System.Collections.Generic.List<Material> _dynamicMaterials = new System.Collections.Generic.List<Material>();

    private static Shader GetParticleShader()
    {
        if (_cachedParticleShader == null)
            _cachedParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"); // VB-IGNORE UNITY-19 -- cached in static field, called once
        return _cachedParticleShader;
    }

    [Header("AoE Config")]
    [SerializeField] private string aoeType = "ground_circle";
    [SerializeField] private string brand = "RUIN";
    [SerializeField] private float radius = 5.0f;
    [SerializeField] private float duration = 3.0f;
    [SerializeField] private int particleCount = 150;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Brand Colors")]
    [SerializeField] private Color brandColor = new Color(1.0f, 0.271f, 0.0f, 1.0f);
    [SerializeField] private Color glowColor = new Color(1.0f, 0.5f, 0.15f, 1.0f);

    // Runtime
    private ParticleSystem mainPS;
    private ParticleSystem ringPS;
    private float elapsedTime = 0f;
    private bool isFading = false;

    public string AoEType => aoeType;
    public float Radius => radius;
    public float ElapsedTime => elapsedTime;
    public bool IsFading => isFading;

    private void Start()
    {
        CreateAoEEffect();
        StartCoroutine(RunAoELifecycle());
    }

    private IEnumerator RunAoELifecycle()
    {
        elapsedTime = 0f;
        float activeTime = duration - fadeOutTime;

        // Active phase
        while (elapsedTime < activeTime)
        {
            elapsedTime += Time.deltaTime;

            // Expand for dome and ring wave types
            if (aoeType == "expanding_dome" || aoeType == "ring_wave")
            {
                float t = elapsedTime / activeTime;
                float currentRadius = Mathf.Lerp(0f, radius, t);
                UpdateRadius(currentRadius);
            }

            yield return null;
        }

        // Fade out phase
        isFading = true;
        float fadeStart = elapsedTime;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float fadeT = (elapsedTime - fadeStart) / fadeOutTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeT);

            if (mainPS != null)
            {
                var main = mainPS.main;
                Color fadedColor = brandColor;
                fadedColor.a *= alpha;
                main.startColor = fadedColor;
            }

            yield return null;
        }

        // Cleanup
        if (mainPS != null) mainPS.Stop();
        if (ringPS != null) ringPS.Stop();
        Destroy(gameObject, 1f);
    }

    private void UpdateRadius(float currentRadius)
    {
        if (mainPS != null)
        {
            var shape = mainPS.shape;
            shape.radius = currentRadius;
        }
    }

    private void CreateAoEEffect()
    {
        switch (aoeType)
        {
            case "ground_circle":
                CreateGroundCircle();
                break;
            case "expanding_dome":
                CreateExpandingDome();
                break;
            case "cone_blast":
                CreateConeBlast();
                break;
            case "ring_wave":
                CreateRingWave();
                break;
        }
    }

    private void CreateGroundCircle()
    {
        // Flat ring of particles on the ground plane
        mainPS = CreatePS("GroundCircle");
        var shape = mainPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.1f; // Concentrate on edge

        var main = mainPS.main;
        main.startSpeed = 0.1f;
        main.startLifetime = duration;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Inner fill particles
        ringPS = CreatePS("InnerFill");
        var ringShape = ringPS.shape;
        ringShape.shapeType = ParticleSystemShapeType.Circle;
        ringShape.radius = radius * 0.8f;
        ringShape.radiusThickness = 1f;

        var ringMain = ringPS.main;
        ringMain.startSpeed = 0f;
        ringMain.startLifetime = duration;
        ringMain.startSize = mainPS.main.startSize.constant * 0.5f;
        ringMain.startColor = new Color(brandColor.r, brandColor.g, brandColor.b, 0.3f);

        mainPS.Play();
        ringPS.Play();
    }

    private void CreateExpandingDome()
    {
        mainPS = CreatePS("Dome");
        var shape = mainPS.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.1f; // Start small, expand in coroutine

        var main = mainPS.main;
        main.startSpeed = 0.5f;
        main.startLifetime = 1.0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        mainPS.Play();
    }

    private void CreateConeBlast()
    {
        mainPS = CreatePS("ConeBlast");
        var shape = mainPS.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.5f;
        shape.length = radius;

        var main = mainPS.main;
        main.startSpeed = radius / Mathf.Max(0.1f, duration * 0.3f);
        main.startLifetime = duration * 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Align cone with forward direction
        mainPS.transform.rotation = transform.rotation;
        mainPS.Play();
    }

    private void CreateRingWave()
    {
        mainPS = CreatePS("RingWave");
        var shape = mainPS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f; // Expands in coroutine
        shape.radiusThickness = 0f; // Only on edge

        var main = mainPS.main;
        main.startSpeed = 0f;
        main.startLifetime = 0.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = mainPS.emission;
        emission.rateOverTime = particleCount * 2;

        mainPS.Play();
    }

    private ParticleSystem CreatePS(string psName)
    {
        GameObject psObj = new GameObject($"AoE_{psName}");
        psObj.transform.SetParent(transform);
        psObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 1f;
        main.startSize = 0.15f;
        main.startColor = brandColor;
        main.maxParticles = particleCount;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = particleCount / Mathf.Max(0.1f, duration);

        // Color over lifetime with glow
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(brandColor, 0.6f),
                new GradientColorKey(brandColor * 0.2f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(GetParticleShader());
        mat.SetColor("_Color", glowColor);
        renderer.material = mat;
        _dynamicMaterials.Add(mat);

        return ps;
    }

    private void OnDestroy()
    {
        // Destroy all dynamically created materials (VFX-01)
        foreach (var mat in _dynamicMaterials)
        {
            if (mat != null) Destroy(mat);
        }
        _dynamicMaterials.Clear();
    }
}
}
