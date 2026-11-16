// ============================================================
// HeartPillUI.cs  –  Arrow Slide Puzzle
// Top-centre heart pill: 3 heart icons (full / empty).
//
// Animations (per PRD):
//   • Heart lost: pop (scale up) → crack/shrink → faded empty.
// The pill is hidden on Level 1 (controlled by GameplayHUD).
//
// Sprites:
//   • icon_heart_full.png  → fullHeartSprite
//   • icon_heart_empty.png → emptyHeartSprite
//   (auto-loaded in Editor if slots empty)
//
// PRD Section 7: Assets/_Game/Scripts/UI/HeartPillUI.cs
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class HeartPillUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Heart Image Slots (index 0 = leftmost)")]
        [Tooltip("Array of 3 Image components representing hearts.")]
        public Image[] heartImages = new Image[3];

        [Header("Sprites")]
        public Sprite fullHeartSprite;
        public Sprite emptyHeartSprite;

        [Header("Colors (PRD / Video Correction)")]
        [Tooltip("Full heart color tint (PRD: bright red glossy).")]
        public Color fullHeartColor = Color.white;

        [Tooltip("Empty heart color tint (PRD: pale beige/brownish, NOT grayish-pink).")]
        public Color emptyHeartColor = new Color(0.85f, 0.77f, 0.65f, 1f);

        [Header("Animation Tunables")]
        [Tooltip("Scale multiplier at peak of the pop anim.")]
        public float popPeak     = 1.4f;
        [Tooltip("Duration of pop-up phase (seconds).")]
        public float popUpDur    = 0.10f;
        [Tooltip("Duration of crack/shrink phase (seconds).")]
        public float crackDur    = 0.22f;

        // ── Runtime ───────────────────────────────────────────
        private int _currentHearts = 3;
        private int _maxHearts     = 3;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
#if UNITY_EDITOR
            TryAutoLoadSprites();
#endif
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Sets hearts instantly with no animation (use on level start).</summary>
        public void SetHearts(int current, int max)
        {
            _currentHearts = Mathf.Clamp(current, 0, max);
            _maxHearts     = max;
            RefreshAll();
        }

        /// <summary>
        /// Animates the loss of one heart (called by GameplayHUD on
        /// GameplayController.OnHeartChanged).
        /// </summary>
        public void LoseHeart(int newCurrent, int max)
        {
            int lostIndex = _currentHearts - 1; // 0-based index of the heart being lost
            _currentHearts = Mathf.Clamp(newCurrent, 0, max);
            _maxHearts     = max;

            if (lostIndex >= 0 && lostIndex < heartImages.Length)
                StartCoroutine(HeartLostAnim(lostIndex));
            else
                RefreshAll(); // just update if index out of range
        }

        // ── Private Helpers ───────────────────────────────────

        private void RefreshAll()
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                if (heartImages[i] == null) continue;
                bool isFull = i < _currentHearts;
                SetHeartState(heartImages[i], isFull, animated: false);
            }
        }

        private void SetHeartState(Image img, bool full, bool animated)
        {
            img.sprite = full ? fullHeartSprite : emptyHeartSprite;
            img.color  = full ? fullHeartColor : emptyHeartColor;
        }

        // ── Heart-lost animation coroutine ────────────────────

        private IEnumerator HeartLostAnim(int idx)
        {
            if (idx < 0 || idx >= heartImages.Length) yield break;
            var img = heartImages[idx];
            if (img == null) yield break;

            var rect = img.rectTransform;
            Vector3 originalScale = Vector3.one;
            Vector3 popScale      = Vector3.one * popPeak;

            // 1. Pop UP  (scale to popPeak)
            float t = 0f;
            while (t < popUpDur)
            {
                t += Time.deltaTime;
                rect.localScale = Vector3.Lerp(originalScale, popScale,
                    Mathf.SmoothStep(0f, 1f, t / popUpDur));
                yield return null;
            }

            // 2. Crack / Shrink + fade to empty  (simultaneously)
            t = 0f;
            Color fullColor  = fullHeartColor;
            Color emptyColor = emptyHeartColor;
            Vector3 shrinkScale = Vector3.one * 0.75f;

            while (t < crackDur)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / crackDur);
                // ease-in quad for snappier crack feel
                float eased = ratio * ratio;

                rect.localScale = Vector3.Lerp(popScale, shrinkScale, eased);
                img.color = Color.Lerp(fullColor, emptyColor, eased);
                yield return null;
            }

            // 3. Snap to final empty state + restore normal scale
            t = 0f;
            float restoreDur = 0.10f;
            while (t < restoreDur)
            {
                t += Time.deltaTime;
                rect.localScale = Vector3.Lerp(shrinkScale, originalScale,
                    t / restoreDur);
                yield return null;
            }

            rect.localScale = originalScale;
            img.sprite = emptyHeartSprite;
            img.color  = emptyColor;
        }

#if UNITY_EDITOR
        private void TryAutoLoadSprites()
        {
            if (fullHeartSprite == null)
                fullHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_heart_full.png");
            if (emptyHeartSprite == null)
                emptyHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_heart_empty.png");
        }
#endif
    }
}

