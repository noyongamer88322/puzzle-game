// ============================================================
// ArrowView.cs  –  Arrow Slide Puzzle
// Visual representation of a single SnakeArrowData.
//
// Creates two child MeshRenderers:
//   • _bodyGO  – the rounded stroke (body + tail-cap)
//   • _glowGO  – soft additive glow (same shape, wider, low alpha) [disabled by default]
//   • _headGO  – the arrowhead triangle
//
// Visual constants (stroke width, head size, glow, colors) are all
// controlled from ArrowVisuals in GameConstants.cs — edit there, not here.
//
// KEY: Arrow thickness uses ArrowVisuals.ComputeHalfWidth(cellSize) which
// clamps the visual unit to [UNIT_MIN, UNIT_MAX] so arrows stay the same
// elegant weight on both sparse (large cell) and dense (small cell) levels.
//
// Call Init() once then PlaySpawnAnim() to trigger the
// staggered scale/fade-in.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    public class ArrowView : MonoBehaviour
    {
        // ── Inspector tunables ────────────────────────────────
        [Header("Stroke")]
        [Tooltip("Deprecated: arrow thickness is now computed from ArrowVisuals.ComputeHalfWidth(cellSize). " +
                 "Visual unit U = Clamp(cellSize, UNIT_MIN, UNIT_MAX); halfWidth = U * STROKE_RATIO * 0.5. " +
                 "This field is kept for Inspector display only and is not used at runtime.")]
        [Range(0.1f, 0.6f)]
        public float widthFraction = ArrowVisuals.WidthFraction;

        [Header("Glow overlay")]
        [Tooltip("Alpha of the soft additive glow mesh (0 = off). Default from ArrowVisuals.GlowAlpha.")]
        [Range(0f, 1f)]
        public float glowAlpha = ArrowVisuals.GlowAlpha;

        [Tooltip("Glow mesh half-width multiplier (how much bigger than the body). Default from ArrowVisuals.GlowWidthMultiplier.")]
        [Range(1f, 3f)]
        public float glowWidthMultiplier = ArrowVisuals.GlowWidthMultiplier;

        [Header("Corner smoothing")]
        [Range(3, 16)]
        public int cornerSteps = ArrowVisuals.CornerSteps;
        [Header("Spawn animation")]
        public float spawnDuration = 0.35f;

        [Header("Slide animation")]
        [Tooltip("Slide duration in seconds (PRD: ~0.6 - 0.9s).")]
        [Range(0.4f, 1.5f)]
        public float slideDuration = 0.75f;

        // ── Public state ──────────────────────────────────────
        public ArrowData Data          => _data;
        public bool      IsSliding     { get; private set; }
        public bool      IsBlockedAnim { get; private set; }
        public bool      IsCleared     { get; private set; }
        public bool      IsHinted      { get; private set; }

        // ── Runtime ──────────────────────────────────────────
        private ArrowData   _data;
        private GridManager _grid;
        private Coroutine   _hintPulseCoroutine;

        // child GameObjects
        private GameObject _bodyGO;
        private GameObject _glowGO;    // soft glow overlay (replaces old highlight)
        private GameObject _headGO;
        private GameObject _highlightGO;
        private GameObject _colliderGO;
        private GameObject _trailGO;
        private TrailRenderer _trailRenderer;

        private Mesh         _bodyMesh;
        private Mesh         _headMesh;
        private Mesh         _glowMesh;
        private Mesh         _highlightMesh;

        private MeshRenderer _bodyRend;
        private MeshRenderer _headRend;
        private MeshRenderer _glowRend;
        private MeshRenderer _highlightRend;

        private Material     _bodyMat;
        private Material     _headMat;
        private Material     _glowMat;
        private Material     _highlightMat;

        private bool _ready;

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Initialises the view from data. Must be called before PlaySpawnAnim().
        /// </summary>
        public void Init(ArrowData data, GridManager grid)
        {
            _data = data;
            _grid = grid;
            _ready = false;
            IsSliding     = false;
            IsBlockedAnim = false;
            IsCleared     = false;
            IsHinted      = false;
            if (_hintPulseCoroutine != null)
            {
                StopCoroutine(_hintPulseCoroutine);
                _hintPulseCoroutine = null;
            }

            EnsureChildren();
            Rebuild();

            // Start invisible; PlaySpawnAnim will reveal
            SetAlpha(0f);
            transform.localScale = Vector3.zero;
        }

        /// <summary>Plays the scale+fade-in spawn animation.</summary>
        public void PlaySpawnAnim(float delay = 0f)
        {
            StartCoroutine(SpawnCoroutine(delay));
        }

        /// <summary>
        /// Plays the snake-like slide out animation along the arrow's head direction.
        /// Body follows the path of the head. Tail exits screen.
        /// </summary>
        public void SlideOut(System.Action onComplete = null)
        {
            if (IsSliding || IsCleared) return;
            StartCoroutine(SlideCoroutine(onComplete));
        }

        /// <summary>
        /// Plays the blocked animation: turns red (#FF1A1A) for 0.35s and shakes for 0.25s.
        /// </summary>
        public void PlayBlocked(System.Action onComplete = null)
        {
            if (IsSliding || IsBlockedAnim || IsCleared) return;
            StartCoroutine(BlockedCoroutine(onComplete));
        }

        /// <summary>
        /// Highlights this arrow in green (#3FD04A) with a gentle pulse.
        /// The user must tap it themselves to slide it out.
        /// </summary>
        public void SetHinted(bool hinted)
        {
            if (IsSliding || IsCleared) return;
            IsHinted = hinted;

            if (_hintPulseCoroutine != null)
            {
                StopCoroutine(_hintPulseCoroutine);
                _hintPulseCoroutine = null;
            }

            if (hinted)
            {
                ApplyColors();
                _hintPulseCoroutine = StartCoroutine(HintPulseAnim());
            }
            else
            {
                transform.localScale = Vector3.one;
                ApplyColors();
            }
        }

        private IEnumerator HintPulseAnim()
        {
            float elapsed = 0f;
            while (IsHinted && !IsSliding && !IsCleared)
            {
                elapsed += Time.deltaTime;
                float s = 1f + 0.08f * Mathf.Sin(elapsed * 5f);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
            _hintPulseCoroutine = null;
        }

        /// <summary>
        /// Magnet pull animation: pulls the arrow along a curved arc towards the magnet center,
        /// emitting sparkle trail, scaling down, and clearing on arrival.
        /// </summary>
        public void PullToMagnet(Vector3 magnetWorldPos, float delay, System.Action onComplete)
        {
            if (IsSliding || IsCleared) return;
            IsSliding = true;

            if (_hintPulseCoroutine != null)
            {
                StopCoroutine(_hintPulseCoroutine);
                _hintPulseCoroutine = null;
                transform.localScale = Vector3.one;
            }

            var cols = GetComponents<BoxCollider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

            StartCoroutine(MagnetPullCoroutine(magnetWorldPos, delay, onComplete));
        }

        private IEnumerator MagnetPullCoroutine(Vector3 magnetPos, float delay, System.Action onComplete)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            Vector3 startPos = transform.position;
            Vector3 mid = (startPos + magnetPos) * 0.5f;
            Vector3 toMagnet = (magnetPos - startPos).normalized;
            Vector3 perp = new Vector3(-toMagnet.y, toMagnet.x, 0f);
            float side = (startPos.x > magnetPos.x) ? 1f : -1f;
            Vector3 controlPoint = mid + perp * (side * 1.5f);

            // Sparkle Trail for magnet pull
            var pullTrailGO = new GameObject("MagnetPullTrail");
            pullTrailGO.transform.position = startPos;
            var tr = pullTrailGO.AddComponent<TrailRenderer>();
            tr.material = new Material(Shader.Find("Sprites/Default"));
            tr.startWidth = 0.45f;
            tr.endWidth = 0f;
            tr.time = 0.35f;
            tr.sortingOrder = 22;

            Color baseCol = ArrowGradient.Resolve(_data);
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(baseCol, 0.4f), new GradientColorKey(baseCol, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            tr.colorGradient = grad;

            float duration = 0.55f;
            float elapsed = 0f;
            Vector3 initialScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t; // Accelerate into magnet

                // Quadratic Bézier curve
                Vector3 cur = (1 - eased) * (1 - eased) * startPos + 2 * (1 - eased) * eased * controlPoint + eased * eased * magnetPos;
                transform.position = cur;
                pullTrailGO.transform.position = cur;

                // Scale down
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, eased);

                // Rotate towards movement
                Vector3 tangent = 2 * (1 - eased) * (controlPoint - startPos) + 2 * eased * (magnetPos - controlPoint);
                if (tangent.sqrMagnitude > 0.001f)
                {
                    float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }

                yield return null;
            }

            Destroy(pullTrailGO, 0.35f);
            IsCleared = true;
            IsSliding = false;
            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            if (GameplayController.Instance != null)
                GameplayController.Instance.HandleArrowTap(this);
        }

        // ── Private: setup ────────────────────────────────────

        private void EnsureChildren()
        {
            _bodyGO   = GetOrCreate("Body");
            _glowGO   = GetOrCreate("Glow");
            _headGO   = GetOrCreate("Head");

            // Meshes
            _bodyMesh   = new Mesh { name = "ArrowBody" };
            _headMesh   = new Mesh { name = "ArrowHead" };
            _glowMesh   = new Mesh { name = "ArrowGlow" };

            SetupRenderer(_bodyGO, _bodyMesh, out _bodyRend, out _bodyMat,
                          renderQueue: 3000, sortingOrder: 10, additive: false);

            // Glow: additive blend so it brightens without changing hue
            SetupRenderer(_glowGO, _glowMesh, out _glowRend, out _glowMat,
                          renderQueue: 3001, sortingOrder: 9, additive: true);

            SetupRenderer(_headGO, _headMesh, out _headRend, out _headMat,
                          renderQueue: 3002, sortingOrder: 12, additive: false);

            if (ArrowVisuals.HIGHLIGHT_ENABLED)
            {
                _highlightGO = GetOrCreate("Highlight");
                _highlightMesh = new Mesh { name = "ArrowHighlight" };
                SetupRenderer(_highlightGO, _highlightMesh, out _highlightRend, out _highlightMat,
                              renderQueue: 3003, sortingOrder: 11, additive: false);
                _highlightGO.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            }
            else
            {
                var h = transform.Find("Highlight");
                if (h != null) Destroy(h.gameObject);
            }

            // Glow sits at the same Z as body (additive blend; no Z offset needed)
            _glowGO.transform.localPosition = Vector3.zero;
            _headGO.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        }

        private GameObject GetOrCreate(string childName)
        {
            var t = transform.Find(childName);
            if (t != null) return t.gameObject;
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            return go;
        }

        private void SetupRenderer(
            GameObject go, Mesh mesh,
            out MeshRenderer rend, out Material mat,
            int renderQueue, int sortingOrder,
            bool additive = false)
        {
            var mf = go.GetComponent<MeshFilter>();
            mf.mesh = mesh;

            rend = go.GetComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            rend.sortingOrder      = sortingOrder;

            Shader shader;
            if (additive)
            {
                // Additive blend: "Particles/Additive" or fall back to Sprites/Default
                shader = Shader.Find("Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default");
            }
            else
            {
                shader = Shader.Find("Sprites/Default");
            }

            mat = new Material(shader)
            {
                name        = go.name + "Mat",
                renderQueue = renderQueue
            };

            rend.material = mat;
            mat = rend.material;
        }

        // ── Private: geometry ─────────────────────────────────

        private void Rebuild()
        {
            if (_data == null || _grid == null || _data.cells == null || _data.cells.Count < 2)
            {
                _bodyMesh?.Clear();
                _headMesh?.Clear();
                _glowMesh?.Clear();
                return;
            }

            // Compute halfWidth from the clamped visual unit so arrows stay the same
            // elegant weight on sparse (large cell) and dense (small cell) levels alike.
            // U = Clamp(cellSize, UNIT_MIN, UNIT_MAX); halfWidth = U * STROKE_RATIO * 0.5
            float cellSize  = _grid.CellSize;
            float halfWidth = ArrowVisuals.ComputeHalfWidth(cellSize);

            // Compute center of all cells to position the ArrowView at its own origin
            Vector2 sum = Vector2.zero;
            foreach (var cell in _data.cells)
                sum += _grid.CellToWorld(cell.x, cell.y);
            Vector3 center = (Vector3)(sum / _data.cells.Count);
            transform.position = center;

            // Convert cells to local space relative to transform.position
            var localPts = new List<Vector3>();
            foreach (var cell in _data.cells)
            {
                Vector2 wp = _grid.CellToWorld(cell.x, cell.y);
                localPts.Add(new Vector3(wp.x - center.x, wp.y - center.y, 0f));
            }

            // ── Body ──────────────────────────────────────────────────────────
            ArrowMeshBuilder.BuildBodyMesh(_bodyMesh, localPts, halfWidth, cornerSteps, cellSize);

            // ── Soft glow (same shape, wider halfWidth, low alpha additive pass) ──
            if (glowAlpha > 0f)
                ArrowMeshBuilder.BuildBodyMesh(_glowMesh, localPts, halfWidth * glowWidthMultiplier, cornerSteps, cellSize);
            else
                _glowMesh.Clear();

            // ── Head: filled triangle arrowhead pointing in _data.direction ────
            Vector3 headPos = localPts[localPts.Count - 1];
            Vector3 headDir = _data.direction.ToVector3().normalized;
            float headLen   = (halfWidth * 2f) * ArrowVisuals.HEAD_LENGTH_RATIO;
            Vector3 tipPos  = headPos + headDir * headLen;
            ArrowMeshBuilder.BuildHeadMesh(_headMesh, tipPos, headDir, halfWidth);

            // ── Optional highlight stripe ─────────────────────────────────────
            if (ArrowVisuals.HIGHLIGHT_ENABLED && _highlightMesh != null)
            {
                var hlPts = new List<Vector3>();
                // Offset towards upper-left for a soft top-edge glossy sheen
                Vector3 hlOffset = new Vector3(-0.65f, 0.65f, 0f).normalized * (halfWidth * 0.30f);
                foreach (var pt in localPts)
                    hlPts.Add(pt + hlOffset);
                float hlHalf = halfWidth * ArrowVisuals.HIGHLIGHT_WIDTH_RATIO;
                ArrowMeshBuilder.BuildBodyMesh(_highlightMesh, hlPts, hlHalf, cornerSteps, cellSize);
            }

            // ── Tap colliders along the body ──────────────────────────────────
            BuildColliders(localPts, halfWidth);

            // ── Apply colours ─────────────────────────────────────────────────
            ApplyColors();

            _ready = true;
        }

        private void BuildColliders(List<Vector3> localPts, float halfWidth)
        {
            // Clear old colliders
            var oldCols = GetComponents<BoxCollider2D>();
            for (int i = 0; i < oldCols.Length; i++)
                Destroy(oldCols[i]);

            // Collider intentionally kept at CellSize-based width — stays generous for touch input
            // even though the visual stroke is now clamped/slimmer on large cells.
            // Minimum = full cell width so tapping anywhere near an arrow registers.
            float colliderWidth = Mathf.Max(_grid.CellSize * 0.85f, halfWidth * 2.6f);

            // Segment colliders between consecutive points
            for (int i = 0; i < localPts.Count - 1; i++)
            {
                Vector3 a = localPts[i];
                Vector3 b = localPts[i + 1];
                Vector3 mid = (a + b) * 0.5f;
                float dist = Vector3.Distance(a, b);

                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.offset = new Vector2(mid.x, mid.y);

                if (Mathf.Abs(b.y - a.y) < 0.001f)
                    box.size = new Vector2(dist + colliderWidth * 0.5f, colliderWidth);
                else if (Mathf.Abs(b.x - a.x) < 0.001f)
                    box.size = new Vector2(colliderWidth, dist + colliderWidth * 0.5f);
                else
                    box.size = new Vector2(dist + colliderWidth * 0.5f, colliderWidth);
            }

            // Head collider at arrowhead
            Vector3 headPos = localPts[localPts.Count - 1];
            Vector3 headDir = _data.direction.ToVector3().normalized;
            Vector3 tipPos  = headPos + headDir * (halfWidth * 0.8f);
            var headBox = gameObject.AddComponent<BoxCollider2D>();
            headBox.isTrigger = true;
            headBox.offset = new Vector2(tipPos.x, tipPos.y);
            headBox.size = new Vector2(colliderWidth, colliderWidth);
        }

        private void ApplyColors()
        {
            Color baseCol = IsHinted ? new Color(0.247f, 0.816f, 0.29f) : ArrowGradient.Resolve(_data); // Green #3FD04A

            // Body and head use the flat saturated colour
            SetMeshColor(_bodyMesh, baseCol);
            SetMeshColor(_headMesh, baseCol);

            // Glow uses same colour at reduced alpha (additive shader ignores alpha differently;
            // we bake alpha into vertex colours so the mesh itself carries the opacity)
            Color glowCol = baseCol;
            glowCol.a = glowAlpha;
            SetMeshColor(_glowMesh, glowCol);

            if (ArrowVisuals.HIGHLIGHT_ENABLED && _highlightMesh != null)
            {
                // Soft white highlight for glossy tube sheen
                Color hlCol = Color.white;
                hlCol.a = ArrowVisuals.HIGHLIGHT_ALPHA;
                SetMeshColor(_highlightMesh, hlCol);
                _highlightMat?.SetColor("_Color", Color.white);
            }

            _bodyMat.SetColor("_Color", Color.white);
            _headMat.SetColor("_Color", Color.white);
            _glowMat.SetColor("_Color", Color.white);
        }

        private static void SetMeshColor(Mesh mesh, Color c)
        {
            if (mesh == null || mesh.vertexCount == 0) return;
            var cols = new Color[mesh.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = c;
            mesh.colors = cols;
        }

        // ── Private: animation ────────────────────────────────

        private void SetAlpha(float a)
        {
            SetMeshAlpha(_bodyMesh, a);
            SetMeshAlpha(_headMesh, a);
            SetMeshAlpha(_glowMesh, a * glowAlpha);
            if (ArrowVisuals.HIGHLIGHT_ENABLED && _highlightMesh != null)
                SetMeshAlpha(_highlightMesh, a * ArrowVisuals.HIGHLIGHT_ALPHA);
        }

        private static void SetMeshAlpha(Mesh mesh, float a)
        {
            if (mesh == null || mesh.vertexCount == 0) return;
            var cols = mesh.colors;
            for (int i = 0; i < cols.Length; i++) cols[i].a = a;
            mesh.colors = cols;
        }

        private IEnumerator SpawnCoroutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            while (elapsed < spawnDuration)
            {
                float t = elapsed / spawnDuration;
                float eased = EaseOutBack(t);
                transform.localScale = Vector3.one * Mathf.Clamp(eased, 0f, 1.15f);
                SetAlpha(Mathf.Clamp01(t * 2.5f));
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = Vector3.one;
            SetAlpha(1f);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private IEnumerator SlideCoroutine(System.Action onComplete)
        {
            if (IsHinted)
            {
                if (_hintPulseCoroutine != null)
                {
                    StopCoroutine(_hintPulseCoroutine);
                    _hintPulseCoroutine = null;
                }
                transform.localScale = Vector3.one;

                // PRD: Green arrow tap -> white streak line shoots across the screen along exit direction
                Vector3 headWorld = _grid != null && _data != null && _data.cells != null && _data.cells.Count > 0
                    ? (Vector3)_grid.CellToWorld(_data.Head.x, _data.Head.y)
                    : transform.position;
                Vector3 dir = _data != null ? _data.direction.ToVector3().normalized : Vector3.up;

                var streakGO = new GameObject("HintExitStreak");
                streakGO.transform.position = headWorld;
                var streak = streakGO.AddComponent<ExitStreakLine>();
                streak.Launch(headWorld, dir);
            }

            // PRD 5: Arrow tap (free) press feedback (slight squish scale to 0.94 -> snap back)
            AudioManager.Instance?.PlayTap();
            float pressDur = 0.05f;
            float pElapsed = 0f;
            Vector3 initScale = transform.localScale;
            while (pElapsed < pressDur)
            {
                pElapsed += Time.deltaTime;
                float pt = Mathf.Clamp01(pElapsed / pressDur);
                transform.localScale = Vector3.Lerp(initScale, initScale * 0.94f, pt);
                yield return null;
            }
            transform.localScale = initScale;

            IsSliding = true;
            AudioManager.Instance?.PlayWhoosh();
            var cols = GetComponents<BoxCollider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

            // Setup glow trail
            if (_trailGO == null)
            {
                _trailGO = new GameObject("Trail");
                _trailGO.transform.SetParent(transform, false);
                _trailRenderer = _trailGO.AddComponent<TrailRenderer>();
                _trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _trailRenderer.receiveShadows    = false;
                _trailRenderer.sortingOrder      = 8;
                _trailRenderer.material          = new Material(Shader.Find("Sprites/Default"));
                _trailRenderer.time              = 0.35f;
                _trailRenderer.minVertexDistance = 0.05f;
            }

            // Compute halfWidth from the clamped visual unit so arrows stay the same
            // elegant weight on sparse (large cell) and dense (small cell) levels alike.
            // U = Clamp(cellSize, UNIT_MIN, UNIT_MAX); halfWidth = U * STROKE_RATIO * 0.5
            float cellSize  = _grid.CellSize;
            float halfWidth = ArrowVisuals.ComputeHalfWidth(cellSize);
            _trailRenderer.startWidth = halfWidth * 1.8f;
            _trailRenderer.endWidth   = 0f;

            Color baseCol = ArrowGradient.Resolve(_data);
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(baseCol, 0f), new GradientColorKey(baseCol, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.40f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            _trailRenderer.colorGradient = grad;
            _trailRenderer.Clear();

            // Construct world polyline
            var poly = new List<Vector3>();
            foreach (var c in _data.cells)
                poly.Add(_grid.CellToWorld(c.x, c.y));

            Vector3 u = _data.direction.ToVector3().normalized;
            Vector3 worldHead = poly[poly.Count - 1];
            float exitDistance = 30f;
            poly.Add(worldHead + u * exitDistance);

            // Cumulative distances
            var polyDists = new List<float> { 0f };
            float bodyLength = 0f;
            for (int i = 1; i < poly.Count; i++)
            {
                float seg = Vector3.Distance(poly[i - 1], poly[i]);
                polyDists.Add(polyDists[i - 1] + seg);
                if (i < poly.Count - 1)
                    bodyLength += seg;
            }

            // Calculate total travel distance to exit screen
            Camera cam = Camera.main;
            float camDim = cam != null ? cam.orthographicSize * 2.5f : 15f;
            float totalTravel = bodyLength + camDim + _grid.CellSize * 2f;

            float elapsed = 0f;
            _trailGO.transform.position = poly[0];

            while (elapsed < slideDuration)
            {
                float t = elapsed / slideDuration;
                // Ease-in: slow start, accelerates (PRD: ease-in ~0.6-0.9s)
                float easedT = Mathf.Pow(t, 2.2f);

                float sTail = totalTravel * easedT;
                float sHead = sTail + bodyLength;

                List<Vector3> sampled = SamplePolyline(poly, polyDists, sTail, sHead);
                if (sampled.Count >= 2)
                {
                    var localPts = new List<Vector3>();
                    for (int i = 0; i < sampled.Count; i++)
                        localPts.Add(sampled[i] - transform.position);

                    // Rebuild body mesh
                    ArrowMeshBuilder.BuildBodyMesh(_bodyMesh, localPts, halfWidth, cornerSteps, cellSize);

                    // Rebuild glow mesh
                    if (glowAlpha > 0f)
                        ArrowMeshBuilder.BuildBodyMesh(_glowMesh, localPts, halfWidth * glowWidthMultiplier, cornerSteps, cellSize);

                    // Rebuild head mesh
                    float slideHeadLen = (halfWidth * 2f) * ArrowVisuals.HEAD_LENGTH_RATIO;
                    Vector3 tip = localPts[localPts.Count - 1] + u * slideHeadLen;
                    ArrowMeshBuilder.BuildHeadMesh(_headMesh, tip, u, halfWidth);

                    // Reapply colors to rebuilt vertices
                    ApplyColors();

                    // Update trail anchor at tail
                    _trailGO.transform.position = sampled[0];
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // PRD 5: Arrow exit: sparkle effect
            Vector3 exitPos = poly.Count > 0 ? poly[poly.Count - 1] : transform.position;
            if (Camera.main != null)
            {
                // Clamp exit sparkle just around camera viewport edge
                Vector3 vp = Camera.main.WorldToViewportPoint(exitPos);
                vp.x = Mathf.Clamp(vp.x, 0.05f, 0.95f);
                vp.y = Mathf.Clamp(vp.y, 0.05f, 0.95f);
                exitPos = Camera.main.ViewportToWorldPoint(vp);
                exitPos.z = 0f;
            }
            VFXManager.Instance?.SpawnExitSparkle(exitPos, baseCol, 10);

            IsSliding = false;
            IsCleared = true;
            onComplete?.Invoke();
            Destroy(gameObject);
        }

        private static List<Vector3> SamplePolyline(List<Vector3> P, List<float> D, float sStart, float sEnd)
        {
            var res = new List<Vector3>();
            if (sStart >= D[D.Count - 1]) return res;

            int i = 0;
            while (i < D.Count - 1 && D[i + 1] < sStart) i++;
            float segLenI = D[i + 1] - D[i];
            float fracI = segLenI > 0.0001f ? (sStart - D[i]) / segLenI : 0f;
            Vector3 tailPos = Vector3.Lerp(P[i], P[i + 1], fracI);
            res.Add(tailPos);

            int j = i;
            while (j < D.Count - 1 && D[j + 1] < sEnd) j++;
            for (int k = i + 1; k <= j; k++)
            {
                if (Vector3.Distance(res[res.Count - 1], P[k]) > 0.01f)
                    res.Add(P[k]);
            }

            float segLenJ = D[j + 1] - D[j];
            float fracJ = segLenJ > 0.0001f ? (sEnd - D[j]) / segLenJ : 0f;
            Vector3 headPos = Vector3.Lerp(P[j], P[j + 1], fracJ);
            if (Vector3.Distance(res[res.Count - 1], headPos) > 0.01f)
                res.Add(headPos);
            else if (res.Count == 1)
                res.Add(headPos + (P[j + 1] - P[j]).normalized * 0.02f);

            return res;
        }

        private IEnumerator BlockedCoroutine(System.Action onComplete)
        {
            IsBlockedAnim = true;
            Vector3 originalPos = transform.position;

            // Turn red (#FF1A1A)
            Color red = ArrowColors.Blocked;
            SetMeshColor(_bodyMesh, red);
            SetMeshColor(_headMesh, red);

            // Small shake (0.25s)
            float shakeDuration = 0.25f;
            float elapsed = 0f;
            float shakeMag = _grid != null ? _grid.CellSize * 0.10f : 0.08f;
            Vector3 perp = new Vector3(-_data.direction.ToVector().y, _data.direction.ToVector().x, 0f);

            while (elapsed < shakeDuration)
            {
                float damp = 1f - (elapsed / shakeDuration);
                float offset = Mathf.Sin(elapsed * Mathf.PI * 2f * 18f) * shakeMag * damp;
                transform.position = originalPos + perp * offset;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = originalPos;

            // Total red duration: 0.35s (0.3 - 0.4s)
            float totalRedTime = 0.35f;
            if (totalRedTime > shakeDuration)
                yield return new WaitForSeconds(totalRedTime - shakeDuration);

            ApplyColors();
            IsBlockedAnim = false;
            onComplete?.Invoke();
        }

        private void OnDestroy()
        {
            if (_bodyMesh)   Destroy(_bodyMesh);
            if (_headMesh)   Destroy(_headMesh);
            if (_glowMesh)   Destroy(_glowMesh);
            if (_highlightMesh) Destroy(_highlightMesh);
            if (_bodyMat)  Destroy(_bodyMat);
            if (_headMat)  Destroy(_headMat);
            if (_glowMat)  Destroy(_glowMat);
            if (_highlightMat) Destroy(_highlightMat);
            if (_trailRenderer && _trailRenderer.material) Destroy(_trailRenderer.material);
        }
    }
}

