// ============================================================
// UIButtonScale.cs  –  Arrow Slide Puzzle
// Universal button press feedback (PRD Section 5 & 7):
//   • Button press: scale down (0.92) -> bounce back (overshoot spring)
//   • Plays AudioManager.Instance.PlayButtonClick() on click
//
// Attach to any UI Button or interactive RectTransform.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class UIButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Tunables (PRD Section 5)")]
        [Tooltip("Scale factor when pressed (PRD: 0.92).")]
        [Range(0.7f, 1.0f)]
        public float pressedScale = 0.92f;

        [Tooltip("Duration to scale down (seconds).")]
        public float pressDownDuration = 0.08f;

        [Tooltip("Duration to spring bounce back (seconds).")]
        public float bounceBackDuration = 0.16f;

        [Tooltip("Overshoot scale on release.")]
        public float overshootScale = 1.04f;

        [Tooltip("Whether to play button click SFX on click.")]
        public bool playSoundOnClick = true;

        private RectTransform _rect;
        private Vector3 _originalScale = Vector3.one;
        private Coroutine _scaleCoroutine;
        private bool _isPressed;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (_rect != null)
                _originalScale = _rect.localScale;
        }

        private void OnEnable()
        {
            if (_rect != null)
                _rect.localScale = _originalScale;
            _isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(AnimateScale(_originalScale * pressedScale, pressDownDuration));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(BounceBack());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playSoundOnClick)
            {
                AudioManager.Instance?.PlayButtonClick();
            }
        }

        private IEnumerator AnimateScale(Vector3 target, float duration)
        {
            if (_rect == null) yield break;
            Vector3 start = _rect.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _rect.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }

            _rect.localScale = target;
        }

        private IEnumerator BounceBack()
        {
            if (_rect == null) yield break;
            Vector3 start = _rect.localScale;
            Vector3 peak = _originalScale * overshootScale;
            float half = bounceBackDuration * 0.55f;
            float elapsed = 0f;

            // 1. Spring overshoot
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                _rect.localScale = Vector3.Lerp(start, peak, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            // 2. Settle to original
            elapsed = 0f;
            float secondHalf = bounceBackDuration - half;
            while (elapsed < secondHalf)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / secondHalf);
                _rect.localScale = Vector3.Lerp(peak, _originalScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            _rect.localScale = _originalScale;
            _scaleCoroutine = null;
        }
    }
}

