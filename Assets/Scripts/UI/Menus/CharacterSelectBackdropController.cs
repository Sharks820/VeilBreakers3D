using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Data;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Controls backdrop transitions for Character Select screen.
    /// Crossfades between Path-themed backgrounds when hero selection changes.
    /// </summary>
    public class CharacterSelectBackdropController : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Backdrop Textures")]
        [SerializeField] private Texture2D _ironboundBackdrop;
        [SerializeField] private Texture2D _fangbornBackdrop;
        [SerializeField] private Texture2D _voidtouchedBackdrop;
        [SerializeField] private Texture2D _unchainedBackdrop;
        [SerializeField] private Texture2D _defaultBackdrop;

        [Header("Animation")]
        [SerializeField] private float _crossfadeDuration = 0.4f;
        [SerializeField] private AnimationCurve _crossfadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("UI References")]
        [SerializeField] private UIDocument _uiDocument;

        // =============================================================================
        // STATE
        // =============================================================================

        private VisualElement _backdropA;
        private VisualElement _backdropB;
        private bool _isBackdropAActive = true;
        private Path _currentPath = (Path)(-1);
        private Coroutine _crossfadeCoroutine;

        // Path to backdrop mapping
        private Dictionary<Path, Texture2D> _pathBackdrops;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            BuildPathBackdropMapping();
        }

        private void Start()
        {
            SetupBackdropElements();
        }

        private void OnDestroy()
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Transition to the backdrop for the given hero's Path.
        /// </summary>
        public void TransitionToHero(HeroData hero)
        {
            if (hero == null) return;

            Path heroPath = hero.GetPrimaryPath();
            TransitionToPath(heroPath);
        }

        /// <summary>
        /// Transition to the backdrop for the given Path.
        /// </summary>
        public void TransitionToPath(Path path)
        {
            if (path == _currentPath) return;

            _currentPath = path;
            Texture2D targetBackdrop = GetBackdropForPath(path);

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            _crossfadeCoroutine = StartCoroutine(CrossfadeToBackdrop(targetBackdrop));
        }

        /// <summary>
        /// Set backdrop immediately without animation.
        /// </summary>
        public void SetBackdropImmediate(Path path)
        {
            _currentPath = path;
            Texture2D backdrop = GetBackdropForPath(path);

            if (_backdropA != null && backdrop != null)
            {
                _backdropA.style.backgroundImage = new StyleBackground(backdrop);
                _backdropA.style.opacity = 1f;
            }

            if (_backdropB != null)
            {
                _backdropB.style.opacity = 0f;
            }

            _isBackdropAActive = true;
        }

        // =============================================================================
        // SETUP
        // =============================================================================

        private void BuildPathBackdropMapping()
        {
            _pathBackdrops = new Dictionary<Path, Texture2D>
            {
                { Path.IRONBOUND, _ironboundBackdrop },
                { Path.FANGBORN, _fangbornBackdrop },
                { Path.VOIDTOUCHED, _voidtouchedBackdrop },
                { Path.UNCHAINED, _unchainedBackdrop }
            };
        }

        private void SetupBackdropElements()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            var bgElement = root?.Q<VisualElement>("bg");

            if (bgElement == null) return;

            // Create two backdrop layers for crossfade
            _backdropA = CreateBackdropLayer("backdrop-a");
            _backdropB = CreateBackdropLayer("backdrop-b");

            // Insert before other content but after the bg
            bgElement.parent?.Insert(1, _backdropA);
            bgElement.parent?.Insert(2, _backdropB);

            // Start with B hidden
            _backdropB.style.opacity = 0f;

            // Apply default backdrop if available
            if (_defaultBackdrop != null)
            {
                _backdropA.style.backgroundImage = new StyleBackground(_defaultBackdrop);
            }
        }

        private VisualElement CreateBackdropLayer(string name)
        {
            var layer = new VisualElement
            {
                name = name
            };

            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.right = 0;
            layer.style.bottom = 0;
            layer.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            layer.pickingMode = PickingMode.Ignore;

            return layer;
        }

        // =============================================================================
        // CROSSFADE ANIMATION
        // =============================================================================

        private IEnumerator CrossfadeToBackdrop(Texture2D targetBackdrop)
        {
            if (targetBackdrop == null)
            {
                yield break;
            }

            VisualElement fromBackdrop = _isBackdropAActive ? _backdropA : _backdropB;
            VisualElement toBackdrop = _isBackdropAActive ? _backdropB : _backdropA;

            if (fromBackdrop == null || toBackdrop == null)
            {
                yield break;
            }

            // Set new backdrop on hidden layer
            toBackdrop.style.backgroundImage = new StyleBackground(targetBackdrop);
            toBackdrop.style.opacity = 0f;

            // Crossfade
            float elapsed = 0f;
            while (elapsed < _crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = _crossfadeCurve.Evaluate(elapsed / _crossfadeDuration);

                toBackdrop.style.opacity = t;
                fromBackdrop.style.opacity = 1f - t;

                yield return null;
            }

            // Finalize
            toBackdrop.style.opacity = 1f;
            fromBackdrop.style.opacity = 0f;

            _isBackdropAActive = !_isBackdropAActive;
            _crossfadeCoroutine = null;
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private Texture2D GetBackdropForPath(Path path)
        {
            if (_pathBackdrops.TryGetValue(path, out Texture2D backdrop) && backdrop != null)
            {
                return backdrop;
            }

            return _defaultBackdrop;
        }
    }
}
