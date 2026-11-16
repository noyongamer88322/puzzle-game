// ============================================================
// GameOverPopupUI.cs  –  Arrow Slide Puzzle
// "Out of Hearts" popup (PRD step – Game Over screen):
//   • Triggered when GameplayController.OnLevelLost fires (hearts == 0)
//   • Shows:  "Out of hearts"  title
//             [Retry]   – restart the current level immediately
//             [Revive]  – restore 1 heart and continue (coin-spend OR reward-ad)
//   • Revive path:
//       1. If player has enough coins (ReviveCoinCost) → spend coins, restore 1 heart
//       2. Otherwise → show reward-ad (mock in this build); on ad complete → restore 1 heart
//   • After Revive the popup closes and the level resumes (Time.timeScale reset to 1)
//   • After Retry  the level scene reloads
//
// Uses UIPanelOvershoot for scale-in animation.
// Attach this component to a child panel GameObject inside the Game scene Canvas.
//
// PRD Section 7: Assets/_Game/Scripts/UI/GameOverPopupUI.cs
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Gameplay;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class GameOverPopupUI : MonoBehaviour
    {
        // ── Singleton ───────────────────────────────────────────────
        public static GameOverPopupUI Instance { get; private set; }

        // ── Inspector: Panel Animation ───────────────────────────────
        [Header("Panel Animation")]
        [Tooltip("UIPanelOvershoot on the panel card – drives scale-in/out.")]
        public UIPanelOvershoot panelOvershoot;

        // ── Inspector: Title ─────────────────────────────────────────
        [Header("Text Labels")]
        [Tooltip("Main title label – should read 'Out of hearts'.")]
        public TextMeshProUGUI titleLabel;

        [Tooltip("Optional sub-label below title (e.g. 'Don't give up!').")]
        public TextMeshProUGUI subtitleLabel;

        // ── Inspector: Heart Display ──────────────────────────────────
        [Header("Heart Icons (optional)")]
        [Tooltip("Array of heart Image slots to display current 0/max hearts.")]
        public Image[] heartSlots;

        [Tooltip("Full heart sprite.")]
        public Sprite heartFullSprite;

        [Tooltip("Empty heart sprite.")]
        public Sprite heartEmptySprite;

        // ── Inspector: Buttons ───────────────────────────────────────
        [Header("Buttons")]
        [Tooltip("Retry button – reloads the current level.")]
        public Button retryButton;

        [Tooltip("Revive button – restore 1 heart with coins or a reward ad.")]
        public Button reviveButton;

        [Tooltip("Label on the Revive button (shows cost or 'Watch Ad').")]
        public TextMeshProUGUI reviveLabel;

        [Tooltip("Coin icon shown on the Revive button (hidden when showing 'Watch Ad').")]
        public GameObject reviveCoinIcon;

        // ── Inspector: Coin Display ───────────────────────────────────
        [Header("Coin Display")]
        [Tooltip("Text showing the player's current coin count in the popup.")]
        public TextMeshProUGUI playerCoinsLabel;

        // ── Inspector: Overlay ───────────────────────────────────────
        [Header("Dim Overlay")]
        [Tooltip("Full-screen dim overlay Image (semi-transparent black).")]
        public Image dimOverlay;

        // ── Config ───────────────────────────────────────────────────
        [Header("Config")]
        [Tooltip("Coin cost to revive (restore 1 heart). PRD/Rule: 300 coins.")]
        public int reviveCoinCost = 300;

        [Tooltip("Delay (seconds) before the popup appears after the level is lost.")]
        public float showDelay = 0.8f;

        // ── Runtime ──────────────────────────────────────────────────
        private bool _isOpen;
        private bool _hasRevived; // only one revive per level

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            gameObject.SetActive(false); // hidden until triggered
        }

        private void OnEnable()
        {
            GameplayController.OnLevelLost += HandleLevelLost;
            if (retryButton  != null) retryButton.onClick.AddListener(OnRetry);
            if (reviveButton != null) reviveButton.onClick.AddListener(OnRevive);
        }

        private void OnDisable()
        {
            GameplayController.OnLevelLost -= HandleLevelLost;
            if (retryButton  != null) retryButton.onClick.RemoveListener(OnRetry);
            if (reviveButton != null) reviveButton.onClick.RemoveListener(OnRevive);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Event handler ────────────────────────────────────────────

        private void HandleLevelLost()
        {
            StartCoroutine(ShowWithDelay());
        }

        private IEnumerator ShowWithDelay()
        {
            yield return new WaitForSecondsRealtime(showDelay);
            ShowPopup();
        }

        // ── Core logic ───────────────────────────────────────────────

        private void ShowPopup()
        {
            _isOpen    = true;
            _hasRevived = false;

            gameObject.SetActive(true);

            // Update title
            if (titleLabel != null)    titleLabel.text    = "Out of hearts";
            if (subtitleLabel != null) subtitleLabel.text = "Don't give up!";

            // Update hearts display (all empty)
            RefreshHeartDisplay(0);

            // Update player coin display
            RefreshCoinDisplay();

            // Decide Revive button text
            RefreshReviveButton();

            // Show dim overlay
            if (dimOverlay != null)
                dimOverlay.color = new Color(0f, 0f, 0f, 0.6f);

            // Animate in
            if (panelOvershoot != null)
                panelOvershoot.Open();
            else
                AudioManager.Instance?.PlayPopupOpen();
        }

        private void RefreshHeartDisplay(int currentHearts)
        {
            if (heartSlots == null) return;
            int max = GameplayController.Instance != null
                ? GameplayController.Instance.maxHearts
                : heartSlots.Length;

            for (int i = 0; i < heartSlots.Length; i++)
            {
                if (heartSlots[i] == null) continue;
                bool filled = i < currentHearts;
                if (filled && heartFullSprite  != null) heartSlots[i].sprite = heartFullSprite;
                if (!filled && heartEmptySprite != null) heartSlots[i].sprite = heartEmptySprite;
            }
        }

        private void RefreshCoinDisplay()
        {
            if (playerCoinsLabel == null) return;
            int coins = SaveManager.Instance?.Coins ?? 0;
            playerCoinsLabel.text = coins.ToString("N0");
        }

        private void RefreshReviveButton()
        {
            if (_hasRevived)
            {
                // Already used revive this level – disable the button
                if (reviveButton != null) reviveButton.interactable = false;
                if (reviveLabel  != null) reviveLabel.text = "Revived";
                return;
            }

            int coins = SaveManager.Instance?.Coins ?? 0;
            bool canAfford = coins >= reviveCoinCost;

            if (reviveLabel != null)
                reviveLabel.text = $"{reviveCoinCost}";
            if (reviveCoinIcon != null)
                reviveCoinIcon.SetActive(true);

            if (reviveButton != null)
                reviveButton.interactable = canAfford;
        }

        // ── Button callbacks ─────────────────────────────────────────

        private void OnRetry()
        {
            if (!_isOpen) return;
            AudioManager.Instance?.PlayButton();

            _isOpen = false;
            Time.timeScale = 1f;

            // Reload the current scene to restart the level
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnRevive()
        {
            if (!_isOpen || _hasRevived) return;
            AudioManager.Instance?.PlayButton();

            int coins      = SaveManager.Instance?.Coins ?? 0;
            bool canAfford = coins >= reviveCoinCost;

            if (canAfford)
            {
                // Spend coins and revive immediately
                SaveManager.Instance.SpendCoins(reviveCoinCost);
                Debug.Log($"[GameOverPopupUI] Revived using {reviveCoinCost} coins.");
                CompleteRevive();
            }
            else
            {
                AudioManager.Instance?.PlayBlockedTap();
                Debug.Log("[GameOverPopupUI] Insufficient coins to revive.");
            }
        }

        // ── Revive implementation ─────────────────────────────────────

        private void CompleteRevive()
        {
            _hasRevived = true;

            // Restore 1 heart in GameplayController and resume the level
            if (GameplayController.Instance != null)
            {
                GameplayController.Instance.ReviveWithOneHeart();
            }

            // Update HUD heart display
            if (GameplayHUD.Instance != null && GameplayHUD.Instance.heartPill != null)
            {
                int current = GameplayController.Instance != null
                    ? GameplayController.Instance.CurrentHearts
                    : 1;
                int max = GameplayController.Instance != null
                    ? GameplayController.Instance.maxHearts
                    : 3;
                GameplayHUD.Instance.heartPill.SetHearts(current, max);
            }

            // Close the popup
            _isOpen        = false;
            Time.timeScale = 1f;

            if (panelOvershoot != null)
                panelOvershoot.Close();
            else
                gameObject.SetActive(false);

            if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, 0f);

            Debug.Log("[GameOverPopupUI] Revive complete – game resumes.");
        }

#if UNITY_EDITOR
        [ContextMenu("Test Show Popup")]
        private void TestShowPopup() => ShowPopup();

        [ContextMenu("Test Close Popup")]
        private void TestClosePopup()
        {
            _isOpen = false;
            if (panelOvershoot != null) panelOvershoot.Close();
            else gameObject.SetActive(false);
        }
#endif
    }
}

