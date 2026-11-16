// ============================================================
// BackgroundRenderer.cs  –  Arrow Slide Puzzle
// Fills the entire screen with a vertical indigo gradient
// and a soft radial vignette, rendered via a full-screen quad
// driven by a procedural shader.
//
// Attach to the "Background" GameObject in the Game scene.
// No external textures needed.
// ============================================================

using UnityEngine;

namespace ArrowPuzzle.Gameplay
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class BackgroundRenderer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Gradient")]
        public Color gradientBottom = new Color(0.165f, 0.039f, 0.620f, 1f); // #2A0A9E
        public Color gradientTop    = new Color(0.231f, 0.078f, 0.784f, 1f); // #3B14C8

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteStrength = 0.45f;
        [Range(0f, 2f)] public float vignetteSoftness = 1.2f;

        // ── Private ───────────────────────────────────────────
        private Material   _mat;
        private MeshFilter _mf;

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            _mf = GetComponent<MeshFilter>();
            BuildFullScreenQuad();
            BuildMaterial();
        }

        private void LateUpdate()
        {
            // Update shader params each frame (cheap – just 4 floats)
            _mat.SetColor("_ColorBottom",     gradientBottom);
            _mat.SetColor("_ColorTop",        gradientTop);
            _mat.SetFloat("_VigStrength",     vignetteStrength);
            _mat.SetFloat("_VigSoftness",     vignetteSoftness);
        }

        // ── Helpers ───────────────────────────────────────────

        /// <summary>
        /// Creates a world-space quad that exactly covers the camera's
        /// orthographic viewport, placed behind everything (z = 10).
        /// </summary>
        private void BuildFullScreenQuad()
        {
            var cam  = Camera.main;
            float h  = cam.orthographicSize;
            float w  = h * cam.aspect;

            var mesh = new Mesh { name = "BgQuad" };
            mesh.vertices = new Vector3[]
            {
                new Vector3(-w, -h, 0f),
                new Vector3( w, -h, 0f),
                new Vector3( w,  h, 0f),
                new Vector3(-w,  h, 0f),
            };
            // UVs: (0,0) bottom-left → (1,1) top-right, used for gradient & vignette
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            _mf.mesh = mesh;

            // Position at camera centre, pushed back so it renders first
            transform.position = new Vector3(
                cam.transform.position.x,
                cam.transform.position.y,
                cam.transform.position.z + 9f);  // behind all gameplay (z = 0)
        }

        /// <summary>
        /// Builds the background material using an inline shader so the
        /// project needs no external shader files.
        /// </summary>
        private void BuildMaterial()
        {
            var shader = Shader.Find("ArrowPuzzle/Background");

            if (shader == null)
            {
                // Fallback: plain solid colour
                Debug.LogWarning("[BackgroundRenderer] ArrowPuzzle/Background shader " +
                                 "not found – using fallback solid colour.");
                _mat = new Material(Shader.Find("Sprites/Default"));
                _mat.color = gradientBottom;
            }
            else
            {
                _mat = new Material(shader);
            }

            _mat.name = "BackgroundMat";
            GetComponent<MeshRenderer>().sharedMaterial = _mat;
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}

