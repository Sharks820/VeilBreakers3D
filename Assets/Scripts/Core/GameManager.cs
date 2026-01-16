using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Core
{
    /// <summary>
    /// GameManager - Central game state manager
    /// Handles game flow, party management, and core game state
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // =============================================================================
        // SINGLETON
        // =============================================================================

        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // =============================================================================
        // GAME STATE
        // =============================================================================

        public enum GameState
        {
            MainMenu,
            Exploring,
            InBattle,
            InDialogue,
            InMenu,
            Paused,
            Loading
        }

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public bool IsPaused => CurrentState == GameState.Paused;

        // =============================================================================
        // PARTY DATA
        // =============================================================================

        /// <summary>
        /// Runtime party member - a captured monster instance
        /// </summary>
        [Serializable]
        public class PartyMember
        {
            public string monsterId;
            public string nickname;
            public int level = 1;
            public int currentHp;
            public int currentMp;
            public float corruption = 50f;
            public int experience;
            public List<string> learnedSkills = new List<string>();

            // Stats calculated from base + level + corruption
            public int maxHp;
            public int maxMp;
            public int attack;
            public int defense;
            public int magic;
            public int resistance;
            public int speed;
        }

        /// <summary>
        /// Active hero data
        /// </summary>
        [Serializable]
        public class ActiveHero
        {
            public string heroId;
            public int level = 1;
            public int currentHp;
            public int currentMp;
            public int experience;
            public float pathLevel;
            public Path chosenPath = Path.NONE;
            public List<string> learnedSkills = new List<string>();

            public int maxHp;
            public int maxMp;
            public int attack;
            public int defense;
            public int magic;
            public int resistance;
            public int speed;
        }

        // The active party: 1 Hero + up to 3 Monsters (max 4 total)
        public ActiveHero CurrentHero { get; private set; }
        public List<PartyMember> Party { get; private set; } = new List<PartyMember>();
        public const int MAX_PARTY_SIZE = 3;  // Monsters only (hero is separate)

        // Currency and inventory
        public int Currency { get; private set; } = 0;

        // =============================================================================
        // INITIALIZATION
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

            Debug.Log("[GameManager] Initialized");
        }

        // =============================================================================
        // STATE MANAGEMENT
        // =============================================================================

        public void ChangeState(GameState newState)
        {
            var oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");

            // Handle state transitions
            switch (newState)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    EventBus.GamePaused();
                    break;

                case GameState.Exploring:
                case GameState.InBattle:
                case GameState.InDialogue:
                case GameState.InMenu:
                    if (oldState == GameState.Paused)
                    {
                        Time.timeScale = 1f;
                        EventBus.GameResumed();
                    }
                    break;
            }
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Paused)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                Time.timeScale = 1f;
                CurrentState = GameState.Exploring;
                EventBus.GameResumed();
            }
        }

        // =============================================================================
        // HERO MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Select and initialize the hero for gameplay
        /// </summary>
        public void SelectHero(string heroId)
        {
            var heroData = GameDatabase.Instance.GetHero(heroId);
            if (heroData == null)
            {
                Debug.LogError($"[GameManager] Hero not found: {heroId}");
                return;
            }

            CurrentHero = new ActiveHero
            {
                heroId = heroId,
                level = 1,
                pathLevel = 0f,
                chosenPath = heroData.GetPrimaryPath()
            };

            // Calculate initial stats
            RecalculateHeroStats();

            // Set HP/MP to max
            CurrentHero.currentHp = CurrentHero.maxHp;
            CurrentHero.currentMp = CurrentHero.maxMp;

            // Add innate skills
            if (heroData.innate_skills != null)
            {
                CurrentHero.learnedSkills.AddRange(heroData.innate_skills);
            }

            Debug.Log($"[GameManager] Hero selected: {heroData.display_name}");
        }

        /// <summary>
        /// Recalculate hero stats based on level and path
        /// </summary>
        public void RecalculateHeroStats()
        {
            if (CurrentHero == null) return;

            var heroData = GameDatabase.Instance.GetHero(CurrentHero.heroId);
            if (heroData == null) return;

            CurrentHero.maxHp = heroData.GetStatAtLevel(Stat.HP, CurrentHero.level);
            CurrentHero.maxMp = heroData.GetStatAtLevel(Stat.MP, CurrentHero.level);
            CurrentHero.attack = heroData.GetStatAtLevel(Stat.ATTACK, CurrentHero.level);
            CurrentHero.defense = heroData.GetStatAtLevel(Stat.DEFENSE, CurrentHero.level);
            CurrentHero.magic = heroData.GetStatAtLevel(Stat.MAGIC, CurrentHero.level);
            CurrentHero.resistance = heroData.GetStatAtLevel(Stat.RESISTANCE, CurrentHero.level);
            CurrentHero.speed = heroData.GetStatAtLevel(Stat.SPEED, CurrentHero.level);

            // Apply path bonuses
            if (CurrentHero.chosenPath != Path.NONE)
            {
                CurrentHero.maxHp = Systems.PathSystem.ApplyPathBonus(CurrentHero.maxHp, Stat.HP, CurrentHero.chosenPath, CurrentHero.pathLevel);
                CurrentHero.attack = Systems.PathSystem.ApplyPathBonus(CurrentHero.attack, Stat.ATTACK, CurrentHero.chosenPath, CurrentHero.pathLevel);
                CurrentHero.defense = Systems.PathSystem.ApplyPathBonus(CurrentHero.defense, Stat.DEFENSE, CurrentHero.chosenPath, CurrentHero.pathLevel);
                CurrentHero.magic = Systems.PathSystem.ApplyPathBonus(CurrentHero.magic, Stat.MAGIC, CurrentHero.chosenPath, CurrentHero.pathLevel);
                CurrentHero.resistance = Systems.PathSystem.ApplyPathBonus(CurrentHero.resistance, Stat.RESISTANCE, CurrentHero.chosenPath, CurrentHero.pathLevel);
                CurrentHero.speed = Systems.PathSystem.ApplyPathBonus(CurrentHero.speed, Stat.SPEED, CurrentHero.chosenPath, CurrentHero.pathLevel);
            }
        }

        // =============================================================================
        // PARTY MANAGEMENT
        // =============================================================================

        /// <summary>
        /// Add a captured monster to the party
        /// </summary>
        public bool AddToParty(string monsterId, int level = 1, float corruption = 50f)
        {
            if (Party.Count >= MAX_PARTY_SIZE)
            {
                Debug.LogWarning("[GameManager] Party is full!");
                return false;
            }

            var monsterData = GameDatabase.Instance.GetMonster(monsterId);
            if (monsterData == null)
            {
                Debug.LogError($"[GameManager] Monster not found: {monsterId}");
                return false;
            }

            var member = new PartyMember
            {
                monsterId = monsterId,
                nickname = monsterData.display_name,
                level = level,
                corruption = corruption
            };

            // Calculate stats
            RecalculateMonsterStats(member);

            // Set HP/MP to max
            member.currentHp = member.maxHp;
            member.currentMp = member.maxMp;

            // Add innate skills
            if (monsterData.innate_skills != null)
            {
                member.learnedSkills.AddRange(monsterData.innate_skills);
            }

            Party.Add(member);
            EventBus.MonsterCaptured(monsterId);

            Debug.Log($"[GameManager] Monster added to party: {monsterData.display_name}");
            return true;
        }

        /// <summary>
        /// Remove monster from party by index
        /// </summary>
        public bool RemoveFromParty(int index)
        {
            if (index < 0 || index >= Party.Count)
            {
                return false;
            }

            Party.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Recalculate monster stats based on level and corruption
        /// </summary>
        public void RecalculateMonsterStats(PartyMember member)
        {
            var monsterData = GameDatabase.Instance.GetMonster(member.monsterId);
            if (monsterData == null) return;

            // Base stats at level
            int baseHp = monsterData.GetStatAtLevel(Stat.HP, member.level);
            int baseMp = monsterData.GetStatAtLevel(Stat.MP, member.level);
            int baseAtk = monsterData.GetStatAtLevel(Stat.ATTACK, member.level);
            int baseDef = monsterData.GetStatAtLevel(Stat.DEFENSE, member.level);
            int baseMag = monsterData.GetStatAtLevel(Stat.MAGIC, member.level);
            int baseRes = monsterData.GetStatAtLevel(Stat.RESISTANCE, member.level);
            int baseSpd = monsterData.GetStatAtLevel(Stat.SPEED, member.level);

            // Apply corruption modifier
            member.maxHp = Systems.CorruptionSystem.ApplyCorruptionModifier(baseHp, member.corruption);
            member.maxMp = Systems.CorruptionSystem.ApplyCorruptionModifier(baseMp, member.corruption);
            member.attack = Systems.CorruptionSystem.ApplyCorruptionModifier(baseAtk, member.corruption);
            member.defense = Systems.CorruptionSystem.ApplyCorruptionModifier(baseDef, member.corruption);
            member.magic = Systems.CorruptionSystem.ApplyCorruptionModifier(baseMag, member.corruption);
            member.resistance = Systems.CorruptionSystem.ApplyCorruptionModifier(baseRes, member.corruption);
            member.speed = Systems.CorruptionSystem.ApplyCorruptionModifier(baseSpd, member.corruption);
        }

        /// <summary>
        /// Heal all party members to full
        /// </summary>
        public void HealParty()
        {
            if (CurrentHero != null)
            {
                CurrentHero.currentHp = CurrentHero.maxHp;
                CurrentHero.currentMp = CurrentHero.maxMp;
            }

            foreach (var member in Party)
            {
                member.currentHp = member.maxHp;
                member.currentMp = member.maxMp;
            }

            Debug.Log("[GameManager] Party fully healed");
        }

        // =============================================================================
        // CURRENCY
        // =============================================================================

        public void AddCurrency(int amount)
        {
            Currency += amount;
            EventBus.CurrencyChanged(Currency);
        }

        public bool SpendCurrency(int amount)
        {
            if (Currency >= amount)
            {
                Currency -= amount;
                EventBus.CurrencyChanged(Currency);
                return true;
            }
            return false;
        }

        // =============================================================================
        // NEW GAME
        // =============================================================================

        /// <summary>
        /// Start a new game with selected hero
        /// </summary>
        public void StartNewGame(string heroId, string starterMonsterId)
        {
            // Clear any existing data
            Party.Clear();
            Currency = 100;  // Starting currency

            // Select hero
            SelectHero(heroId);

            // Add starter monster
            AddToParty(starterMonsterId, 5, 50f);

            ChangeState(GameState.Exploring);
            EventBus.GameStarted();

            Debug.Log("[GameManager] New game started!");
        }
    }
}
