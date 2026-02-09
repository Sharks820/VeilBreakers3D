using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using System.IO;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// DCC Bridge Editor Window for Unity-Blender workflow integration.
    /// Provides one-click export/import and project synchronization.
    /// </summary>
    public class DCCBridgeEditor : EditorWindow
    {
        private const string kBlenderDefaultPath = @"C:\Program Files\Blender Foundation\Blender 4.4\blender.exe";
        private const string kBlenderAddonPath = @"Tools\DCC_Bridge\BlenderAddon\veilbreakers_dcc_bridge.py";
        
        [SerializeField] private string blenderPath = kBlenderDefaultPath;
        [SerializeField] private bool showAdvancedSettings = false;
        [SerializeField] private bool autoReimport = true;
        [SerializeField] private bool autoRefresh = true;
        
        private Vector2 scrollPosition;
        private string statusMessage = "";
        private MessageType statusType = MessageType.Info;
        private bool isBlenderInstalled = false;
        
        [MenuItem("VeilBreakers/DCC Bridge", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<DCCBridgeEditor>("DCC Bridge");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        private void OnEnable()
        {
            CheckBlenderInstallation();
        }
        
        private void CheckBlenderInstallation()
        {
            // Check for Blender installations
            string[] possiblePaths = new[]
            {
                @"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.4\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.2\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.1\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.0\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe",
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    blenderPath = path;
                    isBlenderInstalled = true;
                    break;
                }
            }
            
            // Also check user preference
            string savedPath = EditorPrefs.GetString("VeilBreakers.BlenderPath", "");
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                blenderPath = savedPath;
                isBlenderInstalled = true;
            }
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawStatus();
            DrawQuickActions();
            DrawAdvancedSettings();
            DrawWorkflowTips();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("VeilBreakers DCC Bridge", titleStyle);
            
            // Subtitle
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Unity ↔ Blender Workflow Integration", subtitleStyle);
            
            EditorGUILayout.Space(10);
            
            // Blender Path
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Blender Installation", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            blenderPath = EditorGUILayout.TextField("Blender Path:", blenderPath);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("VeilBreakers.BlenderPath", blenderPath);
                isBlenderInstalled = File.Exists(blenderPath);
            }
            
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Blender Executable", 
                    Path.GetDirectoryName(blenderPath) ?? @"C:\Program Files\Blender Foundation", "exe");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    blenderPath = selectedPath;
                    EditorPrefs.SetString("VeilBreakers.BlenderPath", blenderPath);
                    isBlenderInstalled = File.Exists(blenderPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Status indicator
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
            if (isBlenderInstalled)
            {
                GUIStyle greenStyle = new GUIStyle(EditorStyles.label);
                greenStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField(new GUIContent(" ✓ Installed", "Blender found at specified path"), greenStyle);
            }
            else
            {
                GUIStyle redStyle = new GUIStyle(EditorStyles.label);
                redStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField(new GUIContent(" ✗ Not Found", "Blender not found at specified path"), redStyle);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }
        
        private void DrawQuickActions()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Install Blender Addon
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Blender Addon", GUILayout.Width(120));
            if (GUILayout.Button("Install/Update Addon"))
            {
                InstallBlenderAddon();
            }
            EditorGUILayout.EndHorizontal();
            
            // Open Blender
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Launch", GUILayout.Width(120));
            if (GUILayout.Button("Open Blender"))
            {
                OpenBlender();
            }
            if (GUILayout.Button("Open with Current Selection"))
            {
                OpenBlenderWithSelection();
            }
            EditorGUILayout.EndHorizontal();
            
            // Export/Import
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Workflow", GUILayout.Width(120));
            if (GUILayout.Button("Reimport All Models"))
            {
                ReimportAllModels();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAdvancedSettings()
        {
            EditorGUILayout.Space(10);
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
            
            if (showAdvancedSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                autoReimport = EditorGUILayout.ToggleLeft(
                    new GUIContent("Auto-reimport after export", 
                        "Automatically reimport models when Blender exports new files"),
                    autoReimport);
                
                autoRefresh = EditorGUILayout.ToggleLeft(
                    new GUIContent("Auto-refresh Asset Database", 
                        "Automatically refresh Asset Database after import"),
                    autoRefresh);
                
                EditorGUILayout.Space(10);
                
                if (GUILayout.Button("Create Model Export Folder Structure"))
                {
                    CreateFolderStructure();
                }
                
                if (GUILayout.Button("Validate Import Settings"))
                {
                    ValidateImportSettings();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawWorkflowTips()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Workflow Tips", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("1. Install the Blender addon using the button above", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. In Blender, use the VeilBreakers sidebar panel to export", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. Files are exported to Assets/Resources/Art/3D_Models/", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("4. Unity will auto-import with optimized settings", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Keyboard Shortcuts:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Ctrl+Shift+E: Quick Export Character", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("- Ctrl+Shift+P: Quick Export Prop", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        // =================================================================
        // ACTIONS
        // =================================================================
        
        private void InstallBlenderAddon()
        {
            if (!isBlenderInstalled)
            {
                SetStatus("Blender not found. Please set the correct path.", MessageType.Error);
                return;
            }
            
            string addonPath = Path.Combine(Application.dataPath, "..", kBlenderAddonPath);
            addonPath = Path.GetFullPath(addonPath);
            
            if (!File.Exists(addonPath))
            {
                SetStatus($"Addon file not found: {addonPath}", MessageType.Error);
                return;
            }
            
            // Run Blender with script to install addon
            string script = $@"
import bpy
import addon_utils

addon_path = r""{addonPath}""

# Install addon
bpy.ops.preferences.addon_install(filepath=addon_path, overwrite=True)

# Enable addon
addon_utils.enable('veilbreakers_dcc_bridge', default_set=True, persistent=True)

# Save preferences
bpy.ops.wm.save_userpref()

print('VeilBreakers DCC Bridge addon installed successfully!')
";
            
            RunBlenderPython(script, "Installing Blender addon...");
        }
        
        private void OpenBlender()
        {
            if (!isBlenderInstalled)
            {
                SetStatus("Blender not found. Please set the correct path.", MessageType.Error);
                return;
            }
            
            try
            {
                Process.Start(blenderPath);
                SetStatus("Blender launched successfully!", MessageType.Info);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to open Blender: {ex.Message}", MessageType.Error);
            }
        }
        
        private void OpenBlenderWithSelection()
        {
            if (!isBlenderInstalled)
            {
                SetStatus("Blender not found. Please set the correct path.", MessageType.Error);
                return;
            }
            
            if (Selection.activeObject == null)
            {
                SetStatus("No object selected in Unity.", MessageType.Warning);
                return;
            }
            
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                SetStatus("Selected object is not an asset.", MessageType.Warning);
                return;
            }
            
            // Check if it's an FBX or model file
            string extension = Path.GetExtension(assetPath).ToLower();
            if (extension != ".fbx" && extension != ".blend" && extension != ".obj")
            {
                SetStatus("Selected object is not a 3D model file.", MessageType.Warning);
                return;
            }
            
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            
            try
            {
                Process.Start(blenderPath, $"\"{fullPath}\"");
                SetStatus($"Opening {Selection.activeObject.name} in Blender...", MessageType.Info);
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to open Blender: {ex.Message}", MessageType.Error);
            }
        }
        
        private void ReimportAllModels()
        {
            string modelsPath = "Assets/Resources/Art/3D_Models";
            
            if (!AssetDatabase.IsValidFolder(modelsPath))
            {
                SetStatus($"Models folder not found: {modelsPath}", MessageType.Warning);
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { modelsPath });
            int count = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
            
            SetStatus($"Reimported {count} model(s) with optimized settings.", MessageType.Info);
        }
        
        private void CreateFolderStructure()
        {
            string basePath = "Assets/Resources/Art/3D_Models";
            string[] folders = new[] { "Characters", "Props", "Environment", "Animations", "VFX" };
            
            foreach (string folder in folders)
            {
                string path = $"{basePath}/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(basePath, folder);
                }
            }
            
            AssetDatabase.Refresh();
            SetStatus("Folder structure created successfully!", MessageType.Info);
        }
        
        private void ValidateImportSettings()
        {
            string modelsPath = "Assets/Resources/Art/3D_Models";
            
            if (!AssetDatabase.IsValidFolder(modelsPath))
            {
                SetStatus($"Models folder not found: {modelsPath}", MessageType.Warning);
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { modelsPath });
            int issuesFound = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                
                if (importer == null) continue;
                
                // Check for non-optimal settings
                if (importer.importCameras || importer.importLights)
                {
                    issuesFound++;
                }
            }
            
            if (issuesFound > 0)
            {
                SetStatus($"Found {issuesFound} model(s) with non-optimal settings. Run 'Reimport All Models' to fix.", MessageType.Warning);
            }
            else
            {
                SetStatus("All models have optimal import settings!", MessageType.Info);
            }
        }
        
        private void RunBlenderPython(string pythonScript, string operationName)
        {
            string tempScriptPath = Path.Combine(Path.GetTempPath(), "veilbreakers_blender_script.py");
            File.WriteAllText(tempScriptPath, pythonScript);
            
            var processInfo = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = $"--background --python \"{tempScriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            try
            {
                using (var process = Process.Start(processInfo))
                {
                    // Read stderr async to avoid deadlock when both buffers fill
                    string error = null;
                    process.ErrorDataReceived += (s, e) => { if (e.Data != null) error += e.Data + "\n"; };
                    process.BeginErrorReadLine();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        SetStatus($"{operationName} completed successfully!", MessageType.Info);
                        UnityEngine.Debug.Log($"[DCC Bridge] {output}");
                    }
                    else
                    {
                        SetStatus($"{operationName} failed. Check Console for details.", MessageType.Error);
                        UnityEngine.Debug.LogError($"[DCC Bridge] Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to run Blender: {ex.Message}", MessageType.Error);
            }
            finally
            {
                try { File.Delete(tempScriptPath); } catch { }
            }
        }
        
        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            UnityEngine.Debug.Log($"[DCC Bridge] {message}");
        }
    }
}
