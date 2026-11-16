// ============================================================
// LevelDatabase.cs
// Hard-coded definitions for all 6 prototype levels.
//
// Grid coordinate system
//   col : 0 = leftmost column
//   row : 0 = topmost row   (y increases downward in grid space)
//
// Arrow colour rules (video corrections):
//   L1-3  : mixed  (Pink, Orange, Blue, Cyan, Purple)
//   L4-5  : Orange + Magenta only
//   L6    : Heart gradient colours
//
// UI unlock rules (video corrections):
//   L1    : coin pill + gear only
//   L2-3  : + heart pill
//   L4+   : + back button + hint button (free first time)
//   L6    : + magnet button (free first time)
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace ArrowPuzzle.Core
{
    public static class LevelDatabase
    {
        private static List<LevelData> _levels;

        public static IReadOnlyList<LevelData> All
        {
            get
            {
                if (_levels == null) Build();
                return _levels;
            }
        }

        public static LevelData Get(int levelIndex)
        {
            try
            {
                string jsonPath = System.IO.Path.Combine(Application.dataPath, "_Game/Data", $"level_{levelIndex}.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    string json = System.IO.File.ReadAllText(jsonPath);
                    var ld = LevelData.FromJson(json);
                    if (ld != null) return ld;
                }
            }
            catch { }

            if (_levels == null) Build();
            int idx = levelIndex - 1;
            if (idx < 0 || idx >= _levels.Count)
            {
                Debug.LogError($"[LevelDatabase] Invalid levelIndex {levelIndex}");
                return null;
            }
            return _levels[idx];
        }

        // ── Builder ───────────────────────────────────────────

        private static void Build()
        {
            _levels = new List<LevelData>
            {
                BuildLevel1(),
                BuildLevel2(),
                BuildLevel3(),
                BuildLevel4(),
                BuildLevel5(),
                BuildLevel6(),
            };
        }

        // ─────────────────────────────────────────────────────
        // LEVEL 1  –  Mobile (Phone) shape puzzle
        //             UI: coin + gear only
        // ─────────────────────────────────────────────────────
        private static LevelData BuildLevel1() => new LevelData
        {
            levelIndex      = 1,
            displayName     = "Level 1",
            boardShape      = BoardShape.Rectangle,
            maskFileName    = "mask_phone.png",
            rewardImageName = "phone.png",
            gridCols        = 5,
            gridRows        = 7,
            arrows = new List<ArrowData>
            {
                new ArrowData(1, 1, Direction.Down,  ArrowColor.Orange),
                new ArrowData(1, 3, Direction.Down,  ArrowColor.Pink),
                new ArrowData(2, 5, Direction.Right, ArrowColor.Purple),
                new ArrowData(3, 4, Direction.Left,  ArrowColor.Blue),
                new ArrowData(2, 2, Direction.Up,    ArrowColor.Cyan),
            },
            showHeartPill   = false,
            showBackButton  = false,
            showHintButton  = false,
            showMagnetButton= false,
            hintIsFree      = false,
            magnetIsFree    = false,
            reward          = RewardType.Phone,
            galleryKey      = "phone",
            hasGalleryReward= true,
            chapterName     = "Hills",
            starsForChapter = 5
        };

        // ─────────────────────────────────────────────────────
        // LEVEL 2  –  Book shape puzzle
        //             UI: + heart pill
        // ─────────────────────────────────────────────────────
        private static LevelData BuildLevel2() => new LevelData
        {
            levelIndex      = 2,
            displayName     = "Level 2",
            boardShape      = BoardShape.Rectangle,
            maskFileName    = "mask_book.png",
            rewardImageName = "book.png",
            gridCols        = 6,
            gridRows        = 6,
            arrows = new List<ArrowData>
            {
                new ArrowData(1, 1, Direction.Down,  ArrowColor.Pink),
                new ArrowData(2, 3, Direction.Right, ArrowColor.Cyan),
                new ArrowData(4, 4, Direction.Up,    ArrowColor.Purple),
                new ArrowData(3, 2, Direction.Left,  ArrowColor.Orange),
                new ArrowData(1, 4, Direction.Right, ArrowColor.Blue),
            },
            showHeartPill   = true,
            showBackButton  = false,
            showHintButton  = false,
            showMagnetButton= false,
            hintIsFree      = false,
            magnetIsFree    = false,
            reward          = RewardType.Book,
            galleryKey      = "book",
            hasGalleryReward= true,
            chapterName     = "Hills",
            starsForChapter = 5
        };

        // ─────────────────────────────────────────────────────
        // LEVEL 3  –  Love (Heart) shape puzzle
        //             UI: heart pill still visible
        // ─────────────────────────────────────────────────────
        private static LevelData BuildLevel3() => new LevelData
        {
            levelIndex      = 3,
            displayName     = "Level 3",
            boardShape      = BoardShape.Heart,
            maskFileName    = "mask_heart.png",
            rewardImageName = "love.png",
            gridCols        = 7,
            gridRows        = 7,
            arrows = new List<ArrowData>
            {
                new ArrowData(1, 4, Direction.Up,    ArrowColor.Pink),
                new ArrowData(5, 4, Direction.Up,    ArrowColor.Orange),
                new ArrowData(2, 2, Direction.Left,  ArrowColor.Blue),
                new ArrowData(4, 2, Direction.Right, ArrowColor.Cyan),
                new ArrowData(3, 1, Direction.Down,  ArrowColor.Purple),
                new ArrowData(3, 5, Direction.Down,  ArrowColor.Pink),
            },
            showHeartPill   = true,
            showBackButton  = false,
            showHintButton  = false,
            showMagnetButton= false,
            hintIsFree      = false,
            magnetIsFree    = false,
            reward          = RewardType.Heart,
            galleryKey      = "love",
            hasGalleryReward= true,
            chapterName     = "Hills",
            starsForChapter = 5
        };

        // ─────────────────────────────────────────────────────
        // LEVEL 4  –  Dog shape puzzle, Orange + Magenta
        //             UI: back + hint (FREE) unlocked
        // ─────────────────────────────────────────────────────
        private static LevelData BuildLevel4() => new LevelData
        {
            levelIndex      = 4,
            displayName     = "Level 4",
            boardShape      = BoardShape.Rectangle,
            maskFileName    = "mask_dog.png",
            rewardImageName = "dog.png",
            gridCols        = 7,
            gridRows        = 7,
            arrows = new List<ArrowData>
            {
                new ArrowData(2, 1, Direction.Down,  ArrowColor.Orange),
                new ArrowData(1, 4, Direction.Left,  ArrowColor.Magenta),
                new ArrowData(3, 5, Direction.Up,    ArrowColor.Orange),
                new ArrowData(5, 4, Direction.Right, ArrowColor.Magenta),
                new ArrowData(3, 2, Direction.Down,  ArrowColor.Orange),
                new ArrowData(4, 3, Direction.Right, ArrowColor.Magenta),
                new ArrowData(2, 3, Direction.Up,    ArrowColor.Orange),
            },
            showHeartPill   = true,
            showBackButton  = true,
            showHintButton  = true,
            showMagnetButton= false,
            hintIsFree      = true,
            magnetIsFree    = false,
            reward          = RewardType.Dog,
            galleryKey      = "dog",
            hasGalleryReward= true,
            chapterName     = "Hills",
            starsForChapter = 5
        };

        // ─────────────────────────────────────────────────────
        // LEVEL 5  –  5×5 rectangle, Orange + Magenta
        //             UI: back + hint (paid 650)
        // ─────────────────────────────────────────────────────
        private static LevelData BuildLevel5() => new LevelData
        {
            levelIndex   = 5,
            displayName  = "Level 5",
            boardShape   = BoardShape.Rectangle,
            gridCols     = 5,
            gridRows     = 5,
            arrows = new List<ArrowData>
            {
                new ArrowData(0, 0, Direction.Right,  ArrowColor.Orange),
                new ArrowData(1, 0, Direction.Down,   ArrowColor.Magenta),
                new ArrowData(2, 0, Direction.Left,   ArrowColor.Orange),
                new ArrowData(3, 0, Direction.Down,   ArrowColor.Magenta),
                new ArrowData(4, 0, Direction.Left,   ArrowColor.Orange),

                new ArrowData(0, 1, Direction.Down,   ArrowColor.Magenta),
                new ArrowData(1, 1, Direction.Right,  ArrowColor.Orange),
                new ArrowData(2, 1, Direction.Up,     ArrowColor.Magenta),
                new ArrowData(3, 1, Direction.Left,   ArrowColor.Orange),
                new ArrowData(4, 1, Direction.Down,   ArrowColor.Magenta),

                new ArrowData(0, 2, Direction.Right,  ArrowColor.Orange),
                new ArrowData(1, 2, Direction.Down,   ArrowColor.Magenta),
                new ArrowData(2, 2, Direction.Left,   ArrowColor.Orange),
                new ArrowData(3, 2, Direction.Up,     ArrowColor.Magenta),
                new ArrowData(4, 2, Direction.Left,   ArrowColor.Orange),

                new ArrowData(0, 3, Direction.Up,     ArrowColor.Magenta),
                new ArrowData(1, 3, Direction.Left,   ArrowColor.Orange),
                new ArrowData(2, 3, Direction.Down,   ArrowColor.Magenta),
                new ArrowData(3, 3, Direction.Right,  ArrowColor.Orange),
                new ArrowData(4, 3, Direction.Up,     ArrowColor.Magenta),

                new ArrowData(0, 4, Direction.Right,  ArrowColor.Orange),
                new ArrowData(1, 4, Direction.Up,     ArrowColor.Magenta),
                new ArrowData(2, 4, Direction.Right,  ArrowColor.Orange),
                new ArrowData(3, 4, Direction.Left,   ArrowColor.Magenta),
                new ArrowData(4, 4, Direction.Up,     ArrowColor.Orange),
            },
            showHeartPill   = true,
            showBackButton  = true,
            showHintButton  = true,
            showMagnetButton= false,
            hintIsFree      = false,
            magnetIsFree    = false,
            reward          = RewardType.Tablet,   // second reward cycle
            galleryKey      = "gallery_5",
            hasGalleryReward= true,
            chapterName     = "Hills",
            starsForChapter = 5
        };

        // ─────────────────────────────────────────────────────
        // LEVEL 6  –  HEART-SHAPE board, gradient colours
        //             UI: back + hint (paid) + MAGNET (FREE)
        //
        // Heart shape (7-wide, 6-tall bounding box).
        // Valid cells encoded as (col, row) pairs.
        // ─────────────────────────────────────────────────────
        private static LevelData BuildLevel6()
        {
            // Define the heart-shaped valid cells.
            // Visual heart on a 7×6 grid (0-based):
            //       col: 0 1 2 3 4 5 6
            // row 0:     . X X . X X .
            // row 1:     X X X X X X X
            // row 2:     X X X X X X X
            // row 3:     . X X X X X .
            // row 4:     . . X X X . .
            // row 5:     . . . X . . .
            var valid = new HashSet<Vector2Int>
            {
                // row 0
                new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(4,0), new Vector2Int(5,0),
                // row 1
                new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
                new Vector2Int(3,1), new Vector2Int(4,1), new Vector2Int(5,1), new Vector2Int(6,1),
                // row 2
                new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2),
                new Vector2Int(3,2), new Vector2Int(4,2), new Vector2Int(5,2), new Vector2Int(6,2),
                // row 3
                new Vector2Int(1,3), new Vector2Int(2,3), new Vector2Int(3,3),
                new Vector2Int(4,3), new Vector2Int(5,3),
                // row 4
                new Vector2Int(2,4), new Vector2Int(3,4), new Vector2Int(4,4),
                // row 5
                new Vector2Int(3,5),
            };

            // Assign gradient colours: Orange top-left → Magenta mid → Purple bottom-right.
            // Simple mapping: row+col < 4 → OrangeGrad, 4-7 → MagentaGrad, >7 → PurpleGrad.
            ArrowColor HeartColor(int col, int row)
            {
                int sum = col + row;
                if (sum < 4)  return ArrowColor.GradientHeartOrange;
                if (sum < 8)  return ArrowColor.GradientHeartMagenta;
                return ArrowColor.GradientHeartPurple;
            }

            var arrows = new List<ArrowData>();
            // Fill every valid cell with an arrow (direction varies to create a solvable puzzle).
            Direction[] dirs = { Direction.Right, Direction.Down, Direction.Left, Direction.Up };
            int idx = 0;
            foreach (var cell in valid)
            {
                arrows.Add(new ArrowData(
                    cell.x, cell.y,
                    dirs[idx % dirs.Length],
                    HeartColor(cell.x, cell.y)
                ));
                idx++;
            }

            return new LevelData
            {
                levelIndex      = 6,
                displayName     = "Level 6",
                boardShape      = BoardShape.Heart,
                gridCols        = 7,
                gridRows        = 6,
                validCells      = valid,
                arrows          = arrows,
                showHeartPill   = true,
                showBackButton  = true,
                showHintButton  = true,
                showMagnetButton= true,
                hintIsFree      = false,
                magnetIsFree    = true,
                reward          = RewardType.Heart,
                galleryKey      = "gallery_6",
                hasGalleryReward= true,
                chapterName     = "Hills",
                starsForChapter = 5
            };
        }
    }
}

