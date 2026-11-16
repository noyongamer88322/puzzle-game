// ============================================================
// PuzzleSolver.cs
// Pure-logic module.  Given a list of ArrowData on a grid,
// computes which arrows are "free" (can slide out in one move).
//
// RULES:
//   An arrow is FREE if, starting from its cell and moving one
//   step in its Direction, every cell along that ray until the
//   grid edge (or board boundary) is EMPTY (no other arrow).
//   The arrow itself exits the board when it slides off the edge.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Systems
{
    public static class PuzzleSolver
    {
        /// <summary>
        /// Recomputes the isFree flag for every arrow in <paramref name="arrows"/>
        /// given the current <paramref name="level"/> board definition.
        /// Call this after every successful slide and at level start.
        /// </summary>
        public static void ComputeFreeArrows(LevelData level, List<ArrowData> arrows)
        {
            // Build an occupancy set of all current arrow positions across all cells.
            var occupied = new HashSet<Vector2Int>();
            foreach (var a in arrows)
            {
                if (a.cells != null && a.cells.Count > 0)
                {
                    foreach (var c in a.cells)
                        occupied.Add(c);
                }
                else
                {
                    occupied.Add(a.GridPos);
                }
            }

            foreach (var arrow in arrows)
            {
                arrow.isFree = IsArrowFree(arrow, occupied, level);
            }
        }

        private static bool IsArrowFree(ArrowData arrow, HashSet<Vector2Int> occupied, LevelData level)
        {
            Vector2Int step = arrow.direction.ToVector();
            Vector2Int cur  = arrow.Head + step;

            while (true)
            {
                // If we've stepped outside the valid board, the path is clear → free.
                if (level != null && !level.IsCellValid(cur.x, cur.y))
                    return true;

                // If another arrow occupies this cell, the path is blocked → not free.
                if (occupied.Contains(cur))
                    return false;

                cur += step;
            }
        }

        /// <summary>
        /// Checks whether the puzzle is solved (no arrows remain).
        /// </summary>
        public static bool IsSolved(List<ArrowData> arrows) => arrows.Count == 0;

        /// <summary>
        /// Returns the first free arrow, or null if none exist.
        /// Used by the Hint system to pick a suggestion.
        /// </summary>
        public static ArrowData GetHintArrow(List<ArrowData> arrows)
        {
            foreach (var a in arrows)
                if (a.isFree) return a;
            return null;
        }
    }
}

