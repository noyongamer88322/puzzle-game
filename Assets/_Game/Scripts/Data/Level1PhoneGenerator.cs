// ============================================================
// Level1PhoneGenerator.cs  –  Arrow Slide Puzzle
// Generates Level 1 phone silhouette puzzle data conforming to:
//   • STEP 1: Portrait grid (16x26 for SHOWCASE, 10x18 for BEGINNER)
//   • STEP 2: Phone shape mask:
//             - 4 rounded corners (2-cell radius staircase)
//             - 1-cell thick frame ring
//             - Camera punch-hole (center columns, 2 rows below top)
//             - 3 side button bumps (2 on right, 1 on left)
//   • STEP 3: 100% mask coverage, lengths 2-10 (mostly 3-7),
//             reverse-construction solver guarantee, directional balance ~25%
//   • STEP 4: Golden-yellow #FFB52E frame ring, Pink #FF4FD8 side buttons,
//             flowing wallpaper wave (yellow top, pink bottom, wave middle)
//   • STEP 5: Constant LEVEL1_MODE ("SHOWCASE" vs "BEGINNER")
//   • STEP 6: ASCII mask preview, stats & solver verification
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Data
{
    public static class Level1PhoneGenerator
    {
        // ── STEP 5: Difficulty Option ─────────────────────────
        // "SHOWCASE": Full 16x26 phone silhouette (~79 arrows)
        // "BEGINNER": Smaller 10x18 phone silhouette (~25 arrows)
        public const string LEVEL1_MODE = "SHOWCASE";

        // Fixed deterministic seeds
        public const int FIXED_SEED_SHOWCASE = 1;
        public const int FIXED_SEED_BEGINNER = 42;

        public static int DefaultSeedForMode(string mode)
        {
            return string.Equals(mode, "BEGINNER", StringComparison.OrdinalIgnoreCase)
                ? FIXED_SEED_BEGINNER
                : FIXED_SEED_SHOWCASE;
        }

        public static void GetDimensionsForMode(string mode, out int width, out int height)
        {
            if (string.Equals(mode, "BEGINNER", StringComparison.OrdinalIgnoreCase))
            {
                width = 10;
                height = 18;
            }
            else
            {
                width = 16;
                height = 26;
            }
        }

        // ── STEP 2: Shape Mask ────────────────────────────────

        public static HashSet<Vector2Int> BuildMask(string mode = LEVEL1_MODE)
        {
            GetDimensionsForMode(mode, out int w, out int h);
            var mask = new HashSet<Vector2Int>();

            if (string.Equals(mode, "BEGINNER", StringComparison.OrdinalIgnoreCase))
            {
                // Beginner: 10 cols x 18 rows
                // Body: cols 1..8, rows 0..17
                for (int x = 1; x <= 8; x++)
                    for (int y = 0; y <= 17; y++)
                        mask.Add(new Vector2Int(x, y));

                // 4 Rounded corners (staircase)
                mask.Remove(new Vector2Int(1, 17)); mask.Remove(new Vector2Int(2, 17)); mask.Remove(new Vector2Int(1, 16));
                mask.Remove(new Vector2Int(8, 17)); mask.Remove(new Vector2Int(7, 17)); mask.Remove(new Vector2Int(8, 16));
                mask.Remove(new Vector2Int(1, 0));  mask.Remove(new Vector2Int(2, 0));  mask.Remove(new Vector2Int(1, 1));
                mask.Remove(new Vector2Int(8, 0));  mask.Remove(new Vector2Int(7, 0));  mask.Remove(new Vector2Int(8, 1));

                // Camera punch-hole (center columns 4, 5 at row 15)
                mask.Remove(new Vector2Int(4, 15));
                mask.Remove(new Vector2Int(5, 15));

                // Left bump: col 0, rows 8..10
                mask.Add(new Vector2Int(0, 8));
                mask.Add(new Vector2Int(0, 9));
                mask.Add(new Vector2Int(0, 10));

                // Right bumps: col 9, rows 5..7 and 11..13
                mask.Add(new Vector2Int(9, 5));
                mask.Add(new Vector2Int(9, 6));
                mask.Add(new Vector2Int(9, 7));
                mask.Add(new Vector2Int(9, 11));
                mask.Add(new Vector2Int(9, 12));
                mask.Add(new Vector2Int(9, 13));
            }
            else
            {
                // Showcase: 16 cols x 26 rows
                // Body: cols 1..14, rows 0..25
                for (int x = 1; x <= 14; x++)
                    for (int y = 0; y <= 25; y++)
                        mask.Add(new Vector2Int(x, y));

                // 4 Rounded corners (2-cell radius staircase)
                // Top-Left
                mask.Remove(new Vector2Int(1, 25)); mask.Remove(new Vector2Int(2, 25)); mask.Remove(new Vector2Int(1, 24));
                // Top-Right
                mask.Remove(new Vector2Int(14, 25)); mask.Remove(new Vector2Int(13, 25)); mask.Remove(new Vector2Int(14, 24));
                // Bottom-Left
                mask.Remove(new Vector2Int(1, 0)); mask.Remove(new Vector2Int(2, 0)); mask.Remove(new Vector2Int(1, 1));
                // Bottom-Right
                mask.Remove(new Vector2Int(14, 0)); mask.Remove(new Vector2Int(13, 0)); mask.Remove(new Vector2Int(14, 1));

                // Camera punch-hole (center columns 7, 8 at row 23, 2 rows below top edge)
                mask.Remove(new Vector2Int(7, 23));
                mask.Remove(new Vector2Int(8, 23));

                // Left bump: col 0, rows 10..12 (3 cells tall)
                mask.Add(new Vector2Int(0, 10));
                mask.Add(new Vector2Int(0, 11));
                mask.Add(new Vector2Int(0, 12));

                // Right bumps: col 15, rows 7..9 and rows 13..15 (3 cells tall each)
                mask.Add(new Vector2Int(15, 7));
                mask.Add(new Vector2Int(15, 8));
                mask.Add(new Vector2Int(15, 9));

                mask.Add(new Vector2Int(15, 13));
                mask.Add(new Vector2Int(15, 14));
                mask.Add(new Vector2Int(15, 15));
            }

            return mask;
        }

        public static string GetAsciiMaskPreview(string mode = LEVEL1_MODE)
        {
            GetDimensionsForMode(mode, out int w, out int h);
            var mask = BuildMask(mode);
            var sb = new StringBuilder();

            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = 0; x < w; x++)
                {
                    sb.Append(mask.Contains(new Vector2Int(x, y)) ? "X" : ".");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ── Predefined Frame & Button Paths ───────────────────

        private class PredefinedPath
        {
            public List<Vector2Int> cells;
            public bool isButton;
            public bool isFrame;

            public PredefinedPath(List<Vector2Int> c, bool btn, bool frm)
            {
                cells = c;
                isButton = btn;
                isFrame = frm;
            }
        }

        private static List<PredefinedPath> BuildPredefinedPaths(string mode)
        {
            var list = new List<PredefinedPath>();

            if (string.Equals(mode, "BEGINNER", StringComparison.OrdinalIgnoreCase))
            {
                // Beginner: 10x18
                // Side buttons
                list.Add(new PredefinedPath(new List<Vector2Int> { new Vector2Int(0, 8), new Vector2Int(0, 9), new Vector2Int(0, 10) }, true, false));
                list.Add(new PredefinedPath(new List<Vector2Int> { new Vector2Int(9, 5), new Vector2Int(9, 6), new Vector2Int(9, 7) }, true, false));
                list.Add(new PredefinedPath(new List<Vector2Int> { new Vector2Int(9, 11), new Vector2Int(9, 12), new Vector2Int(9, 13) }, true, false));

                // Left edge (x=1, y=2..15)
                list.Add(new PredefinedPath(MakeStraightLine(1, 2, 1, 8), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(1, 9, 1, 15), false, true));

                // Right edge (x=8, y=2..15)
                list.Add(new PredefinedPath(MakeStraightLine(8, 2, 8, 8), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(8, 9, 8, 15), false, true));

                // Bottom edge (y=0, x=3..6)
                list.Add(new PredefinedPath(MakeStraightLine(3, 0, 6, 0), false, true));

                // Top edge (y=17, x=3..6)
                list.Add(new PredefinedPath(MakeStraightLine(3, 17, 6, 17), false, true));
            }
            else
            {
                // Showcase: 16x26
                // Side buttons: 3 cells tall each
                list.Add(new PredefinedPath(new List<Vector2Int> { new Vector2Int(0, 10), new Vector2Int(0, 11), new Vector2Int(0, 12) }, true, false));
                list.Add(new PredefinedPath(new List<Vector2Int> { new Vector2Int(15, 7), new Vector2Int(15, 8), new Vector2Int(15, 9) }, true, false));
                list.Add(new PredefinedPath(new List<Vector2Int> { new Vector2Int(15, 13), new Vector2Int(15, 14), new Vector2Int(15, 15) }, true, false));

                // Frame ring: left edge (x=1, y=2..23) -> strictly orthogonal vertical lines
                list.Add(new PredefinedPath(MakeStraightLine(1, 2, 1, 6), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(1, 7, 1, 11), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(1, 12, 1, 17), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(1, 18, 1, 23), false, true));

                // Frame ring: right edge (x=14, y=2..23) -> strictly orthogonal vertical lines
                list.Add(new PredefinedPath(MakeStraightLine(14, 2, 14, 6), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(14, 7, 14, 11), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(14, 12, 14, 17), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(14, 18, 14, 23), false, true));

                // Frame ring: bottom edge (y=0, x=3..12) -> strictly orthogonal horizontal lines
                list.Add(new PredefinedPath(MakeStraightLine(3, 0, 7, 0), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(8, 0, 12, 0), false, true));

                // Frame ring: top edge (y=25, x=3..12) -> strictly orthogonal horizontal lines
                list.Add(new PredefinedPath(MakeStraightLine(3, 25, 7, 25), false, true));
                list.Add(new PredefinedPath(MakeStraightLine(8, 25, 12, 25), false, true));
            }

            return list;
        }

        private static List<Vector2Int> MakeStraightLine(int x1, int y1, int x2, int y2)
        {
            var l = new List<Vector2Int>();
            int dx = Math.Sign(x2 - x1);
            int dy = Math.Sign(y2 - y1);
            int curX = x1, curY = y1;
            while (true)
            {
                l.Add(new Vector2Int(curX, curY));
                if (curX == x2 && curY == y2) break;
                curX += dx;
                curY += dy;
            }
            return l;
        }

        // ── Interior Snake Partitioning ───────────────────────

        private static readonly Vector2Int[] OrthogonalNeighbors = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0)   // Left
        };

        private static int CountNeighbors(Vector2Int cell, HashSet<Vector2Int> set)
        {
            int c = 0;
            for (int i = 0; i < 4; i++)
                if (set.Contains(cell + OrthogonalNeighbors[i])) c++;
            return c;
        }

        private static List<PredefinedPath> PartitionInterior(HashSet<Vector2Int> interiorCells, int minLen, int maxLen, System.Random rng)
        {
            var unassigned = new HashSet<Vector2Int>(interiorCells);
            var paths = new List<PredefinedPath>();

            int safety = 0;
            while (unassigned.Count > 0 && safety++ < 3000)
            {
                var list = new List<Vector2Int>(unassigned);
                list.Sort((a, b) => CountNeighbors(a, unassigned).CompareTo(CountNeighbors(b, unassigned)));

                Vector2Int start = list[0];
                var path = new List<Vector2Int> { start };
                unassigned.Remove(start);

                int desiredLen = rng.Next(minLen, maxLen + 1);
                Vector2Int curr = start;

                while (path.Count < desiredLen)
                {
                    var neighbors = new List<Vector2Int>();
                    for (int i = 0; i < 4; i++)
                    {
                        var n = curr + OrthogonalNeighbors[i];
                        if (unassigned.Contains(n)) neighbors.Add(n);
                    }
                    if (neighbors.Count == 0) break;

                    neighbors.Sort((a, b) =>
                    {
                        int c1 = CountNeighbors(a, unassigned);
                        int c2 = CountNeighbors(b, unassigned);
                        if (c1 != c2) return c1.CompareTo(c2);
                        return rng.Next(-1, 2);
                    });

                    Vector2Int next = neighbors[0];
                    path.Add(next);
                    unassigned.Remove(next);
                    curr = next;
                }

                if (path.Count == 1)
                {
                    bool merged = false;
                    Vector2Int single = path[0];
                    for (int i = 0; i < paths.Count; i++)
                    {
                        var p = paths[i].cells;
                        if (Math.Abs(p[0].x - single.x) + Math.Abs(p[0].y - single.y) == 1)
                        {
                            p.Insert(0, single);
                            merged = true;
                            break;
                        }
                        if (Math.Abs(p[p.Count - 1].x - single.x) + Math.Abs(p[p.Count - 1].y - single.y) == 1)
                        {
                            p.Add(single);
                            merged = true;
                            break;
                        }
                    }
                    if (!merged) return null;
                }
                else
                {
                    paths.Add(new PredefinedPath(path, false, false));
                }
            }

            return unassigned.Count == 0 ? paths : null;
        }

        // ── Reverse-Construction Solver Unwinding ─────────────

        private class PathVariant
        {
            public List<Vector2Int> cells; // tail at index 0, head at index Count - 1
            public Vector2Int head;
            public Direction direction;

            public PathVariant(List<Vector2Int> c, Vector2Int h, Direction d)
            {
                cells = c;
                head = h;
                direction = d;
            }
        }

        public static LevelGenerationResult GenerateLevel1(string mode = LEVEL1_MODE, int baseSeed = 0)
        {
            GetDimensionsForMode(mode, out int w, out int h);
            var mask = BuildMask(mode);

            if (baseSeed == 0) baseSeed = DefaultSeedForMode(mode);

            var predefined = BuildPredefinedPaths(mode);
            var used = new HashSet<Vector2Int>();
            foreach (var p in predefined)
                foreach (var c in p.cells) used.Add(c);

            var interior = new HashSet<Vector2Int>();
            foreach (var c in mask)
                if (!used.Contains(c)) interior.Add(c);

            // Attempt generation starting from baseSeed
            for (int seedOffset = 0; seedOffset < 100; seedOffset++)
            {
                int currentSeed = baseSeed + seedOffset;
                var rng = new System.Random(currentSeed);

                List<PredefinedPath> interiorPaths = null;
                int minL = string.Equals(mode, "BEGINNER", StringComparison.OrdinalIgnoreCase) ? 3 : 3;
                int maxL = string.Equals(mode, "BEGINNER", StringComparison.OrdinalIgnoreCase) ? 5 : 6;

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    interiorPaths = PartitionInterior(interior, minL, maxL, rng);
                    if (interiorPaths != null) break;
                }
                if (interiorPaths == null) continue;

                var allPaths = new List<PredefinedPath>();
                allPaths.AddRange(predefined);
                allPaths.AddRange(interiorPaths);

                int n = allPaths.Count;
                var pathVariants = new List<List<PathVariant>>();
                var cellToPath = new Dictionary<Vector2Int, int>();

                for (int i = 0; i < n; i++)
                {
                    var p = allPaths[i].cells;
                    foreach (var c in p) cellToPath[c] = i;

                    var vars = new List<PathVariant>();

                    // Variant A: tail at index 0, head at index p.Count - 1
                    Vector2Int headA = p[p.Count - 1];
                    Vector2Int neckA = p[p.Count - 2];
                    Direction dirA = VecToDir(headA - neckA);
                    vars.Add(new PathVariant(new List<Vector2Int>(p), headA, dirA));

                    // Variant B: reverse so tail is at index 0, head at rev.Count - 1 (which was p[0])
                    var rev = new List<Vector2Int>(p);
                    rev.Reverse();
                    Vector2Int headB = rev[rev.Count - 1];
                    Vector2Int neckB = rev[rev.Count - 2];
                    Direction dirB = VecToDir(headB - neckB);
                    vars.Add(new PathVariant(rev, headB, dirB));

                    pathVariants.Add(vars);
                }

                var activePaths = new HashSet<int>();
                for (int i = 0; i < n; i++) activePaths.Add(i);

                var chosenVariants = new PathVariant[n];
                var solveOrder = new List<int>();
                int[] dirCounts = new int[4];
                int assigned = 0;
                bool deadlock = false;

                while (activePaths.Count > 0)
                {
                    int bestPath = -1;
                    PathVariant bestVar = null;
                    double bestScore = double.MinValue;

                    foreach (int pIdx in activePaths)
                    {
                        var candVars = chosenVariants[pIdx] != null ? new List<PathVariant> { chosenVariants[pIdx] } : pathVariants[pIdx];

                        foreach (var v in candVars)
                        {
                            Vector2Int step = v.direction.ToVector();
                            Vector2Int cur = v.head + step;
                            bool blocked = false;

                            while (cur.x >= 0 && cur.x < w && cur.y >= 0 && cur.y < h)
                            {
                                if (cellToPath.TryGetValue(cur, out int occ))
                                {
                                    if (activePaths.Contains(occ) && occ != pIdx)
                                    {
                                        blocked = true;
                                        break;
                                    }
                                }
                                cur += step;
                            }

                            if (!blocked)
                            {
                                int dIdx = (int)v.direction;
                                double expected = (assigned + 1) / 4.0;
                                double imbalance = dirCounts[dIdx] - expected;
                                double score = 100.0 - imbalance * 35.0;

                                // Outward preference
                                if (v.direction == Direction.Up && v.head.y >= h / 2) score += 12;
                                if (v.direction == Direction.Down && v.head.y < h / 2) score += 12;
                                if (v.direction == Direction.Right && v.head.x >= w / 2) score += 12;
                                if (v.direction == Direction.Left && v.head.x < w / 2) score += 12;

                                score += rng.NextDouble() * 3.0;

                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestPath = pIdx;
                                    bestVar = v;
                                }
                            }
                        }
                    }

                    if (bestPath == -1)
                    {
                        deadlock = true;
                        break;
                    }

                    chosenVariants[bestPath] = bestVar;
                    activePaths.Remove(bestPath);
                    solveOrder.Add(bestPath);
                    dirCounts[(int)bestVar.direction]++;
                    assigned++;
                }

                if (deadlock) continue;

                // Step 4: Visual Flavor (Color Assignment)
                var arrowDtos = new List<ArrowJsonData>();
                int longestLen = 0;
                int yellowCount = 0;
                int pinkCount = 0;

                for (int i = 0; i < n; i++)
                {
                    var p = allPaths[i];
                    var v = chosenVariants[i];
                    if (v.cells.Count > longestLen) longestLen = v.cells.Count;

                    int colIdx = 0;
                    if (p.isButton)
                    {
                        colIdx = 1; // Pink #FF4FD8 for side buttons
                    }
                    else if (p.isFrame)
                    {
                        colIdx = 0; // Golden yellow #FFB52E for frame ring
                    }
                    else
                    {
                        // Screen Area flowing wave
                        double sumX = 0, sumY = 0;
                        foreach (var c in v.cells) { sumX += c.x; sumY += c.y; }
                        double avgX = sumX / v.cells.Count;
                        double avgY = sumY / v.cells.Count;

                        double splitTop = h * 0.58;
                        double splitBottom = h * 0.38;

                        if (avgY >= splitTop)
                        {
                            colIdx = 0; // Upper ~40%: Yellow #FFB52E
                        }
                        else if (avgY <= splitBottom)
                        {
                            colIdx = 1; // Lower ~40%: Pink #FF4FD8
                        }
                        else
                        {
                            // Middle band: smooth S-curve wave boundary
                            double waveY = h * 0.48 + (h * 0.08) * Math.Sin((avgX - 1.0) / (w - 3.0) * Math.PI);
                            if (avgY > waveY + 0.5) colIdx = 0;
                            else if (avgY < waveY - 0.5) colIdx = 1;
                            else colIdx = (int)(Math.Floor(avgX + avgY)) % 2 == 0 ? 0 : 1;
                        }
                    }

                    if (colIdx == 0) yellowCount++; else pinkCount++;

                    var dto = new ArrowJsonData
                    {
                        direction = v.direction.ToString(),
                        color = colIdx == 0 ? "Orange" : "Pink",
                        colorIndex = colIdx,
                        cells = new List<CellJson>()
                    };

                    foreach (var c in v.cells)
                        dto.cells.Add(new CellJson(c.x, c.y));

                    arrowDtos.Add(dto);
                }

                // Forward Solver Check & Assert
                bool isSolvable = ValidateSolvability(w, h, arrowDtos);
                if (!isSolvable) continue;

                // LevelDataJson
                var levelJson = new LevelDataJson
                {
                    id = 1,
                    chapter = "Hills",
                    maskFileName = "mask_phone.png",
                    gridWidth = w,
                    gridHeight = h,
                    rewardImageName = "phone.png",
                    colorMode = "mixed",
                    arrows = arrowDtos
                };

                var res = new LevelGenerationResult
                {
                    success = true,
                    levelId = 1,
                    dataJson = levelJson,
                    jsonString = JsonUtility.ToJson(levelJson, true),
                    arrowCount = arrowDtos.Count,
                    cellCount = mask.Count
                };

                return res;
            }

            return new LevelGenerationResult
            {
                success = false,
                levelId = 1,
                errorMessage = "Failed to generate solvable Level 1 after seed search."
            };
        }

        // ── Forward Solver Simulation ─────────────────────────

        public static bool ValidateSolvability(int width, int height, List<ArrowJsonData> arrows)
        {
            if (arrows == null || arrows.Count == 0) return false;

            var occupied = new HashSet<Vector2Int>();
            foreach (var a in arrows)
            {
                if (a.cells == null || a.cells.Count < 2) return false;
                for (int ci = 0; ci < a.cells.Count; ci++)
                {
                    var pt = new Vector2Int(a.cells[ci].x, a.cells[ci].y);
                    if (occupied.Contains(pt)) return false; // Overlap!
                    occupied.Add(pt);

                    if (ci > 0)
                    {
                        var prev = new Vector2Int(a.cells[ci - 1].x, a.cells[ci - 1].y);
                        if (Math.Abs(pt.x - prev.x) + Math.Abs(pt.y - prev.y) != 1)
                            return false; // Non-adjacent!
                    }
                }

                // Direction matches last segment
                var head = new Vector2Int(a.cells[a.cells.Count - 1].x, a.cells[a.cells.Count - 1].y);
                var neck = new Vector2Int(a.cells[a.cells.Count - 2].x, a.cells[a.cells.Count - 2].y);
                Direction actualDir = VecToDir(head - neck);
                if (actualDir != ParseDirection(a.direction)) return false;
            }

            var remaining = new List<ArrowJsonData>(arrows);
            while (remaining.Count > 0)
            {
                int freeIdx = -1;

                for (int i = 0; i < remaining.Count; i++)
                {
                    var a = remaining[i];
                    var head = new Vector2Int(a.cells[a.cells.Count - 1].x, a.cells[a.cells.Count - 1].y);
                    Direction dir = ParseDirection(a.direction);
                    Vector2Int step = dir.ToVector();
                    Vector2Int cur = head + step;

                    bool blocked = false;
                    while (cur.x >= 0 && cur.x < width && cur.y >= 0 && cur.y < height)
                    {
                        if (occupied.Contains(cur))
                        {
                            blocked = true;
                            break;
                        }
                        cur += step;
                    }

                    if (!blocked)
                    {
                        freeIdx = i;
                        break;
                    }
                }

                if (freeIdx == -1) return false; // Deadlock!

                var freed = remaining[freeIdx];
                remaining.RemoveAt(freeIdx);
                foreach (var c in freed.cells)
                    occupied.Remove(new Vector2Int(c.x, c.y));
            }

            return true;
        }

        // ── File Save & Mask Generation ───────────────────────

        public static void GenerateAndSaveLevel1(string mode = LEVEL1_MODE, int seed = 0)
        {
            var res = GenerateLevel1(mode, seed);
            if (!res.success)
            {
                Debug.LogError($"[Level1PhoneGenerator] Failed to generate Level 1: {res.errorMessage}");
                return;
            }

            // 1. Save level_1.json to Assets/_Game/Data/
            string dataFolder = Path.Combine(Application.dataPath, "_Game/Data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            string jsonPath = Path.Combine(dataFolder, "level_1.json");
            File.WriteAllText(jsonPath, res.jsonString);
            Debug.Log($"[Level1PhoneGenerator] Saved {res.arrowCount} arrows to {jsonPath}");

            // 2. Save mask_phone.png to Assets/_Game/Art/Masks/
            string masksFolder = Path.Combine(Application.dataPath, "_Game/Art/Masks");
            if (!Directory.Exists(masksFolder)) Directory.CreateDirectory(masksFolder);
            string maskPath = Path.Combine(masksFolder, "mask_phone.png");
            SaveMaskTexture(mode, maskPath);
        }

        public static void SaveMaskTexture(string mode, string filePath)
        {
            GetDimensionsForMode(mode, out int w, out int h);
            var mask = BuildMask(mode);

            // Create high-res mask texture so MaskLoader samples exact cells
            int texW = w * 8;
            int texH = h * 8;
            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            var pixels = new Color[texW * texH];

            for (int py = 0; py < texH; py++)
            {
                int gridY = Mathf.Clamp(Mathf.FloorToInt(((float)py / texH) * h), 0, h - 1);
                for (int px = 0; px < texW; px++)
                {
                    int gridX = Mathf.Clamp(Mathf.FloorToInt(((float)px / texW) * w), 0, w - 1);
                    bool inside = mask.Contains(new Vector2Int(gridX, gridY));
                    pixels[py * texW + px] = inside ? Color.white : Color.black;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, png);
            Debug.Log($"[Level1PhoneGenerator] Saved mask texture ({texW}x{texH}) to {filePath}");
        }

        private static Direction VecToDir(Vector2Int v)
        {
            if (v.x == 0 && v.y > 0) return Direction.Up;
            if (v.x > 0 && v.y == 0) return Direction.Right;
            if (v.x == 0 && v.y < 0) return Direction.Down;
            if (v.x < 0 && v.y == 0) return Direction.Left;
            return Direction.Up;
        }

        private static Direction ParseDirection(string dirStr)
        {
            if (string.IsNullOrEmpty(dirStr)) return Direction.Up;
            return dirStr.Trim().ToLowerInvariant() switch
            {
                "up" => Direction.Up,
                "right" => Direction.Right,
                "down" => Direction.Down,
                "left" => Direction.Left,
                _ => Direction.Up
            };
        }
    }
}
