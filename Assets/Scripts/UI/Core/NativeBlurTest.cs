using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VeilBreakers.Core;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Disposable smoke test for FilterFunction / FilterFunctionType.Blur in UI Toolkit.
    /// Tests whether native blur renders correctly in the title screen context.
    ///
    /// API verified against Unity 6000.3 docs:
    ///   - FilterFunction is a struct, constructed via new FilterFunction(FilterFunctionType)
    ///   - FilterFunctionType.Blur is a built-in type expecting one float parameter (sigma)
    ///   - IStyle.filter is StyleList&lt;FilterFunction&gt; (not an array)
    ///
    /// Remove after Phase 5 blur verification is complete.
    /// </summary>
    [AddComponentMenu("VeilBreakers/Tests/NativeBlurTest")]
    public class NativeBlurTest : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        private const float kBlurSigma = 12f;
        private const float kTestWidth = 200f;
        private const float kTestHeight = 200f;
        private const float kTestLeft = 100f;
        private const float kTestTop = 100f;
        private const float kCornerRadius = 20f;

        private void Start()
        {
            if (_uiDocument == null)
                _uiDocument = FindFirstObjectByType<UIDocument>();

            if (_uiDocument == null)
            {
                ErrorLogger.Error("NativeBlurTest: No UIDocument found in scene");
                return;
            }

            var root = _uiDocument.rootVisualElement;
            if (root == null)
            {
                ErrorLogger.Error("NativeBlurTest: rootVisualElement is null");
                return;
            }

            // Create test element with semi-transparent orange background
            var testElement = new VisualElement();
            testElement.name = "blur-test-element";
            testElement.pickingMode = PickingMode.Ignore;
            testElement.style.position = Position.Absolute;
            testElement.style.width = kTestWidth;
            testElement.style.height = kTestHeight;
            testElement.style.left = kTestLeft;
            testElement.style.top = kTestTop;
            testElement.style.backgroundColor = new Color(1f, 0.4f, 0.12f, 0.4f);
            testElement.style.borderTopLeftRadius = kCornerRadius;
            testElement.style.borderTopRightRadius = kCornerRadius;
            testElement.style.borderBottomLeftRadius = kCornerRadius;
            testElement.style.borderBottomRightRadius = kCornerRadius;

            // Apply native blur filter using the correct Unity 6 API.
            // FilterFunction constructor takes a FilterFunctionType enum.
            // Blur expects a single float parameter (sigma).
            // IStyle.filter is StyleList<FilterFunction>, not a plain array.
            var blurFilter = new FilterFunction(FilterFunctionType.Blur);
            blurFilter.AddParameter(new FilterParameter(kBlurSigma));

            testElement.style.filter = new StyleList<FilterFunction>(
                new List<FilterFunction> { blurFilter }
            );

            root.Add(testElement);
            ErrorLogger.UI("NativeBlurTest: Blur applied successfully — "
                + $"check for blurred orange rectangle at ({kTestLeft},{kTestTop}) "
                + $"size {kTestWidth}x{kTestHeight} with sigma={kBlurSigma}");
        }
    }
}
