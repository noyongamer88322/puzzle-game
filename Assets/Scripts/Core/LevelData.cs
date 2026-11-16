// ============================================================
// LevelData.cs
// Complete definition of one puzzle level.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace ArrowPuzzle.Core
{
    /// <summary>
    /// Shape of the grid/board for a level.
    /// </summary>
    public enum BoardShape
    {
        /// <summary>Standard rectangular grid.</summary>
        Rectangle,
        /// <summary>Heart-shaped grid used in level 6.</summary>
        Heart
    }

    /// <summary>
    /// Which reward item is revealed when the level is cleared.
    /// Video correction: order is tablet → phone → book → heart (across first 6 levels).
    /// </summary>
    public enum RewardType
    {
        None   = 0,
        Tablet = 1,
        Phone  = 2,
        Book   = 3,
        Heart  = 4,
        Dog    = 5
    }

    /// <summary>
    /// Immutable definition of one puzzle level.
    /// All levels for the prototype are hard-coded in LevelDatabase.
    /// </summary>
    [System.Serializable]
    public class LevelData
    {
        // ── Identity ──────────────────────────────────────────
        /// <summary>1-based level number shown in the UI.</summary>
        public int levelIndex;

        /// <summary>Level identifier (synced with levelIndex).</summary>
        public int id;

        /// <summary>Display name shown on the Map screen (e.g. "Level 1").</summary>
        public string displayName;

        // ── Shape Mask & File Info ───────────────────────────
        /// <summary>Mask file name in Masks/ folder (e.g. "mask_heart.png").</summary>
        public string maskFileName;

        /// <summary>Reward picture file name in Gallery/ (e.g. "pic_heart.png").</summary>
        public string rewardImageName;

        /// <summary>Arrow colour scheme: "mixed", "orangeMagenta", or "gradient".</summary>
        public string colorMode = "mixed";

        // ── Board ─────────────────────────────────────────────
        public BoardShape boardShape;

        /// <summary>Number of columns in the grid (synced with gridWidth).</summary>
        public int gridCols;

        /// <summary>Grid width in columns.</summary>
        public int gridWidth;

        /// <summary>Number of rows in the grid (synced with gridHeight).</summary>
        public int gridRows;

        /// <summary>Grid height in rows.</summary>
        public int gridHeight;

        /// <summary>
        /// For non-rectangle boards: set of valid (col,row) cells.
        /// Empty means all cells in the gridCols×gridRows rectangle are valid.
        /// </summary>
        public HashSet<Vector2Int> validCells;

        // ── Arrows ────────────────────────────────────────────
        public List<ArrowData> arrows;

        // ── UI / Feature flags ────────────────────────────────
        /// <summary>Show heart/life pill in the HUD.</summary>
        public bool showHeartPill;

        /// <summary>Show the Back button.</summary>
        public bool showBackButton;

        /// <summary>Show the Hint button.</summary>
        public bool showHintButton;

        /// <summary>Show the Magnet button (only for the heart-shape level).</summary>
        public bool showMagnetButton;

        /// <summary>Hint button is free on first use in this level.</summary>
        public bool hintIsFree;

        /// <summary>Magnet button is free on first use in this level.</summary>
        public bool magnetIsFree;

        // Aliases matching PRD specifications
        public bool showHearts
        {
            get => showHeartPill;
            set => showHeartPill = value;
        }

        public bool showBack
        {
            get => showBackButton;
            set => showBackButton = value;
        }

        public bool showHint
        {
            get => showHintButton;
            set => showHintButton = value;
        }

        public bool showMagnet
        {
            get => showMagnetButton;
            set => showMagnetButton = value;
        }

        // ── Reward ────────────────────────────────────────────
        public RewardType reward;

        /// <summary>Key into the gallery sprite atlas (e.g. "gallery_1").</summary>
        public string galleryKey;

        /// <summary>True if clearing this level unlocks a gallery picture.</summary>
        public bool hasGalleryReward;

        // ── Chapter info ──────────────────────────────────────
        public string chapterName;    // e.g. "Hills"
        public string chapter;        // Synced with chapterName
        public int    starsForChapter; // total stars needed to complete chapter

        // ── Helpers ───────────────────────────────────────────
        /// <summary>Is the given cell position valid on this board?</summary>
        public bool IsCellValid(int col, int row)
        {
            if (validCells != null && validCells.Count > 0)
                return validCells.Contains(new Vector2Int(col, row));
            // Default: any cell inside the rectangle is valid
            return col >= 0 && col < gridCols && row >= 0 && row < gridRows;
        }

        /// <summary>Returns all arrows that are currently free (can slide out).</summary>
        public List<ArrowData> GetFreeArrows()
        {
            var result = new List<ArrowData>();
            foreach (var arrow in arrows)
                if (arrow.isFree) result.Add(arrow);
            return result;
        }

        /// <summary>
        /// Deserializes a LevelData object from JSON string using LevelDataJson.
        /// </summary>
        public static LevelData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var dto = JsonUtility.FromJson<LevelDataJson>(json);
            return dto?.ToRuntimeLevelData();
        }
    }

    // ── JSON Serialization DTOs ───────────────────────────────

    [System.Serializable]
    public class LevelDataJson
    {
        public int id;
        public string chapter;
        public string maskFileName;
        public int gridWidth;
        public int gridHeight;
        public string rewardImageName;
        public string colorMode;
        public List<ArrowJsonData> arrows = new List<ArrowJsonData>();

        public LevelData ToRuntimeLevelData()
        {
            var data = new LevelData
            {
                id              = id,
                levelIndex      = id,
                displayName     = "Level " + id,
                chapter         = chapter,
                chapterName     = chapter,
                maskFileName    = maskFileName,
                rewardImageName = rewardImageName,
                colorMode       = string.IsNullOrEmpty(colorMode) ? "mixed" : colorMode,
                gridWidth       = gridWidth,
                gridCols        = gridWidth,
                gridHeight      = gridHeight,
                gridRows        = gridHeight,
                arrows          = new List<ArrowData>()
            };

            if (arrows != null)
            {
                for (int i = 0; i < arrows.Count; i++)
                {
                    var aDto = arrows[i];
                    var arrow = new ArrowData
                    {
                        direction  = ParseDirection(aDto.direction),
                        colorIndex = aDto.colorIndex,
                        cells      = new List<Vector2Int>()
                    };

                    if (aDto.cells != null)
                    {
                        foreach (var c in aDto.cells)
                            arrow.cells.Add(new Vector2Int(c.x, c.y));
                    }

                    // Apply colorMode rules
                    if (data.colorMode == "gradient")
                    {
                        arrow.useGradient = true;
                        arrow.gradientT   = arrows.Count > 1 ? (float)i / (arrows.Count - 1) : 0f;
                    }
                    else if (data.colorMode == "orangeMagenta")
                    {
                        arrow.useGradient = false;
                        // Alternate or match colorIndex (0=Orange, 1=Pink/Magenta)
                        arrow.colorIndex = aDto.colorIndex % 2;
                    }
                    else
                    {
                        arrow.useGradient = false;
                        arrow.colorIndex  = aDto.colorIndex;
                    }

                    data.arrows.Add(arrow);
                }
            }

            // Set UI feature flags (PRD Corrections 3 / Section 4):
            // L1: coin + gear only
            // L2-3: + heart pill
            // L4+: + back button + hint button (free first time on L4)
            // L6: + magnet button (free first time on L6)
            var db = LevelDatabase.Get(id);
            if (db != null)
            {
                data.showHeartPill    = db.showHeartPill;
                data.showBackButton   = db.showBackButton;
                data.showHintButton   = db.showHintButton;
                data.showMagnetButton = db.showMagnetButton;
                data.hintIsFree       = db.hintIsFree;
                data.magnetIsFree     = db.magnetIsFree;
            }
            else
            {
                data.showHeartPill    = (id >= 2);
                data.showBackButton   = (id >= 4);
                data.showHintButton   = (id >= 4);
                data.showMagnetButton = (id == 6 || string.Equals(maskFileName, "mask_heart.png", System.StringComparison.OrdinalIgnoreCase));
                data.hintIsFree       = (id == 4);
                data.magnetIsFree     = (id == 6);
            }

            return data;
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

    [System.Serializable]
    public class ArrowJsonData
    {
        public List<CellJson> cells = new List<CellJson>();
        public string direction;
        public string color;
        public int colorIndex;
    }

    [System.Serializable]
    public class CellJson
    {
        public int x;
        public int y;

        public CellJson() { }
        public CellJson(int x, int y) { this.x = x; this.y = y; }
    }
}

