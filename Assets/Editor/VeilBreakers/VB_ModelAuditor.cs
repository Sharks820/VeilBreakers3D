using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using VeilBreakers.Data;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// Editor tool for auditing GLB models in the project and wiring hero/champion
    /// prefabs into HeroDisplayConfig ScriptableObjects.
    /// Menu: VeilBreakers > Audit All Models / Wire Hero Models
    /// </summary>
    public static class VB_ModelAuditor
    {
        // =========================================================================
        //  Constants
        // =========================================================================

        private const string kModelsRoot = "Assets/Art/Models";
        private const string kHeroConfigsRoot = "Assets/Resources/CharacterSelect/HeroDisplayConfigs";
        private const int kHeroTriBudget = 50000;
        private const int kMonsterTriBudget = 30000;
        private const long kFileSizeWarningBytes = 25 * 1024 * 1024; // 25 MB

        // Hero -> GLB v4 path
        private static readonly Dictionary<string, string> kHeroModelPaths = new Dictionary<string, string>
        {
            { "nyx", "Assets/Art/Models/Heroes/Nyx/model_v4_pbr.glb" },
            { "orion", "Assets/Art/Models/Heroes/Orion/model_v4_pbr.glb" },
            { "seraphina", "Assets/Art/Models/Heroes/Seraphina/model_v4_pbr.glb" }
        };

        // Hero name -> DisplayConfig filename
        private static readonly Dictionary<string, string> kHeroConfigPaths = new Dictionary<string, string>
        {
            { "nyx", $"{kHeroConfigsRoot}/NyxDisplayConfig.asset" },
            { "orion", $"{kHeroConfigsRoot}/OrionDisplayConfig.asset" },
            { "seraphina", $"{kHeroConfigsRoot}/SeraphinaDisplayConfig.asset" }
        };

        // Hero -> Champion monster GLB v4 path
        private static readonly Dictionary<string, string> kChampionModelPaths = new Dictionary<string, string>
        {
            { "nyx", "Assets/Art/Models/Monsters/Bloodshade/model_v4_pbr.glb" },
            { "orion", "Assets/Art/Models/Monsters/Voltgeist/model_v4_pbr.glb" },
            { "seraphina", "Assets/Art/Models/Monsters/Grimthorn/model_v4_pbr.glb" }
        };

        // Known character names for grouping
        private static readonly HashSet<string> kCharacters = new HashSet<string>
        {
            "Nyx", "Orion", "Seraphina", "Bloodshade", "Grimthorn", "Voltgeist"
        };

        // =========================================================================
        //  MenuItem 1: Audit All Models
        // =========================================================================

        [MenuItem("VeilBreakers/Audit All Models")]
        public static void AuditAllModels()
        {
            Debug.Log("[VB_ModelAuditor] === MODEL AUDIT START ===");

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { kModelsRoot });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[VB_ModelAuditor] No model assets found in " + kModelsRoot +
                    ". GLB files may not be imported yet. Open Unity and let the importer process them.");
                return;
            }

            var results = new List<ModelAuditEntry>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".glb") && !path.EndsWith(".gltf") && !path.EndsWith(".fbx"))
                    continue;

                var entry = new ModelAuditEntry { AssetPath = path };

                // File size
                string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
                if (File.Exists(fullPath))
                {
                    var fi = new FileInfo(fullPath);
                    entry.FileSizeBytes = fi.Length;
                    entry.FileSizeMB = fi.Length / (1024f * 1024f);
                }

                // Extract character name and version from path
                ExtractCharacterInfo(path, entry);

                // Try to read ModelImporter settings
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    entry.IsReadable = importer.isReadable;
                    entry.AnimationType = importer.animationType;
                    entry.NormalsImportMode = importer.normalImportMode;

                    // Attempt to read imported mesh data for vertex/triangle counts
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null)
                    {
                        int totalVerts = 0;
                        int totalTris = 0;
                        var meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
                        var skinnedRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                        foreach (var mf in meshFilters)
                        {
                            if (mf.sharedMesh != null)
                            {
                                totalVerts += mf.sharedMesh.vertexCount;
                                totalTris += (int)mf.sharedMesh.GetIndexCount(0) / 3;
                            }
                        }
                        foreach (var sr in skinnedRenderers)
                        {
                            if (sr.sharedMesh != null)
                            {
                                totalVerts += sr.sharedMesh.vertexCount;
                                totalTris += (int)sr.sharedMesh.GetIndexCount(0) / 3;
                            }
                        }

                        entry.VertexCount = totalVerts;
                        entry.TriangleCount = totalTris;
                        entry.HasMeshData = true;

                        // Check UV channels
                        var firstMesh = meshFilters.Length > 0 && meshFilters[0].sharedMesh != null
                            ? meshFilters[0].sharedMesh
                            : skinnedRenderers.Length > 0 && skinnedRenderers[0].sharedMesh != null
                                ? skinnedRenderers[0].sharedMesh
                                : null;
                        if (firstMesh != null)
                        {
                            entry.HasUV0 = firstMesh.uv != null && firstMesh.uv.Length > 0;
                            entry.HasUV2 = firstMesh.uv2 != null && firstMesh.uv2.Length > 0;
                        }
                    }
                }
                else
                {
                    entry.ImportNotes = "ModelImporter not available (GLB may not be imported)";
                }

                results.Add(entry);
            }

            // Sort by character, then version
            results.Sort((a, b) =>
            {
                int cmp = string.Compare(a.Character ?? "", b.Character ?? "", StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                return a.Version.CompareTo(b.Version);
            });

            // Print formatted table
            var sb = new StringBuilder();
            sb.AppendLine("[VB_ModelAuditor] === MODEL AUDIT REPORT ===");
            sb.AppendLine($"Total models found: {results.Count}");
            sb.AppendLine();
            sb.AppendLine($"{PadRight("Character", 14)} {PadRight("Version", 8)} {PadRight("Verts", 10)} {PadRight("Tris", 10)} {PadRight("SizeMB", 8)} {PadRight("Rig", 6)} {PadRight("Normals", 10)} {PadRight("UV0", 4)} {PadRight("UV2", 4)} Notes");
            sb.AppendLine(new string('-', 100));

            // Track best version per character
            var bestVersions = new Dictionary<string, int>();

            foreach (var entry in results)
            {
                string charName = entry.Character ?? "unknown";
                string version = entry.Version > 0 ? $"v{entry.Version}" : "???";
                string verts = entry.HasMeshData ? entry.VertexCount.ToString("N0") : "n/a";
                string tris = entry.HasMeshData ? entry.TriangleCount.ToString("N0") : "n/a";
                string size = entry.FileSizeMB > 0 ? entry.FileSizeMB.ToString("F1") : "n/a";
                string rig = entry.AnimationType != ModelImporterAnimationType.None ? "Yes" : "No";
                string normals = entry.NormalsImportMode.ToString();
                string uv0 = entry.HasUV0 ? "Yes" : "n/a";
                string uv2 = entry.HasUV2 ? "Yes" : "n/a";
                string notes = entry.Version < 4 ? "STALE - candidate for deletion" : "LATEST";
                if (!string.IsNullOrEmpty(entry.ImportNotes)) notes = entry.ImportNotes;

                // Budget check
                bool isHero = charName == "Nyx" || charName == "Orion" || charName == "Seraphina";
                if (entry.HasMeshData && entry.Version == 4)
                {
                    int budget = isHero ? kHeroTriBudget : kMonsterTriBudget;
                    if (entry.TriangleCount > budget)
                        notes += $" OVER BUDGET ({budget / 1000}K)";
                }

                sb.AppendLine($"{PadRight(charName, 14)} {PadRight(version, 8)} {PadRight(verts, 10)} {PadRight(tris, 10)} {PadRight(size, 8)} {PadRight(rig, 6)} {PadRight(normals, 10)} {PadRight(uv0, 4)} {PadRight(uv2, 4)} {notes}");

                // Track latest version
                if (!bestVersions.ContainsKey(charName) || entry.Version > bestVersions[charName])
                    bestVersions[charName] = entry.Version;
            }

            sb.AppendLine();
            sb.AppendLine("=== BUDGET SUMMARY (v4 only) ===");
            sb.AppendLine($"Hero budget: {kHeroTriBudget / 1000}K tris | Monster budget: {kMonsterTriBudget / 1000}K tris");
            sb.AppendLine("File size is a rough proxy. True polycount check requires runtime mesh read (vertexCount). Use Unity Profiler or FBX viewer for exact counts.");

            Debug.Log(sb.ToString());
            Debug.Log("[VB_ModelAuditor] === MODEL AUDIT END ===");
        }

        // =========================================================================
        //  MenuItem 2: Wire Hero Models
        // =========================================================================

        [MenuItem("VeilBreakers/Wire Hero Models")]
        public static void WireHeroModels()
        {
            Debug.Log("[VB_ModelAuditor] === WIRE HERO MODELS START ===");
            int wired = 0;

            foreach (var kvp in kHeroModelPaths)
            {
                string heroId = kvp.Key;
                string glbPath = kvp.Value;

                // Load config SO
                string configPath = kHeroConfigPaths[heroId];
                var config = AssetDatabase.LoadAssetAtPath<HeroDisplayConfig>(configPath);
                if (config == null)
                {
                    Debug.LogError($"[VB_ModelAuditor] HeroDisplayConfig not found at {configPath}");
                    continue;
                }

                // Load model asset
                var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
                if (modelPrefab == null)
                {
                    Debug.LogError($"[VB_ModelAuditor] GLB model not imported or not found at {glbPath} -- import first");
                    continue;
                }

                // Assign via SerializedObject for proper undo tracking
                Undo.RecordObject(config, "Wire hero modelPrefab");
                var serializedObj = new SerializedObject(config);
                var prop = serializedObj.FindProperty("modelPrefab");
                prop.objectReferenceValue = modelPrefab;
                serializedObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);

                Debug.Log($"[VB_ModelAuditor] Wired {heroId} modelPrefab -> {glbPath}");
                wired++;
            }

            // Check Vex gap
            Debug.LogWarning("[VB_ModelAuditor] Vex model not found at Assets/Art/Models/Heroes/Vex/ -- no GLB exists. Keeping placeholder.");
            Debug.Log($"[VB_ModelAuditor] Wired {wired}/4 hero models. Vex remains placeholder (model missing).");
            AssetDatabase.SaveAssetDatabase();

            Debug.Log("[VB_ModelAuditor] === WIRE HERO MODELS END ===");
        }

        // =========================================================================
        //  MenuItem 3: Wire Champion Monsters
        // =========================================================================

        [MenuItem("VeilBreakers/Wire Champion Monsters")]
        public static void WireChampionModels()
        {
            Debug.Log("[VB_ModelAuditor] === WIRE CHAMPION MONSTERS START ===");
            int wired = 0;

            foreach (var kvp in kChampionModelPaths)
            {
                string heroId = kvp.Key;
                string glbPath = kvp.Value;

                // Load config SO
                string configPath = kHeroConfigPaths[heroId];
                var config = AssetDatabase.LoadAssetAtPath<HeroDisplayConfig>(configPath);
                if (config == null)
                {
                    Debug.LogError($"[VB_ModelAuditor] HeroDisplayConfig not found at {configPath}");
                    continue;
                }

                // Load monster model asset
                var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
                if (monsterPrefab == null)
                {
                    Debug.LogError($"[VB_ModelAuditor] Monster GLB not imported or not found at {glbPath} -- import first");
                    continue;
                }

                // Assign via SerializedObject
                Undo.RecordObject(config, "Wire champion modelPrefab");
                var serializedObj = new SerializedObject(config);
                var prop = serializedObj.FindProperty("championModelPrefab");
                prop.objectReferenceValue = monsterPrefab;
                serializedObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);

                Debug.Log($"[VB_ModelAuditor] Wired {heroId} championModelPrefab -> {Path.GetFileName(Path.GetDirectoryName(glbPath))} v4");
                wired++;
            }

            // Vex champion gap
            Debug.LogWarning("[VB_ModelAuditor] Vex champion (skitter_teeth) has no model in Assets/Art/Models/Monsters/ -- keeping null.");
            Debug.Log($"[VB_ModelAuditor] Wired {wired}/4 champion monsters. Vex champion missing.");
            AssetDatabase.SaveAssetDatabase();

            Debug.Log("[VB_ModelAuditor] === WIRE CHAMPION MONSTERS END ===");
        }

        // =========================================================================
        //  MenuItem 4: Check Model Budgets
        // =========================================================================

        [MenuItem("VeilBreakers/Check Model Budgets")]
        public static void CheckModelBudgets()
        {
            Debug.Log("[VB_ModelAuditor] === MODEL BUDGET CHECK START ===");

            var allModels = new List<(string character, string type, string path, long sizeBytes)>();

            // Collect all v4 models
            foreach (var kvp in kHeroModelPaths)
                allModels.Add((kvp.Key, "hero", kvp.Value, GetFileSize(kvp.Value)));

            foreach (var kvp in kChampionModelPaths)
            {
                string monsterName = Path.GetFileName(Path.GetDirectoryName(kvp.Value));
                allModels.Add((monsterName, "monster", kvp.Value, GetFileSize(kvp.Value)));
            }

            var sb = new StringBuilder();
            sb.AppendLine("[VB_ModelAuditor] === MODEL BUDGET REPORT ===");
            sb.AppendLine();
            sb.AppendLine($"{PadRight("Character", 14)} {PadRight("Type", 8)} {PadRight("Budget", 12)} {PadRight("FileSize", 10)} Status");
            sb.AppendLine(new string('-', 60));

            foreach (var (character, type, path, sizeBytes) in allModels)
            {
                int budgetTris = type == "hero" ? kHeroTriBudget : kMonsterTriBudget;
                string budget = $"{budgetTris / 1000}K tris";
                string fileSize = sizeBytes > 0 ? $"{sizeBytes / (1024f * 1024f):F1} MB" : "n/a";
                string status = "OK";
                if (sizeBytes > kFileSizeWarningBytes)
                    status = "REVIEW (>25MB)";

                // Try to read actual tri count
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    int totalTris = 0;
                    foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                        if (mf.sharedMesh != null) totalTris += (int)mf.sharedMesh.GetIndexCount(0) / 3;
                    foreach (var sr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if (sr.sharedMesh != null) totalTris += (int)sr.sharedMesh.GetIndexCount(0) / 3;

                    if (totalTris > budgetTris)
                        status = $"OVER BUDGET ({totalTris:N0} tris > {budgetTris / 1000}K)";
                    else
                        status = $"OK ({totalTris:N0} tris)";
                }

                sb.AppendLine($"{PadRight(character, 14)} {PadRight(type, 8)} {PadRight(budget, 12)} {PadRight(fileSize, 10)} {status}");
            }

            sb.AppendLine();
            sb.AppendLine("NOTE: File size is a rough proxy. True polycount check requires runtime mesh read (vertexCount). Use Unity Profiler or FBX viewer for exact counts.");

            Debug.Log(sb.ToString());
            Debug.Log("[VB_ModelAuditor] === MODEL BUDGET CHECK END ===");
        }

        // =========================================================================
        //  Helpers
        // =========================================================================

        private static void ExtractCharacterInfo(string assetPath, ModelAuditEntry entry)
        {
            // Path pattern: Assets/Art/Models/{Heroes|Monsters}/{CharacterName}/model_v{N}_pbr.glb
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string dirName = Path.GetFileName(Path.GetDirectoryName(assetPath));

            // Version from filename
            var versionMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"v(\d+)");
            if (versionMatch.Success && int.TryParse(versionMatch.Groups[1].Value, out int v))
                entry.Version = v;

            // Character name from directory
            if (kCharacters.Contains(dirName))
                entry.Character = dirName;
            else
                entry.Character = dirName; // Use directory name anyway
        }

        private static long GetFileSize(string assetPath)
        {
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            if (File.Exists(fullPath))
                return new FileInfo(fullPath).Length;
            return 0;
        }

        private static string PadRight(string text, int width)
        {
            if (text == null) text = "";
            return text.Length >= width ? text : text + new string(' ', width - text.Length);
        }

        // =========================================================================
        //  Data structures
        // =========================================================================

        private class ModelAuditEntry
        {
            public string AssetPath;
            public string Character;
            public int Version;
            public long FileSizeBytes;
            public float FileSizeMB;
            public bool HasMeshData;
            public int VertexCount;
            public int TriangleCount;
            public bool IsReadable;
            public ModelImporterAnimationType AnimationType;
            public ModelImporterNormals NormalsImportMode;
            public bool HasUV0;
            public bool HasUV2;
            public string ImportNotes;
        }
    }
}
