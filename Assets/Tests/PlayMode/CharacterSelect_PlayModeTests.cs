using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace VeilBreakers.Tests.PlayMode
{
    public class CharacterSelect_PlayModeTests
    {
        private const int kMaxPollFrames = 600; // ~10 seconds at 60fps

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Each test loads a scene via LoadSceneMode.Single which auto-unloads previous
            // This ensures clean state even if a test fails midway
            yield return null;
        }

        private static bool TryGetLabelText(VisualElement root, string elementName, out string value)
        {
            value = string.Empty;
            if (root == null)
            {
                return false;
            }

            var label = root.Q<Label>(elementName);
            if (label == null)
            {
                return false;
            }

            value = label.text ?? string.Empty;
            return true;
        }

        [UnityTest]
        [Category("Suite.Smoke")]
        [Category("Phase.PreProd")]
        public IEnumerator CharacterSelect_DisplaysHeroIdentityAndStats()
        {
            yield return SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);

            UIDocument doc = null;
            for (int i = 0; i < kMaxPollFrames; i++)
            {
                doc = Object.FindFirstObjectByType<UIDocument>();
                if (doc != null && doc.rootVisualElement != null)
                {
                    break;
                }
                yield return null;
            }

            Assert.NotNull(doc, "UIDocument not found in CharacterSelect.");
            var root = doc.rootVisualElement;
            Assert.NotNull(root, "Root visual element is null in CharacterSelect.");

            string heroName = string.Empty;
            string heroTitle = string.Empty;
            string strValue = string.Empty;
            string dexValue = string.Empty;
            string indexLabel = string.Empty;

            bool hasPopulatedStats = false;
            for (int i = 0; i < kMaxPollFrames; i++)
            {
                TryGetLabelText(root, "hero-name", out heroName);
                TryGetLabelText(root, "hero-title", out heroTitle);
                TryGetLabelText(root, "stat-val-str", out strValue);
                TryGetLabelText(root, "stat-val-dex", out dexValue);
                TryGetLabelText(root, "hero-index-indicator", out indexLabel);

                bool strOk = int.TryParse(strValue, out var strParsedLoop) && strParsedLoop > 0;
                bool dexOk = int.TryParse(dexValue, out var dexParsedLoop) && dexParsedLoop > 0;
                if (!string.IsNullOrWhiteSpace(heroName) &&
                    !string.IsNullOrWhiteSpace(heroTitle) &&
                    strOk &&
                    dexOk)
                {
                    hasPopulatedStats = true;
                    break;
                }

                yield return null;
            }

            Assert.IsFalse(string.IsNullOrWhiteSpace(heroName), "hero-name is blank.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(heroTitle), "hero-title is blank.");
            Assert.IsTrue(hasPopulatedStats, $"Stats did not populate to > 0 values in time. STR='{strValue}', DEX='{dexValue}'");
            Assert.IsFalse(string.IsNullOrWhiteSpace(indexLabel), "hero-index-indicator is blank.");
        }

        [UnityTest]
        [Category("Suite.Smoke")]
        [Category("Phase.PreProd")]
        public IEnumerator CharacterSelect_CreatesHeroStageModel()
        {
            yield return SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);

            GameObject stageRoot = null;
            for (int i = 0; i < kMaxPollFrames; i++)
            {
                stageRoot = GameObject.Find("CharacterStage");
                if (stageRoot != null)
                {
                    break;
                }
                yield return null;
            }

            Assert.NotNull(stageRoot, "Hero stage root was not created.");

            Transform heroModel = null;
            for (int i = 0; i < kMaxPollFrames; i++)
            {
                for (int childIndex = 0; childIndex < stageRoot.transform.childCount; childIndex++)
                {
                    var child = stageRoot.transform.GetChild(childIndex);
                    if (child == null)
                    {
                        continue;
                    }

                    if (child.name.StartsWith("Hero_", System.StringComparison.OrdinalIgnoreCase))
                    {
                        heroModel = child;
                        break;
                    }
                }

                if (heroModel != null)
                {
                    break;
                }

                yield return null;
            }

            Assert.NotNull(heroModel, "No hero model instance found under CharacterStage.");
            Assert.AreNotEqual("HeroPlaceholder", heroModel.name, "Imported hero model failed to load; placeholder is still being used.");
        }
    }
}
