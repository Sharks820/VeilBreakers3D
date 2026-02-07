using System;
using System.Collections;

using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace VeilBreakers.Tests.PlayMode
{
    public class MainMenuOverlay_PlayModeTests
    {
        private const int kWarmupFrames = 180; // ~3 seconds at 60fps; allow shaders/UI to settle
        private const int kSampleFrames = 120;
        private const float kEditorBatchGcBudget = 12288f; // 12KB/frame budget for Unity 6000 Editor batchmode

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }

        private static Type FindType(string fullName)
        {
            // Avoid compile-time dependency on game code assemblies; these tests should still compile
            // even if assembly definitions change. We search loaded assemblies at runtime.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, throwOnError: false);
                if (t != null) return t;
            }
            return null;
        }

        [UnityTest]
        [Category("Suite.Smoke")]
        [Category("Phase.PreProd")]
        public IEnumerator MainMenu_OverlayVfxRendersAndDoesNotBlockInput()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var overlayType = FindType("VeilBreakers.UI.Effects.MainMenuVFXOverlayController");
            Assert.NotNull(overlayType, "Type not found: VeilBreakers.UI.Effects.MainMenuVFXOverlayController");

            MonoBehaviour overlay = null;
            var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in allBehaviours)
            {
                if (mb != null && mb.GetType() == overlayType)
                {
                    overlay = mb;
                    break;
                }
            }
            Assert.NotNull(overlay, "MainMenuVFXOverlayController component not found in MainMenu scene.");

            var canvas = overlay.GetComponentInChildren<Canvas>(true);
            Assert.NotNull(canvas, "Overlay Canvas not found under MainMenuVFXOverlayController.");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "Overlay Canvas must be ScreenSpaceOverlay.");
            Assert.GreaterOrEqual(canvas.sortingOrder, 1000, "Overlay sortingOrder is too low to guarantee it's above UI Toolkit.");

            // Must not intercept clicks intended for UI Toolkit.
            Assert.IsNull(canvas.GetComponent<GraphicRaycaster>(), "Overlay Canvas must not have GraphicRaycaster (it blocks UI Toolkit input).");

            // Validate all RawImages do not raycast.
            var images = overlay.GetComponentsInChildren<RawImage>(true);
            Assert.IsNotEmpty(images, "Expected at least one RawImage layer in overlay.");
            foreach (var img in images)
            {
                Assert.IsFalse(img.raycastTarget, $"RawImage '{img.name}' has raycastTarget=true (must be false).");
            }
        }

        [UnityTest]
        [Category("Suite.Perf")]
        [Category("Phase.VerticalSlice")]
        public IEnumerator MainMenu_IdleGcAllocIsLowAfterWarmup()
        {
            if (!Application.isBatchMode)
            {
                Assert.Ignore("GC allocation test only reliable in batchmode.");
            }

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

            // Warmup: allow shaders, UI Toolkit, etc to settle.
            for (int i = 0; i < kWarmupFrames; i++) yield return null;

            using var gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            Assert.IsTrue(gcAlloc.Valid, "ProfilerRecorder for 'GC Allocated In Frame' is not valid on this platform.");

            // Sample a short window.
            long sum = 0;
            for (int i = 0; i < kSampleFrames; i++)
            {
                yield return null;
                sum += gcAlloc.LastValue;
            }

            float avg = (float)sum / kSampleFrames;
            Assert.Less(avg, kEditorBatchGcBudget,
                $"GC allocations too high in MainMenu idle after warmup (avg {avg:0} bytes/frame).");
        }
    }
}
