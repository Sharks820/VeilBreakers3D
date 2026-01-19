using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;
using VeilBreakers.Managers;

namespace VeilBreakers.Test
{
    /// <summary>
    /// Comprehensive test script for the Save/Load system.
    /// Attach to a GameObject and call RunAllTests() or use the Inspector button.
    /// </summary>
    public class SaveSystemTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runOnStart = false;
        [SerializeField] private bool _cleanupAfterTests = true;

        [Header("Test Results")]
        [SerializeField] private int _testsPassed;
        [SerializeField] private int _testsFailed;
        [SerializeField] private List<string> _failedTests = new List<string>();

        private const int TEST_SLOT = 0; // Use slot 0 for testing

        // =============================================================================
        // UNITY LIFECYCLE
        // =============================================================================

        private async void Start()
        {
            if (_runOnStart)
            {
                await Task.Delay(500); // Wait for managers to initialize
                await RunAllTestsAsync();
            }
        }

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        [ContextMenu("Run All Tests")]
        public async void RunAllTests()
        {
            await RunAllTestsAsync();
        }

        public async Task RunAllTestsAsync()
        {
            _testsPassed = 0;
            _testsFailed = 0;
            _failedTests.Clear();

            Debug.Log("=== SAVE SYSTEM TESTS STARTING ===");

            // Ensure SaveManager exists
            if (SaveManager.Instance == null)
            {
                var go = new GameObject("SaveManager");
                go.AddComponent<SaveManager>();
                await Task.Delay(100); // Let it initialize
            }

            // Run tests in order
            await Test_SaveData_CreateNew();
            await Test_SaveData_Validate();
            await Test_SaveFileHandler_SerializeDeserialize();
            await Test_SaveFileHandler_Compression();
            await Test_SaveFileHandler_Checksum();
            await Test_SaveManager_CreateAndSave();
            await Test_SaveManager_Load();
            await Test_SaveManager_SlotMetadata();
            await Test_SaveManager_Delete();
            await Test_MigrationRunner_CanMigrate();
            await Test_ShrineManager_Discovery();
            await Test_FullSaveCycle();

            // Report results
            Debug.Log("=== SAVE SYSTEM TESTS COMPLETE ===");
            Debug.Log($"Passed: {_testsPassed} | Failed: {_testsFailed}");

            if (_testsFailed > 0)
            {
                Debug.LogError("FAILED TESTS:");
                foreach (var test in _failedTests)
                {
                    Debug.LogError($"  - {test}");
                }
            }

            // Cleanup
            if (_cleanupAfterTests)
            {
                CleanupTestFiles();
            }
        }

        // =============================================================================
        // INDIVIDUAL TESTS
        // =============================================================================

