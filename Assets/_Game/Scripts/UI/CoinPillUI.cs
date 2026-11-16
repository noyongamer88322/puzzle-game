// ============================================================
// CoinPillUI.cs  –  Arrow Slide Puzzle
// Top-left coin pill: coin icon + animated number counter.
//
// Animations:
//   • Coin added: number rolls up (count-up over 0.5 s), icon bounces.
//   • Button press: scale 0.92 → bounce back (spring tween).
//
// Sprite slot: icon_coin  (assign in Inspector or auto-loaded)
// Font: TextMeshPro (Lilita One / Fredoka / Baloo 2 — see note)
//
// PRD Section 7: Assets/_Game/Scripts/UI/CoinPillUI.cs
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ArrowPuzzle;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class CoinPillUI : MonoBehaviour, IPointerClickHandler
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("References")]
        [Tooltip("Image component showing the coin icon.")]
        public Image coinIcon;

        [Tooltip("TextMeshPro label showing the coin count.")]
        public TextMeshProUGUI coinLabel;

        [Header("Sprites")]
        [Tooltip("Coin sprite. If null, a yellow circle placeholder is used.")]
        public Sprite coinSprite;

        [Header("Tunables")]
        [Tooltip("Duration of the roll-up number animation (seconds).")]
        public float rollDuration = 0.5f;

        [Tooltip("Scale-down factor when pill is tapped.")]
        public float pressScale = 0.92f;

        [Tooltip("Duration of button press bounce animation (seconds).")]
        public float pressDuration = 0.12f;

        // ── Events ────────────────────────────────────────────
        /// <summary>Fired when coin pill or booster requests shop.</summary>
        public static event System.Action OnShopRequested;

        /// <summary>Raises the OnShopRequested event from outside the class.</summary>
        public static void RaiseShopRequested() => OnShopRequested?.Invoke();

        // ── Runtime ───────────────────────────────────────────
        private int _displayedCoins = 0;
        private int _targetCoins    = 0;
        private Coroutine _rollCoroutine;
        private Coroutine _bounceCoroutine;
        private RectTransform _rect;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            ApplySprite();
            RefreshLabel(_displayedCoins);

#if UNITY_EDITOR
            if (coinSprite == null)
                coinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_coin.png");
            ApplySprite();
#endif
        }

        private void Start()
        {
            if (ArrowPuzzle.Systems.GameState.Instance != null)
            {
                SetCoins(ArrowPuzzle.Systems.GameState.Instance.Coins);
            }
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Sets coin count instantly without animation.</summary>
        public void SetCoins(int amount)
        {
            _displayedCoins = amount;
            _targetCoins    = amount;
            RefreshLabel(amount);
        }

        /// <summary>Adds coins with roll-up + icon bounce animation.</summary>
        public void AddCoins(int delta)
        {
            if (delta <= 0) return;
            _targetCoins += delta;
            AudioManager.Instance?.PlayCoinCollect();
            StartRollUp();
            StartIconBounce();
        }

        /// <summary>Deducts coins with smooth roll-down animation.</summary>
        public void DeductCoins(int delta)
        {
            if (delta <= 0) return;
            _targetCoins = Mathf.Max(0, _targetCoins - delta);
            StartRollUp();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnPillPressed();
        }

        /// <summary>Call from UI Button onClick to play press animation.</summary>
        public void OnPillPressed()
        {
            if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = StartCoroutine(PressAnim());
            Debug.Log("[CoinPillUI] Coin pill pressed -> OnShopRequested fired.");
            OnShopRequested?.Invoke();
        }

        // ── Private ───────────────────────────────────────────

        private void ApplySprite()
        {
            if (coinIcon == null) return;
            if (coinSprite != null)
                coinIcon.sprite = coinSprite;
            // else: leave whatever placeholder sprite is already on the Image
        }

        private void RefreshLabel(int value)
        {
            if (coinLabel != null)
                coinLabel.text = value.ToString("N0").Replace(",", " ");
        }

        // ── Roll-up animation ─────────────────────────────────

        private void StartRollUp()
        {
            if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
            _rollCoroutine = StartCoroutine(RollUpCoroutine());
        }

        private IEnumerator RollUpCoroutine()
        {
            int startVal = _displayedCoins;
            int endVal   = _targetCoins;
            float elapsed = 0f;

            while (elapsed < rollDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rollDuration);
                // Ease-out cubic
                t = 1f - Mathf.Pow(1f - t, 3f);
                _displayedCoins = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, t));
                RefreshLabel(_displayedCoins);
                yield return null;
            }

            _displayedCoins = endVal;
            RefreshLabel(_displayedCoins);
            _rollCoroutine = null;
        }

        // ── Icon bounce animation ─────────────────────────────

        private void StartIconBounce()
        {
            if (coinIcon == null) return;
            StopAllCoroutinesOnIcon();
            StartCoroutine(IconBounceCoroutine());
        }

        private void StopAllCoroutinesOnIcon() { /* handled by coroutine chaining */ }

        private IEnumerator IconBounceCoroutine()
        {
            if (coinIcon == null) yield break;
            var iconRect = coinIcon.rectTransform;
            Vector3 originalScale = Vector3.one;

            // Pop up
            float t = 0f;
            float upDur   = 0.12f;
            float downDur = 0.18f;
            Vector3 bigScale = Vector3.one * 1.3f;

            while (t < upDur)
            {
                t += Time.deltaTime;
                iconRect.localScale = Vector3.Lerp(originalScale, bigScale,
                    Mathf.SmoothStep(0f, 1f, t / upDur));
                yield return null;
            }
            t = 0f;
            while (t < downDur)
            {
                t += Time.deltaTime;
                iconRect.localScale = Vector3.Lerp(bigScale, originalScale,
                    Mathf.SmoothStep(0f, 1f, t / downDur));
                yield return null;
            }
            iconRect.localScale = originalScale;
        }

        // ── Button press bounce ───────────────────────────────

        private IEnumerator PressAnim()
        {
            if (_rect == null) yield break;

            Vector3 orig = Vector3.one;
            Vector3 pressed = Vector3.one * pressScale;

            // Squish
            float t = 0f;
            while (t < pressDuration)
            {
                t += Time.deltaTime;
                _rect.localScale = Vector3.Lerp(orig, pressed, t / pressDuration);
                yield return null;
            }

            // Spring back (overshoot)
            t = 0f;
            float bounceDur = pressDuration * 1.5f;
            Vector3 overshoot = Vector3.one * 1.04f;
            while (t < bounceDur)
            {
                t += Time.deltaTime;
                float ratio = t / bounceDur;
                _rect.localScale = Vector3.Lerp(pressed, ratio < 0.6f ? overshoot : orig,
                    Mathf.SmoothStep(0f, 1f, ratio));
                yield return null;
            }
            _rect.localScale = orig;
            _bounceCoroutine = null;
        }
    }
}

