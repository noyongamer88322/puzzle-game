// ============================================================
// HUDButtonUI.cs  –  Arrow Slide Puzzle
// Generic HUD button (Back, Gear) with press scale animation.
//
// Press animation: scale 0.92 → spring bounce back.
// Wire OnButtonClicked() to the Button's onClick in the Canvas.
//
// PRD Section 7: Assets/_Game/Scripts/UI/HUDButtonUI.cs
// ============================================================

using System;
using System.Collections;
using UnityEngine;

namespace ArrowPuzzle.UI
{
    public enum HUDButtonType { Back, Gear }

    [DisallowMultipleComponent]
    public class HUDButtonUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Type")]
        public HUDButtonType buttonType = HUDButtonType.Gear;

        [Header("Animation")]
        public float pressScale    = 0.92f;
        public float pressDuration = 0.12f;

        // ── Events ────────────────────────────────────────────
        public event Action<HUDButtonType> OnClicked;

        // ── Runtime ───────────────────────────────────────────
        private RectTransform _rect;
        private Coroutine _bounceCoroutine;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Wire to Button onClick in the Canvas.</summary>
        public void OnButtonClicked()
        {
            if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = StartCoroutine(PressAnim());

            AudioManager.Instance?.PlayButtonClick();

            OnClicked?.Invoke(buttonType);
            Debug.Log($"[HUDButton] {buttonType} clicked.");

            // Stub behaviour
            if (buttonType == HUDButtonType.Back)
            {
                // Future step: return to Map screen
            }
            else if (buttonType == HUDButtonType.Gear)
            {
                // Future step: open Settings panel
            }
        }

        // ── Press animation ───────────────────────────────────

        private IEnumerator PressAnim()
        {
            if (_rect == null) yield break;
            Vector3 orig      = Vector3.one;
            Vector3 pressed   = Vector3.one * pressScale;
            Vector3 overshoot = Vector3.one * 1.04f;

            float t = 0f;
            while (t < pressDuration)
            {
                t += Time.deltaTime;
                _rect.localScale = Vector3.Lerp(orig, pressed, t / pressDuration);
                yield return null;
            }

            t = 0f;
            float bounceDur = pressDuration * 1.6f;
            while (t < bounceDur)
            {
                t += Time.deltaTime;
                float r = t / bounceDur;
                _rect.localScale = Vector3.Lerp(pressed, r < 0.55f ? overshoot : orig,
                    Mathf.SmoothStep(0f, 1f, r));
                yield return null;
            }
            _rect.localScale = orig;
            _bounceCoroutine = null;
        }
    }
}

