// ============================================================
// DotGrid.cs  –  Arrow Slide Puzzle
// Renders the dot at the centre of every grid cell using
// Graphics.DrawMeshInstanced so the entire grid is a single
// draw call regardless of size (up to 1023 dots per batch).
//
// Requires a companion GridManager on the same GameObject.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowPuzzle.Gameplay
{
    [RequireComponent(typeof(GridManager))]
    public class DotGrid : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Dot appearance")]
        [Range(0.02f, 0.4f)]
        [Tooltip("Dot radius as a fraction of CellSize.")]
        public float dotRadiusFraction = 0.08f;

        [Range(0f, 1f)]
        [Tooltip("Target alpha of the dots (PRD: ~30 %).")]
        public float dotAlpha = 0.30f;

        [Header("Fade-in")]
        public float fadeInDuration = 0.3f;

        // ── Private ───────────────────────────────────────────
        private GridManager          _grid;
        private Mesh                 _dotMesh;
        private Material             _dotMaterial;
        private Matrix4x4[][]        _batches;        // GPU instancing batches
        private MaterialPropertyBlock _mpb;
        private float                _currentAlpha = 0f;
        private bool                 _fading       = false;
        private HashSet<Vector2Int>  _activeShapeCells = null;

        // Unity's DrawMeshInstanced cap per call
        private const int BatchSize = 1023;

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            _grid = GetComponent<GridManager>();
            _mpb  = new MaterialPropertyBlock();
            CreateDotMesh();
            CreateDotMaterial();
        }

        private void Start()
        {
            Rebuild();
            StartCoroutine(FadeIn());
        }

        private void Update()
        {
            if (_batches == null) return;

            // Push current alpha to material
            _mpb.SetColor("_Color", new Color(1f, 1f, 1f, _currentAlpha));

            foreach (var batch in _batches)
                Graphics.DrawMeshInstanced(
                    _dotMesh, 0, _dotMaterial,
                    batch,    batch.Length,
                    _mpb);
        }

        // ── Public ────────────────────────────────────────────

        /// <summary>
        /// Updates the shape mask and rebuilds dots only for cells inside the shape.
        /// </summary>
        public void RebuildWithShape(HashSet<Vector2Int> shapeCells)
        {
            _activeShapeCells = shapeCells;
            Rebuild();
        }

        /// <summary>Rebuilds all dot transforms after a grid resize.</summary>
        public void Rebuild()
        {
            // Collect cells to draw: only inside shape if mask is provided
            var cellsToDraw = new List<Vector2Int>();
            for (int r = 0; r < _grid.gridRows; r++)
            {
                for (int c = 0; c < _grid.gridCols; c++)
                {
                    var pos = new Vector2Int(c, r);
                    if (_activeShapeCells == null || _activeShapeCells.Contains(pos))
                    {
                        cellsToDraw.Add(pos);
                    }
                }
            }

            int total = cellsToDraw.Count;
            if (total == 0)
            {
                _batches = new Matrix4x4[0][];
                return;
            }

            int batchCount = Mathf.CeilToInt((float)total / BatchSize);
            _batches = new Matrix4x4[batchCount][];

            int idx = 0;
            for (int b = 0; b < batchCount; b++)
            {
                int count = Mathf.Min(BatchSize, total - b * BatchSize);
                _batches[b] = new Matrix4x4[count];

                for (int i = 0; i < count; i++, idx++)
                {
                    Vector2Int cell = cellsToDraw[idx];
                    Vector2 centre = _grid.CellToWorld(cell.x, cell.y);
                    float   radius = _grid.CellSize * dotRadiusFraction;

                    _batches[b][i] = Matrix4x4.TRS(
                        new Vector3(centre.x, centre.y, 0f),
                        Quaternion.identity,
                        new Vector3(radius * 2f, radius * 2f, 1f));
                }
            }
        }

        // ── Private helpers ───────────────────────────────────

        private IEnumerator FadeIn()
        {
            _currentAlpha = 0f;
            _fading = true;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                _currentAlpha = Mathf.Lerp(0f, dotAlpha, elapsed / fadeInDuration);
                yield return null;
            }

            _currentAlpha = dotAlpha;
            _fading = false;
        }

        /// <summary>Creates a simple quad mesh for the dot.</summary>
        private void CreateDotMesh()
        {
            // A quad made of two triangles – the shader will round it into a circle
            _dotMesh = new Mesh { name = "DotQuad" };
            _dotMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            };
            _dotMesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            _dotMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            _dotMesh.RecalculateNormals();
            _dotMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        }

        /// <summary>
        /// Creates a material that uses the built-in Sprites/Default shader
        /// (supports GPU instancing) and renders a soft circle via UV.
        /// </summary>
        private void CreateDotMaterial()
        {
            // Sprites/Default supports _Color tint and GPU instancing
            _dotMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                enableInstancing = true,
                name             = "DotInstancedMat"
            };
            _dotMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
            // Render on top of background, behind arrows
            _dotMaterial.renderQueue = 2500;
        }

        private void OnDestroy()
        {
            if (_dotMesh    != null) Destroy(_dotMesh);
            if (_dotMaterial != null) Destroy(_dotMaterial);
        }
    }
}

