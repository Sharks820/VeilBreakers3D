using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VeilBreakers.Tests.EditMode
{
    public class SceneIntegrity_EditModeTests
    {
        [Test]
        [Category("Suite.Integrity")]
        [Category("Phase.PreProd")]
        public void Scenes_HaveNoMissingScripts()
        {
            // This is a high-signal regression catch: missing scripts often happen after merges/renames.
            string scenesRoot = Path.Combine(Application.dataPath, "Scenes");
            Assert.True(Directory.Exists(scenesRoot), $"Scenes folder not found: {scenesRoot}");

            string[] scenePaths = Directory.GetFiles(scenesRoot, "*.unity", SearchOption.AllDirectories);
            Assert.IsNotEmpty(scenePaths, "No scenes found under Assets/Scenes");

            foreach (string absPath in scenePaths)
            {
                string relPath = "Assets" + absPath.Replace(Application.dataPath, "").Replace('\\', '/');
                Scene s = EditorSceneManager.OpenScene(relPath, OpenSceneMode.Single);

                int missing = 0;
                var roots = s.GetRootGameObjects();
                foreach (var go in roots)
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                Assert.AreEqual(0, missing, $"Missing scripts in scene: {relPath}");
            }
        }

        [Test]
        [Category("Suite.Smoke")]
        [Category("Suite.Integrity")]
        [Category("Phase.PreProd")]
        public void MainMenuScene_LoadsInEditor()
        {
            // Simple canary test: if this fails, nothing else matters.
            Scene s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            Assert.IsTrue(s.IsValid(), "MainMenu scene did not load (invalid scene).");
            Assert.AreEqual("MainMenu", s.name, "Loaded scene name mismatch.");
        }
    }
}
