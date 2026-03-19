using UnityEngine;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Reusable screen-level veil transition system. Drives the VeilCrack shader
    /// on a full-screen quad positioned in front of the hero stage camera.
    /// Used by EmbarkCinematicController in Phase 4 and title screen in Phase 5.
    /// </summary>
    public class VeilTransitionController : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private Material _veilCrackMaterial;
        [SerializeField] private ParticleSystem _shatterParticles;

        // =============================================================================
        // SHADER PROPERTY IDS
        // =============================================================================

        private static readonly int kCrackProgress = Shader.PropertyToID("_CrackProgress");
        private static readonly int kCrackColor = Shader.PropertyToID("_CrackColor");
        private static readonly int kCrackGlowIntensity = Shader.PropertyToID("_CrackGlowIntensity");
        private static readonly int kShatterProgress = Shader.PropertyToID("_ShatterProgress");
        private static readonly int kBackgroundAlpha = Shader.PropertyToID("_BackgroundAlpha");

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private Renderer _fullScreenQuadRenderer;
        private MaterialPropertyBlock _mpb;
        private Sequence _activeSequence;

        /// <summary>Fired when the active transition completes.</summary>
        public event System.Action OnTransitionComplete;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            CreateFullScreenQuad();
            Reset();
        }

        private void OnDestroy()
        {
            _activeSequence.Stop();
        }

        // =============================================================================
        // SETUP
        // =============================================================================

        /// <summary>
        /// Creates a full-screen quad as a child object with the VeilCrack material.
        /// Positioned at near clip + 0.1 of the main camera.
        /// </summary>
        private void CreateFullScreenQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "VeilCrackQuad";
            quad.transform.SetParent(transform);

            // Remove collider (not needed for display)
            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Position in front of camera
            var cam = Camera.main;
            if (cam != null)
            {
                float distance = cam.nearClipPlane + 0.1f;
                quad.transform.position = cam.transform.position + cam.transform.forward * distance;
                quad.transform.rotation = cam.transform.rotation;

                // Scale to fill screen at the given distance
                float frustumHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float frustumWidth = frustumHeight * cam.aspect;
                quad.transform.localScale = new Vector3(frustumWidth * 1.1f, frustumHeight * 1.1f, 1f);
            }
            else
            {
                quad.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                quad.transform.localScale = new Vector3(5f, 5f, 1f);
            }

            _fullScreenQuadRenderer = quad.GetComponent<Renderer>();
            if (_fullScreenQuadRenderer != null && _veilCrackMaterial != null)
            {
                _fullScreenQuadRenderer.material = _veilCrackMaterial;
                _fullScreenQuadRenderer.enabled = false; // Hidden until needed
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Spreads procedural cracks from screen center. Progress 0-1.
        /// </summary>
        public Tween PlayCrackSpread(float duration, Color accentColor)
        {
            ShowQuad();

            _mpb.SetColor(kCrackColor, accentColor);
            _mpb.SetFloat(kCrackProgress, 0f);
            _mpb.SetFloat(kShatterProgress, 0f);
            _mpb.SetFloat(kBackgroundAlpha, 0f);
            _fullScreenQuadRenderer.SetPropertyBlock(_mpb);

            return Tween.Custom(this, 0f, 1f, duration, Ease.InQuad,
                (ctrl, val) =>
                {
                    ctrl._mpb.SetFloat(kCrackProgress, val);
                    ctrl._fullScreenQuadRenderer.SetPropertyBlock(ctrl._mpb);
                });
        }

        /// <summary>
        /// Shatters the cracks outward with fragment displacement + optional particle burst.
        /// </summary>
        public Tween PlayShatter(Color accentColor, float duration = 0.3f)
        {
            if (_shatterParticles != null)
            {
                var main = _shatterParticles.main;
                main.startColor = accentColor;
                _shatterParticles.Play();
            }

            return Tween.Custom(this, 0f, 1f, duration, Ease.OutQuad,
                (ctrl, val) =>
                {
                    ctrl._mpb.SetFloat(kShatterProgress, val);
                    ctrl._fullScreenQuadRenderer.SetPropertyBlock(ctrl._mpb);
                });
        }

        /// <summary>
        /// Fades to white overlay (for scene transition white-out).
        /// </summary>
        public Tween PlayWhiteOut(float duration = 0.2f)
        {
            return Tween.Custom(this, 0f, 1f, duration, Ease.InQuad,
                (ctrl, val) =>
                {
                    ctrl._mpb.SetFloat(kBackgroundAlpha, val);
                    ctrl._fullScreenQuadRenderer.SetPropertyBlock(ctrl._mpb);
                });
        }

        /// <summary>
        /// Materializes from a shattered/white state back to clear. For title screen reuse.
        /// </summary>
        public Tween PlayMaterialize(Color accentColor, float duration = 1f)
        {
            ShowQuad();

            _mpb.SetColor(kCrackColor, accentColor);
            _mpb.SetFloat(kCrackProgress, 1f);
            _mpb.SetFloat(kShatterProgress, 1f);
            _mpb.SetFloat(kBackgroundAlpha, 1f);
            _fullScreenQuadRenderer.SetPropertyBlock(_mpb);

            return Tween.Custom(this, 1f, 0f, duration, Ease.OutCubic,
                (ctrl, val) =>
                {
                    ctrl._mpb.SetFloat(kBackgroundAlpha, val);
                    ctrl._mpb.SetFloat(kShatterProgress, val);
                    ctrl._mpb.SetFloat(kCrackProgress, val);
                    ctrl._fullScreenQuadRenderer.SetPropertyBlock(ctrl._mpb);

                    if (val <= 0.01f)
                    {
                        ctrl.HideQuad();
                        ctrl.OnTransitionComplete?.Invoke();
                    }
                });
        }

        /// <summary>
        /// Resets all shader properties to 0 and hides the quad.
        /// </summary>
        public void Reset()
        {
            if (_fullScreenQuadRenderer == null || _mpb == null) return;

            _mpb.SetFloat(kCrackProgress, 0f);
            _mpb.SetFloat(kShatterProgress, 0f);
            _mpb.SetFloat(kBackgroundAlpha, 0f);
            _fullScreenQuadRenderer.SetPropertyBlock(_mpb);
            HideQuad();
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void ShowQuad()
        {
            if (_fullScreenQuadRenderer != null)
                _fullScreenQuadRenderer.enabled = true;
        }

        private void HideQuad()
        {
            if (_fullScreenQuadRenderer != null)
                _fullScreenQuadRenderer.enabled = false;
        }
    }
}
