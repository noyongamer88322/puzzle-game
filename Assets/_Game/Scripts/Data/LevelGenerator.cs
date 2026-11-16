// ============================================================
// LevelGenerator.cs  –  Arrow Slide Puzzle
// Automatic Solvable Level Generator implementing:
//   • PRD Section 4.6: Arrows placed sequentially; a newly placed
//     arrow's exit ray must not collide with any already placed arrow.
//     Guarantees reverse-order solvability.
//   • 100% Mask Coverage: All mask cells partitioned into contiguous
//     bendy arrows of length >= 2 (up to 15-25 for large levels).
//   • PRD Section 4.7 Difficulty:
//       Level 1    : 1-2 arrows, small grid.
//       Level 2-3  : 3-5 arrows, small grid.
//       Level 4-5  : ~10-25 arrows, medium grid (Orange + Magenta).
//       Level 6-7+ : Shape level, 40+ arrows (Heart), large grid (Gradient).
//       Later      : More arrows, longer, more bends, breather levels.
//   • Validator: Simulates the puzzle solver to prove complete solvability.
//   • PRD Section 4.8 Chapters: 5 levels per chapter.
//   • Reward images: L1-3 placeholder, L4 phone, L5 book, L6 heart.
// PRD Section 7: Assets/_Game/Scripts/Data/LevelGenerator.cs
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Data
{
    [System.Serializable]
    public class LevelGenerationConfig
    {
        public int id;
        public string chapter;
        public string maskFileName;
        public int gridWidth;
        public int gridHeight;
        public int minArrowLength = 2;
        public int maxArrowLength = 4;
        public string colorMode = "mixed"; // "mixed", "orangeMagenta", "gradient"
        public string rewardImageName;
        public int maxAttempts = 100;

        public LevelGenerationConfig() { }

        public LevelGenerationConfig(int id, string chapter, string maskFileName, int width, int height,
            int minLen, int maxLen, string colorMode, string rewardImageName)
        {
            this.id = id;
            this.chapter = chapter;
            this.maskFileName = maskFileName;
            this.gridWidth = width;
            this.gridHeight = height;
            this.minArrowLength = minLen;
            this.maxArrowLength = maxLen;
            this.colorMode = colorMode;
            this.rewardImageName = rewardImageName;
        }
    }

    public class LevelGenerationResult
    {
        public bool success;
        public int levelId;
        public LevelDataJson dataJson;
        public string jsonString;
        public int arrowCount;
        public int cellCount;
        public string errorMessage;
    }

    public static class LevelGenerator
    {
        private static readonly Vector2Int[] OrthogonalDirs = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0)   // Left
        };

        private static readonly string[] Chapters = new string[]
        {
            "Hills", "Forest", "Meadow", "Mountains", "Sunset", "Sky"
        };

        // ── Public Generator API ──────────────────────────────

        /// <summary>
        /// Generates a complete, validated, solvable level according to the provided config.
        /// </summary>
        public static LevelGenerationResult GenerateLevel(LevelGenerationConfig config, int randomSeed = 0)
        {
            var result = new LevelGenerationResult { levelId = config.id };

            if (config.gridWidth <= 0 || config.gridHeight <= 0)
            {
                result.success = false;
                result.errorMessage = $"Invalid grid dimensions: {config.gridWidth}x{config.gridHeight}";
                return result;
            }

            var rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();

            // 1. Obtain mask cells
            HashSet<Vector2Int> maskCells = null;
            if (!string.IsNullOrEmpty(config.maskFileName))
            {
                maskCells = MaskLoader.LoadMask(config.maskFileName, config.gridWidth, config.gridHeight);
            }

            // Fallback: full rectangular grid if no mask file or mask empty
            if (maskCells == null || maskCells.Count == 0)
            {
                maskCells = new HashSet<Vector2Int>();
                for (int y = 0; y < config.gridHeight; y++)
                    for (int x = 0; x < config.gridWidth; x++)
                        maskCells.Add(new Vector2Int(x, y));
            }

            result.cellCount = maskCells.Count;

            // 2. Generation loop with retries
            for (int attempt = 0; attempt < config.maxAttempts; attempt++)
            {
                // Step A: Partition mask cells into contiguous snake paths
                var paths = PartitionMaskIntoPaths(maskCells, config.minArrowLength, config.maxArrowLength, rng);
                if (paths == null || paths.Count == 0) continue;

                // Step B: Order paths and assign heads/directions according to PRD 4.6
                var orderedArrows = OrderArrowsPRD46(paths, config.gridWidth, config.gridHeight, rng);
                if (orderedArrows == null || orderedArrows.Count == 0) continue;

                // Step C: Format into DTO arrows and assign colors
                var arrowDtos = new List<ArrowJsonData>();
                for (int i = 0; i < orderedArrows.Count; i++)
                    arrowDtos.Add(orderedArrows[i].ToArrowJsonData(i, config.colorMode, orderedArrows.Count));

                // Step D: Run Validator simulation
                if (!ValidateAndSolveLevel(config.gridWidth, config.gridHeight, maskCells, arrowDtos))
                    continue;

                // Success! Construct LevelDataJson
                var levelJson = new LevelDataJson
                {
                    id              = config.id,
                    chapter         = config.chapter,
                    maskFileName    = config.maskFileName ?? "",
                    gridWidth       = config.gridWidth,
                    gridHeight      = config.gridHeight,
                    rewardImageName = config.rewardImageName ?? "pic_placeholder.png",
                    colorMode       = config.colorMode ?? "mixed",
                    arrows          = arrowDtos
                };

                result.success    = true;
                result.dataJson   = levelJson;
                result.arrowCount = arrowDtos.Count;
                result.jsonString = JsonUtility.ToJson(levelJson, true);
                return result;
            }

            result.success = false;
            result.errorMessage = $"Failed to generate solvable level after {config.maxAttempts} attempts.";
            return result;
        }

        /// <summary>
        /// Generates the standard default 30 levels per PRD 4.7 specifications and saves to disk.
        /// </summary>
        public static List<LevelGenerationResult> GenerateDefault30Levels(string outputFolder = null)
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = Path.Combine(Application.dataPath, "_Game/Data");
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var results = new List<LevelGenerationResult>();

            for (int id = 1; id <= 30; id++)
            {
                var cfg = GetConfigForLevel(id);
                var res = GenerateLevel(cfg, id * 1009 + 42);

                if (res.success)
                {
                    string filePath = Path.Combine(outputFolder, $"level_{id}.json");
                    File.WriteAllText(filePath, res.jsonString);
                    Debug.Log($"[LevelGenerator] Level {id} saved: {res.arrowCount} arrows ({res.cellCount} cells, mode={cfg.colorMode}) -> {filePath}");
                }
                else
                {
                    Debug.LogError($"[LevelGenerator] Level {id} generation failed: {res.errorMessage}");
                }

                results.Add(res);
            }

            return results;
        }

        /// <summary>
        /// Returns the default PRD 4.7 & 4.8 configuration for a specific level ID (1-30).
        /// Updated for tighter board fill, longer winding paths, mixed lengths.
        /// </summary>
        public static LevelGenerationConfig GetConfigForLevel(int id)
        {
            int chapterIdx = Mathf.Clamp((id - 1) / 5, 0, Chapters.Length - 1);
            string chapterName = Chapters[chapterIdx];

            int w = 10, h = 14;
            if (id >= 6 && id < 15) { w = 11; h = 15; }
            else if (id >= 15 && id < 25) { w = 11; h = 16; }
            else if (id >= 25) { w = 12; h = 17; }

            int minLen = 2;
            int maxLen = 14;
            string colorMode = "mixed";

            string[] picPool = new string[] { "phone.png", "book.png", "love.png", "dog.png", "phone.png", "book.png" };
            string rewardPic = picPool[(id - 1) % picPool.Length];

            return new LevelGenerationConfig(id, chapterName, "", w, h, minLen, maxLen, colorMode, rewardPic);
        }

        // ── Core Algorithm Steps ──────────────────────────────

        /// <summary>
        /// Step 1: Partitions mask cells into contiguous snake paths using Warnsdorff heuristic.
        /// Produces winding paths with many bends (turns every 2-4 cells), mixed lengths,
        /// and varied directions to match the reference image style.
        /// Absorbs any isolated single cells so 100% of cells are covered by paths of length >= 2.
        /// </summary>
        public static List<List<Vector2Int>> PartitionMaskIntoPaths(HashSet<Vector2Int> maskCells, int minLen, int maxLen, System.Random rng)
        {
            var unassigned = new HashSet<Vector2Int>(maskCells);
            var paths = new List<List<Vector2Int>>();

            // Track the last direction used per path to encourage turning after 2-4 straight cells
            int safety = 0;
            while (unassigned.Count > 0 && safety++ < 2500)
            {
                var list = new List<Vector2Int>(unassigned);
                // Warnsdorff heuristic: sort ascending by remaining unassigned neighbor count
                list.Sort((a, b) => CountNeighbors(a, unassigned).CompareTo(CountNeighbors(b, unassigned)));

                Vector2Int start = list[0];
                var path = new List<Vector2Int> { start };
                unassigned.Remove(start);

                // Mix of lengths: short (2-3), medium (4-8), long (9-15+)
                // Weight towards medium-long to match reference
                int desiredLen = PickPathLength(minLen, maxLen, rng);
                Vector2Int curr = start;
                Vector2Int lastDir = Vector2Int.zero;
                int straightCount = 0;      // consecutive steps in same direction
                int maxStraight   = rng.Next(2, 5); // prefer turns after 2-4 straight cells

                while (path.Count < desiredLen)
                {
                    var neighbors = GetUnassignedNeighbors(curr, unassigned);
                    if (neighbors.Count == 0) break;

                    // Choose next cell: prefer turning after maxStraight straight cells
                    Vector2Int chosen;
                    if (straightCount >= maxStraight && lastDir != Vector2Int.zero)
                    {
                        // Prefer neighbors in a different direction
                        var turningNeighbors = new List<Vector2Int>();
                        foreach (var nb in neighbors)
                        {
                            if (nb - curr != lastDir)
                                turningNeighbors.Add(nb);
                        }
                        if (turningNeighbors.Count > 0)
                        {
                            // Among turning neighbors pick lowest-degree (Warnsdorff)
                            turningNeighbors.Sort((a, b) =>
                                CountNeighbors(a, unassigned).CompareTo(CountNeighbors(b, unassigned)));
                            chosen = turningNeighbors[0];
                        }
                        else
                        {
                            // No turning option; continue straight
                            neighbors.Sort((a, b) =>
                                CountNeighbors(a, unassigned).CompareTo(CountNeighbors(b, unassigned)));
                            chosen = neighbors[0];
                        }
                    }
                    else
                    {
                        // Normal Warnsdorff: lowest-degree neighbor
                        neighbors.Sort((a, b) =>
                            CountNeighbors(a, unassigned).CompareTo(CountNeighbors(b, unassigned)));
                        chosen = neighbors[0];
                    }

                    Vector2Int newDir = chosen - curr;
                    if (newDir == lastDir)
                        straightCount++;
                    else
                    {
                        straightCount = 1;
                        maxStraight = rng.Next(2, 5); // reset random turn interval
                    }
                    lastDir = newDir;

                    path.Add(chosen);
                    unassigned.Remove(chosen);
                    curr = chosen;
                }

                // If path is a single cell, attempt to absorb it into an adjacent path's end
                if (path.Count == 1)
                {
                    bool merged = false;
                    Vector2Int single = path[0];

                    foreach (var p in paths)
                    {
                        if (IsOrthogonalAdjacent(p[0], single))
                        {
                            p.Insert(0, single);
                            merged = true;
                            break;
                        }
                        if (IsOrthogonalAdjacent(p[p.Count - 1], single))
                        {
                            p.Add(single);
                            merged = true;
                            break;
                        }
                    }

                    if (!merged)
                    {
                        // Could not merge single cell; abort partition attempt
                        return null;
                    }
                }
                else
                {
                    paths.Add(path);
                }
            }

            return unassigned.Count == 0 ? paths : null;
        }

        /// <summary>
        /// Picks a path length with a distribution that matches the reference:
        /// some short (2-3), many medium (4-8), some long snakes (9-maxLen).
        /// </summary>
        private static int PickPathLength(int minLen, int maxLen, System.Random rng)
        {
            int roll = rng.Next(100);
            if (roll < 15)
                return Mathf.Clamp(rng.Next(2, 4), minLen, maxLen);       // 15% short
            else if (roll < 70)
                return Mathf.Clamp(rng.Next(4, 9), minLen, maxLen);       // 55% medium
            else
                return Mathf.Clamp(rng.Next(9, maxLen + 1), minLen, maxLen); // 30% long
        }

        /// <summary>
        /// Step 2: PRD Section 4.6 Ordering.
        /// Determines the head and direction for each path, and orders arrows such that each
        /// arrow's exit ray does not intersect any previously placed arrow.
        /// </summary>
        public static List<GeneratedArrow> OrderArrowsPRD46(List<List<Vector2Int>> paths, int width, int height, System.Random rng)
        {
            var candidates = new List<PathCandidate>();

            for (int pIdx = 0; pIdx < paths.Count; pIdx++)
            {
                var p = paths[pIdx];
                var variants = new List<ArrowVariant>();

                // Variant A: Head at end of path (index p.Count - 1)
                Vector2Int headA = p[p.Count - 1];
                Vector2Int neckA = p[p.Count - 2];
                Vector2Int dirVecA = headA - neckA;
                Direction dirA = VectorToDirection(dirVecA);
                variants.Add(new ArrowVariant(p, headA, dirA));

                // Variant B: Head at beginning of path (index 0)
                Vector2Int headB = p[0];
                Vector2Int neckB = p[1];
                Vector2Int dirVecB = headB - neckB;
                Direction dirB = VectorToDirection(dirVecB);
                var revPath = new List<Vector2Int>(p);
                revPath.Reverse();
                variants.Add(new ArrowVariant(revPath, headB, dirB));

                candidates.Add(new PathCandidate(pIdx, variants));
            }

            var cellToPath = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < paths.Count; i++)
            {
                foreach (var c in paths[i])
                    cellToPath[c] = i;
            }

            var activePaths = new HashSet<int>();
            for (int i = 0; i < paths.Count; i++)
                activePaths.Add(i);

            // solveOrder records arrows in game-removal order (first out -> last out)
            var solveOrder = new List<GeneratedArrow>();
            int[] dirCounts = new int[4];

            while (activePaths.Count > 0)
            {
                int bestPath = -1;
                ArrowVariant bestVar = null;
                float bestScore = float.MinValue;

                foreach (int pIdx in activePaths)
                {
                    var cand = candidates[pIdx];
                    var varsToTry = cand.chosen != null ? new List<ArrowVariant> { cand.chosen } : cand.variants;

                    foreach (var v in varsToTry)
                    {
                        Vector2Int step = v.direction.ToVector();
                        Vector2Int cur = v.head + step;
                        bool blocked = false;

                        // Ray trace along direction to board boundary
                        while (cur.x >= 0 && cur.x < width && cur.y >= 0 && cur.y < height)
                        {
                            if (cellToPath.TryGetValue(cur, out int occ) && activePaths.Contains(occ) && occ != pIdx)
                            {
                                blocked = true;
                                break;
                            }
                            cur += step;
                        }

                        if (!blocked)
                        {
                            // Balance all 4 directions evenly
                            int dIdx = (int)v.direction;
                            float score = 100f - dirCounts[dIdx] * 20f + (float)rng.NextDouble() * 5f;

                            // Outward bonus
                            if (v.direction == Direction.Up && v.head.y >= height / 2) score += 10f;
                            if (v.direction == Direction.Down && v.head.y < height / 2) score += 10f;
                            if (v.direction == Direction.Right && v.head.x >= width / 2) score += 10f;
                            if (v.direction == Direction.Left && v.head.x < width / 2) score += 10f;

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
                    // Deadlock encountered
                    return null;
                }

                candidates[bestPath].chosen = bestVar;
                activePaths.Remove(bestPath);
                dirCounts[(int)bestVar.direction]++;
                solveOrder.Add(new GeneratedArrow(bestVar.cells, bestVar.direction));
            }

            // In PRD 4.6 placement order: reverse of solve order!
            // Arrow 1 is placed first; last arrow placed is solveOrder[0] which exits first!
            solveOrder.Reverse();
            return solveOrder;
        }

        /// <summary>
        /// Step 3: Pure-logic Validator.
        /// Simulates gameplay solving: repeatedly finds a free arrow and slides it out.
        /// Returns true if and only if all arrows can be cleared without deadlock.
        /// </summary>
        public static bool ValidateAndSolveLevel(int width, int height, HashSet<Vector2Int> maskCells, List<ArrowJsonData> arrows)
        {
            if (arrows == null || arrows.Count == 0) return false;

            // 1. Verify coverage and cell validity
            var occupied = new HashSet<Vector2Int>();
            foreach (var a in arrows)
            {
                if (a.cells == null || a.cells.Count < 2) return false;

                for (int i = 0; i < a.cells.Count; i++)
                {
                    var c = new Vector2Int(a.cells[i].x, a.cells[i].y);
                    if (!maskCells.Contains(c)) return false;
                    if (occupied.Contains(c)) return false;
                    occupied.Add(c);

                    if (i > 0)
                    {
                        var prev = new Vector2Int(a.cells[i - 1].x, a.cells[i - 1].y);
                        if (!IsOrthogonalAdjacent(c, prev)) return false;
                    }
                }
            }

            if (occupied.Count != maskCells.Count) return false;

            // 2. Simulate solve loop
            var remaining = new List<ArrowJsonData>(arrows);
            while (remaining.Count > 0)
            {
                int freeIdx = -1;

                for (int i = 0; i < remaining.Count; i++)
                {
                    var a = remaining[i];
                    var headDto = a.cells[a.cells.Count - 1];
                    var head = new Vector2Int(headDto.x, headDto.y);
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

                if (freeIdx == -1)
                {
                    // Deadlock!
                    return false;
                }

                // Remove the free arrow
                var freed = remaining[freeIdx];
                remaining.RemoveAt(freeIdx);
                foreach (var c in freed.cells)
                {
                    occupied.Remove(new Vector2Int(c.x, c.y));
                }
            }

            return true;
        }

        // ── Helper Data Structures ────────────────────────────

        public class GeneratedArrow
        {
            public List<Vector2Int> cells;
            public Direction direction;

            public GeneratedArrow(List<Vector2Int> cells, Direction direction)
            {
                this.cells = cells;
                this.direction = direction;
            }

            public ArrowJsonData ToArrowJsonData(int arrowIndex, string colorMode, int totalArrows)
            {
                var dto = new ArrowJsonData
                {
                    direction = direction.ToString(),
                    cells = new List<CellJson>()
                };

                foreach (var c in cells)
                    dto.cells.Add(new CellJson(c.x, c.y));

                if (string.Equals(colorMode, "gradient", StringComparison.OrdinalIgnoreCase))
                {
                    dto.color = "GradientHeartOrange";
                    dto.colorIndex = 0;
                }
                else if (string.Equals(colorMode, "orangeMagenta", StringComparison.OrdinalIgnoreCase))
                {
                    // Group in blocks: first half orange, second half pink — avoids random noise
                    int groupSize = Mathf.Max(1, totalArrows / 2);
                    dto.colorIndex = (arrowIndex / groupSize) % 2;
                    dto.color = dto.colorIndex == 0 ? "Orange" : "Pink";
                }
                else
                {
                    // "mixed": two colours (orange/pink) in groups, NOT per-arrow alternating.
                    // Group size ≈ totalArrows / 2 so each colour covers a contiguous region.
                    int groupSize = Mathf.Max(2, totalArrows / 2);
                    dto.colorIndex = (arrowIndex / groupSize) % 2;
                    dto.color = dto.colorIndex == 0 ? "Orange" : "Pink";
                }

                return dto;
            }
        }

        private class PathCandidate
        {
            public int pathIndex;
            public List<ArrowVariant> variants;
            public ArrowVariant chosen;

            public PathCandidate(int pathIndex, List<ArrowVariant> variants)
            {
                this.pathIndex = pathIndex;
                this.variants = variants;
                this.chosen = null;
            }
        }

        private class ArrowVariant
        {
            public List<Vector2Int> cells;
            public Vector2Int head;
            public Direction direction;

            public ArrowVariant(List<Vector2Int> cells, Vector2Int head, Direction direction)
            {
                this.cells = cells;
                this.head = head;
                this.direction = direction;
            }
        }

        // ── Math & Grid Helpers ───────────────────────────────

        private static int CountNeighbors(Vector2Int cell, HashSet<Vector2Int> set)
        {
            int c = 0;
            foreach (var d in OrthogonalDirs)
                if (set.Contains(cell + d)) c++;
            return c;
        }

        private static List<Vector2Int> GetUnassignedNeighbors(Vector2Int cell, HashSet<Vector2Int> set)
        {
            var res = new List<Vector2Int>();
            foreach (var d in OrthogonalDirs)
            {
                var n = cell + d;
                if (set.Contains(n)) res.Add(n);
            }
            return res;
        }

        private static bool IsOrthogonalAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private static Direction VectorToDirection(Vector2Int v)
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
                "up"    => Direction.Up,
                "right" => Direction.Right,
                "down"  => Direction.Down,
                "left"  => Direction.Left,
                _       => Direction.Up
            };
        }
    }
}

