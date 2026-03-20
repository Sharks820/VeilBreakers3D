using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Lerps URP Volume overrides between per-hero profiles using PrimeTween Tween.Custom.
    /// Each hero has a unique VolumeProfile defining Bloom, Vignette, DoF, ColorAdjustments,
    /// and ChromaticAberration. On hero switch, all properties interpolate over 0.8s.
    /// </summary>
    public class VolumeProfileTransitioner : MonoBehaviour
    {
        // =============================================================================
        // SERIALIZED FIELDS
        // =============================================================================

        [SerializeField] private Volume _volume;

        // =============================================================================
        // CACHED OVERRIDE REFERENCES
        // =============================================================================

        private Bloom _bloom;
        private Vignette _vignette;
        private DepthOfField _dof;
        private ColorAdjustments _colorAdj;
        private ChromaticAberration _chromatic;

        // =============================================================================
        // SOURCE VALUES (snapshot of current state before lerp)
        // =============================================================================

        private float _srcBloomIntensity, _dstBloomIntensity;
        private Color _srcBloomTint, _dstBloomTint;
        private float _srcVignetteIntensity, _dstVignetteIntensity;
        private Color _srcVignetteColor, _dstVignetteColor;
        private float _srcDofStart, _dstDofStart;
        private float _srcDofEnd, _dstDofEnd;
        private float _srcColorTemp, _dstColorTemp;
        private float _srcColorSat, _dstColorSat;
        private Color _srcColorFilter, _dstColorFilter;
        private float _srcChromaticIntensity, _dstChromaticIntensity;

        // =============================================================================
        // TWEEN STATE
        // =============================================================================

        private Tween _activeTween;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_volume == null)
            {
                Debug.LogError("[VolumeProfileTransitioner] No Volume component assigned.");
                return;
            }

            // CRITICAL: Instantiate volume profile at runtime to prevent Editor asset mutation.
            if (_volume.sharedProfile == null)
            {
                Debug.LogError("[VolumeProfileTransitioner] Volume has no sharedProfile assigned.");
                return;
            }
            _volume.profile = Instantiate(_volume.sharedProfile);

            var profile = _volume.profile;

            // Cache all override references
            profile.TryGet(out _bloom);
            profile.TryGet(out _vignette);
            profile.TryGet(out _dof);
            profile.TryGet(out _colorAdj);
            profile.TryGet(out _chromatic);

            // Enable overrideState on ALL properties that will be animated.
            // Without this, setting .value has no visual effect (URP ignores it).
            EnsureOverrideStates();
        }

        private void OnDestroy()
        {
            // Destroy the instantiated profile to prevent memory leak
            if (_volume != null && _volume.profile != null)
            {
                Destroy(_volume.profile);
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Lerps all volume overrides from current values to the target hero's profile values.
        /// Returns the tween for sequence composition.
        /// </summary>
        public Tween LerpTo(HeroThemeConfig theme, float duration = 0.8f)
        {
            _activeTween.Stop();

            CacheSourceValues();
            CacheTargetValues(theme);

            _activeTween = Tween.Custom(this, 0f, 1f, duration,
                ease: Ease.InOutSine,
                onValueChange: (target, t) => target.ApplyLerp(t));

            return _activeTween;
        }

        // =============================================================================
        // CACHE METHODS
        // =============================================================================

        private void CacheSourceValues()
        {
            if (_bloom != null)
            {
                _srcBloomIntensity = _bloom.intensity.value;
                _srcBloomTint = _bloom.tint.value;
            }

            if (_vignette != null)
            {
                _srcVignetteIntensity = _vignette.intensity.value;
                _srcVignetteColor = _vignette.color.value;
            }

            if (_dof != null)
            {
                _srcDofStart = _dof.gaussianStart.value;
                _srcDofEnd = _dof.gaussianEnd.value;
            }

            if (_colorAdj != null)
            {
                _srcColorTemp = _colorAdj.colorFilter.value.r; // Use postExposure for temperature proxy
                _srcColorSat = _colorAdj.saturation.value;
                _srcColorFilter = _colorAdj.colorFilter.value;
            }

            if (_chromatic != null)
            {
                _srcChromaticIntensity = _chromatic.intensity.value;
            }
        }

        private void CacheTargetValues(HeroThemeConfig theme)
        {
            if (theme.volumeProfile == null)
            {
                Debug.LogWarning($"[VolumeProfileTransitioner] HeroThemeConfig '{theme.heroId}' has no volumeProfile assigned. Using current values as target.");
                CopySourceToDestination();
                return;
            }

            // Read target values from the hero's VolumeProfile asset
            if (theme.volumeProfile.TryGet(out Bloom targetBloom))
            {
                _dstBloomIntensity = targetBloom.intensity.value;
                _dstBloomTint = targetBloom.tint.value;
            }
            else
            {
                _dstBloomIntensity = _srcBloomIntensity;
                _dstBloomTint = _srcBloomTint;
            }

            if (theme.volumeProfile.TryGet(out Vignette targetVignette))
            {
                _dstVignetteIntensity = targetVignette.intensity.value;
                _dstVignetteColor = targetVignette.color.value;
            }
            else
            {
                _dstVignetteIntensity = _srcVignetteIntensity;
                _dstVignetteColor = _srcVignetteColor;
            }

            if (theme.volumeProfile.TryGet(out DepthOfField targetDof))
            {
                _dstDofStart = targetDof.gaussianStart.value;
                _dstDofEnd = targetDof.gaussianEnd.value;
            }
            else
            {
                _dstDofStart = _srcDofStart;
                _dstDofEnd = _srcDofEnd;
            }

            if (theme.volumeProfile.TryGet(out ColorAdjustments targetColorAdj))
            {
                _dstColorSat = targetColorAdj.saturation.value;
                _dstColorFilter = targetColorAdj.colorFilter.value;
            }
            else
            {
                _dstColorSat = _srcColorSat;
                _dstColorFilter = _srcColorFilter;
            }

            // ChromaticAberration intensity comes from HeroThemeConfig directly
            // (only Nyx has > 0, so we don't require it in every VolumeProfile)
            _dstChromaticIntensity = theme.chromaticAberrationIntensity;
        }

        private void CopySourceToDestination()
        {
            _dstBloomIntensity = _srcBloomIntensity;
            _dstBloomTint = _srcBloomTint;
            _dstVignetteIntensity = _srcVignetteIntensity;
            _dstVignetteColor = _srcVignetteColor;
            _dstDofStart = _srcDofStart;
            _dstDofEnd = _srcDofEnd;
            _dstColorSat = _srcColorSat;
            _dstColorFilter = _srcColorFilter;
            _dstChromaticIntensity = _srcChromaticIntensity;
        }

        // =============================================================================
        // LERP APPLICATION
        // =============================================================================

        private void ApplyLerp(float t)
        {
            if (_bloom != null)
            {
                _bloom.intensity.overrideState = true;
                _bloom.intensity.value = Mathf.Lerp(_srcBloomIntensity, _dstBloomIntensity, t);

                _bloom.tint.overrideState = true;
                _bloom.tint.value = Color.Lerp(_srcBloomTint, _dstBloomTint, t);
            }

            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignette.intensity.value = Mathf.Lerp(_srcVignetteIntensity, _dstVignetteIntensity, t);

                _vignette.color.overrideState = true;
                _vignette.color.value = Color.Lerp(_srcVignetteColor, _dstVignetteColor, t);
            }

            if (_dof != null)
            {
                _dof.gaussianStart.overrideState = true;
                _dof.gaussianStart.value = Mathf.Lerp(_srcDofStart, _dstDofStart, t);

                _dof.gaussianEnd.overrideState = true;
                _dof.gaussianEnd.value = Mathf.Lerp(_srcDofEnd, _dstDofEnd, t);
            }

            if (_colorAdj != null)
            {
                _colorAdj.saturation.overrideState = true;
                _colorAdj.saturation.value = Mathf.Lerp(_srcColorSat, _dstColorSat, t);

                _colorAdj.colorFilter.overrideState = true;
                _colorAdj.colorFilter.value = Color.Lerp(_srcColorFilter, _dstColorFilter, t);
            }

            if (_chromatic != null)
            {
                _chromatic.intensity.overrideState = true;
                _chromatic.intensity.value = Mathf.Lerp(_srcChromaticIntensity, _dstChromaticIntensity, t);
            }
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void EnsureOverrideStates()
        {
            // Pre-enable overrideState on all animated properties so they take effect immediately.
            if (_bloom != null)
            {
                _bloom.intensity.overrideState = true;
                _bloom.tint.overrideState = true;
            }

            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignette.color.overrideState = true;
            }

            if (_dof != null)
            {
                _dof.gaussianStart.overrideState = true;
                _dof.gaussianEnd.overrideState = true;
            }

            if (_colorAdj != null)
            {
                _colorAdj.saturation.overrideState = true;
                _colorAdj.colorFilter.overrideState = true;
            }

            if (_chromatic != null)
            {
                _chromatic.intensity.overrideState = true;
            }
        }
    }
}
