// ============================================================
// ScreenVignetteFlash.cs  –  Arrow Slide Puzzle
// Renders a soft pink/red inward glow vignette around the screen
// edges when a blocked tap occurs.
// PRD Section 7: _Game/Scripts/Gameplay
// ============================================================

using System.Collections;
using UnityEngine;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ScreenVignetteFlash : MonoBehaviour
    {
        public static ScreenVignetteFlash Instance { get; private set; }

        [Header("Appearance")]
        [Tooltip("Color of the outer border glow.")]
        public Color flashColor = new Color(1f, 0.1f, 0.1f, 1f); // #FF1A1A

        [Range(0.1f, 0.5f)]
        [Tooltip("Thickness of the vignette glow inward from the screen edge.")]
        public float borderInsetFraction = 0.22f;

        [Range(0.1f, 1f)]
        public float peakAlpha = 0.85f;

        [Tooltip("Total flash duration in seconds (PRD: ~0.3 - 0.4s).")]
        public float flashDuration = 0.35f;

        private Mesh         _mesh;
        private MeshFilter   _mf;
        private MeshRenderer _mr;
        private Material     _mat;
        private Coroutine    _flashRoutine;
        private Camera       _cam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = Camera.main;
            _mf  = GetComponent<MeshFilter>();
            _mr  = GetComponent<MeshRenderer>();

            SetupMaterial();
            BuildMesh();
            SetAlpha(0f);
        }

        private void SetupMaterial()
        {
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows    = false;
            _mr.sortingOrder      = 500; // In front of gameplay

            var shader = Shader.Find("Sprites/Default");
            _mat = new Material(shader)
            {
                name        = "VignetteFlashMat",
                renderQueue = 3500
            };
            _mr.material = _mat;
        }

        private void BuildMesh()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float h = _cam.orthographicSize;
            float w = h * _cam.aspect;

            float insetX = w * borderInsetFraction;
            float insetY = h * borderInsetFraction;

            _mesh = new Mesh { name = "VignetteBorderMesh" };

            // 8 vertices: 0..3 = outer corners, 4..7 = inner corners
            // Outer:
            // 0: Bottom-Left, 1: Bottom-Right, 2: Top-Right, 3: Top-Left
            // Inner:
            // 4: Bottom-Left, 5: Bottom-Right, 6: Top-Right, 7: Top-Left
            var verts = new Vector3[]
            {
                new Vector3(-w,          -h,          0f), // 0: Outer BL
                new Vector3( w,          -h,          0f), // 1: Outer BR
                new Vector3( w,           h,          0f), // 2: Outer TR
                new Vector3(-w,           h,          0f), // 3: Outer TL

                new Vector3(-w + insetX, -h + insetY, 0f), // 4: Inner BL
                new Vector3( w - insetX, -h + insetY, 0f), // 5: Inner BR
                new Vector3( w - insetX,  h - insetY, 0f), // 6: Inner TR
                new Vector3(-w + insetX,  h - insetY, 0f)  // 7: Inner TL
            };

            var uvs = new Vector2[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.2f), new Vector2(0.8f, 0.8f), new Vector2(0.2f, 0.8f)
            };

            // 4 border quads = 8 triangles
            var tris = new int[]
            {
                // Bottom border: 0, 1, 5, 4
                0, 1, 5,  0, 5, 4,
                // Right border: 1, 2, 6, 5
                1, 2, 6,  1, 6, 5,
                // Top border: 2, 3, 7, 6
                2, 3, 7,  2, 7, 6,
                // Left border: 3, 0, 4, 7
                3, 0, 4,  3, 4, 7
            };

            _mesh.vertices  = verts;
            _mesh.uv        = uvs;
            _mesh.triangles = tris;
            _mesh.RecalculateNormals();
            _mf.mesh = _mesh;

            // Position at camera center
            transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, 0f);
        }

        public void TriggerFlash()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashCoroutine());

            ArrowPuzzle.UI.GameplayHUD.Instance?.TriggerBlockedVignette();

            // Phone vibration
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                if (GameState.Instance == null || GameState.Instance.VibrationOn)
                {
                    Handheld.Vibrate();
                }
            }
            catch (System.Exception) { }
#endif
        }

        private IEnumerator FlashCoroutine()
        {
            float fadeInTime  = 0.05f;
            float fadeOutTime = flashDuration - fadeInTime;
            float elapsed     = 0f;

            // Fast ramp up
            while (elapsed < fadeInTime)
            {
                float t = elapsed / fadeInTime;
                SetAlpha(Mathf.Lerp(0f, peakAlpha, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetAlpha(peakAlpha);
            elapsed = 0f;

            // Smooth fade out
            while (elapsed < fadeOutTime)
            {
                float t = elapsed / fadeOutTime;
                float eased = 1f - (t * t); // smooth fade
                SetAlpha(Mathf.Lerp(0f, peakAlpha, eased));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetAlpha(0f);
            _flashRoutine = null;
        }

        private void SetAlpha(float a)
        {
            if (_mesh == null || _mesh.vertexCount < 8) return;

            Color outer = flashColor;
            outer.a = a;

            Color inner = flashColor;
            inner.a = 0f; // Center remains fully clear

            var cols = new Color[]
            {
                outer, outer, outer, outer, // 0..3 Outer
                inner, inner, inner, inner  // 4..7 Inner
            };

            _mesh.colors = cols;
        }

        private void OnDestroy()
        {
            if (_mesh) Destroy(_mesh);
            if (_mat)  Destroy(_mat);
        }
    }
}

