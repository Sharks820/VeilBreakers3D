using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;

namespace VeilBreakers.Systems
{
    /// <summary>
    /// VERA (Veil Emergency Response AI) System
    /// Manages the AI companion's dialogue, personality, and veil integrity responses.
    /// VERA has a dual personality that emerges based on Veil Integrity level.
    /// </summary>
    public class VERASystem : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static VERASystem _instance;
        private static bool _isQuitting;

        public static VERASystem Instance
        {
            get
            {
                if (_isQuitting) return null;
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _isQuitting = false;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Veil Integrity")]
        [SerializeField] private float _veilIntegrity = 100f;
        [SerializeField] private float _veilDecayRate = 0.1f;
        [SerializeField] private float _veilRecoveryRate = 0.05f;

        [Header("Glitch Settings")]
        [SerializeField] private float _glitchChanceBase = 0.05f;
        [SerializeField] private float _glitchChancePerCorruption = 0.01f;
        [SerializeField] private float _glitchDuration = 0.5f;

        [Header("Personality Thresholds")]
        [SerializeField] private float _corruptedThreshold = 50f;
        [SerializeField] private float _criticalThreshold = 25f;
        [SerializeField] private float _abyssalThreshold = 10f;

        // =============================================================================
        // STATE
        // =============================================================================

        private VERAPersonality _currentPersonality = VERAPersonality.Normal;
        private bool _isGlitching = false;
        private float _glitchTimer = 0f;
        private Queue<VERADialogueRequest> _dialogueQueue = new Queue<VERADialogueRequest>();
        private bool _isSpeaking = false;

        // =============================================================================
        // PROPERTIES
        // =============================================================================

        public float VeilIntegrity => _veilIntegrity;
        public VERAPersonality CurrentPersonality => _currentPersonality;
        public bool IsGlitching => _isGlitching;
        public bool IsSpeaking => _isSpeaking;

        // =============================================================================
        // EVENTS
        // =============================================================================

        public event Action<float> OnVeilIntegrityChanged;
        public event Action<VERAPersonality> OnPersonalityChanged;
        public event Action<bool> OnGlitchStateChanged;
        public event Action<string, VERAPersonality> OnDialogueRequested;
        public event Action OnDialogueComplete;

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
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            UpdatePersonality();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateGlitchState();
            ProcessDialogueQueue();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // =============================================================================
        // EVENT SUBSCRIPTIONS (Using static EventBus pattern)
        // =============================================================================

        private void SubscribeToEvents()
        {
            // Subscribe to static EventBus events
            EventBus.OnBattleStarted += HandleBattleStarted;
            EventBus.OnBattleEnded += HandleBattleEnded;
            EventBus.OnCombatantDied += HandleCombatantDied;
            EventBus.OnMonsterCaptured += HandleMonsterCaptured;
            EventBus.OnCorruptionChanged += HandleCorruptionChanged;
            EventBus.OnShrineEntered += HandleShrineEntered;
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.OnBattleStarted -= HandleBattleStarted;
            EventBus.OnBattleEnded -= HandleBattleEnded;
            EventBus.OnCombatantDied -= HandleCombatantDied;
            EventBus.OnMonsterCaptured -= HandleMonsterCaptured;
            EventBus.OnCorruptionChanged -= HandleCorruptionChanged;
            EventBus.OnShrineEntered -= HandleShrineEntered;
        }

        // =============================================================================
        // VEIL INTEGRITY
        // =============================================================================

        /// <summary>
        /// Modify the Veil Integrity by a delta amount.
        /// Positive values increase integrity, negative values decrease it.
        /// </summary>
        public void ModifyVeilIntegrity(float delta)
        {
            float oldValue = _veilIntegrity;
            _veilIntegrity = Mathf.Clamp(_veilIntegrity + delta, 0f, 100f);

            if (!Mathf.Approximately(oldValue, _veilIntegrity))
            {
                OnVeilIntegrityChanged?.Invoke(_veilIntegrity);
                UpdatePersonality();
                ErrorLogger.Combat($"VERA: Veil Integrity changed from {oldValue:F1}% to {_veilIntegrity:F1}%");
            }
        }

        /// <summary>
        /// Set the Veil Integrity to an absolute value.
        /// </summary>
        public void SetVeilIntegrity(float value)
        {
            float oldValue = _veilIntegrity;
            _veilIntegrity = Mathf.Clamp(value, 0f, 100f);

            if (!Mathf.Approximately(oldValue, _veilIntegrity))
            {
                OnVeilIntegrityChanged?.Invoke(_veilIntegrity);
                UpdatePersonality();
            }
        }

        /// <summary>
        /// Apply passive decay to Veil Integrity over time.
        /// Called during combat or high-stress situations.
        /// </summary>
        public void ApplyVeilDecay(float deltaTime)
        {
            ModifyVeilIntegrity(-_veilDecayRate * deltaTime);
        }

        /// <summary>
        /// Apply passive recovery to Veil Integrity over time.
        /// Called during rest or at shrines.
        /// </summary>
        public void ApplyVeilRecovery(float deltaTime)
        {
            ModifyVeilIntegrity(_veilRecoveryRate * deltaTime);
        }

        // =============================================================================
        // PERSONALITY
        // =============================================================================

        private void UpdatePersonality()
        {
            VERAPersonality newPersonality;

            if (_veilIntegrity <= _abyssalThreshold)
            {
                newPersonality = VERAPersonality.Abyssal;
            }
            else if (_veilIntegrity <= _criticalThreshold)
            {
                newPersonality = VERAPersonality.Critical;
            }
            else if (_veilIntegrity <= _corruptedThreshold)
            {
                newPersonality = VERAPersonality.Corrupted;
            }
            else
            {
                newPersonality = VERAPersonality.Normal;
            }

            if (newPersonality != _currentPersonality)
            {
                _currentPersonality = newPersonality;
                OnPersonalityChanged?.Invoke(_currentPersonality);
                ErrorLogger.Combat($"VERA: Personality changed to {_currentPersonality}");
            }
        }

        // =============================================================================
        // GLITCH SYSTEM
        // =============================================================================

        private void UpdateGlitchState()
        {
            if (_isGlitching)
            {
                _glitchTimer -= Time.deltaTime;
                if (_glitchTimer <= 0f)
                {
                    _isGlitching = false;
                    OnGlitchStateChanged?.Invoke(false);
                }
            }
            else
            {
                // Random glitch chance based on corruption
                float glitchChance = _glitchChanceBase +
                    (_glitchChancePerCorruption * (100f - _veilIntegrity));

                if (UnityEngine.Random.value < glitchChance * Time.deltaTime)
                {
                    TriggerGlitch();
                }
            }
        }

        /// <summary>
        /// Force a glitch effect.
        /// </summary>
        public void TriggerGlitch()
        {
            if (!_isGlitching)
            {
                _isGlitching = true;
                _glitchTimer = _glitchDuration;
                OnGlitchStateChanged?.Invoke(true);
                ErrorLogger.Combat("VERA: Glitch triggered");
            }
        }

        // =============================================================================
        // DIALOGUE SYSTEM
        // =============================================================================

        /// <summary>
        /// Request VERA to speak a line of dialogue.
        /// </summary>
        public void RequestDialogue(VERADialogueTrigger trigger, Dictionary<string, string> context = null)
        {
            string dialogue = GenerateDialogue(trigger, context);

            var request = new VERADialogueRequest
            {
                Text = dialogue,
                Personality = _currentPersonality,
                Trigger = trigger,
                Priority = GetDialoguePriority(trigger)
            };

            // High priority interrupts, otherwise queue
            if (request.Priority >= 10 && _isSpeaking)
            {
                // Interrupt current dialogue
                _dialogueQueue.Clear();
            }

            _dialogueQueue.Enqueue(request);
        }

        private void ProcessDialogueQueue()
        {
            if (!_isSpeaking && _dialogueQueue.Count > 0)
            {
                var request = _dialogueQueue.Dequeue();
                _isSpeaking = true;
                OnDialogueRequested?.Invoke(request.Text, request.Personality);
            }
        }

        /// <summary>
        /// Called by UI when dialogue display is complete.
        /// </summary>
        public void CompleteDialogue()
        {
            _isSpeaking = false;
            OnDialogueComplete?.Invoke();
        }

        private string GenerateDialogue(VERADialogueTrigger trigger, Dictionary<string, string> context)
        {
            // Get base dialogue for trigger
            string[] options = GetDialogueOptions(trigger, _currentPersonality);
            string baseDialogue = options[UnityEngine.Random.Range(0, options.Length)];

            // Apply context replacements
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    baseDialogue = baseDialogue.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            // Apply glitch effects if corrupted
            if (_currentPersonality >= VERAPersonality.Corrupted)
            {
                baseDialogue = ApplyGlitchText(baseDialogue);
            }

            return baseDialogue;
        }

        private string[] GetDialogueOptions(VERADialogueTrigger trigger, VERAPersonality personality)
        {
            // Return dialogue options based on trigger and personality
            // This will be expanded with full dialogue trees

            return trigger switch
            {
                VERADialogueTrigger.BattleStart => personality switch
                {
                    VERAPersonality.Normal => new[] {
                        "Combat detected. Analyzing threat levels.",
                        "Engaging combat protocols. Stay focused.",
                        "Hostiles identified. Recommend tactical approach."
                    },
                    VERAPersonality.Corrupted => new[] {
                        "Combat... yes... let the Veil t̷e̷a̷r̷...",
                        "They're here. Make them b̶l̶e̶e̶d̶.",
                        "F-fight... or be consumed..."
                    },
                    VERAPersonality.Critical => new[] {
                        "V̸̧͓̈́E̶̢̛I̵͎̔L̴̰̆ ̷͓̌F̵̣̈́A̶̰̐I̵̺͠L̴̰̆I̵̺͠N̴̰̆G̵̣̈́... k-kill them all...",
                        "C̷̨̛a̴͎̔n̵̺͠'̷͓̌t̶̢̛ ̴͎̔s̵̺͠ť̷͓ơ̶̢p̴͎̔ ̵̺͠ǐ̷͓t̶̢̛... embrace the void...",
                        "They're beautiful when they s̷̨̈́c̴͎̔r̵̺͠ě̷͓a̶̢̛m̴͎̔..."
                    },
                    VERAPersonality.Abyssal => new[] {
                        "T̴̛H̸̨̛E̶̢̛ ̴͎̔V̵̺͠Ǒ̷͓I̶̢̛D̴͎̔ ̵̺͠Ȟ̷͓Ư̶̢N̴͎̔G̵̺͠Ě̷͓R̶̢̛S̴͎̔",
                        "L̴̛Ę̸̛T̶̢̛ ̴͎̔M̵̺͠Ě̷͓ ̶̢̛I̴͎̔N̵̺͠...",
                        "W̴̛Ę̸̛ ̶̢̛A̴͎̔R̵̺͠Ě̷͓ ̶̢̛O̴͎̔N̵̺͠Ě̷͓..."
                    },
                    _ => new[] { "..." }
                },

                VERADialogueTrigger.BattleVictory => personality switch
                {
                    VERAPersonality.Normal => new[] {
                        "Combat concluded. Well fought.",
                        "Threat neutralized. Analyzing combat data.",
                        "Victory achieved. Recommend recovery period."
                    },
                    VERAPersonality.Corrupted => new[] {
                        "Yes... more... w-wait, what was I saying?",
                        "They're gone. Good. G̷̨̈́o̴͎̔o̵̺͠ď̷͓...",
                        "The silence is... almost p̶e̶a̶c̶e̶f̶u̶l̶."
                    },
                    _ => new[] { "..." }
                },

                VERADialogueTrigger.AllyDefeated => personality switch
                {
                    VERAPersonality.Normal => new[] {
                        "Ally down! Recommend immediate revival.",
                        "Warning: Party member incapacitated.",
                        "Tactical disadvantage. Consider retreat."
                    },
                    VERAPersonality.Corrupted => new[] {
                        "One falls... more will f̷o̷l̷l̷o̷w̷...",
                        "W-weakness... cannot be t̶o̶l̶e̶r̶a̶t̶e̶d̶...",
                        "Let them rest... forever..."
                    },
                    _ => new[] { "..." }
                },

                VERADialogueTrigger.MonsterCaptured => personality switch
                {
                    VERAPersonality.Normal => new[] {
                        "Capture successful. New ally acquired.",
                        "Binding complete. Monster contained.",
                        "Excellent. This one has potential."
                    },
                    VERAPersonality.Corrupted => new[] {
                        "Bound... like us... w-welcome to the cage...",
                        "Another s̷o̷u̷l̷ chained to our cause...",
                        "Yes... join us... j̶o̶i̶n̶ ̶u̶s̶..."
                    },
                    _ => new[] { "..." }
                },

                VERADialogueTrigger.LowHealth => personality switch
                {
                    VERAPersonality.Normal => new[] {
                        "Critical health warning! Heal immediately.",
                        "Danger! Health levels critical.",
                        "Warning: Imminent death if not healed."
                    },
                    VERAPersonality.Corrupted => new[] {
                        "Pain... such sweet p̷a̷i̷n̷...",
                        "Let it end... no, f-fight! FIGHT!",
                        "The void calls... d-don't answer..."
                    },
                    _ => new[] { "..." }
                },

                VERADialogueTrigger.Idle => personality switch
                {
                    VERAPersonality.Normal => new[] {
                        "Systems nominal. Awaiting input.",
                        "All clear. What's our next objective?",
                        "Standing by."
                    },
                    VERAPersonality.Corrupted => new[] {
                        "The silence... it s̷c̷r̷e̷a̷m̷s̷...",
                        "W-waiting... always waiting...",
                        "Can you hear them too?"
                    },
                    _ => new[] { "..." }
                },

                _ => new[] { "..." }
            };
        }

        private string ApplyGlitchText(string text)
        {
            // Apply Zalgo-style glitch effects based on corruption level
            float glitchIntensity = 1f - (_veilIntegrity / 100f);

            if (UnityEngine.Random.value > glitchIntensity)
                return text;

            char[] glitchChars = { '̷', '̶', '̴', '̵', '̸' };
            var result = new System.Text.StringBuilder();

            foreach (char c in text)
            {
                result.Append(c);
                if (char.IsLetter(c) && UnityEngine.Random.value < glitchIntensity * 0.3f)
                {
                    result.Append(glitchChars[UnityEngine.Random.Range(0, glitchChars.Length)]);
                }
            }

            return result.ToString();
        }

        private int GetDialoguePriority(VERADialogueTrigger trigger)
        {
            return trigger switch
            {
                VERADialogueTrigger.AllyDefeated => 10,
                VERADialogueTrigger.LowHealth => 9,
                VERADialogueTrigger.BattleStart => 8,
                VERADialogueTrigger.BattleVictory => 7,
                VERADialogueTrigger.MonsterCaptured => 6,
                VERADialogueTrigger.Idle => 1,
                _ => 5
            };
        }

        // =============================================================================
        // EVENT HANDLERS (Matching EventBus delegate signatures)
        // =============================================================================

        private void HandleBattleStarted()
        {
            RequestDialogue(VERADialogueTrigger.BattleStart);
            // Start veil decay during combat
            ErrorLogger.Combat("VERA: Battle started - engaging combat mode");
        }

        private void HandleBattleEnded(bool victory)
        {
            if (victory)
            {
                RequestDialogue(VERADialogueTrigger.BattleVictory);
            }
            else
            {
                RequestDialogue(VERADialogueTrigger.BattleDefeat);
            }
        }

        private void HandleCombatantDied(string combatantId)
        {
            // Check if this was a player ally (simple heuristic - starts with "hero_" or "ally_")
            bool isPlayerAlly = combatantId.StartsWith("hero_") || combatantId.StartsWith("ally_");
            
            if (isPlayerAlly)
            {
                RequestDialogue(VERADialogueTrigger.AllyDefeated, new Dictionary<string, string>
                {
                    { "name", combatantId }
                });
            }
            else
            {
                // Enemy defeated - minor veil recovery
                ModifyVeilIntegrity(1f);
            }
        }

        private void HandleMonsterCaptured(string monsterId)
        {
            RequestDialogue(VERADialogueTrigger.MonsterCaptured, new Dictionary<string, string>
            {
                { "monster", monsterId }
            });
            // Capturing monsters slightly corrupts VERA (moral ambiguity)
            ModifyVeilIntegrity(-2f);
        }

        private void HandleCorruptionChanged(string monsterId, float newCorruption)
        {
            // Veil Integrity is affected by party corruption levels
            // High corruption in party = faster VERA degradation
            if (newCorruption > 50f)
            {
                ModifyVeilIntegrity(-0.5f);
            }
        }

        private void HandleShrineEntered(string shrineId)
        {
            // Shrines help recover VERA's veil integrity
            RequestDialogue(VERADialogueTrigger.ShrineActivated);
            ModifyVeilIntegrity(10f);
            ErrorLogger.Combat($"VERA: Shrine entered ({shrineId}) - veil recovering");
        }

        // =============================================================================
        // SAVE/LOAD
        // =============================================================================

        public VERASaveData GetSaveData()
        {
            return new VERASaveData
            {
                VeilIntegrity = _veilIntegrity,
                CurrentPersonality = _currentPersonality
            };
        }

        public void LoadSaveData(VERASaveData data)
        {
            _veilIntegrity = data.VeilIntegrity;
            _currentPersonality = data.CurrentPersonality;
            OnVeilIntegrityChanged?.Invoke(_veilIntegrity);
            OnPersonalityChanged?.Invoke(_currentPersonality);
        }
    }

    // =============================================================================
    // SUPPORTING TYPES
    // =============================================================================

    public enum VERAPersonality
    {
        Normal = 0,
        Corrupted = 1,
        Critical = 2,
        Abyssal = 3
    }

    public enum VERADialogueTrigger
    {
        BattleStart,
        BattleVictory,
        BattleDefeat,
        AllyDefeated,
        EnemyDefeated,
        MonsterCaptured,
        CaptureFailed,
        LowHealth,
        LevelUp,
        ItemFound,
        BossEncounter,
        StoryMoment,
        Idle,
        AreaEnter,
        ShrineActivated
    }

    public struct VERADialogueRequest
    {
        public string Text;
        public VERAPersonality Personality;
        public VERADialogueTrigger Trigger;
        public int Priority;
    }

    [Serializable]
    public class VERASaveData
    {
        public float VeilIntegrity = 100f;
        public VERAPersonality CurrentPersonality = VERAPersonality.Normal;
    }
}
