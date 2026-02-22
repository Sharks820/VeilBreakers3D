using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public class SceneAuditor
{
    [MenuItem("Tools/Audit Scene")]
    public static void Audit()
    {
        Scene scene = SceneManager.GetActiveScene();
        Debug.Log("--- Scene Audit: " + scene.name + " ---");
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            Debug.Log("Root: " + go.name + " (Active: " + go.activeSelf + ")");
            var uiDoc = go.GetComponentInChildren<UnityEngine.UIElements.UIDocument>();
            if (uiDoc != null)
            {
                Debug.Log("  - Found UIDocument on " + uiDoc.gameObject.name);
                Debug.Log("  - Source Asset: " + (uiDoc.visualTreeAsset != null ? uiDoc.visualTreeAsset.name : "NULL"));
                Debug.Log("  - Panel Settings: " + (uiDoc.panelSettings != null ? uiDoc.panelSettings.name : "NULL"));
                Debug.Log("  - Enabled: " + uiDoc.enabled);
            }
        }
    }
}
