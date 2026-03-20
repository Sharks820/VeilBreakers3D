using System;
using System.Collections;
using UnityEngine;

namespace VeilBreakers.UI.Menus
{
    /// <summary>
    /// Manages dual 3D model preview for Character Select screen.
    /// Shows hero on left, starter monster on right with smooth transitions.
    /// </summary>
    public class HeroMonsterPairPreview : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Model Positions")]
        [SerializeField] private Vector3 _heroPosition = new Vector3(-0.5f, 0f, 0f);
        [SerializeField] private Vector3 _monsterPosition = new Vector3(0.6f, 0f, 0.2f);
        [SerializeField] private Vector3 _heroRotation = new Vector3(0f, 15f, 0f);
        [SerializeField] private Vector3 _monsterRotation = new Vector3(0f, -20f, 0f);

        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.4f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _monsterSpawnDelay = 0.15f;
        [SerializeField] private AnimationCurve _spawnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Spawn Effects")]
        [SerializeField] private float _spawnStartScale = 0.3f;
        [SerializeField] private float _spawnStartY = -0.5f;

        // =============================================================================
        // STATE
        // =============================================================================

        private GameObject _currentHeroModel;
        private GameObject _currentMonsterModel;
        private Coroutine _heroTransitionCoroutine;
        private Coroutine _monsterTransitionCoroutine;
        private int _uiLayer;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnTransitionComplete;

