using System;
using System.Collections;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Capture
{
    /// <summary>
    /// State of the QTE minigame.
    /// </summary>
    public enum QTEState
    {
        IDLE,
        COUNTDOWN,
        ACTIVE,
        COMPLETE
    }

    /// <summary>
    /// Type of QTE minigame.
    /// </summary>
    public enum QTEType
    {
        TIMING_BAR,         // Stop moving bar in target zone
        BUTTON_MASH,        // Mash button to fill meter
        SEQUENCE,           // Press sequence of buttons
        HOLD_RELEASE        // Hold and release at right moment
    }

    /// <summary>
    /// Controls the Quick Time Event minigame for capture attempts.
    /// </summary>
    public class QTEController : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static QTEController _instance;
        public static QTEController Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    Debug.LogError("[QTEController] Instance is null. Ensure QTEController exists in scene.");
                }
                return _instance;
            }
        }

        private static bool _isQuitting = false;

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Timing Bar Settings")]
        [Tooltip("Duration of one cycle (bar moving left to right)")]
        [SerializeField] private float _cycleDuration = 2f;

        [Tooltip("Number of cycles before QTE times out")]
        [SerializeField] private int _maxCycles = 3;

        [Tooltip("Width of perfect zone (0-1)")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _perfectZoneWidth = 0.05f;

        [Tooltip("Width of good zone (0-1)")]
        [Range(0.05f, 0.3f)]
        [SerializeField] private float _goodZoneWidth = 0.12f;

        [Tooltip("Width of okay zone (0-1)")]
        [Range(0.1f, 0.4f)]
        [SerializeField] private float _okayZoneWidth = 0.20f;

        [Header("Input")]
        [SerializeField] private KeyCode _actionKey = KeyCode.Space;
        [SerializeField] private KeyCode _altActionKey = KeyCode.Return;

        [Header("Countdown")]
        [SerializeField] private float _countdownDuration = 3f;

        // =============================================================================
        // STATE
        // =============================================================================

        private QTEState _state = QTEState.IDLE;
        private QTEType _type = QTEType.TIMING_BAR;
        private float _currentTime = 0f;
        private float _countdownTime = 0f;
        private int _currentCycle = 0;
        private float _barPosition = 0f;
        private bool _barMovingRight = true;
        private QTEResult _result = QTEResult.MISS;

        // Target position (center of zones)
        private float _targetPosition = 0.5f;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action OnQTEStarted;
        public event Action<int> OnCountdownTick;
        public event Action OnQTEActive;
        public event Action<QTEResult> OnQTEComplete;
        public event Action<float> OnBarPositionChanged;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public QTEState State => _state;
        public QTEType Type => _type;
        public float BarPosition => _barPosition;
        public float TargetPosition => _targetPosition;
        public float PerfectZoneWidth => _perfectZoneWidth;
        public float GoodZoneWidth => _goodZoneWidth;
        public float OkayZoneWidth => _okayZoneWidth;
        public int CurrentCycle => _currentCycle;
        public int MaxCycles => _maxCycles;
        public QTEResult Result => _result;
        public float CountdownRemaining => _countdownTime;

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnDisable()
        {
            // Clear event subscribers to prevent memory leaks
            OnQTEStarted = null;
            OnCountdownTick = null;
            OnQTEActive = null;
            OnQTEComplete = null;
            OnBarPositionChanged = null;

            // Stop any running coroutines
            StopAllCoroutines();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void Update()
        {
            switch (_state)
            {
                case QTEState.COUNTDOWN:
                    UpdateCountdown();
                    break;
                case QTEState.ACTIVE:
                    UpdateTimingBar();
                    CheckInput();
                    break;
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Start a QTE for capture.
        /// </summary>
        public void StartQTE(QTEType type = QTEType.TIMING_BAR)
        {
            _type = type;
            _state = QTEState.COUNTDOWN;
            _countdownTime = _countdownDuration;
            _currentCycle = 0;
            _barPosition = 0f;
            _barMovingRight = true;
            _result = QTEResult.MISS;

            // Randomize target position slightly for variety
            _targetPosition = UnityEngine.Random.Range(0.35f, 0.65f);

            OnQTEStarted?.Invoke();
            Debug.Log("[QTEController] QTE started - countdown beginning");
        }

        /// <summary>
        /// Skip countdown (for testing or fast play).
        /// </summary>
        public void SkipCountdown()
        {
            if (_state == QTEState.COUNTDOWN)
            {
                _countdownTime = 0f;
                ActivateQTE();
            }
        }

        /// <summary>
        /// Cancel the current QTE.
        /// </summary>
        public void CancelQTE()
        {
            if (_state != QTEState.IDLE)
            {
                _state = QTEState.IDLE;
                _result = QTEResult.MISS;
                Debug.Log("[QTEController] QTE cancelled");
            }
        }

        /// <summary>
        /// Force a specific result (for testing).
        /// </summary>
        public void ForceResult(QTEResult result)
        {
            _result = result;
            CompleteQTE();
        }

        // =============================================================================
        // COUNTDOWN
        // =============================================================================

        private void UpdateCountdown()
        {
            _countdownTime -= Time.unscaledDeltaTime;

            // Fire countdown ticks (3, 2, 1)
            int tick = Mathf.CeilToInt(_countdownTime);
            if (tick > 0 && tick <= 3)
            {
                OnCountdownTick?.Invoke(tick);
            }

            if (_countdownTime <= 0f)
            {
                ActivateQTE();
            }
        }

        private void ActivateQTE()
        {
            _state = QTEState.ACTIVE;
            _currentTime = 0f;
            OnQTEActive?.Invoke();
            Debug.Log("[QTEController] QTE active - GO!");
        }

        // =============================================================================
        // TIMING BAR
        // =============================================================================

        private void UpdateTimingBar()
        {
            // Use unscaled time so time slow doesn't affect QTE
            float delta = Time.unscaledDeltaTime;
            float moveSpeed = 1f / _cycleDuration;

            // Move bar
            if (_barMovingRight)
            {
                _barPosition += moveSpeed * delta;
                if (_barPosition >= 1f)
                {
                    _barPosition = 1f;
                    _barMovingRight = false;
                }
            }
            else
            {
                _barPosition -= moveSpeed * delta;
                if (_barPosition <= 0f)
                {
                    _barPosition = 0f;
                    _barMovingRight = true;
                    _currentCycle++;

                    // Check for timeout
                    if (_currentCycle >= _maxCycles)
                    {
                        _result = QTEResult.MISS;
                        CompleteQTE();
                        return;
                    }
                }
            }

            OnBarPositionChanged?.Invoke(_barPosition);
        }

        private void CheckInput()
        {
            if (Input.GetKeyDown(_actionKey) || Input.GetKeyDown(_altActionKey))
            {
                EvaluateTimingBar();
            }
        }

        private void EvaluateTimingBar()
        {
            float distance = Mathf.Abs(_barPosition - _targetPosition);

            // Check zones (from smallest to largest)
            if (distance <= _perfectZoneWidth / 2f)
            {
                _result = QTEResult.PERFECT;
            }
            else if (distance <= _goodZoneWidth / 2f)
            {
                _result = QTEResult.GOOD;
            }
            else if (distance <= _okayZoneWidth / 2f)
            {
                _result = QTEResult.OKAY;
            }
            else
            {
                _result = QTEResult.MISS;
            }

            CompleteQTE();
        }

        private void CompleteQTE()
        {
            _state = QTEState.COMPLETE;
            OnQTEComplete?.Invoke(_result);

            Debug.Log($"[QTEController] QTE complete: {_result} (bar at {_barPosition:F2}, target at {_targetPosition:F2})");

            // Auto-reset to idle after a brief moment
            StartCoroutine(ResetToIdleCoroutine());
        }

        private IEnumerator ResetToIdleCoroutine()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            _state = QTEState.IDLE;
        }

        // =============================================================================
        // DIFFICULTY SCALING
        // =============================================================================

        /// <summary>
        /// Adjust difficulty based on monster factors.
        /// </summary>
        public void SetDifficulty(MonsterRarity rarity, float corruption)
        {
            // Base values
            float cycleDuration = 2f;
            float perfectWidth = 0.05f;
            float goodWidth = 0.12f;
            float okayWidth = 0.20f;

            // Harder for higher rarity
            float rarityMod = 1f - ((int)rarity * 0.1f);
            perfectWidth *= rarityMod;
            goodWidth *= rarityMod;
            okayWidth *= rarityMod;

            // Faster for high corruption
            if (corruption > 50f)
            {
                cycleDuration *= 0.8f;
            }

            _cycleDuration = Mathf.Max(1f, cycleDuration);
            _perfectZoneWidth = Mathf.Max(0.02f, perfectWidth);
            _goodZoneWidth = Mathf.Max(0.05f, goodWidth);
            _okayZoneWidth = Mathf.Max(0.1f, okayWidth);
        }

        /// <summary>
        /// Reset to default difficulty.
        /// </summary>
        public void ResetDifficulty()
        {
            _cycleDuration = 2f;
            _perfectZoneWidth = 0.05f;
            _goodZoneWidth = 0.12f;
            _okayZoneWidth = 0.20f;
        }

        // =============================================================================
        // UTILITY
        // =============================================================================

        /// <summary>
        /// Get the bonus for a QTE result.
        /// </summary>
        public static float GetBonus(QTEResult result)
        {
            return result switch
            {
                QTEResult.PERFECT => CaptureFormulaConfig.QTE_PERFECT_BONUS,
                QTEResult.GOOD => CaptureFormulaConfig.QTE_GOOD_BONUS,
                QTEResult.OKAY => CaptureFormulaConfig.QTE_OKAY_BONUS,
                _ => CaptureFormulaConfig.QTE_MISS_BONUS
            };
        }

        /// <summary>
        /// Get display text for a QTE result.
        /// </summary>
        public static string GetResultText(QTEResult result)
        {
            return result switch
            {
                QTEResult.PERFECT => "PERFECT!",
                QTEResult.GOOD => "GOOD!",
                QTEResult.OKAY => "OKAY",
                _ => "MISS..."
            };
        }
    }
}
