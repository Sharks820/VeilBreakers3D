using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

namespace VeilBreakers.UI.CharacterSelect
{
    /// <summary>
    /// Builds PrimeTween sequences that scramble text through glitch characters then
    /// resolve character-by-character. Creates the "veil corruption" text reveal effect
    /// used for hero name/title on hero switch and embark cinematic name flash.
    /// Pure C# class (not MonoBehaviour) -- produces composable Sequences.
    /// </summary>
    public class GlitchTextEffect
    {
        // =============================================================================
        // GLITCH CHARACTER SET
        // =============================================================================

        /// <summary>
        /// Unicode block drawing characters for veil-corruption themed text scramble.
        /// Full block, light/medium/dark shade, upper/lower half, left/right half,
        /// black square, triangle, diamond, diaeresis.
        /// </summary>
        private static readonly char[] kGlitchChars = new char[]
        {
            '\u2588', // Full block
            '\u2591', // Light shade
            '\u2592', // Medium shade
            '\u2593', // Dark shade
            '\u2580', // Upper half block
            '\u2584', // Lower half block
            '\u258C', // Left half block
            '\u2590', // Right half block
            '\u25A0', // Black square
            '\u25B2', // Black up-pointing triangle
            '\u2666', // Black diamond suit
            '\u00A8'  // Diaeresis
        };

        // =============================================================================
        // PUBLIC API
        // =============================================================================

        /// <summary>
        /// Builds a PrimeTween Sequence that glitch-reveals text on a Label.
        /// Characters scramble through block drawing characters then resolve left-to-right.
        /// </summary>
        /// <param name="label">The UI Toolkit Label to animate</param>
        /// <param name="targetText">The final resolved text</param>
        /// <param name="resolveSpeed">Characters per second (Vex=60, Nyx=35)</param>
        /// <returns>PrimeTween Sequence for composition into larger timelines</returns>
        public static Sequence BuildGlitchReveal(Label label, string targetText, float resolveSpeed = 50f)
        {
            if (label == null || string.IsNullOrEmpty(targetText))
            {
                return Sequence.Create();
            }

            int length = targetText.Length;

            // Pre-allocate char buffer to avoid per-frame string.ToCharArray() allocation.
            // This buffer is captured by reference in the tween closures and reused across
            // all character animations within this sequence.
            char[] buffer = new char[length];

            // Initialize label text to all glitch characters
            for (int i = 0; i < length; i++)
            {
                buffer[i] = kGlitchChars[0];
            }
            label.text = new string(buffer);

            var seq = Sequence.Create();

            float charDelay = 1f / resolveSpeed;
            float perCharDuration = 3f / resolveSpeed; // Glitch frames before resolve

            for (int i = 0; i < length; i++)
            {
                int charIndex = i;
                float startTime = charIndex * charDelay;

                // Insert a tween for each character position.
                // During t < 0.7: cycles through glitch chars (deterministic cycling, no Random allocation).
                // At t >= 0.7: resolves to the target character.
                seq.Insert(startTime,
                    Tween.Custom(label, 0f, 1f, perCharDuration,
                        onValueChange: (lbl, t) =>
                        {
                            if (t < 0.7f)
                            {
                                // Deterministic cycling through glitch charset based on t value.
                                // Avoids UnityEngine.Random allocation in hot path.
                                int glyphIndex = Mathf.FloorToInt(t * kGlitchChars.Length * 2f) % kGlitchChars.Length;
                                buffer[charIndex] = kGlitchChars[glyphIndex];
                            }
                            else
                            {
                                buffer[charIndex] = targetText[charIndex];
                            }

                            // Create string from buffer and assign to label.
                            // One allocation per frame update -- acceptable since glitch text
                            // runs for ~250ms total and is not a hot path.
                            lbl.text = new string(buffer);
                        }));
            }

            return seq;
        }
    }
}