        private async Task Test_SaveData_CreateNew()
        {
            string testName = "SaveData.CreateNew";
            try
            {
                var data = SaveData.CreateNew("hero_bastion", "TestHero", Path.IRONBOUND);

                Assert(data != null, "Data should not be null");
                Assert(data.version == SaveVersion.CURRENT, "Version should match current");
                Assert(data.heroId == "hero_bastion", "HeroId should match");
                Assert(data.heroName == "TestHero", "HeroName should match");
                Assert(data.heroPath == Path.IRONBOUND, "Path should match");
                Assert(!string.IsNullOrEmpty(data.saveId), "SaveId should be generated");
                Assert(!string.IsNullOrEmpty(data.saveDate), "SaveDate should be set");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_SaveData_Validate()
        {
            string testName = "SaveData.Validate";
            try
            {
                // Valid data
                var validData = SaveData.CreateNew("hero_bastion", "Test", Path.IRONBOUND);
                Assert(validData.Validate(), "Valid data should pass validation");

                // Invalid data - missing heroId
                var invalidData = new SaveData { heroId = null, heroLevel = 1, version = 1 };
                Assert(!invalidData.Validate(), "Invalid data should fail validation");

                // Invalid data - level < 1
                var invalidLevel = new SaveData { heroId = "test", heroLevel = 0, version = 1 };
                Assert(!invalidLevel.Validate(), "Zero level should fail validation");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_SaveFileHandler_SerializeDeserialize()
        {
            string testName = "SaveFileHandler.SerializeDeserialize";
            try
            {
                // Create test data
                var original = SaveData.CreateNew("hero_rend", "SerializeTest", Path.FANGBORN);
                original.heroLevel = 42;
                original.currency = 9999;
                original.party.Add(SavedMonster.Create("monster_hollow", 15, 25.5f));

                // Serialize
                byte[] bytes = SaveFileHandler.SerializeToBytes(original);
                Assert(bytes != null && bytes.Length > 0, "Serialized bytes should not be empty");
                Assert(SaveFileHandler.ValidateMagicBytes(bytes), "Magic bytes should be valid");

                // Deserialize
                var restored = SaveFileHandler.DeserializeFromBytes(bytes);
                Assert(restored != null, "Deserialized data should not be null");
                Assert(restored.heroId == original.heroId, "HeroId should match");
                Assert(restored.heroName == original.heroName, "HeroName should match");
                Assert(restored.heroLevel == original.heroLevel, "HeroLevel should match");
                Assert(restored.currency == original.currency, "Currency should match");
                Assert(restored.party.Count == 1, "Party count should match");
                Assert(restored.party[0].monsterId == "monster_hollow", "Monster ID should match");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_SaveFileHandler_Compression()
        {
            string testName = "SaveFileHandler.Compression";
            try
            {
                // Create large test data
                var data = SaveData.CreateNew("hero_marrow", "CompressionTest", Path.VOIDTOUCHED);
                for (int i = 0; i < 100; i++)
                {
                    data.storage.Add(SavedMonster.Create($"monster_{i}", i, i * 0.5f));
                }

                // Serialize (includes compression)
                byte[] bytes = SaveFileHandler.SerializeToBytes(data);

                // Raw JSON size for comparison
                string json = JsonUtility.ToJson(data);
                int rawSize = System.Text.Encoding.UTF8.GetByteCount(json);

                Debug.Log($"[Test] Raw JSON: {rawSize} bytes, Compressed+Encrypted: {bytes.Length} bytes");

                // Compression should reduce size (typically 80%+)
                Assert(bytes.Length < rawSize, "Compressed size should be smaller than raw");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_SaveFileHandler_Checksum()
        {
            string testName = "SaveFileHandler.Checksum";
            try
            {
                var data = SaveData.CreateNew("hero_mirage", "ChecksumTest", Path.UNCHAINED);
                byte[] bytes = SaveFileHandler.SerializeToBytes(data);

                // Valid checksum should deserialize
                var restored = SaveFileHandler.DeserializeFromBytes(bytes);
                Assert(restored != null, "Valid checksum should allow deserialization");

                // Corrupt the data (change a byte in payload)
                byte[] corrupted = (byte[])bytes.Clone();
                corrupted[50] = (byte)(corrupted[50] ^ 0xFF); // Flip bits

                // Corrupted data should fail
                var corruptedResult = SaveFileHandler.DeserializeFromBytes(corrupted);
                Assert(corruptedResult == null, "Corrupted data should fail checksum");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_SaveManager_CreateAndSave()
        {
            string testName = "SaveManager.CreateAndSave";
            try
            {
                bool success = await SaveManager.Instance.CreateNewSaveAsync(
                    TEST_SLOT,
                    "hero_bastion",
                    "SaveManagerTest",
                    Path.IRONBOUND
                );

                Assert(success, "Create and save should succeed");
                Assert(SaveManager.Instance.HasActiveSave, "Should have active save");
                Assert(SaveManager.Instance.CurrentSlot == TEST_SLOT, "Current slot should match");
                Assert(SaveManager.Instance.SlotExists(TEST_SLOT), "Slot should exist");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }
        }

        private async Task Test_SaveManager_Load()
        {
            string testName = "SaveManager.Load";
            try
            {
                // First create a save
                await SaveManager.Instance.CreateNewSaveAsync(
                    TEST_SLOT,
                    "hero_rend",
                    "LoadTest",
                    Path.FANGBORN
                );

                // Clear current save
                var tempSave = SaveManager.Instance.CurrentSave;

                // Load it back
                bool success = await SaveManager.Instance.LoadAsync(TEST_SLOT);

                Assert(success, "Load should succeed");
                Assert(SaveManager.Instance.CurrentSave != null, "Current save should not be null");
                Assert(SaveManager.Instance.CurrentSave.heroName == "LoadTest", "Hero name should match");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }
        }

        private async Task Test_SaveManager_SlotMetadata()
        {
            string testName = "SaveManager.SlotMetadata";
            try
            {
                // Create a save with specific data
                await SaveManager.Instance.CreateNewSaveAsync(
                    TEST_SLOT,
                    "hero_marrow",
                    "MetadataTest",
                    Path.VOIDTOUCHED
                );

                // Get metadata
                var metadata = await SaveManager.Instance.GetSlotMetadataAsync(TEST_SLOT);

                Assert(metadata.hasData, "Slot should have data");
                Assert(!metadata.isCorrupted, "Slot should not be corrupted");
                Assert(metadata.heroId == "hero_marrow", "HeroId should match");
                Assert(metadata.heroName == "MetadataTest", "HeroName should match");
                Assert(metadata.heroPath == Path.VOIDTOUCHED, "Path should match");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }
        }

        private async Task Test_SaveManager_Delete()
        {
            string testName = "SaveManager.Delete";
            try
            {
                // Ensure save exists
                if (!SaveManager.Instance.SlotExists(TEST_SLOT))
                {
                    await SaveManager.Instance.CreateNewSaveAsync(TEST_SLOT, "hero_bastion", "DeleteTest", Path.IRONBOUND);
                }

                Assert(SaveManager.Instance.SlotExists(TEST_SLOT), "Slot should exist before delete");

                // Delete
                bool success = SaveManager.Instance.DeleteSlot(TEST_SLOT);

                Assert(success, "Delete should succeed");
                Assert(!SaveManager.Instance.SlotExists(TEST_SLOT), "Slot should not exist after delete");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_MigrationRunner_CanMigrate()
        {
            string testName = "MigrationRunner.CanMigrate";
            try
            {
                var runner = MigrationFactory.Create();

                // Current version can always "migrate" (no-op)
                Assert(runner.CanMigrate(SaveVersion.CURRENT), "Current version should be migratable");

                // Future version cannot migrate (downgrade)
                Assert(!runner.CanMigrate(SaveVersion.CURRENT + 1), "Future version should not be migratable");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }

            await Task.Yield();
        }

        private async Task Test_ShrineManager_Discovery()
        {
            string testName = "ShrineManager.Discovery";
            try
            {
                // Create shrine manager if needed
                if (ShrineManager.Instance == null)
                {
                    var go = new GameObject("ShrineManager");
                    go.AddComponent<ShrineManager>();
                    await Task.Delay(100);
                }

                // Create test shrine data
                var shrine = ScriptableObject.CreateInstance<ShrineData>();
                shrine.shrineId = "test_shrine_001";
                shrine.shrineName = "Test Shrine";
                shrine.areaName = "Test Area";
                shrine.shrinePosition = Vector3.zero;
                shrine.saveRadius = 50f;

                // Register shrine
                ShrineManager.Instance.RegisterShrine(shrine);

                Assert(!ShrineManager.Instance.IsShrineDiscovered("test_shrine_001"), "Shrine should not be discovered initially");

                // Discover shrine
                ShrineManager.Instance.DiscoverShrine("test_shrine_001");

                Assert(ShrineManager.Instance.IsShrineDiscovered("test_shrine_001"), "Shrine should be discovered after discovery");

                // Check save zone
                ShrineManager.Instance.UpdatePlayerPosition(Vector3.zero);
                Assert(ShrineManager.Instance.CanSave, "Should be able to save at shrine position");

                ShrineManager.Instance.UpdatePlayerPosition(new Vector3(100, 0, 0));
                Assert(!ShrineManager.Instance.CanSave, "Should not be able to save far from shrine");

                // Cleanup
                DestroyImmediate(shrine);

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }
        }

        private async Task Test_FullSaveCycle()
        {
            string testName = "FullSaveCycle";
            try
            {
                // 1. Create new save
                bool createSuccess = await SaveManager.Instance.CreateNewSaveAsync(
                    TEST_SLOT,
                    "hero_bastion",
                    "FullCycleTest",
                    Path.IRONBOUND
                );
                Assert(createSuccess, "Create should succeed");

                // 2. Modify save data
                SaveManager.Instance.CurrentSave.heroLevel = 50;
                SaveManager.Instance.CurrentSave.currency = 12345;
                SaveManager.Instance.CurrentSave.party.Add(SavedMonster.Create("monster_test", 25, 10f));
                SaveManager.Instance.SetCurrentLocation("Test Location");

                // 3. Save modifications
                bool saveSuccess = await SaveManager.Instance.SaveAsync(TEST_SLOT);
                Assert(saveSuccess, "Save should succeed");

                // 4. Load and verify
                bool loadSuccess = await SaveManager.Instance.LoadAsync(TEST_SLOT);
                Assert(loadSuccess, "Load should succeed");

                var loaded = SaveManager.Instance.CurrentSave;
                Assert(loaded.heroLevel == 50, "Level should persist");
                Assert(loaded.currency == 12345, "Currency should persist");
                Assert(loaded.party.Count == 1, "Party should persist");
                Assert(loaded.currentLocation == "Test Location", "Location should persist");

                Pass(testName);
            }
            catch (Exception ex)
            {
                Fail(testName, ex.Message);
            }
        }

        // =============================================================================
        // HELPERS
        // =============================================================================

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        private void Pass(string testName)
        {
            _testsPassed++;
            Debug.Log($"<color=green>[PASS]</color> {testName}");
        }

        private void Fail(string testName, string error)
        {
            _testsFailed++;
            _failedTests.Add($"{testName}: {error}");
            Debug.LogError($"<color=red>[FAIL]</color> {testName}: {error}");
        }

        private void CleanupTestFiles()
        {
            try
            {
                // Delete test slot files
                SaveManager.Instance?.DeleteSlot(TEST_SLOT);
                Debug.Log("[Test] Cleanup complete");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Test] Cleanup failed: {ex.Message}");
            }
        }
    }
}
