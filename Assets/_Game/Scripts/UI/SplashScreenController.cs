// ============================================================
// SplashScreenController.cs  –  Arrow Slide Puzzle
// Controls the Splash screen:
//   • Solid light-blue background (#2FB4E8) with faint repeating icon pattern
//   • 2×2 grid of four 3D glossy arrows (130x130, 30px gap):
//       Top-Left: Pink ↑, Top-Right: Orange →, Bottom-Left: Light-Blue ←, Bottom-Right: Purple ↓
//   • Arrows start dim (~30% opacity), then brighten to 100% in ~0.5s
//   • 5 loading dots at bottom-center (288px up, 22px dots, 18px gap)
//       Active dot is bold and larger, looping left-to-right
//   • After splash sequence: seamless transition to Map (or Level 1 for new users)
//
// Attach to a root GameObject in Splash.unity.
// PRD Section 7: Assets/_Game/Scripts/UI/SplashScreenController.cs
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class SplashScreenController : MonoBehaviour
    {
        public static readonly Color SplashBlue = new Color(0.1843f, 0.7059f, 0.9098f, 1f); // #2FB4E8

        // ── Inspector: Background ────────────────────────────────────
        [Header("Background")]
        [Tooltip("Full-screen background Image – set color to #2FB4E8 (light blue).")]
        public Image backgroundImage;

        [Tooltip("Optional faint repeating icon-pattern overlay Image.")]
        public Image iconPatternOverlay;

        [Tooltip("Optional faint repeating icon-pattern overlay RawImage (tiled).")]
        public RawImage rawPatternOverlay;

        // ── Inspector: Arrow Grid ────────────────────────────────────
        [Header("Arrow Grid (2×2)")]
        [Tooltip("CanvasGroup on ArrowGrid parent container for opacity control.")]
        public CanvasGroup arrowGroup;

        [Tooltip("Pink Up arrow (Top-Left).")]
        public RectTransform arrowPinkUp;

        [Tooltip("Orange Right arrow (Top-Right).")]
        public RectTransform arrowOrangeRight;

        [Tooltip("Light-Blue Left arrow (Bottom-Left).")]
        public RectTransform arrowCyanLeft;

        [Tooltip("Purple Down arrow (Bottom-Right).")]
        public RectTransform arrowPurpleDown;

        // ── Inspector: Loading Dots ──────────────────────────────────
        [Header("Loading Dots (Bottom Center)")]
        public RectTransform dotsContainer;
        public Image[] loadingDots = new Image[5];
        public float dotStepInterval = 0.22f;

        // ── Inspector: Fade Overlay ──────────────────────────────────
        [Header("Fade Overlay")]
        [Tooltip("Full-screen blue overlay Image used for exit fade.")]
        public Image fadeOverlay;

        // ── Inspector: Timing (seconds) ──────────────────────────────
        [Header("Timing (seconds)")]
        [Tooltip("Duration of the dim (30%) → bright (100%) fade-in for arrows.")]
        public float fadeInDuration  = 0.5f;

        [Tooltip("Duration of each arrow's pop animation.")]
        public float popDuration     = 0.25f;

        [Tooltip("Pause before the exit fade begins.")]
        public float holdDuration    = 1.0f;

        [Tooltip("Duration of the blue fade-out before scene load.")]
        public float exitFadeDuration = 0.5f;

        // ── Runtime ──────────────────────────────────────────────────
        private Coroutine _dotsCoroutine;
        private int _activeDotIndex = 0;

        // ─────────────────────────────────────────────────────────────
        private void Start()
        {
            // Apply background color #2FB4E8
            if (backgroundImage != null)
                backgroundImage.color = SplashBlue;

            if (iconPatternOverlay != null)
            {
                Color c = iconPatternOverlay.color;
                c.a = 0.12f;
                iconPatternOverlay.color = c;
            }

            if (rawPatternOverlay != null)
            {
                Color c = rawPatternOverlay.color;
                c.a = 0.12f;
                rawPatternOverlay.color = c;
            }

            if (fadeOverlay != null)
                fadeOverlay.color = new Color(SplashBlue.r, SplashBlue.g, SplashBlue.b, 0f); // transparent initially

            StartDotsLoop();
            StartCoroutine(RunSplashSequence());
        }

        // ── Dots Animation ───────────────────────────────────────────
        private void StartDotsLoop()
        {
            if (_dotsCoroutine != null) StopCoroutine(_dotsCoroutine);
            _dotsCoroutine = StartCoroutine(LoopDotsRoutine());
        }

        private IEnumerator LoopDotsRoutine()
        {
            _activeDotIndex = 0;
            while (true)
            {
                UpdateDotsVisuals(_activeDotIndex);
                yield return new WaitForSeconds(dotStepInterval);
                _activeDotIndex = (_activeDotIndex + 1) % 5;
            }
        }

        private void UpdateDotsVisuals(int activeIdx)
        {
            if (loadingDots == null) return;
            for (int i = 0; i < loadingDots.Length; i++)
            {
                var dot = loadingDots[i];
                if (dot == null) continue;
                bool isActive = (i == activeIdx);
                dot.transform.localScale = Vector3.one * (isActive ? 1.35f : 1.0f);
                Color c = Color.white;
                c.a = isActive ? 1.0f : 0.35f;
                dot.color = c;
            }
        }

        // ─────────────────────────────────────────────────────────────
        private IEnumerator RunSplashSequence()
        {
            // Collect the four arrows
            RectTransform[] arrows = new RectTransform[]
            {
                arrowPinkUp, arrowOrangeRight, arrowCyanLeft, arrowPurpleDown
            };

            // Arrows start at ~30% opacity
            if (arrowGroup != null)
            {
                arrowGroup.alpha = 0.3f;
            }
            else
            {
                foreach (var arrow in arrows)
                {
                    if (arrow != null) SetAlpha(arrow, 0.3f);
                }
            }

            // Phase 1: Fade arrows from 30% to 100% in ~0.5s
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                float alpha = Mathf.Lerp(0.3f, 1f, t);

                if (arrowGroup != null)
                {
                    arrowGroup.alpha = alpha;
                }
                else
                {
                    foreach (var arrow in arrows)
                    {
                        if (arrow != null) SetAlpha(arrow, alpha);
                    }
                }
                yield return null;
            }

            if (arrowGroup != null) arrowGroup.alpha = 1f;

            // Phase 2: Hold while loading dots animate
            yield return new WaitForSeconds(holdDuration);

            // Phase 3: Transition to next scene
            LoadNextScene();
        }

        // ─────────────────────────────────────────────────────────────
        private void LoadNextScene()
        {
            bool isNewUser = true;
            try
            {
                if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
                {
                    isNewUser = SaveManager.Instance.HighestUnlockedLevel <= 1 &&
                                !SaveManager.Instance.Data.tutorialStep1Done;
                }
            }
            catch
            {
                isNewUser = true;
            }

            // Navigation flow specified by PRD / Rule: Splash -> Map -> Game -> Level cleared -> Map
            string targetScene = SceneNames.Map;

            // Use SceneTransition if available, otherwise direct scene load
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Load(targetScene);
            }
            else
            {
                StartCoroutine(FadeOutAndLoad(targetScene));
            }
        }

        private IEnumerator FadeOutAndLoad(string sceneName)
        {
            if (fadeOverlay != null)
            {
                float elapsed = 0f;
                Color startCol = new Color(SplashBlue.r, SplashBlue.g, SplashBlue.b, 0f);
                Color endCol   = new Color(SplashBlue.r, SplashBlue.g, SplashBlue.b, 1f);
                while (elapsed < exitFadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / exitFadeDuration);
                    fadeOverlay.color = Color.Lerp(startCol, endCol, t);
                    yield return null;
                }
                fadeOverlay.color = endCol;
            }

            SceneManager.LoadScene(sceneName);
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>Sets alpha on the first Image of a RectTransform.</summary>
        private static void SetAlpha(RectTransform rt, float alpha)
        {
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }
    }
}