        // =============================================================================
        // LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            _uiLayer = LayerMask.NameToLayer("UI");
        }

        private void OnDestroy()
        {
            ClearAllModels();
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Display a hero and monster pair with animated transition.
        /// </summary>
        public void ShowPair(GameObject heroPrefab, GameObject monsterPrefab)
        {
            // Stop any running transitions
            StopAllTransitions();

            // Start transition sequence
            StartCoroutine(TransitionSequence(heroPrefab, monsterPrefab));
        }

        /// <summary>
        /// Clear all models with fade out animation.
        /// </summary>
        public void HidePair()
        {
            StopAllTransitions();

            if (_currentHeroModel != null)
            {
                _heroTransitionCoroutine = StartCoroutine(FadeOutModel(_currentHeroModel, () =>
                {
                    if (_currentHeroModel != null)
                    {
                        Destroy(_currentHeroModel);
                        _currentHeroModel = null;
                    }
                }));
            }

            if (_currentMonsterModel != null)
            {
                _monsterTransitionCoroutine = StartCoroutine(FadeOutModel(_currentMonsterModel, () =>
                {
                    if (_currentMonsterModel != null)
                    {
                        Destroy(_currentMonsterModel);
                        _currentMonsterModel = null;
                    }
                }));
            }
        }

        /// <summary>
        /// Immediately clear all models without animation.
        /// </summary>
        public void ClearAllModels()
        {
            StopAllTransitions();

            if (_currentHeroModel != null)
            {
                Destroy(_currentHeroModel);
                _currentHeroModel = null;
            }

            if (_currentMonsterModel != null)
            {
                Destroy(_currentMonsterModel);
                _currentMonsterModel = null;
            }
        }

        /// <summary>
        /// Get current hero model for external manipulation.
        /// </summary>
        public GameObject GetHeroModel() => _currentHeroModel;

        /// <summary>
        /// Get current monster model for external manipulation.
        /// </summary>
        public GameObject GetMonsterModel() => _currentMonsterModel;

        // =============================================================================
        // TRANSITION LOGIC
        // =============================================================================

        private IEnumerator TransitionSequence(GameObject heroPrefab, GameObject monsterPrefab)
        {
            // Phase 1: Fade out existing models
            bool hasExistingModels = _currentHeroModel != null || _currentMonsterModel != null;

            if (hasExistingModels)
            {
                // Start fade out for both
                if (_currentHeroModel != null)
                {
                    StartCoroutine(FadeOutAndDestroy(_currentHeroModel));
                    _currentHeroModel = null;
                }

                if (_currentMonsterModel != null)
                {
                    StartCoroutine(FadeOutAndDestroy(_currentMonsterModel));
                    _currentMonsterModel = null;
                }

                // Wait for fade out
                yield return new WaitForSeconds(_fadeOutDuration * 0.5f);
            }

            // Phase 2: Spawn hero
            if (heroPrefab != null)
            {
                _currentHeroModel = SpawnModel(heroPrefab, _heroPosition, _heroRotation);
                _heroTransitionCoroutine = StartCoroutine(AnimateSpawn(_currentHeroModel));
            }

            // Phase 3: Delay then spawn monster
            yield return new WaitForSeconds(_monsterSpawnDelay);

            if (monsterPrefab != null)
            {
                _currentMonsterModel = SpawnModel(monsterPrefab, _monsterPosition, _monsterRotation);
                _monsterTransitionCoroutine = StartCoroutine(AnimateSpawn(_currentMonsterModel));
            }

            // Wait for spawn animations to complete
            yield return new WaitForSeconds(_fadeInDuration);

            OnTransitionComplete?.Invoke();
        }

        private GameObject SpawnModel(GameObject prefab, Vector3 localPosition, Vector3 localRotation)
        {
            GameObject model = Instantiate(prefab, transform);
            model.transform.localPosition = localPosition;
            model.transform.localRotation = Quaternion.Euler(localRotation);

            // Set UI layer for all renderers
            SetLayerRecursively(model, _uiLayer);

            return model;
        }

        private IEnumerator AnimateSpawn(GameObject model)
        {
            if (model == null) yield break;

            Vector3 targetPosition = model.transform.localPosition;
            Vector3 startPosition = targetPosition + Vector3.up * _spawnStartY;
            Vector3 targetScale = model.transform.localScale;
            Vector3 startScale = targetScale * _spawnStartScale;

            model.transform.localPosition = startPosition;
            model.transform.localScale = startScale;

            // Enable renderers that might be disabled
            SetRenderersAlpha(model, 0f);

            float elapsed = 0f;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = _spawnCurve.Evaluate(elapsed / _fadeInDuration);

                if (model == null) yield break;

                model.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                model.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                SetRenderersAlpha(model, t);

                yield return null;
            }

            // Ensure final state
            if (model != null)
            {
                model.transform.localPosition = targetPosition;
                model.transform.localScale = targetScale;
                SetRenderersAlpha(model, 1f);
            }
        }

        private IEnumerator FadeOutModel(GameObject model, Action onComplete)
        {
            if (model == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startPosition = model.transform.localPosition;
            Vector3 endPosition = startPosition + Vector3.back * 0.3f;

            float elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fadeOutDuration;

                if (model == null) yield break;

                model.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
                SetRenderersAlpha(model, 1f - t);

                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator FadeOutAndDestroy(GameObject model)
        {
            yield return FadeOutModel(model, () =>
            {
                if (model != null)
                {
                    Destroy(model);
                }
            });
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        private void StopAllTransitions()
        {
            if (_heroTransitionCoroutine != null)
            {
                StopCoroutine(_heroTransitionCoroutine);
                _heroTransitionCoroutine = null;
            }

            if (_monsterTransitionCoroutine != null)
            {
                StopCoroutine(_monsterTransitionCoroutine);
                _monsterTransitionCoroutine = null;
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static readonly int kBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int kColor = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;

        private void SetRenderersAlpha(GameObject obj, float alpha)
        {
            if (obj == null) return;

            _mpb ??= new MaterialPropertyBlock();

            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                // Apply per-material-index to avoid multi-material color crushing
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat == null) continue;

                    _mpb.Clear();
                    renderer.GetPropertyBlock(_mpb, i);

                    if (mat.HasProperty(kBaseColor))
                    {
                        Color c = mat.GetColor(kBaseColor);
                        c.a = alpha;
                        _mpb.SetColor(kBaseColor, c);
                    }
                    else if (mat.HasProperty(kColor))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        _mpb.SetColor(kColor, c);
                    }

                    renderer.SetPropertyBlock(_mpb, i);
                }
            }
        }
    }
}
