using System;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// EventBus - Centralized event system for game-wide communication
    /// Replaces Godot's signal system with C# events
    /// </summary>
    public static class EventBus
    {
        // =============================================================================
        // GAME STATE EVENTS
        // =============================================================================

        public static event Action OnGameStarted;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameOver;

        public static void GameStarted() => OnGameStarted?.Invoke();
        public static void GamePaused() => OnGamePaused?.Invoke();
        public static void GameResumed() => OnGameResumed?.Invoke();
        public static void GameOver() => OnGameOver?.Invoke();

        // =============================================================================
        // BATTLE EVENTS
        // =============================================================================

        public static event Action OnBattleStarted;
        public static event Action<bool> OnBattleEnded;  // bool = victory
        public static event Action<int> OnTurnStarted;   // turn number
        public static event Action<int> OnTurnEnded;

        public static void BattleStarted() => OnBattleStarted?.Invoke();
        public static void BattleEnded(bool victory) => OnBattleEnded?.Invoke(victory);
        public static void TurnStarted(int turn) => OnTurnStarted?.Invoke(turn);
        public static void TurnEnded(int turn) => OnTurnEnded?.Invoke(turn);

        // Combat action events
        public static event Action<string, string, int, bool> OnDamageDealt;  // source, target, amount, isCrit
        public static event Action<string, int> OnHealing;                      // target, amount
        public static event Action<string, StatusEffect, int> OnStatusApplied;  // target, effect, duration
        public static event Action<string, StatusEffect> OnStatusRemoved;       // target, effect
        public static event Action<string> OnUnitDefeated;                       // unitId
        public static event Action<string, string> OnSkillUsed;                  // userId, skillId

        public static void DamageDealt(string source, string target, int amount, bool isCrit)
            => OnDamageDealt?.Invoke(source, target, amount, isCrit);
        public static void Healing(string target, int amount)
            => OnHealing?.Invoke(target, amount);
        public static void StatusApplied(string target, StatusEffect effect, int duration)
            => OnStatusApplied?.Invoke(target, effect, duration);
        public static void StatusRemoved(string target, StatusEffect effect)
            => OnStatusRemoved?.Invoke(target, effect);
        public static void UnitDefeated(string unitId)
            => OnUnitDefeated?.Invoke(unitId);
        public static void SkillUsed(string userId, string skillId)
            => OnSkillUsed?.Invoke(userId, skillId);

        // =============================================================================
        // MONSTER EVENTS
        // =============================================================================

        public static event Action<string> OnMonsterCaptured;      // monsterId
        public static event Action<string, int> OnMonsterLevelUp;  // monsterId, newLevel
        public static event Action<string, float> OnCorruptionChanged; // monsterId, newCorruption
        public static event Action<string> OnMonsterEvolved;       // monsterId

        public static void MonsterCaptured(string monsterId)
            => OnMonsterCaptured?.Invoke(monsterId);
        public static void MonsterLevelUp(string monsterId, int newLevel)
            => OnMonsterLevelUp?.Invoke(monsterId, newLevel);
        public static void CorruptionChanged(string monsterId, float newCorruption)
            => OnCorruptionChanged?.Invoke(monsterId, newCorruption);
        public static void MonsterEvolved(string monsterId)
            => OnMonsterEvolved?.Invoke(monsterId);

        // =============================================================================
        // HERO EVENTS
        // =============================================================================

        public static event Action<string, int> OnHeroLevelUp;     // heroId, newLevel
        public static event Action<string, float> OnPathProgress;  // heroId, newPathLevel
        public static event Action<string, string> OnSkillLearned; // heroId, skillId

        public static void HeroLevelUp(string heroId, int newLevel)
            => OnHeroLevelUp?.Invoke(heroId, newLevel);
        public static void PathProgress(string heroId, float newPathLevel)
            => OnPathProgress?.Invoke(heroId, newPathLevel);
        public static void SkillLearned(string heroId, string skillId)
            => OnSkillLearned?.Invoke(heroId, skillId);

        // =============================================================================
        // INVENTORY EVENTS
        // =============================================================================

        public static event Action<string, int> OnItemAdded;       // itemId, quantity
        public static event Action<string, int> OnItemRemoved;     // itemId, quantity
        public static event Action<string> OnItemUsed;             // itemId
        public static event Action<int> OnCurrencyChanged;         // newAmount

        public static void ItemAdded(string itemId, int quantity)
            => OnItemAdded?.Invoke(itemId, quantity);
        public static void ItemRemoved(string itemId, int quantity)
            => OnItemRemoved?.Invoke(itemId, quantity);
        public static void ItemUsed(string itemId)
            => OnItemUsed?.Invoke(itemId);
        public static void CurrencyChanged(int newAmount)
            => OnCurrencyChanged?.Invoke(newAmount);

        // =============================================================================
        // UI EVENTS
        // =============================================================================

        public static event Action<string> OnScreenChanged;        // screenName
        public static event Action<string, string> OnDialogueStarted;  // speakerId, dialogueId
        public static event Action OnDialogueEnded;
        public static event Action<string> OnNotification;         // message

        public static void ScreenChanged(string screenName)
            => OnScreenChanged?.Invoke(screenName);
        public static void DialogueStarted(string speakerId, string dialogueId)
            => OnDialogueStarted?.Invoke(speakerId, dialogueId);
        public static void DialogueEnded()
            => OnDialogueEnded?.Invoke();
        public static void Notification(string message)
            => OnNotification?.Invoke(message);

        // =============================================================================
        // AUDIO EVENTS
        // =============================================================================

        public static event Action<string> OnPlaySFX;              // sfxId
        public static event Action<string> OnPlayMusic;            // musicId
        public static event Action OnStopMusic;

        public static void PlaySFX(string sfxId)
            => OnPlaySFX?.Invoke(sfxId);
        public static void PlayMusic(string musicId)
            => OnPlayMusic?.Invoke(musicId);
        public static void StopMusic()
            => OnStopMusic?.Invoke();

        // =============================================================================
        // CLEANUP
        // =============================================================================

        /// <summary>
        /// Clear all event subscribers (call during scene transitions or cleanup)
        /// </summary>
        public static void ClearAllListeners()
        {
            OnGameStarted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameOver = null;

            OnBattleStarted = null;
            OnBattleEnded = null;
            OnTurnStarted = null;
            OnTurnEnded = null;
            OnDamageDealt = null;
            OnHealing = null;
            OnStatusApplied = null;
            OnStatusRemoved = null;
            OnUnitDefeated = null;
            OnSkillUsed = null;

            OnMonsterCaptured = null;
            OnMonsterLevelUp = null;
            OnCorruptionChanged = null;
            OnMonsterEvolved = null;

            OnHeroLevelUp = null;
            OnPathProgress = null;
            OnSkillLearned = null;

            OnItemAdded = null;
            OnItemRemoved = null;
            OnItemUsed = null;
            OnCurrencyChanged = null;

            OnScreenChanged = null;
            OnDialogueStarted = null;
            OnDialogueEnded = null;
            OnNotification = null;

            OnPlaySFX = null;
            OnPlayMusic = null;
            OnStopMusic = null;

            Debug.Log("[EventBus] All listeners cleared");
        }
    }
}
