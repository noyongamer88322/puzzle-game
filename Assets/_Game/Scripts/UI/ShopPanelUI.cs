// ============================================================
// ShopPanelUI.cs  –  Arrow Slide Puzzle
// Main controller for the Shop pop-up panel.
//
// Layout:
//   • Dim overlay (darkens background, tap closes panel)
//   • Center card panel with scale-in overshoot bounce
//   • Top: 'SHOP' title (bold golden/white), Red '✕' close button top-right
//   • Horizontal ScrollRect containing the 6 shop items:
//       1. Disable Ads ($3.99 + 1000 Coins bonus)
//       2. 1 500 Coins ($2.99, Best Seller badge)
//       3. 6 250 Coins ($9.99)
//       4. 13 500 Coins ($19.99)
//       5. 25 000 Coins ($29.99)
//       6. 100 000 Coins ($99.99, Save badge)
//   • Green price buttons with spring click animation
//   • Web 'Allow pop-ups...' message is omitted per PRD instruction
//
// PRD Section 3.9 & 7: Assets/_Game/Scripts/UI/ShopPanelUI.cs
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;
using ArrowPuzzle.Gameplay;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class ShopPanelUI : MonoBehaviour
    {
        public static ShopPanelUI Instance { get; private set; }

        [Header("Hierarchy References")]
        [Tooltip("The central card panel that scales in.")]
        public RectTransform panelCard;

        [Tooltip("Full-screen black dim overlay.")]
        public Image dimOverlay;

        [Tooltip("CanvasGroup on the panel for smooth fading.")]
        public CanvasGroup panelCanvasGroup;

        [Tooltip("Red close button (top-right of panel).")]
        public Button closeButton;

        [Tooltip("Top SHOP title text.")]
        public TextMeshProUGUI titleLabel;

        [Header("Scroll Area")]
        public ScrollRect scrollRect;
        public RectTransform cardsContainer;

        [Header("Cards List")]
        public List<ShopCardUI> cards = new List<ShopCardUI>();

        [Header("Sprites")]
        public Sprite iconNoAds;
        public Sprite iconPack1;
        public Sprite iconPack2;
        public Sprite iconPack3;
        public Sprite iconPack4;
        public Sprite iconPack5;

        [Header("Animation Tunables")]
        public float openDuration  = 0.35f;
        public float closeDuration = 0.20f;

        private Coroutine _animCoroutine;
        private bool _isOpen = false;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // Tapping dim overlay closes the panel
            if (dimOverlay != null)
            {
                var dimBtn = dimOverlay.GetComponent<Button>();
                if (dimBtn != null)
                    dimBtn.onClick.AddListener(Close);
            }

            if (cards == null || cards.Count == 0)
            {
                cards = new List<ShopCardUI>(GetComponentsInChildren<ShopCardUI>(true));
            }

            WireCardEvents();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitRuntimeHooks()
        {
            CoinPillUI.OnShopRequested -= HandleStaticShopRequested;
            CoinPillUI.OnShopRequested += HandleStaticShopRequested;

            BoosterController.OnShopRequested -= HandleStaticShopRequested;
            BoosterController.OnShopRequested += HandleStaticShopRequested;
        }

        private static void HandleStaticShopRequested()
        {
            EnsureInstance();
            Instance?.Open();
        }

        public static void EnsureInstance()
        {
            if (Instance != null) return;
            var prefab = Resources.Load<GameObject>("UI/Panel_Shop");
            if (prefab != null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    var go = Instantiate(prefab, canvas.transform);
                    go.name = "Panel_Shop";
                }
            }
        }

        private void OnEnable()
        {
            CoinPillUI.OnShopRequested += HandleShopRequested;
            BoosterController.OnShopRequested += HandleShopRequested;
        }

        private void OnDisable()
        {
            CoinPillUI.OnShopRequested -= HandleShopRequested;
            BoosterController.OnShopRequested -= HandleShopRequested;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void WireCardEvents()
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card != null)
                    card.OnPurchased += HandleCardPurchased;
            }
        }

        private void HandleShopRequested()
        {
            Open();
        }

        private void HandleCardPurchased(ShopCardUI card)
        {
            AudioManager.Instance?.PlayCoin();

            // Refresh UI coin pill in current scene if available
            if (card != null && card.coinReward > 0)
            {
                GameplayHUD.Instance?.AddCoins(card.coinReward);
                // Also update MapSceneController coin pill if present
                var map = FindFirstObjectByType<MapSceneController>();
                if (map != null && map.coinPill != null)
                {
                    map.coinPill.AddCoins(card.coinReward);
                }
            }

            // Refresh state across all cards (e.g. if ads removed)
            RefreshCards();
        }

        // ── Public API ────────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            _isOpen = true;

            // Reset scroll to first card (Disable Ads)
            if (scrollRect != null)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
            }

            RefreshCards();
            AudioManager.Instance?.PlayPopupOpen();

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateOpen());
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            AudioManager.Instance?.PlayButton();

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateClose());
        }

        public void RefreshCards()
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card != null)
                    card.RefreshState();
            }
        }

        // ── Animations ────────────────────────────────────────

        private IEnumerator AnimateOpen()
        {
            if (panelCard == null) yield break;

            panelCard.localScale = Vector3.zero;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

            Color dimTarget = new Color(0f, 0f, 0f, 0.72f);
            if (dimOverlay != null) dimOverlay.color = Color.clear;

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);

                // Overshoot spring curve: 0 -> 1.08 -> 1.0
                float s = EaseOutBack(t);
                panelCard.localScale = Vector3.one * s;

                if (panelCanvasGroup != null)
                    panelCanvasGroup.alpha = Mathf.Clamp01(t * 1.5f);

                if (dimOverlay != null)
                    dimOverlay.color = Color.Lerp(Color.clear, dimTarget, t);

                yield return null;
            }

            panelCard.localScale = Vector3.one;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
            if (dimOverlay != null) dimOverlay.color = dimTarget;
            _animCoroutine = null;
        }

        private IEnumerator AnimateClose()
        {
            if (panelCard == null) yield break;

            Vector3 startScale = panelCard.localScale;
            Color dimStart = dimOverlay != null ? dimOverlay.color : Color.clear;
            float elapsed = 0f;

            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);

                // Smooth scale down
                float s = 1f - Mathf.SmoothStep(0f, 1f, t);
                panelCard.localScale = startScale * s;

                if (panelCanvasGroup != null)
                    panelCanvasGroup.alpha = s;

                if (dimOverlay != null)
                    dimOverlay.color = Color.Lerp(dimStart, Color.clear, t);

                yield return null;
            }

            panelCard.localScale = Vector3.zero;
            if (dimOverlay != null) dimOverlay.color = Color.clear;
            gameObject.SetActive(false);
            _animCoroutine = null;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
