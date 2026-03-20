using UnityEngine;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Drives the VeilDissolve shader on 3D hero/monster models via MaterialPropertyBlock.
    /// Uses MaterialPropertyBlock to avoid material instance creation and memory leaks.
    /// Exposes PrimeTween-based AnimateDissolveOut/In methods for sequence composition.
    /// </summary>
    public class VeilDissolveController : MonoBehaviour
    {
        // =============================================================================
        // SHADER PROPERTY IDS (cached, no per-frame string hashing)
        // =============================================================================

        private static readonly int kDissolveThreshold = Shader.PropertyToID("_DissolveThreshold");
        private static readonly int kDissolveEdgeColor = Shader.PropertyToID("_DissolveEdgeColor");
        private static readonly int kNoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int kEmissionIntensity = Shader.PropertyToID("_EmissionIntensity");

        // =============================================================================
        // RUNTIME STATE
        // =============================================================================

        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;

        // =============================================================================
        // INITIALIZATION
        // =============================================================================

        /// <summary>
        /// Initializes the controller with a target renderer. Must be called before any
        /// dissolve operations. Typically called by HeroStageController after model instantiation.
        /// </summary>
        public void Init(Renderer renderer)
        {
            _renderer = renderer;
            _mpb = new MaterialPropertyBlock();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Sets the dissolve threshold directly (0 = fully visible, 1 = fully dissolved).
        /// Applied via MaterialPropertyBlock to avoid material instance creation.
        /// </summary>
        public void SetDissolveThreshold(float threshold)
        {
            if (_renderer == null || _mpb == null) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(kDissolveThreshold, threshold);
            _renderer.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// Configures per-hero dissolve properties: edge color and noise scale.
        /// Called once on hero switch before starting dissolve animation.
        /// </summary>
        public void SetHeroProperties(HeroThemeConfig theme)
        {
            if (_renderer == null || _mpb == null) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(kDissolveEdgeColor, theme.dissolveEdgeColor);
            _mpb.SetFloat(kNoiseScale, theme.dissolveNoiseScale);
            _renderer.SetPropertyBlock(_mpb);
        }

        /// <summary>
        /// Animates dissolve threshold from 0 to 1 (hero disappears into veil energy).
        /// Returns a PrimeTween Tween for sequence composition.
        /// Uses Tween.Custom with explicit target to avoid closure allocations.
        /// </summary>
        public Tween AnimateDissolveOut(float duration = 0.4f, Ease ease = Ease.InQuad)
        {
            return Tween.Custom(this, 0f, 1f, duration,
                (VeilDissolveController ctrl, float val) => ctrl.SetDissolveThreshold(val), ease);
        }

        /// <summary>
        /// Animates dissolve threshold from 1 to 0 (hero materializes from veil energy).
        /// Returns a PrimeTween Tween for sequence composition.
        /// Uses Tween.Custom with explicit target to avoid closure allocations.
        /// </summary>
        public Tween AnimateDissolveIn(float duration = 0.4f, Ease ease = Ease.OutQuad)
        {
            return Tween.Custom(this, 1f, 0f, duration,
                (VeilDissolveController ctrl, float val) => ctrl.SetDissolveThreshold(val), ease);
        }

        /// <summary>
        /// Sets the HDR emission intensity on the dissolve edge.
        /// Higher values produce a more dramatic glow at the dissolve boundary.
        /// </summary>
        public void SetEmissionIntensity(float intensity)
        {
            if (_renderer == null || _mpb == null) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(kEmissionIntensity, intensity);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
