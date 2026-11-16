// ============================================================
// ArrowGradient.cs  –  Arrow Slide Puzzle
// Resolves arrow colours from the reference-image two-tone palette:
//   warm golden-yellow (#FFB52E) ↔ hot pink / magenta (#FF4FD8)
// The gradient alternates between the two reference colours so
// neighbouring arrows look like the reference image.
// ============================================================

using UnityEngine;

namespace ArrowPuzzle.Core
{
    public static class ArrowGradient
    {
        // Reference palette: golden-yellow at t=0, hot-pink at t=1.
        // Gradient arrows oscillate between these two rather than adding a
        // third purple to stay true to the reference image.
        private static readonly Color _gradA = ArrowColors.Orange;   // t=0  (#FFB52E)
        private static readonly Color _gradB = ArrowColors.Pink;     // t=1  (#FF4FD8)

        /// <summary>
        /// Sample the level gradient at t∈[0,1].
        /// Per user requirement: strictly two flat colors, NO in-between or muddy shades.
        /// </summary>
        public static Color Sample(float t)
        {
            return t < 0.5f ? _gradA : _gradB;
        }

        /// <summary>
        /// Resolve the base colour for an ArrowData.
        /// Odd colorIndex → hot pink, even → golden-yellow (matches reference two-tone look).
        /// </summary>
        public static Color Resolve(ArrowData data)
        {
            if (data == null) return ArrowColors.Orange;

            if (data.useGradient)
                return Sample(data.gradientT);

            int idx = Mathf.Clamp(data.colorIndex, 0, ArrowColors.All.Length - 1);
            return ArrowColors.All[idx];
        }

        /// <summary>
        /// Returns a slightly lighter colour (kept for API compatibility).
        /// </summary>
        public static Color Highlight(Color base_, float strength = 0.55f)
        {
            return Color.Lerp(base_, Color.white, strength);
        }
    }
}

