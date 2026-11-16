// ============================================================
// ArrowData.cs
// Describes an arrow on the grid.
// An arrow occupies a path of cells from tail (index 0) to head (last index).
// Used both for level definitions and runtime state.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace ArrowPuzzle.Core
{
    /// <summary>
    /// Visual colour theme of an arrow tile.
    /// Corresponds to the per-level colour rules in the PRD + video corrections.
    /// </summary>
    public enum ArrowColor
    {
        Pink    = 0,
        Orange  = 1,
        Blue    = 2,
        Cyan    = 3,
        Purple  = 4,
        Magenta = 5,
        // Special: used only on the heart-shape level (L6) gradient arrows
        GradientHeartOrange  = 6,
        GradientHeartMagenta = 7,
        GradientHeartPurple  = 8
    }

    /// <summary>
    /// Describes one arrow in a puzzle level.
    /// Cells are listed tail-to-head (index 0 = tail, last = head).
    /// </summary>
    [System.Serializable]
    public class ArrowData
    {
        /// <summary>
        /// Cells from tail (index 0) to head (last index), in grid coordinates.
        /// </summary>
        public List<Vector2Int> cells = new List<Vector2Int>();

        /// <summary>Which way the head of this arrow points.</summary>
        public Direction direction;

        /// <summary>
        /// 0-based index into palette (0=Orange, 1=Pink, 2=Purple, 3=Blue, 4=Cyan).
        /// Used when useGradient = false.
        /// </summary>
        public int colorIndex;

        /// <summary>
        /// If true, this arrow takes its colour from the level-wide
        /// orange → magenta → purple gradient according to gradientT.
        /// </summary>
        public bool useGradient;

        /// <summary>
        /// Normalised position in the level (0 to 1) for gradient colour evaluation.
        /// </summary>
        [System.NonSerialized]
        public float gradientT;

        /// <summary>
        /// True if this arrow can be slid out in one move right now
        /// (no other arrow blocks its path in the slide direction).
        /// Computed at runtime by the puzzle solver — not stored in level defs.
        /// </summary>
        [System.NonSerialized]
        public bool isFree;

        // ── Convenience properties ────────────────────────────
        public Vector2Int Head => (cells != null && cells.Count > 0) ? cells[cells.Count - 1] : new Vector2Int(col, row);
        public Vector2Int Tail => (cells != null && cells.Count > 0) ? cells[0] : new Vector2Int(col, row);
        public Vector2Int GridPos => Head;

        // Legacy accessors for single-cell compatibility
        public int col
        {
            get => (cells != null && cells.Count > 0) ? cells[cells.Count - 1].x : _legacyCol;
            set { _legacyCol = value; SyncLegacy(); }
        }
        public int row
        {
            get => (cells != null && cells.Count > 0) ? cells[cells.Count - 1].y : _legacyRow;
            set { _legacyRow = value; SyncLegacy(); }
        }
        private int _legacyCol;
        private int _legacyRow;

        public ArrowColor color
        {
            get
            {
                if (useGradient) return ArrowColor.GradientHeartOrange;
                return colorIndex switch
                {
                    0 => ArrowColor.Orange,
                    1 => ArrowColor.Pink,
                    2 => ArrowColor.Purple,
                    3 => ArrowColor.Blue,
                    4 => ArrowColor.Cyan,
                    _ => ArrowColor.Orange
                };
            }
            set
            {
                colorIndex = value switch
                {
                    ArrowColor.Orange  => 0,
                    ArrowColor.Pink    => 1,
                    ArrowColor.Purple  => 2,
                    ArrowColor.Blue    => 3,
                    ArrowColor.Cyan    => 4,
                    ArrowColor.Magenta => 1,
                    _                  => 0
                };
            }
        }

        // ── Constructors ──────────────────────────────────────

        public ArrowData()
        {
            cells = new List<Vector2Int>();
        }

        /// <summary>Multi-cell constructor with explicit palette colorIndex.</summary>
        public ArrowData(List<Vector2Int> cells, Direction dir, int colorIndex)
        {
            this.cells       = cells ?? new List<Vector2Int>();
            this.direction   = dir;
            this.colorIndex  = colorIndex;
            this.useGradient = false;
            this.isFree      = false;
        }

        /// <summary>Multi-cell constructor with gradient flag.</summary>
        public ArrowData(List<Vector2Int> cells, Direction dir, bool gradient)
        {
            this.cells       = cells ?? new List<Vector2Int>();
            this.direction   = dir;
            this.colorIndex  = 0;
            this.useGradient = gradient;
            this.isFree      = false;
        }

        /// <summary>Legacy single-cell constructor.</summary>
        public ArrowData(int col, int row, Direction dir, ArrowColor color)
        {
            this._legacyCol  = col;
            this._legacyRow  = row;
            this.cells       = new List<Vector2Int> { new Vector2Int(col, row) };
            this.direction   = dir;
            this.color       = color;
            this.isFree      = false;
        }

        private void SyncLegacy()
        {
            if (cells == null) cells = new List<Vector2Int>();
            if (cells.Count == 0) cells.Add(new Vector2Int(_legacyCol, _legacyRow));
            else cells[cells.Count - 1] = new Vector2Int(_legacyCol, _legacyRow);
        }

        public override string ToString() =>
            $"Arrow[cells={cells.Count} Head={Head} {direction} {(useGradient ? "Gradient" : $"Color={colorIndex}")}]";
    }
}

