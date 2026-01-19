using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Data;

namespace VeilBreakers.Audio
{
    /// <summary>
    /// Integrates the audio system with the battle system.
    /// Handles audio triggers for combat events.
    /// </summary>
    public class AudioBattleIntegration : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Settings")]
        [SerializeField] private bool _autoSubscribe = true;

        [Header("Hit Sound Mapping")]
        [SerializeField] private int _lightDamageThreshold = 50;
        [SerializeField] private int _heavyDamageThreshold = 200;

        // =============================================================================
        // STATE
        // =============================================================================

        private bool _isSubscribed = false;
        private float _lastPlayerHealthPercent = 1f;

        // Pre-allocated list for enemy IDs
        private List<string> _enemyIds = new List<string>(8);

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            if (_autoSubscribe)
            {
                SubscribeToBattleEvents();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromBattleEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromBattleEvents();
        }

        private void Update()
        {
            // Track player health for low health audio
            UpdatePlayerHealth();
        }

        // =============================================================================
        // EVENT SUBSCRIPTION
        // =============================================================================

        /// <summary>
        /// Subscribe to battle events.
        /// </summary>
        public void SubscribeToBattleEvents()
        {
            if (_isSubscribed) return;
            if (BattleManager.Instance == null)
            {
                // Try again later
                Invoke(nameof(SubscribeToBattleEvents), 0.5f);
                return;
            }

            BattleManager.Instance.OnBattleStart += HandleBattleStart;
            BattleManager.Instance.OnBattleEnd += HandleBattleEnd;
            BattleManager.Instance.OnDamageDealt += HandleDamageDealt;
            BattleManager.Instance.OnHealApplied += HandleHealApplied;
            BattleManager.Instance.OnCombatantDeath += HandleCombatantDeath;

            _isSubscribed = true;
            Debug.Log("[AudioBattleIntegration] Subscribed to battle events");
        }

        /// <summary>
        /// Unsubscribe from battle events.
        /// </summary>
        public void UnsubscribeFromBattleEvents()
        {
            if (!_isSubscribed) return;
            if (BattleManager.Instance == null) return;

            BattleManager.Instance.OnBattleStart -= HandleBattleStart;
            BattleManager.Instance.OnBattleEnd -= HandleBattleEnd;
            BattleManager.Instance.OnDamageDealt -= HandleDamageDealt;
            BattleManager.Instance.OnHealApplied -= HandleHealApplied;
            BattleManager.Instance.OnCombatantDeath -= HandleCombatantDeath;

            _isSubscribed = false;
        }

        // =============================================================================
        // BATTLE EVENT HANDLERS
        // =============================================================================

        private void HandleBattleStart()
        {
            Debug.Log("[AudioBattleIntegration] Battle started - triggering audio");

            // Collect enemy IDs for audio loading
            _enemyIds.Clear();
            var enemies = BattleManager.Instance.EnemyParty;
            foreach (var enemy in enemies)
            {
                if (enemy != null && !string.IsNullOrEmpty(enemy.MonsterId))
                {
                    _enemyIds.Add(enemy.MonsterId);
                }
            }

            // Load enemy audio banks
            if (_enemyIds.Count > 0 && AudioManager.Instance != null)
            {
                AudioManager.Instance.OnCombatStart(_enemyIds);
            }

            // Start combat music
            MusicManager.Instance?.StartCombatMusic();

            // Reset player health tracking
            _lastPlayerHealthPercent = 1f;
            LowHealthAudio.Instance?.UpdateHealth(1f);
        }

        private void HandleBattleEnd()
        {
            Debug.Log("[AudioBattleIntegration] Battle ended - triggering audio");

            var state = BattleManager.Instance.State;

            // Play victory/defeat stinger
            if (state == BattleState.VICTORY)
            {
                MusicManager.Instance?.PlayVictory();
            }
            else if (state == BattleState.DEFEAT)
            {
                MusicManager.Instance?.PlayDefeat();
            }

            // Unload combat audio (delayed)
            AudioManager.Instance?.OnCombatEnd();

            // Reset low health audio
            LowHealthAudio.Instance?.UpdateHealth(1f);
        }

        private void HandleDamageDealt(Combatant attacker, Combatant defender, DamageResult result)
        {
            if (AudioManager.Instance == null) return;

            // Determine hit intensity
            string intensity = GetHitIntensity(result.finalDamage);

            // Play hit sound
            if (result.wasBlocked)
            {
                AudioManager.Instance.PlayBlock();
            }
            else if (result.wasDodged)
            {
                AudioManager.Instance.PlayMiss();
            }
            else if (result.isCritical)
            {
                AudioManager.Instance.PlayCriticalHit();
            }
            else
            {
                AudioManager.Instance.PlayCombatHit(intensity);
            }

            // Update combat intensity based on damage
            UpdateCombatIntensity();
        }

        private void HandleHealApplied(Combatant target, int amount)
        {
            // Play heal sound
            AudioManager.Instance?.PlayOneShot("event:/SFX/Combat/Heal");

            // Update low health if player was healed
            if (target.IsPlayer)
            {
                UpdatePlayerHealth();
            }
        }

        private void HandleCombatantDeath(Combatant combatant)
        {
            // Play death sound
            if (!string.IsNullOrEmpty(combatant.MonsterId))
            {
                AudioManager.Instance?.PlayOneShot($"event:/Monsters/{combatant.MonsterId}/Death");
            }
            else
            {
                AudioManager.Instance?.PlayOneShot("event:/SFX/Combat/Death");
            }

            // Update music intensity
            UpdateCombatIntensity();
        }

        // =============================================================================
        // PLAYER HEALTH TRACKING
        // =============================================================================

        private void UpdatePlayerHealth()
        {
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.State != BattleState.PLAYER_TURN &&
                BattleManager.Instance.State != BattleState.ENEMY_TURN)
                return;

            var player = BattleManager.Instance.Player;
            if (player == null) return;

            float currentHealthPercent = player.HealthPercent;

            // Only update if changed significantly
            if (Mathf.Abs(currentHealthPercent - _lastPlayerHealthPercent) > 0.01f)
            {
                _lastPlayerHealthPercent = currentHealthPercent;

                // Update low health audio
                LowHealthAudio.Instance?.UpdateHealth(currentHealthPercent);

                // Update music
                MusicManager.Instance?.UpdatePlayerHealth(currentHealthPercent);
            }
        }

        // =============================================================================
        // COMBAT INTENSITY
        // =============================================================================

        private void UpdateCombatIntensity()
        {
            if (MusicManager.Instance == null) return;
            if (BattleManager.Instance == null) return;

            // Calculate intensity based on:
            // - Player health
            // - Number of enemies remaining
            // - Boss presence

            var player = BattleManager.Instance.Player;
            var enemies = BattleManager.Instance.EnemyParty;

            float healthFactor = player != null ? 1f - player.HealthPercent : 0f;
            float enemyFactor = enemies.Count(e => e.IsAlive) / (float)enemies.Count;

            // Check for boss (simplified - would check actual boss flag)
            bool hasBoss = enemies.Any(e => e.IsAlive && e.IsBoss);

            float intensity = Mathf.Max(healthFactor, enemyFactor * 0.5f);
            if (hasBoss)
            {
                intensity = Mathf.Max(intensity, 0.7f);
            }

            MusicManager.Instance.SetCombatIntensity(intensity);
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private string GetHitIntensity(int damage)
        {
            if (damage >= _heavyDamageThreshold)
                return "Heavy";
            if (damage >= _lightDamageThreshold)
                return "Medium";
            return "Light";
        }
    }
}
