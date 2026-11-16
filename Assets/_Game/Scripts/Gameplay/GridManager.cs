// ============================================================
// GridManager.cs  –  Arrow Slide Puzzle
// Manages the logical cell grid: size calculation, coordinate
// conversion, and cell-centre dot visualisation.
//
// Place on a GameObject in the Game scene.
// Inspector-exposed fields let you tweak grid size at runtime.
// ============================================================

using System.Collections;
using UnityEngine;

namespace ArrowPuzzle.Gameplay
{
    public class GridManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static GridManager Instance { get; private set; }

        // ── Inspector fields ──────────────────────────────────
        [Header("Grid Size (editable in Play Mode)")]
        [Range(2, 25)] public int gridCols = 7;
        [Range(2, 25)] public int gridRows = 9;

        [Header("Safe-area padding (world units / pixel ratios)")]
        [Tooltip("Reserved space at the top for the HUD bar (~140px in 1080x1920).")]
        public float topPadding    = 1.0f;
        [Tooltip("Reserved space on the right for power buttons (~190px in 1080x1920).")]
        public float rightPadding  = 1.1f;
        [Tooltip("Reserved space on the left margin.")]
        public float leftPadding   = 0.25f;
        [Tooltip("Reserved space at the bottom of the grid area.")]
        public float bottomPadding = 0.5f;
        [Tooltip("Legacy left/right margin fallback.")]
        public float sidePadding   = 0.5f;

        // ── Runtime read-only ────────────────────────────────
        /// <summary>World-space size of one square cell.</summary>
        public float CellSize { get; private set; }

        /// <summary>World-space origin of cell (0,0) – bottom-left corner.</summary>
        public Vector2 GridOrigin { get; private set; }

        /// <summary>Active cells belonging to the current shape mask (null = all rectangular cells).</summary>
        public System.Collections.Generic.HashSet<Vector2Int> ActiveShapeCells { get; private set; }

        // ── Private ───────────────────────────────────────────
        private Camera _cam;

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _cam = Camera.main;
            Recalculate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

#if UNITY_EDITOR
        // Allow live tweaking in the Inspector during Play Mode
        private int _prevCols, _prevRows;
        private void Update()
        {
            if (gridCols != _prevCols || gridRows != _prevRows)
            {
                Recalculate();
                GetComponent<DotGrid>()?.Rebuild();
            }
        }
#endif

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Recalculates CellSize and GridOrigin so the board fits the portrait screen
        /// using the Inspector padding fields (topPadding, rightPadding, leftPadding, bottomPadding).
        /// Grid is centered in the playable area, slightly below screen centre.
        /// </summary>
        public void Recalculate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float camH = _cam.orthographicSize * 2f;
            float camW = camH * _cam.aspect;

            float effTop    = Mathf.Max(topPadding,    (140f / 1920f) * camH);
            float effRight  = Mathf.Max(rightPadding,  (190f / 1080f) * camW);
            float effLeft   = Mathf.Max(leftPadding,   ( 24f / 1080f) * camW);
            float effBottom = Mathf.Max(bottomPadding, ( 48f / 1920f) * camH);

            float availW = camW - effLeft - effRight;
            float availH = camH - effTop  - effBottom;

            float cellByW = availW / Mathf.Max(1, gridCols);
            float cellByH = availH / Mathf.Max(1, gridRows);
            float rawCell = Mathf.Min(cellByW, cellByH);

            float maxCell = ArrowPuzzle.Core.ArrowVisuals.UNIT_MAX * ArrowPuzzle.Core.ArrowVisuals.CELL_MAX_RATIO;
            CellSize = Mathf.Min(rawCell, maxCell);

            float gridW = CellSize * gridCols;
            float gridH = CellSize * gridRows;

            float playableCenterX = (_cam.transform.position.x - camW * 0.5f) + effLeft + (availW * 0.5f);
            float targetCenterY   = _cam.transform.position.y - (0.02f * camH);
            float maxCenterY = (_cam.transform.position.y + camH * 0.5f) - effTop    - (gridH * 0.5f);
            float minCenterY = (_cam.transform.position.y - camH * 0.5f) + effBottom + (gridH * 0.5f);
            float gridCenterY = Mathf.Clamp(targetCenterY, minCenterY, maxCenterY);

            GridOrigin = new Vector2(playableCenterX - gridW * 0.5f, gridCenterY - gridH * 0.5f);

#if UNITY_EDITOR
            _prevCols = gridCols;
            _prevRows = gridRows;
#endif
        }

        // ── Coordinate conversion ─────────────────────────────

        /// <summary>Returns the world-space centre of grid cell (col, row).</summary>
        public Vector2 CellToWorld(int col, int row)
        {
            return GridOrigin + new Vector2(
                col * CellSize + CellSize * 0.5f,
                row * CellSize + CellSize * 0.5f);
        }

        /// <summary>
        /// Returns the grid cell (col, row) that contains the given world position.
        /// Does NOT clamp — caller must check bounds.
        /// </summary>
        public Vector2Int WorldToCell(Vector2 worldPos)
        {
            Vector2 local = worldPos - GridOrigin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / CellSize),
                Mathf.FloorToInt(local.y / CellSize));
        }

        /// <summary>Returns true if (col, row) is inside the grid rectangle.</summary>
        public bool IsInBounds(int col, int row)
            => col >= 0 && col < gridCols && row >= 0 && row < gridRows;

        /// <summary>Returns true if (col, row) is inside the active shape mask.</summary>
        public bool IsCellInShape(int col, int row)
            => IsInBounds(col, row) && (ActiveShapeCells == null || ActiveShapeCells.Contains(new Vector2Int(col, row)));

        /// <summary>
        /// Updates the grid dimensions and optional shape mask, recalculating layout and updating DotGrid.
        /// </summary>
        public void SetGridDimensions(int cols, int rows, System.Collections.Generic.HashSet<Vector2Int> shapeCells = null)
        {
            gridCols = cols;
            gridRows = rows;
            ActiveShapeCells = (shapeCells != null && shapeCells.Count > 0) ? shapeCells : null;
            Recalculate();
            GetComponent<DotGrid>()?.RebuildWithShape(shapeCells);
        }

        // ── Gizmos ───────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            for (int c = 0; c < gridCols; c++)
                for (int r = 0; r < gridRows; r++)
                {
                    Vector2 centre = CellToWorld(c, r);
                    Gizmos.DrawWireCube(centre, Vector3.one * CellSize * 0.95f);
                }
        }
#endif
    }
}

