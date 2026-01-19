using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Combat;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Systems;
using VeilBreakers.Audio;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Test arena manager for rapid combat testing.
    /// Spawns player party and enemies for battle testing.
    /// </summary>
    public class TestArenaManager : MonoBehaviour
    {
        // =============================================================================
        // CONFIGURATION
        // =============================================================================

        [Header("Test Configuration")]
        [SerializeField] private Path _playerPath = Path.IRONBOUND;
        [SerializeField] private int _playerLevel = 10;

        [Header("Player Party Setup")]
        [SerializeField] private TestCombatantConfig _playerConfig;
        [SerializeField] private TestCombatantConfig[] _partyMonsterConfigs = new TestCombatantConfig[3];

        [Header("Enemy Setup")]
        [SerializeField] private TestCombatantConfig[] _enemyConfigs = new TestCombatantConfig[3];

        [Header("Spawn Points")]
        [SerializeField] private Transform _playerSpawnPoint;
        [SerializeField] private Transform[] _partySpawnPoints;
        [SerializeField] private Transform[] _enemySpawnPoints;

        [Header("Debug")]
        [SerializeField] private bool _autoStartBattle = true;
        [SerializeField] private bool _logBattleEvents = true;

        // =============================================================================
        // STATE
        // =============================================================================

        private Combatant _player;
        private List<Combatant> _playerParty = new List<Combatant>();
        private List<Combatant> _enemyParty = new List<Combatant>();

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private void Start()
        {
            // Ensure bootstrap is initialized
            if (GameBootstrap.Instance == null || !GameBootstrap.Instance.IsInitialized)
            {
                Debug.LogWarning("[TestArena] GameBootstrap not initialized. Creating...");
                var bootstrapObj = new GameObject("[GameBootstrap]");
                bootstrapObj.AddComponent<GameBootstrap>();
            }

            StartCoroutine(InitializeDelayed());
        }

        private IEnumerator InitializeDelayed()
        {
            yield return new WaitForSeconds(0.5f);

            InitializeSpawnPoints();

            if (_autoStartBattle)
            {
                SpawnTestBattle();
            }
        }

        private void OnDestroy()
        {
            // Cleanup spawned combatants
            foreach (var c in _playerParty)
            {
                if (c != null) Destroy(c.gameObject);
            }
            foreach (var c in _enemyParty)
            {
                if (c != null) Destroy(c.gameObject);
            }
            if (_player != null) Destroy(_player.gameObject);
        }

        // =============================================================================
        // SPAWN POINT SETUP
        // =============================================================================

        private void InitializeSpawnPoints()
        {
            // Create default spawn points if not assigned
            if (_playerSpawnPoint == null)
            {
                var psp = new GameObject("PlayerSpawnPoint");
                psp.transform.position = new Vector3(-5f, 0f, 0f);
                _playerSpawnPoint = psp.transform;
            }

            if (_partySpawnPoints == null || _partySpawnPoints.Length == 0)
            {
                _partySpawnPoints = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    var sp = new GameObject($"PartySpawnPoint_{i}");
                    sp.transform.position = new Vector3(-3f, 0f, -2f + i * 2f);
                    _partySpawnPoints[i] = sp.transform;
                }
            }

            if (_enemySpawnPoints == null || _enemySpawnPoints.Length == 0)
            {
                _enemySpawnPoints = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    var sp = new GameObject($"EnemySpawnPoint_{i}");
                    sp.transform.position = new Vector3(5f, 0f, -2f + i * 2f);
                    _enemySpawnPoints[i] = sp.transform;
                }
            }
        }

        // =============================================================================
        // BATTLE SPAWNING
        // =============================================================================

        [ContextMenu("Spawn Test Battle")]
        public void SpawnTestBattle()
        {
            Debug.Log("[TestArena] === SPAWNING TEST BATTLE ===");

            // Clear existing combatants
            ClearCombatants();

            // Spawn player
            _player = SpawnCombatant(GetDefaultPlayerConfig(), _playerSpawnPoint.position, true, true);

            // Spawn party monsters
            _playerParty.Clear();
            for (int i = 0; i < 3; i++)
            {
                var config = i < _partyMonsterConfigs.Length && _partyMonsterConfigs[i] != null
                    ? _partyMonsterConfigs[i]
                    : GetDefaultMonsterConfig(i);

                var monster = SpawnCombatant(config, _partySpawnPoints[i].position, true, false);
                _playerParty.Add(monster);
            }

            // Spawn enemies
            _enemyParty.Clear();
            for (int i = 0; i < 3; i++)
            {
                var config = i < _enemyConfigs.Length && _enemyConfigs[i] != null
                    ? _enemyConfigs[i]
                    : GetDefaultEnemyConfig(i);

                var enemy = SpawnCombatant(config, _enemySpawnPoints[i].position, false, false);
                _enemyParty.Add(enemy);
            }

            // Start battle
            StartCoroutine(StartBattleDelayed());
        }

        private IEnumerator StartBattleDelayed()
        {
            yield return new WaitForEndOfFrame();

            if (BattleManager.Instance != null)
            {
                // Combine player and party monsters into single list
                // Player must be marked with SetPlayer(true) to be identified as the player
                var allPlayers = new List<Combatant> { _player };
                allPlayers.AddRange(_playerParty);

                BattleManager.Instance.StartBattle(allPlayers, _enemyParty, _playerPath);
                Debug.Log("[TestArena] Battle started!");

                // Subscribe to battle events for logging
                if (_logBattleEvents)
                {
                    SubscribeToBattleEvents();
                }
            }
            else
            {
                Debug.LogError("[TestArena] BattleManager not found!");
            }
        }

        private void ClearCombatants()
        {
            foreach (var c in _playerParty)
            {
                if (c != null) Destroy(c.gameObject);
            }
            foreach (var c in _enemyParty)
            {
                if (c != null) Destroy(c.gameObject);
            }
            if (_player != null) Destroy(_player.gameObject);

            _playerParty.Clear();
            _enemyParty.Clear();
            _player = null;
        }

        // =============================================================================
        // COMBATANT SPAWNING
        // =============================================================================

        private Combatant SpawnCombatant(TestCombatantConfig config, Vector3 position, bool isPlayerControlled, bool isPlayer)
        {
            var go = new GameObject(config.displayName);
            go.transform.position = position;

            var combatant = go.AddComponent<Combatant>();
            combatant.Initialize(
                config.id,
                config.displayName,
                config.brand,
                config.maxHp,
                config.maxMp,
                config.attack,
                config.defense,
                config.magic,
                config.resistance,
                config.speed,
                isPlayerControlled
            );

            combatant.SetLevel(config.level);
            combatant.SetRarity(config.rarity);
            combatant.SetCorruption(config.corruption);
            combatant.SetPlayer(isPlayer);
            combatant.SetMonsterId(config.monsterId);
            combatant.SetBoss(config.isBoss);

            // Set abilities
            var abilities = AbilityLoadout.CreateFromSkills(
                "basic_attack",
                config.skill1Id,
                config.skill2Id,
                config.skill3Id,
                config.ultimateId
            );
            combatant.SetAbilities(abilities);

            Debug.Log($"[TestArena] Spawned: {config.displayName} (Brand: {config.brand}, HP: {config.maxHp}, ATK: {config.attack})");

            return combatant;
        }

        // =============================================================================
        // DEFAULT CONFIGS
        // =============================================================================

        private TestCombatantConfig GetDefaultPlayerConfig()
        {
            return _playerConfig ?? new TestCombatantConfig
            {
                id = "player_bastion",
                displayName = "Bastion",
                monsterId = "hero_bastion",
                brand = Brand.NONE, // Heroes have no brand
                level = _playerLevel,
                maxHp = 150,
                maxMp = 80,
                attack = 25,
                defense = 30,
                magic = 15,
                resistance = 25,
                speed = 12,
                rarity = MonsterRarity.LEGENDARY,
                corruption = 0f,
                skill1Id = "skill_aegis_dome",
                skill2Id = "skill_guardian_mark",
                skill3Id = "skill_fortress_stance",
                ultimateId = "ultimate_iron_bastion"
            };
        }

        private TestCombatantConfig GetDefaultMonsterConfig(int index)
        {
            // Create monsters with brands matching IRONBOUND synergy
            Brand[] ironboundBrands = { Brand.IRON, Brand.MEND, Brand.LEECH };

            return new TestCombatantConfig
            {
                id = $"party_monster_{index}",
                displayName = $"Party Monster {index + 1}",
                monsterId = $"monster_test_{index}",
                brand = ironboundBrands[index % ironboundBrands.Length],
                level = _playerLevel,
                maxHp = 100 + index * 20,
                maxMp = 50,
                attack = 20 + index * 5,
                defense = 15 + index * 3,
                magic = 12,
                resistance = 12,
                speed = 10 + index,
                rarity = MonsterRarity.COMMON,
                corruption = 10f * index,
                skill1Id = "skill_slash",
                skill2Id = "skill_power_strike",
                skill3Id = "skill_whirlwind",
                ultimateId = "ultimate_devastation"
            };
        }

        private TestCombatantConfig GetDefaultEnemyConfig(int index)
        {
            // Create enemies with varied brands
            Brand[] enemyBrands = { Brand.SAVAGE, Brand.SURGE, Brand.DREAD };

            return new TestCombatantConfig
            {
                id = $"enemy_monster_{index}",
                displayName = $"Enemy {index + 1}",
                monsterId = $"monster_enemy_{index}",
                brand = enemyBrands[index % enemyBrands.Length],
                level = _playerLevel + index,
                maxHp = 80 + index * 30,
                maxMp = 40,
                attack = 18 + index * 4,
                defense = 12 + index * 2,
                magic = 10,
                resistance = 10,
                speed = 8 + index,
                rarity = index == 2 ? MonsterRarity.RARE : MonsterRarity.COMMON,
                corruption = 25f + index * 15f,
                isBoss = index == 2, // Last enemy is a mini-boss
                skill1Id = "skill_slash",
                skill2Id = "skill_venom_strike",
                skill3Id = "skill_shadow_bolt",
                ultimateId = "ultimate_dark_fury"
            };
        }

        // =============================================================================
        // BATTLE EVENT LOGGING
        // =============================================================================

        private void SubscribeToBattleEvents()
        {
            if (BattleManager.Instance == null) return;

            BattleManager.Instance.OnBattleStart += () => Debug.Log("[Battle] BATTLE STARTED!");
            BattleManager.Instance.OnBattleEnd += () => Debug.Log($"[Battle] BATTLE ENDED - State: {BattleManager.Instance.State}");
            BattleManager.Instance.OnDamageDealt += (atk, def, result) =>
                Debug.Log($"[Battle] {atk.DisplayName} dealt {result.finalDamage} damage to {def.DisplayName} (Brand: {result.brandMultiplier}x, Crit: {result.isCritical})");
            BattleManager.Instance.OnHealApplied += (target, amount) =>
                Debug.Log($"[Battle] {target.DisplayName} healed for {amount}");
            BattleManager.Instance.OnCombatantDeath += (c) =>
                Debug.Log($"[Battle] {c.DisplayName} has been defeated!");
        }

        // =============================================================================
        // DEBUG COMMANDS
        // =============================================================================

        [ContextMenu("Force End Battle - Victory")]
        public void ForceVictory()
        {
            foreach (var enemy in _enemyParty)
            {
                if (enemy.IsAlive)
                {
                    enemy.TakeDamage(enemy.CurrentHp + 1);
                }
            }
        }

        [ContextMenu("Force End Battle - Defeat")]
        public void ForceDefeat()
        {
            if (_player != null && _player.IsAlive)
            {
                _player.TakeDamage(_player.CurrentHp + 1);
            }
            foreach (var monster in _playerParty)
            {
                if (monster.IsAlive)
                {
                    monster.TakeDamage(monster.CurrentHp + 1);
                }
            }
        }

        [ContextMenu("Damage All Enemies (50%)")]
        public void DamageAllEnemies()
        {
            foreach (var enemy in _enemyParty)
            {
                if (enemy.IsAlive)
                {
                    enemy.TakeDamage(enemy.MaxHp / 2);
                }
            }
        }

        [ContextMenu("Heal All Party")]
        public void HealAllParty()
        {
            if (_player != null) _player.Heal(_player.MaxHp);
            foreach (var monster in _playerParty)
            {
                if (monster != null) monster.Heal(monster.MaxHp);
            }
        }

        [ContextMenu("Log Battle State")]
        public void LogBattleState()
        {
            Debug.Log("=== BATTLE STATE ===");
            Debug.Log($"State: {BattleManager.Instance?.State}");
            Debug.Log($"Synergy: {BattleManager.Instance?.SynergyTier}");
            Debug.Log($"Current Target: {BattleManager.Instance?.CurrentTarget?.DisplayName ?? "None"}");

            Debug.Log("\n--- PLAYER PARTY ---");
            if (_player != null)
            {
                Debug.Log($"  Player: {_player.DisplayName} HP:{_player.CurrentHp}/{_player.MaxHp} (Level {_player.Level})");
            }
            foreach (var m in _playerParty)
            {
                if (m != null)
                {
                    Debug.Log($"  {m.DisplayName} HP:{m.CurrentHp}/{m.MaxHp} Brand:{m.Brand} Alive:{m.IsAlive}");
                }
            }

            Debug.Log("\n--- ENEMY PARTY ---");
            foreach (var e in _enemyParty)
            {
                if (e != null)
                {
                    Debug.Log($"  {e.DisplayName} HP:{e.CurrentHp}/{e.MaxHp} Brand:{e.Brand} Alive:{e.IsAlive} Boss:{e.IsBoss}");
                }
            }
        }

        [ContextMenu("Run All System Tests")]
        public void RunAllSystemTests()
        {
            var testSetup = FindFirstObjectByType<CombatTestSetup>();
            if (testSetup != null)
            {
                testSetup.RunAllTests();
            }
            else
            {
                // Create temporary test runner
                var tempObj = new GameObject("[TempTestRunner]");
                var test = tempObj.AddComponent<CombatTestSetup>();
                test.RunAllTests();
                Destroy(tempObj);
            }
        }
    }

    // =============================================================================
    // TEST COMBATANT CONFIG
    // =============================================================================

    [System.Serializable]
    public class TestCombatantConfig
    {
        public string id;
        public string displayName;
        public string monsterId;
        public Brand brand;
        public int level = 1;
        public int maxHp = 100;
        public int maxMp = 50;
        public int attack = 15;
        public int defense = 10;
        public int magic = 10;
        public int resistance = 10;
        public int speed = 10;
        public MonsterRarity rarity = MonsterRarity.COMMON;
        public float corruption = 0f;
        public bool isBoss = false;
        public string skill1Id = "skill_slash";
        public string skill2Id = "skill_power_strike";
        public string skill3Id = "skill_whirlwind";
        public string ultimateId = "ultimate_devastation";
    }
}
