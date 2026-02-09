using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace VeilBreakers.Editor
{
    /// <summary>
    /// Unity Editor integration for Tripo3D Blender Bridge.
    /// Manages the WebSocket connection between Tripo Studio and Blender.
    /// </summary>
    public class Tripo3DBridgeEditor : EditorWindow
    {
        private const string kBlenderDefaultPath = @"C:\Program Files\Blender Foundation\Blender 4.4\blender.exe";
        private const string kTripoAddonPath = @"Tools\DCC_Bridge\Tripo3d_Blender_Bridge";
        private const int kTripoPort = 60600;
        
        [SerializeField] private string blenderPath = kBlenderDefaultPath;
        [SerializeField] private bool autoImportToUnity = true;
        [SerializeField] private string unityImportPath = "Assets/Resources/Art/3D_Models/Tripo/";
        
        private bool isBlenderInstalled = false;
        private bool isTripoAddonInstalled = false;
        private bool isPortInUse = false;
        private string statusMessage = "";
        private MessageType statusType = MessageType.Info;
        private Vector2 scrollPosition;
        
        [MenuItem("VeilBreakers/Tripo3D Bridge", priority = 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<Tripo3DBridgeEditor>("Tripo3D Bridge");
            window.minSize = new Vector2(400, 350);
            window.Show();
        }
        
        private void OnEnable()
        {
            CheckPrerequisites();
        }
        
        private void CheckPrerequisites()
        {
            // Check Blender
            string[] possiblePaths = new[]
            {
                @"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.4\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.2\blender.exe",
                @"C:\Program Files\Blender Foundation\Blender 4.1\blender.exe",
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
            
            string savedPath = EditorPrefs.GetString("VeilBreakers.BlenderPath", "");
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                blenderPath = savedPath;
                isBlenderInstalled = true;
            }
            
            // Check if Tripo addon exists
            string addonPath = Path.Combine(Application.dataPath, "..", kTripoAddonPath);
            isTripoAddonInstalled = Directory.Exists(addonPath) && 
                                    File.Exists(Path.Combine(addonPath, "__init__.py"));
            
            // Check port
            CheckPortStatus();
        }
        
        private void CheckPortStatus()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("127.0.0.1", kTripoPort);
                    isPortInUse = client.Connected;
                }
            }
            catch
            {
                isPortInUse = false;
            }
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawStatus();
            DrawPrerequisites();
            DrawActions();
            DrawSettings();
            DrawInstructions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Tripo3D Bridge", titleStyle);
            
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("AI 3D Model Generation from Tripo Studio", subtitleStyle);
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
                EditorGUILayout.Space(5);
            }
        }
        
        private void DrawPrerequisites()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Prerequisites Check", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Blender 4.1+:", GUILayout.Width(100));
            DrawStatusIndicator(isBlenderInstalled, isBlenderInstalled ? "Found" : "Not Found");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tripo Addon:", GUILayout.Width(100));
            DrawStatusIndicator(isTripoAddonInstalled, isTripoAddonInstalled ? "Found" : "Not Found");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Port 60600:", GUILayout.Width(100));
            DrawStatusIndicator(isPortInUse, isPortInUse ? "Active (Blender running)" : "Available");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawStatusIndicator(bool isGood, string message)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = isGood ? Color.green : Color.red;
            EditorGUILayout.LabelField(isGood ? "✓ " : "✗ " + message, style);
        }
        
        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Blender Path
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            blenderPath = EditorGUILayout.TextField("Blender:", blenderPath);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("VeilBreakers.BlenderPath", blenderPath);
                isBlenderInstalled = File.Exists(blenderPath);
            }
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string selected = EditorUtility.OpenFilePanel("Select Blender", 
                    Path.GetDirectoryName(blenderPath) ?? "", "exe");
                if (!string.IsNullOrEmpty(selected))
                {
                    blenderPath = selected;
                    EditorPrefs.SetString("VeilBreakers.BlenderPath", blenderPath);
                    isBlenderInstalled = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Install buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Install Tripo Addon"))
            {
                InstallTripoAddon();
            }
            if (GUILayout.Button("Open Blender"))
            {
                OpenBlender();
            }
            EditorGUILayout.EndHorizontal();
            
            // Connection check
            if (GUILayout.Button("Check Connection Status"))
            {
                CheckPortStatus();
                if (isPortInUse)
                {
                    SetStatus("Tripo Bridge is active! Blender is ready to receive models.", MessageType.Info);
                }
                else
                {
                    SetStatus("Port 60600 is not in use. Open Blender to start the bridge.", MessageType.Warning);
                }
            }
            
            // Diagnostics
            if (GUILayout.Button("Run Diagnostics"))
            {
                RunDiagnostics();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Unity Integration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            autoImportToUnity = EditorGUILayout.ToggleLeft(
                "Auto-import models to Unity",
                autoImportToUnity);
            
            if (autoImportToUnity)
            {
                EditorGUILayout.BeginHorizontal();
                unityImportPath = EditorGUILayout.TextField("Import Path:", unityImportPath);
                if (GUILayout.Button("Browse...", GUILayout.Width(70)))
                {
                    string selected = EditorUtility.SaveFolderPanel("Select Import Folder",
                        Application.dataPath, "Tripo");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        unityImportPath = "Assets" + selected.Replace(Application.dataPath, "").Replace('\\', '/');
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Create Import Folder"))
                {
                    CreateImportFolder();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("How to Use", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("1. Install the Tripo addon (click above)", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. Open Blender", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. Look for the 'Tripo' panel in the sidebar (press N)", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("4. Open Tripo Studio: https://studio.tripo3d.ai/", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("5. Generate a 3D model in Tripo Studio", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("6. Click 'Send to Blender' in Tripo Studio", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("7. The model appears in Blender automatically!", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Need an account?", EditorStyles.boldLabel);
            if (GUILayout.Button("Get Tripo3D Account"))
            {
                Application.OpenURL("https://www.tripo3d.ai/");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void InstallTripoAddon()
        {
            if (!isBlenderInstalled)
            {
                SetStatus("Blender not found. Please set the correct path.", MessageType.Error);
                return;
            }
            
            string addonPath = Path.Combine(Application.dataPath, "..", kTripoAddonPath);
            addonPath = Path.GetFullPath(addonPath);
            
            if (!Directory.Exists(addonPath))
            {
                SetStatus($"Tripo addon not found at: {addonPath}", MessageType.Error);
                return;
            }
            
            // Check for required Python version (Blender 4.1+ uses Python 3.11)
            string script = $@"
import bpy
import addon_utils
import sys

print('='*60)
print('Installing Tripo3D Blender Bridge')
print('='*60)

addon_path = r""{addonPath}""

try:
    # Install addon
    print(f'Installing from: {{addon_path}}')
    bpy.ops.preferences.addon_install(filepath=addon_path, overwrite=True)
    print('✓ Addon installed')
    
    # Enable addon
    addon_utils.enable('Tripo3d_Blender_Bridge', default_set=True, persistent=True)
    print('✓ Addon enabled')
    
    # Save preferences
    bpy.ops.wm.save_userpref()
    print('✓ Preferences saved')
    
    print('='*60)
    print('Installation successful!')
    print('='*60)
    
except Exception as e:
    print(f'Error: {{e}}')
    import traceback
    traceback.print_exc()
    sys.exit(1)
";
            
            RunBlenderCommand(script, "Installing Tripo3D addon...");
        }
        
        private void OpenBlender()
        {
            if (!isBlenderInstalled)
            {
                SetStatus("Blender not found.", MessageType.Error);
                return;
            }
            
            try
            {
                Process.Start(blenderPath);
                SetStatus("Blender launched. The Tripo Bridge will start automatically.", MessageType.Info);
            }
            catch (System.Exception ex)
            {
                SetStatus($"Failed to open Blender: {ex.Message}", MessageType.Error);
            }
        }
        
        private void CreateImportFolder()
        {
            string path = unityImportPath.TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder("Assets/Resources/Art/3D_Models", "Tripo");
                }
                else
                {
                    AssetDatabase.CreateFolder(parent, folderName);
                }
                
                AssetDatabase.Refresh();
                SetStatus($"Created folder: {path}", MessageType.Info);
            }
            else
            {
                SetStatus("Folder already exists.", MessageType.Warning);
            }
        }
        
        private void RunBlenderCommand(string pythonScript, string operationName)
        {
            string tempScriptPath = Path.Combine(Path.GetTempPath(), "tripo3d_install_script.py");
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
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    UnityEngine.Debug.Log($"[Tripo3D Bridge] {output}");
                    
                    if (process.ExitCode == 0)
                    {
                        SetStatus($"{operationName} completed successfully!", MessageType.Info);
                        isTripoAddonInstalled = true;
                    }
                    else
                    {
                        SetStatus($"{operationName} failed. Check Console for details.", MessageType.Error);
                        UnityEngine.Debug.LogError($"[Tripo3D Bridge] Error: {error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                SetStatus($"Failed: {ex.Message}", MessageType.Error);
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
            UnityEngine.Debug.Log($"[Tripo3D Bridge] {message}");
        }
        
        private void RunDiagnostics()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== Tripo3D Bridge Diagnostics ===\n");
            
            // 1. Check Blender path
            report.AppendLine("1. Blender Installation:");
            report.AppendLine($"   Path: {blenderPath}");
            report.AppendLine($"   Exists: {File.Exists(blenderPath)}");
            
            // 2. Check addon files
            string addonPath = Path.Combine(Application.dataPath, "..", kTripoAddonPath);
            addonPath = Path.GetFullPath(addonPath);
            report.AppendLine("\n2. Tripo Addon Files:");
            report.AppendLine($"   Path: {addonPath}");
            report.AppendLine($"   Exists: {Directory.Exists(addonPath)}");
            report.AppendLine($"   __init__.py: {File.Exists(Path.Combine(addonPath, "__init__.py"))}");
            
            // 3. Check port
            CheckPortStatus();
            report.AppendLine("\n3. WebSocket Server (Port 60600):");
            report.AppendLine($"   Port in use: {isPortInUse}");
            
            // 4. Check Unity import path
            report.AppendLine("\n4. Unity Import Path:");
            report.AppendLine($"   Path: {unityImportPath}");
            report.AppendLine($"   Exists: {AssetDatabase.IsValidFolder(unityImportPath)}");
            
            // Show report
            UnityEngine.Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Tripo3D Diagnostics", report.ToString(), "OK");
            
            SetStatus("Diagnostics complete. Check Console for details.", MessageType.Info);
        }
    }
}
