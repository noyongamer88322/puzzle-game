// ============================================================
// PowerButtonUI.cs  –  Arrow Slide Puzzle
// Right-side vertical power button (Hint or Magnet).
//
// Layout (self-contained):
//   • Icon image (top)
//   • Cost label (e.g. "650" or "1300")
//   • FREE badge (child GameObject, shown only on first use)
//
// Animations:
//   • Press: scale 0.92 → spring bounce back.
//
// Sprites:
//   • icon_hint.png   / icon_magnet.png  (PRD naming)
//   Auto-loaded in Editor if null.
//
// Logic for Hint/Magnet actions is deferred to a future step.
// Only click event stub is wired here.
//
// PRD Section 7: Assets/_Game/Scripts/UI/PowerButtonUI.cs
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ArrowPuzzle.UI
{
    public enum PowerButtonType { Hint, Magnet }

    [DisallowMultipleComponent]
    public class PowerButtonUI : MonoBehaviour, IPointerClickHandler
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Type")]
        public PowerButtonType buttonType = PowerButtonType.Hint;

        [Header("References")]
        public Image        iconImage;
        public TextMeshProUGUI costLabel;

        [Tooltip("Child GameObject showing the FREE badge/tag.")]
        public GameObject freeBadge;

        [Tooltip("Optional child GameObject showing the coin icon next to cost.")]
        public GameObject costCoinIcon;

        [Header("Sprites")]
        [Tooltip("Icon sprite. Assigned auto in Editor from Art/UI/.")]
        public Sprite iconSprite;

        [Header("Cost")]
        [Tooltip("Coin cost shown below the icon. Set per Economy constants.")]
        public int coinCost = 650;

        [Header("Animation")]
        public float pressScale   = 0.92f;
        public float pressDuration = 0.12f;

        // ── Events ────────────────────────────────────────────
        /// <summary>Raised when the button is clicked. Subscribe for game logic.</summary>
        public event Action<PowerButtonType> OnClicked;

        // ── Runtime ───────────────────────────────────────────
        private RectTransform _rect;
        private Coroutine _bounceCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

#if UNITY_EDITOR
            TryAutoLoadSprite();
#endif
            ApplySprite();
            RefreshCostLabel();
            if (freeBadge != null) freeBadge.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Shows or hides the FREE badge.</summary>
        public void SetFreeTag(bool isFree)
        {
            if (freeBadge != null)
                freeBadge.SetActive(isFree);

            if (costLabel != null)
            {
                costLabel.gameObject.SetActive(!isFree);
                if (!isFree) RefreshCostLabel();
            }

            if (costCoinIcon != null)
            {
                costCoinIcon.SetActive(!isFree);
            }
        }

        public void SetCost(int cost)
        {
            coinCost = cost;
            RefreshCostLabel();
        }

        public void PlayErrorShake()
        {
            StartCoroutine(ErrorShakeAnim());
        }

        private IEnumerator ErrorShakeAnim()
        {
            if (_rect == null) yield break;
            Vector2 origPos = _rect.anchoredPosition;
            float duration = 0.25f;
            float elapsed = 0f;
            float mag = 8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                float offset = Mathf.Sin(elapsed * Mathf.PI * 20f) * mag * damp;
                _rect.anchoredPosition = origPos + new Vector2(offset, 0f);
                yield return null;
            }
            _rect.anchoredPosition = origPos;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnButtonClicked();
        }

        /// <summary>Call from UnityEvent (Button onClick) in the Canvas.</summary>
        public void OnButtonClicked()
        {
            if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = StartCoroutine(PressAnim());

            AudioManager.Instance?.PlayButtonClick();

            // Stub — game-logic handlers subscribe via event
            OnClicked?.Invoke(buttonType);

            // Log for now (future step will add Hint/Magnet behaviour)
            Debug.Log($"[PowerButton] {buttonType} clicked.");
        }

        // ── Private ───────────────────────────────────────────

        private void ApplySprite()
        {
            if (iconImage != null && iconSprite != null)
                iconImage.sprite = iconSprite;
        }

        private void RefreshCostLabel()
        {
            if (costLabel != null)
                costLabel.text = coinCost.ToString("N0").Replace(",", " ");
        }

        // ── Press / Bounce animation ──────────────────────────

        private IEnumerator PressAnim()
        {
            if (_rect == null) yield break;
            Vector3 orig     = Vector3.one;
            Vector3 pressed  = Vector3.one * pressScale;
            Vector3 overshoot= Vector3.one * 1.04f;

            // Squish down
            float t = 0f;
            while (t < pressDuration)
            {
                t += Time.deltaTime;
                _rect.localScale = Vector3.Lerp(orig, pressed, t / pressDuration);
                yield return null;
            }

            // Bounce back with overshoot
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

#if UNITY_EDITOR
        private void TryAutoLoadSprite()
        {
            if (iconSprite != null) return;
            string path = buttonType == PowerButtonType.Hint
                ? "Assets/_Game/Art/UI/icon_hint.png"
                : "Assets/_Game/Art/UI/icon_magnet.png";
            iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif
    }
}

