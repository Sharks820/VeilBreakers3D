using UnityEngine;
using UnityEngine.UIElements;

namespace VeilBreakers.UI.Core
{
    /// <summary>
    /// Generates gradient Texture2D at runtime and applies them as background-image
    /// on VisualElements. This is the ONLY way to achieve gradient fills in UI Toolkit
    /// since USS does not support CSS linear-gradient().
    /// </summary>
    public static class UIGradientHelper
    {
        private const int kDefaultWidth = 4; // Narrow — stretches via background-size
        private const int kDefaultHeight = 64; // Enough vertical resolution for smooth gradient

        /// <summary>
        /// Creates a vertical gradient texture (top color to bottom color).
        /// </summary>
        public static Texture2D CreateVerticalGradient(Color topColor, Color bottomColor, int width = kDefaultWidth, int height = kDefaultHeight)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1); // 0 = bottom, 1 = top
                Color c = Color.Lerp(bottomColor, topColor, t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false); // Keep readable for potential reuse
            return tex;
        }

        /// <summary>
        /// Creates a 3-stop vertical gradient (top → mid → bottom).
        /// </summary>
        public static Texture2D CreateVerticalGradient3(Color topColor, Color midColor, Color bottomColor, int width = kDefaultWidth, int height = kDefaultHeight)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[width * height];
            int midPoint = height / 2;

            for (int y = 0; y < height; y++)
            {
                Color c;
                if (y <= midPoint)
                {
                    float t = (float)y / Mathf.Max(1, midPoint);
                    c = Color.Lerp(bottomColor, midColor, t);
                }
                else
                {
                    float t = (float)(y - midPoint) / Mathf.Max(1, height - midPoint - 1);
                    c = Color.Lerp(midColor, topColor, t);
                }

                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>
        /// Creates a radial gradient texture (center color fading to edge color).
        /// Useful for glow effects and spotlight backing.
        /// </summary>
        public static Texture2D CreateRadialGradient(Color centerColor, Color edgeColor, int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];
            float halfSize = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - halfSize) / halfSize;
                    float dy = (y - halfSize) / halfSize;
                    float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = Color.Lerp(centerColor, edgeColor, dist);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>
        /// Applies a gradient texture as background-image on a VisualElement.
        /// </summary>
        public static void ApplyGradient(VisualElement element, Texture2D gradient)
        {
            if (element == null || gradient == null) return;
            element.style.backgroundImage = new StyleBackground(gradient);
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        }

        /// <summary>
        /// Creates and applies a vertical gradient to a VisualElement in one call.
        /// Returns the texture for cleanup.
        /// </summary>
        public static Texture2D ApplyVerticalGradient(VisualElement element, Color topColor, Color bottomColor)
        {
            var tex = CreateVerticalGradient(topColor, bottomColor);
            ApplyGradient(element, tex);
            return tex;
        }

        /// <summary>
        /// Creates a glow overlay VisualElement and adds it behind the target.
        /// Returns the overlay for animation/cleanup.
        /// </summary>
        public static VisualElement CreateGlowOverlay(VisualElement parent, Color glowColor, float spread = 8f, float opacity = 0.3f)
        {
            if (parent == null) return null;

            var glow = new VisualElement();
            glow.name = "glow-overlay";
            glow.pickingMode = PickingMode.Ignore;
            glow.style.position = Position.Absolute;
            glow.style.left = -spread;
            glow.style.top = -spread;
            glow.style.right = -spread;
            glow.style.bottom = -spread;
            glow.style.borderTopLeftRadius = parent.resolvedStyle.borderTopLeftRadius + spread;
            glow.style.borderTopRightRadius = parent.resolvedStyle.borderTopRightRadius + spread;
            glow.style.borderBottomLeftRadius = parent.resolvedStyle.borderBottomLeftRadius + spread;
            glow.style.borderBottomRightRadius = parent.resolvedStyle.borderBottomRightRadius + spread;

            // Radial gradient texture for the glow
            var glowTex = CreateRadialGradient(
                new Color(glowColor.r, glowColor.g, glowColor.b, opacity),
                new Color(glowColor.r, glowColor.g, glowColor.b, 0f),
                64
            );
            glow.style.backgroundImage = new StyleBackground(glowTex);
            glow.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;

            // Insert at position 0 so it's behind other children
            if (parent.childCount > 0)
                parent.Insert(0, glow);
            else
                parent.Add(glow);

            return glow;
        }

        /// <summary>
        /// Creates a horizontal highlight line at the top of an element.
        /// Simulates the "inner light" effect seen in AAA panel designs.
        /// </summary>
        public static VisualElement CreateTopHighlight(VisualElement parent, Color highlightColor, float heightPx = 1f)
        {
            if (parent == null) return null;

            var highlight = new VisualElement();
            highlight.name = "top-highlight";
            highlight.pickingMode = PickingMode.Ignore;
            highlight.style.position = Position.Absolute;
            highlight.style.top = 0;
            highlight.style.left = 0;
            highlight.style.right = 0;
            highlight.style.height = heightPx;

            // Gradient: transparent → color → transparent (center-bright)
            var tex = CreateVerticalGradient(highlightColor, highlightColor, 128, 1);
            // Actually we want a horizontal gradient for the highlight line
            var hTex = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            hTex.wrapMode = TextureWrapMode.Clamp;
            hTex.filterMode = FilterMode.Bilinear;
            var hPixels = new Color[128];
            for (int x = 0; x < 128; x++)
            {
                float t = (float)x / 127f;
                // Fade from edges to center
                float fade = 1f - Mathf.Abs(t * 2f - 1f);
                fade = fade * fade; // Quadratic falloff
                hPixels[x] = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a * fade);
            }
            hTex.SetPixels(hPixels);
            hTex.Apply(false, false);

            // Clean up the unused vertical texture
            Object.Destroy(tex);

            highlight.style.backgroundImage = new StyleBackground(hTex);
            highlight.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;

            parent.Add(highlight);
            return highlight;
        }
    }
}
