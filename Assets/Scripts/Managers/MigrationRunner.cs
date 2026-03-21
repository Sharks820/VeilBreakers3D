using System;
using System.Collections.Generic;
using UnityEngine;
using VeilBreakers.Core;
using VeilBreakers.Data;

namespace VeilBreakers.Managers
{
    /// <summary>
    /// Interface for save file migrations.
    /// Each migration handles upgrading from one version to the next.
    /// </summary>
    public interface ISaveMigration
    {
        /// <summary>Source version this migration upgrades from</summary>
        int FromVersion { get; }

        /// <summary>Target version this migration upgrades to</summary>
        int ToVersion { get; }

        /// <summary>
        /// Performs the migration on save data.
        /// </summary>
        /// <param name="data">Save data to migrate (modified in place)</param>
        /// <returns>True if migration succeeded, false if failed</returns>
        bool Migrate(SaveData data);

        /// <summary>Description of what this migration does</summary>
        string Description { get; }
    }

    /// <summary>
    /// Runs sequential migrations to upgrade save files from old versions.
    /// Supports migration chains (v1 → v2 → v3 → ... → current).
    /// </summary>
    public class MigrationRunner
    {
        private readonly Dictionary<int, ISaveMigration> _migrations;
        private readonly int _currentVersion;

        /// <summary>
        /// Creates a new migration runner.
        /// </summary>
        /// <param name="currentVersion">Current save format version</param>
        public MigrationRunner(int currentVersion)
        {
            _currentVersion = currentVersion;
            _migrations = new Dictionary<int, ISaveMigration>();
        }

        /// <summary>
        /// Registers a migration.
        /// </summary>
        public void RegisterMigration(ISaveMigration migration)
        {
            if (migration == null)
                throw new ArgumentNullException(nameof(migration));

            if (_migrations.ContainsKey(migration.FromVersion))
            {
                ErrorLogger.Warn($"[MigrationRunner] Overwriting migration from v{migration.FromVersion}");
            }

            _migrations[migration.FromVersion] = migration;
            ErrorLogger.Log($"[MigrationRunner] Registered migration: v{migration.FromVersion} → v{migration.ToVersion} ({migration.Description})");
        }

        /// <summary>
        /// Migrates save data to the current version.
        /// Runs sequential migrations (v1 → v2 → v3 → ... → current).
        /// </summary>
        /// <param name="data">Save data to migrate</param>
        /// <returns>Migrated save data, or null if migration failed</returns>
        public SaveData MigrateToLatest(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[MigrationRunner] Cannot migrate null data");
                return null;
            }

            int startVersion = data.version;

            // Already at current version
            if (data.version == _currentVersion)
            {
                ErrorLogger.Log($"[MigrationRunner] Save already at current version v{_currentVersion}");
                return data;
            }

            // Future version - cannot downgrade
            if (data.version > _currentVersion)
            {
                Debug.LogError($"[MigrationRunner] Save version v{data.version} is newer than current v{_currentVersion}. Cannot downgrade.");
                return null;
            }

            // Run sequential migrations
            ErrorLogger.Log($"[MigrationRunner] Migrating from v{data.version} to v{_currentVersion}...");

            int maxIterations = _currentVersion - data.version + 1;
            int iterations = 0;
            while (data.version < _currentVersion)
            {
                if (++iterations > maxIterations)
                {
                    Debug.LogError($"[MigrationRunner] Migration loop exceeded max iterations ({maxIterations}). Possible circular dependency.");
                    return null;
                }
                if (!_migrations.TryGetValue(data.version, out ISaveMigration migration))
                {
                    Debug.LogError($"[MigrationRunner] No migration found from v{data.version}");
                    return null;
                }

                ErrorLogger.Log($"[MigrationRunner] Running: {migration.Description}");

                try
                {
                    if (!migration.Migrate(data))
                    {
                        Debug.LogError($"[MigrationRunner] Migration v{migration.FromVersion} → v{migration.ToVersion} failed");
                        return null;
                    }

                    data.version = migration.ToVersion;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MigrationRunner] Migration exception: {ex.Message}");
                    return null;
                }
            }

            ErrorLogger.Log($"[MigrationRunner] Migration complete: v{startVersion} → v{_currentVersion}");
            return data;
        }

        /// <summary>
        /// Checks if a save file can be migrated to current version.
        /// </summary>
        public bool CanMigrate(int fromVersion)
        {
            if (fromVersion == _currentVersion) return true;
            if (fromVersion > _currentVersion) return false;

            int version = fromVersion;
            while (version < _currentVersion)
            {
                if (!_migrations.ContainsKey(version))
                {
                    return false;
                }
                version = _migrations[version].ToVersion;
            }
            return true;
        }

        /// <summary>
        /// Gets the migration path as a string for debugging.
        /// </summary>
        public string GetMigrationPath(int fromVersion)
        {
            if (fromVersion == _currentVersion) return $"v{fromVersion} (current)";
            if (fromVersion > _currentVersion) return $"v{fromVersion} → v{_currentVersion} (cannot downgrade)";

            var path = new List<string> { $"v{fromVersion}" };
            int version = fromVersion;

            while (version < _currentVersion)
            {
                if (!_migrations.TryGetValue(version, out ISaveMigration migration))
                {
                    path.Add($"v{version + 1} (MISSING)");
                    break;
                }
                path.Add($"v{migration.ToVersion}");
                version = migration.ToVersion;
            }

            return string.Join(" → ", path);
        }
    }

    // =============================================================================
    // EXAMPLE MIGRATIONS (Add new migrations here as save format changes)
    // =============================================================================

    /// <summary>
    /// Migration template - copy this when adding new migrations.
    /// </summary>
    public class Migration_V1_To_V2 : ISaveMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 2;
        public string Description => "Add save integrity HMAC/device key support";

        public bool Migrate(SaveData data)
        {
            // No structural changes yet; version bump to signal new security scheme
            return true;
        }
    }

    /// <summary>
    /// Creates and configures the migration runner with all registered migrations.
    /// </summary>
    public static class MigrationFactory
    {
        /// <summary>
        /// Creates a fully configured migration runner.
        /// </summary>
        public static MigrationRunner Create()
        {
            var runner = new MigrationRunner(SaveVersion.CURRENT);

            // Register all migrations here
            runner.RegisterMigration(new Migration_V1_To_V2());
            // runner.RegisterMigration(new Migration_V2_To_V3());
            // etc.

            return runner;
        }
    }
}
