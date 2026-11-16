// ============================================================
// ArrowTestScene.cs  –  Arrow Slide Puzzle
// Test controller: spawns 10 hard-coded ArrowData
// (straight, L-shaped, S-shaped, snake-like) with all four
// head directions, using the 5 palette colours + gradient mode.
//
// Attach to any GameObject in the test scene alongside a
// GridManager (and optionally DotGrid + BackgroundRenderer).
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Gameplay
{
    public class ArrowTestScene : MonoBehaviour
    {
        public enum TestLayout
        {
            /// <summary>8 arrows with interlocking dependencies, fully solvable to Win, and blocked taps to test Lose.</summary>
            SolvablePuzzle,
            /// <summary>Step 3 showcase with 10 arrows (various bends and directions).</summary>
            Step3VisualDemo
        }

        [Header("Test layout")]
        [Tooltip("When true, runs legacy Step 3/4 hardcoded arrow test. Set false when using LevelLoader.")]
        public bool runLegacyTestScene = false;

        [Tooltip("Select whether to spawn a fully solvable puzzle or the Step 3 visual showcase.")]
        public TestLayout testLayout = TestLayout.SolvablePuzzle;

        [Header("Spawn stagger")]
        [Tooltip("Delay between each arrow's spawn animation (seconds).")]
        public float staggerDelay = 0.12f;

        [Header("Arrow view prefab (optional – leave null to create at runtime)")]
        public ArrowView arrowViewPrefab;

        // ── Unity lifecycle ──────────────────────────────────
        private void Start()
        {
            if (!runLegacyTestScene) return;

            var grid = GridManager.Instance;
            if (grid == null)
            {
                Debug.LogError("[ArrowTestScene] No GridManager found in scene.");
                return;
            }

            // Ensure GameplayController has clean state
            if (GameplayController.Instance != null)
            {
                GameplayController.Instance.ClearRegisteredArrows();
                GameplayController.Instance.InitHearts();
            }

            var arrows = testLayout == TestLayout.SolvablePuzzle
                ? BuildSolvableArrows()
                : BuildStep3Arrows();

            // Assign gradientT to gradient arrows
            int gradCount = 0;
            int totalGrad = 0;
            foreach (var a in arrows) if (a.useGradient) totalGrad++;
            foreach (var a in arrows)
            {
                if (!a.useGradient) continue;
                a.gradientT = totalGrad > 1
                    ? (float)gradCount / (totalGrad - 1)
                    : 0f;
                gradCount++;
            }

            // Spawn
            for (int i = 0; i < arrows.Count; i++)
            {
                var view = SpawnView(arrows[i]);
                view.Init(arrows[i], grid);
                view.PlaySpawnAnim(i * staggerDelay);

                // Register with gameplay controller
                GameplayController.Instance?.RegisterArrow(view);
            }
        }

        // ── Arrow definitions ────────────────────────────────

        /// <summary>
        /// 8 arrows on a 7×9 grid with a clean solve order:
        /// 1 & 5 start Free; unblocks 2, 3, 4, 6, 7, 8 in sequence!
        /// </summary>
        private static List<ArrowData> BuildSolvableArrows()
        {
            return new List<ArrowData>
            {
                // 1. Straight → Right (Orange, palette 0) [FREE: starts unblocked]
                MakePalette(
                    new[]{ V(0,8), V(1,8), V(2,8) },
                    Direction.Right, colorIdx: 0),

                // 2. Straight ↑ Up (Pink, palette 1) [BLOCKED by Arrow 1 until cleared]
                MakePalette(
                    new[]{ V(0,5), V(0,6), V(0,7) },
                    Direction.Up, colorIdx: 1),

                // 3. Snake ↓ Down (Blue, palette 3) [FREE: starts unblocked]
                MakePalette(
                    new[]{ V(6,5), V(5,5), V(5,4), V(5,3) },
                    Direction.Down, colorIdx: 3),

                // 4. L-Shape → Right (Purple, palette 2) [BLOCKED by Arrow 3]
                MakePalette(
                    new[]{ V(3,3), V(4,3) },
                    Direction.Right, colorIdx: 2),

                // 5. Snake ← Left (Cyan, palette 4) [FREE: starts unblocked]
                MakePalette(
                    new[]{ V(6,1), V(5,1), V(4,1), V(3,1), V(2,1) },
                    Direction.Left, colorIdx: 4),

                // 6. Snake ↑ Up (Gradient mode: Orange→Magenta) [BLOCKED by Arrow 5]
                MakeGradient(
                    new[]{ V(2,0), V(3,0), V(4,0) },
                    Direction.Up),

                // 7. Snake ← Left (Gradient mode: Magenta→Purple) [BLOCKED by Arrow 2]
                MakeGradient(
                    new[]{ V(6,7), V(5,7), V(4,7), V(3,7) },
                    Direction.Left),

                // 8. Straight ↓ Down (Orange, palette 0) [BLOCKED by Arrows 4 and 5]
                MakePalette(
                    new[]{ V(3,5), V(3,4) },
                    Direction.Down, colorIdx: 0),
            };
        }

        /// <summary>
        /// Step 3 visual showcase with 10 arrows (straight, L-shapes, S-shapes, snake).
        /// </summary>
        private static List<ArrowData> BuildStep3Arrows()
        {
            return new List<ArrowData>
            {
                // 1. Short straight → Right (Orange, palette 0)
                MakePalette(
                    new[]{ V(0,8), V(1,8), V(2,8) },
                    Direction.Right, colorIdx: 0),

                // 2. Short straight ↑ Up (Pink, palette 1)
                MakePalette(
                    new[]{ V(0,4), V(0,5), V(0,6) },
                    Direction.Up, colorIdx: 1),

                // 3. Short straight ↓ Down (Blue, palette 3)
                MakePalette(
                    new[]{ V(6,8), V(6,7), V(6,6) },
                    Direction.Down, colorIdx: 3),

                // 4. Short straight ← Left (Cyan, palette 4)
                MakePalette(
                    new[]{ V(6,4), V(5,4), V(4,4) },
                    Direction.Left, colorIdx: 4),

                // 5. L-shape: goes right then up → head Up (Purple, palette 2)
                MakePalette(
                    new[]{ V(1,0), V(2,0), V(3,0), V(3,1), V(3,2) },
                    Direction.Up, colorIdx: 2),

                // 6. L-shape: goes up then right → head Right (Orange, palette 0)
                MakePalette(
                    new[]{ V(5,0), V(5,1), V(5,2), V(5,3), V(6,3) },
                    Direction.Right, colorIdx: 0),

                // 7. S-shape / zigzag snake (gradient mode: orange→magenta→purple)
                MakeGradient(
                    new[]{ V(1,1), V(2,1), V(3,1), V(3,2), V(3,3),
                           V(2,3), V(1,3), V(1,4), V(1,5) },
                    Direction.Up),

                // 8. Long snake winding right then down (gradient mode)
                MakeGradient(
                    new[]{ V(2,6), V(3,6), V(4,6), V(5,6), V(5,5),
                           V(4,5), V(3,5), V(2,5), V(2,4), V(3,4) },
                    Direction.Right),

                // 9. Coiled snake, head pointing Left (gradient mode)
                MakeGradient(
                    new[]{ V(4,1), V(4,2), V(4,3), V(5,3), V(6,3),
                           V(6,2), V(6,1), V(5,1) },
                    Direction.Left),

                // 10. Diagonal-step snake, head Down (Pink, palette 1)
                MakePalette(
                    new[]{ V(0,0), V(0,1), V(1,1), V(1,2), V(2,2),
                           V(2,3), V(3,3), V(3,2), V(3,1) },
                    Direction.Down, colorIdx: 1),
            };
        }

        // ── Helpers ──────────────────────────────────────────

        private static Vector2Int V(int c, int r) => new Vector2Int(c, r);

        private static ArrowData MakePalette(
            Vector2Int[] cells, Direction dir, int colorIdx)
        {
            return new ArrowData(new List<Vector2Int>(cells), dir, colorIdx);
        }

        private static ArrowData MakeGradient(
            Vector2Int[] cells, Direction dir)
        {
            return new ArrowData(new List<Vector2Int>(cells), dir, gradient: true);
        }

        private ArrowView SpawnView(ArrowData data)
        {
            if (arrowViewPrefab != null)
                return Instantiate(arrowViewPrefab, Vector3.zero, Quaternion.identity);

            // Create at runtime
            var go   = new GameObject($"Arrow_{data.Head.x}_{data.Head.y}");
            var view = go.AddComponent<ArrowView>();
            return view;
        }
    }
}

