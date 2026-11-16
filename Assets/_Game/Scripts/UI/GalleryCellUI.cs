// ============================================================
// GalleryCellUI.cs  –  Arrow Slide Puzzle
// A single cell in the Gallery grid.
//
// States:
//   Loading  – empty blue cell (shown while image fetches)
//   Unlocked – picture visible + green tick overlay
//   Locked   – grey tinted placeholder + lock icon overlay
//
// PRD Section 7: Assets/_Game/Scripts/UI/GalleryCellUI.cs
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class GalleryCellUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("References")]
        [Tooltip("Main picture image component.")]
        public Image photoImage;

        [Tooltip("Blue loading background (shown while image fetches).")]
        public Image loadingBg;

        [Tooltip("Lock icon image (shown when locked).")]
        public Image lockIcon;

        [Tooltip("Green tick image (shown when unlocked).")]
        public Image tickIcon;

        [Tooltip("Dark overlay tint image (shown when locked, semi-transparent grey).")]
        public Image lockedOverlay;

        // ── Runtime ───────────────────────────────────────────
        private bool _isUnlocked;

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Sets up the cell state without loading the actual image yet.
        /// Call ShowLoading() then ShowImage() to load asynchronously.
        /// </summary>
        public void Setup(string imageName, bool unlocked,
                          Sprite lockSprite, Sprite tickSprite, Sprite placeholder)
        {
            _isUnlocked = unlocked;

            // Apply lock/tick sprites
            if (lockIcon != null && lockSprite != null) lockIcon.sprite = lockSprite;
            if (tickIcon != null && tickSprite != null) tickIcon.sprite = tickSprite;

            if (unlocked)
            {
                // Start in loading state; image comes in later
                ShowLoading();
            }
            else
            {
                // Show locked state immediately with placeholder
                SetLockedState(placeholder);
            }
        }

        /// <summary>Shows the blue loading placeholder cell.</summary>
        public void ShowLoading()
        {
            if (loadingBg != null) loadingBg.gameObject.SetActive(true);
            if (photoImage != null)
            {
                photoImage.gameObject.SetActive(false);
                photoImage.color = Color.white;
            }
            if (lockIcon     != null) lockIcon.gameObject.SetActive(false);
            if (tickIcon     != null) tickIcon.gameObject.SetActive(false);
            if (lockedOverlay != null) lockedOverlay.gameObject.SetActive(false);
        }

        /// <summary>Shows the loaded image with tick (unlocked state).</summary>
        public void ShowImage(Sprite sprite)
        {
            if (loadingBg != null) loadingBg.gameObject.SetActive(false);

            if (photoImage != null)
            {
                photoImage.gameObject.SetActive(true);
                photoImage.sprite = sprite;
                photoImage.color  = Color.white;
                photoImage.preserveAspect = true;
            }

            if (lockIcon      != null) lockIcon.gameObject.SetActive(false);
            if (lockedOverlay != null) lockedOverlay.gameObject.SetActive(false);

            // Show tick for unlocked
            if (tickIcon != null) tickIcon.gameObject.SetActive(_isUnlocked);
        }

        // ── Private ───────────────────────────────────────────

        private void SetLockedState(Sprite placeholder)
        {
            if (loadingBg != null) loadingBg.gameObject.SetActive(false);

            if (photoImage != null)
            {
                photoImage.gameObject.SetActive(true);
                photoImage.sprite = placeholder;
                // Grey tint for locked
                photoImage.color = new Color(0.55f, 0.55f, 0.60f, 1f);
                photoImage.preserveAspect = true;
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.gameObject.SetActive(true);
                lockedOverlay.color = new Color(0f, 0f, 0f, 0.38f);
            }

            if (lockIcon != null) lockIcon.gameObject.SetActive(true);
            if (tickIcon != null) tickIcon.gameObject.SetActive(false);
        }
    }
}

