// ============================================================
// UIPanelOvershoot.cs  –  Arrow Slide Puzzle
// Reusable popup / panel scale-in overshoot animator (PRD Section 5 & 7):
//   • Panel: scale-in overshoot (0 -> 1.10 -> 1.0)
//   • Background dim overlay fade-in
//   • Plays AudioManager.Instance.PlayPopupOpen() on open
//   • Unscaled time updates so it works during slow-mo or pause
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class UIPanelOvershoot : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The panel RectTransform to scale.")]
        public RectTransform panelCard;

        [Tooltip("Optional dark dim overlay image.")]
        public Image dimOverlay;

        [Tooltip("Optional CanvasGroup on panel.")]
        public CanvasGroup canvasGroup;

        [Header("Tunables (PRD Section 5)")]
        [Tooltip("Duration of scale-in open animation (PRD: 0.25s).")]
        public float openDuration = 0.25f;

        [Tooltip("Duration of scale-down close animation.")]
        public float closeDuration = 0.18f;

        [Tooltip("Maximum alpha of the dark dim overlay.")]
        [Range(0f, 1f)]
        public float dimTargetAlpha = 0.72f;

        [Tooltip("Whether to play popup open sound.")]
        public bool playSoundOnOpen = true;

        public event Action OnOpened;
        public event Action OnClosed;

        private Coroutine _animRoutine;
        private bool _isOpen;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (panelCard == null)
                panelCard = GetComponent<RectTransform>();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            _isOpen = true;

            if (playSoundOnOpen)
                AudioManager.Instance?.PlayPopupOpen();

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateOpen()
        {
            if (panelCard != null) panelCard.localScale = Vector3.one * 0.8f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);

                // Spring overshoot: 0.8 -> 1.05 -> 1.0 (PRD / prompt: 0.25s)
                float s;
                if (t < 0.65f)
                {
                    float subT = t / 0.65f;
                    s = Mathf.Lerp(0.8f, 1.05f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else
                {
                    float subT = (t - 0.65f) / 0.35f;
                    s = Mathf.Lerp(1.05f, 1.0f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }

                if (panelCard != null) panelCard.localScale = Vector3.one * s;

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Clamp01(t * 1.8f);

                if (dimOverlay != null)
                    dimOverlay.color = new Color(0f, 0f, 0f, t * dimTargetAlpha);

                yield return null;
            }

            if (panelCard != null) panelCard.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, dimTargetAlpha);

            _animRoutine = null;
            OnOpened?.Invoke();
        }

        private IEnumerator AnimateClose()
        {
            float elapsed = 0f;
            Vector3 startScale = panelCard != null ? panelCard.localScale : Vector3.one;
            float startDimAlpha = dimOverlay != null ? dimOverlay.color.a : 0f;

            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);

                if (panelCard != null) panelCard.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                if (canvasGroup != null) canvasGroup.alpha = 1f - t;
                if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(startDimAlpha, 0f, t));

                yield return null;
            }

            if (panelCard != null) panelCard.localScale = Vector3.zero;
            gameObject.SetActive(false);
            _animRoutine = null;
            OnClosed?.Invoke();
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}

