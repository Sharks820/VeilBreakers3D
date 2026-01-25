using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace VeilBreakers.Editor
{
    public static class UITextSettingsSetup
    {
        [MenuItem("VeilBreakers/Setup/Fix UI Text Settings")]
        public static void FixUITextSettings()
        {
            // Find the PanelSettings asset
            string[] panelSettingsGuids = AssetDatabase.FindAssets("VeilBreakersPanelSettings t:PanelSettings");

            if (panelSettingsGuids.Length == 0)
            {
                Debug.LogError("[VB] VeilBreakersPanelSettings not found!");
                return;
            }

            string panelSettingsPath = AssetDatabase.GUIDToAssetPath(panelSettingsGuids[0]);
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath);

            if (panelSettings == null)
            {
                Debug.LogError("[VB] Failed to load PanelSettings!");
                return;
            }

            // Create PanelTextSettings if it doesn't exist
            string textSettingsPath = "Assets/UI/VeilBreakersTextSettings.asset";
            PanelTextSettings textSettings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(textSettingsPath);

            if (textSettings == null)
            {
                textSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(textSettings, textSettingsPath);
                Debug.Log("[VB] Created PanelTextSettings at " + textSettingsPath);
            }

            // Assign text settings to panel settings using SerializedObject
            SerializedObject serializedPanel = new SerializedObject(panelSettings);
            SerializedProperty textSettingsProp = serializedPanel.FindProperty("textSettings");

            if (textSettingsProp != null)
            {
                textSettingsProp.objectReferenceValue = textSettings;
                serializedPanel.ApplyModifiedProperties();
                Debug.Log("[VB] Assigned PanelTextSettings to VeilBreakersPanelSettings");
            }
            else
            {
                Debug.LogError("[VB] Could not find textSettings property!");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[VB] UI Text Settings setup complete! Re-enter Play mode to see changes.");
        }
    }
}
