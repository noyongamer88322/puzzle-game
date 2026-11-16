// ============================================================
// ExitStreakLine.cs  –  Arrow Slide Puzzle
// VFX spawned when a hinted (green) arrow is tapped and slides out.
// Shoots a brilliant white streak line across the entire screen
// in the arrow's exit direction, then fades out smoothly.
//
// PRD Section 5 & Section 7: Assets/_Game/Scripts/Gameplay/ExitStreakLine.cs
// ============================================================

using System.Collections;
using UnityEngine;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    public class ExitStreakLine : MonoBehaviour
    {
        private LineRenderer _line;

        public void Launch(Vector3 origin, Vector3 direction)
        {
            StartCoroutine(StreakCoroutine(origin, direction.normalized));
        }

        private IEnumerator StreakCoroutine(Vector3 origin, Vector3 dir)
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.sortingOrder = 25;

            var mat = new Material(Shader.Find("Sprites/Default"));
            _line.material = mat;

            float maxDist = 45f; // Across entire screen
            float expandDuration = 0.12f;
            float fadeDuration = 0.24f;

            float baseStartWidth = 0.32f;
            float baseEndWidth = 0.04f;

            _line.startWidth = baseStartWidth;
            _line.endWidth = baseEndWidth;

            // Phase 1: Rapid beam shoot-out
            float elapsed = 0f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / expandDuration);
                float curDist = Mathf.Lerp(0.5f, maxDist, t);

                _line.SetPosition(0, origin);
                _line.SetPosition(1, origin + dir * curDist);

                Color c = new Color(1f, 1f, 1f, 0.95f);
                _line.startColor = c;
                _line.endColor = new Color(1f, 1f, 1f, 0.5f);

                yield return null;
            }

            _line.SetPosition(0, origin);
            _line.SetPosition(1, origin + dir * maxDist);

            // Phase 2: Fade alpha and taper width
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = 1f - t;

                _line.startWidth = baseStartWidth * (1f - t * 0.4f);
                _line.endWidth = baseEndWidth * (1f - t);

                Color c1 = new Color(1f, 1f, 1f, 0.95f * alpha);
                Color c2 = new Color(1f, 1f, 1f, 0.4f * alpha);
                _line.startColor = c1;
                _line.endColor = c2;

                yield return null;
            }

            if (mat != null) Destroy(mat);
            Destroy(gameObject);
        }
    }
}

