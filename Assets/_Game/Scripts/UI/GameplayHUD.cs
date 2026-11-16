// ============================================================
// GameplayHUD.cs  –  Arrow Slide Puzzle
// Master controller for the Gameplay screen HUD.
// Reads LevelData flags (showHeartPill, showBackButton,
// showHintButton, showMagnetButton) and shows/hides each panel.
// Subscribes to GameplayController events for live updates.
//
// Canvas: 1080×1920, Canvas Scaler – Scale With Screen Size,
//         Match = 0.5 (width+height blend).
//
// PRD Section 7: Assets/_Game/Scripts/UI/GameplayHUD.cs
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Gameplay;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class GameplayHUD : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static GameplayHUD Instance { get; private set; }

        // ── Inspector References ──────────────────────────────
        [Header("Panels")]
        [Tooltip("Root GameObject of the coin pill (top-left).")]
        public CoinPillUI coinPill;

        [Tooltip("Root GameObject of the heart pill (top-centre).")]
        public HeartPillUI heartPill;

        [Tooltip("Back button (orange, top-right area).")]
        public GameObject backButton;

        [Tooltip("Gear/settings button (green, top-right).")]
        public GameObject gearButton;

        [Header("Level Info")]
        [Tooltip("TMP text showing the current level goal (e.g. 'Level 1').")]
        public TextMeshProUGUI levelGoalText;

        [Header("Power Buttons")]
        [Tooltip("Hint button (right side, vertical stack).")]
        public PowerButtonUI hintButton;

        [Tooltip("Magnet button (right side, below hint).")]
        public PowerButtonUI magnetButton;

        [Header("Shop")]
        [Tooltip("Shop pop-up panel.")]
        public ShopPanelUI shopPanel;

        [Header("Blocked Feedback Overlay")]
        [Tooltip("Full-screen UI overlay Image for pink/red edge glow vignette on blocked tap.")]
        public Image blockedVignetteOverlay;

        [Tooltip("Peak alpha of the blocked vignette overlay flash.")]
        public float vignettePeakAlpha = 0.85f;

        [Tooltip("Duration of the blocked vignette overlay flash (seconds).")]
        public float vignetteFlashDuration = 0.35f;

        private Coroutine _vignetteFlashRoutine;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureBoosterController();

            if (blockedVignetteOverlay != null)
            {
                var c = blockedVignetteOverlay.color;
                c.a = 0f;
                blockedVignetteOverlay.color = c;
                blockedVignetteOverlay.raycastTarget = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            EnsureBoosterController();

            if (coinPill != null && ArrowPuzzle.Systems.GameState.Instance != null)
            {
                coinPill.SetCoins(ArrowPuzzle.Systems.GameState.Instance.Coins);
            }

            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelData != null)
            {
                ApplyLevelData(LevelLoader.Instance.CurrentLevelData);
            }
            else if (GameManager.Instance != null && GameManager.Instance.ActiveLevel != null)
            {
                ApplyLevelData(GameManager.Instance.ActiveLevel);
            }
            else
            {
                int lvl = ArrowPuzzle.Systems.SaveManager.Instance?.PendingLevel ?? 1;
                ApplyLevelIndex(lvl);
            }
        }

        /// <summary>
        /// Fallback to configure progressive HUD visibility based on level index.
        /// L1: coin + gear only
        /// L2-3: + heart
        /// L4-5: + back + hint
        /// L6+: + magnet
        /// </summary>
        public void ApplyLevelIndex(int levelIndex)
        {
            if (coinPill != null) coinPill.gameObject.SetActive(true);
            if (gearButton != null) gearButton.SetActive(true);

            // Level goal text
            if (levelGoalText != null)
                levelGoalText.text = $"Level {levelIndex}";

            bool showHeart = levelIndex >= 2;
            if (heartPill != null)
            {
                heartPill.gameObject.SetActive(showHeart);
                if (showHeart)
                    heartPill.SetHearts(GameplayController.Instance != null ? GameplayController.Instance.CurrentHearts : 3, 3);
            }

            bool showBackAndHint = levelIndex >= 4;
            if (backButton != null) backButton.SetActive(showBackAndHint);
            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(showBackAndHint);
                if (showBackAndHint)
                {
                    bool isFree = ArrowPuzzle.Systems.GameState.Instance != null
                        ? ArrowPuzzle.Systems.GameState.Instance.IsHintFree
                        : true;
                    hintButton.SetFreeTag(isFree);
                    hintButton.SetCost(Economy.HintCost);
                }
            }

            bool showMagnet = levelIndex >= 6;
            if (magnetButton != null)
            {
                magnetButton.gameObject.SetActive(showMagnet);
                if (showMagnet)
                {
                    bool isFree = ArrowPuzzle.Systems.GameState.Instance != null
                        ? ArrowPuzzle.Systems.GameState.Instance.IsMagnetFree
                        : true;
                    magnetButton.SetFreeTag(isFree);
                    magnetButton.SetCost(Economy.MagnetCost);
                }
            }
        }

        private void OnEnable()
        {
            GameplayController.OnHeartChanged += HandleHeartChanged;
            GameplayController.OnBlocked      += HandleBlockedTap;
            if (hintButton != null) hintButton.OnClicked += HandlePowerButtonClicked;
            if (magnetButton != null) magnetButton.OnClicked += HandlePowerButtonClicked;
            BoosterController.OnShopRequested += HandleShopRequested;
            CoinPillUI.OnShopRequested += HandleShopRequested;
        }

        private void OnDisable()
        {
            GameplayController.OnHeartChanged -= HandleHeartChanged;
            GameplayController.OnBlocked      -= HandleBlockedTap;
            if (hintButton != null) hintButton.OnClicked -= HandlePowerButtonClicked;
            if (magnetButton != null) magnetButton.OnClicked -= HandlePowerButtonClicked;
            BoosterController.OnShopRequested -= HandleShopRequested;
            CoinPillUI.OnShopRequested -= HandleShopRequested;
        }

        private void HandleShopRequested()
        {
            if (shopPanel != null)
                shopPanel.Open();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Call this after a level is loaded to configure HUD visibility
        /// based on LevelData flags.
        /// </summary>
        public void ApplyLevelData(LevelData data)
        {
            if (data == null) return;

            // Coin pill is always visible
            if (coinPill != null) coinPill.gameObject.SetActive(true);

            // Gear is always visible
            if (gearButton != null) gearButton.SetActive(true);

            // Level goal text
            if (levelGoalText != null)
                levelGoalText.text = string.IsNullOrEmpty(data.displayName)
                    ? $"Level {data.levelIndex}"
                    : data.displayName;

            // Heart pill – level 2/3+
            if (heartPill != null)
            {
                heartPill.gameObject.SetActive(data.showHeartPill);
                if (data.showHeartPill)
                    heartPill.SetHearts(GameplayController.Instance != null
                        ? GameplayController.Instance.CurrentHearts
                        : 3, 3);
            }

            // Back button – level 4+
            if (backButton != null)
                backButton.SetActive(data.showBackButton);

            // Hint button – level 4+
            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(data.showHintButton);
                if (data.showHintButton)
                {
                    bool isFree = ArrowPuzzle.Systems.GameState.Instance != null
                        ? ArrowPuzzle.Systems.GameState.Instance.IsHintFree
                        : data.hintIsFree;
                    hintButton.SetFreeTag(isFree);
                    hintButton.SetCost(Economy.HintCost);
                }
            }

            // Magnet button – heart-shape level (level 6) only
            if (magnetButton != null)
            {
                magnetButton.gameObject.SetActive(data.showMagnetButton);
                if (data.showMagnetButton)
                {
                    bool isFree = ArrowPuzzle.Systems.GameState.Instance != null
                        ? ArrowPuzzle.Systems.GameState.Instance.IsMagnetFree
                        : data.magnetIsFree;
                    magnetButton.SetFreeTag(isFree);
                    magnetButton.SetCost(Economy.MagnetCost);
                }
            }
        }

        /// <summary>Adds coins visually to the coin pill.</summary>
        public void AddCoins(int amount)
        {
            if (coinPill != null)
                coinPill.AddCoins(amount);
        }

        private void EnsureBoosterController()
        {
            if (BoosterController.Instance == null)
            {
                var bc = FindObjectOfType<BoosterController>();
                if (bc == null)
                {
                    var go = new GameObject("BoosterController");
                    go.AddComponent<BoosterController>();
                }
            }
        }

        private void HandlePowerButtonClicked(PowerButtonType type)
        {
            EnsureBoosterController();

            // Tutorial input gating: in step 2 only Hint works; in step 3 only Magnet works
            if (TutorialController.Instance != null && TutorialController.Instance.IsTutorialActive)
            {
                if (type == PowerButtonType.Hint && TutorialController.Instance.CurrentStep == 2)
                {
                    TutorialController.Instance.CompleteCurrentStep();
                }
                else if (type == PowerButtonType.Magnet && TutorialController.Instance.CurrentStep == 3)
                {
                    TutorialController.Instance.CompleteCurrentStep();
                }
                else
                {
                    return; // Block clicking wrong booster during tutorial
                }
            }

            if (type == PowerButtonType.Hint)
            {
                BoosterController.Instance?.UseHint();
            }
            else if (type == PowerButtonType.Magnet)
            {
                BoosterController.Instance?.UseMagnet();
            }
        }

        // ── Event Handlers ────────────────────────────────────

        private void HandleHeartChanged(int current, int max)
        {
            if (heartPill != null && heartPill.gameObject.activeSelf)
                heartPill.LoseHeart(current, max);
        }

        private void HandleBlockedTap(ArrowData data)
        {
            TriggerBlockedVignette();
        }

        /// <summary>
        /// Flashes the pink/red screen-edge glow vignette overlay image.
        /// </summary>
        public void TriggerBlockedVignette()
        {
            if (blockedVignetteOverlay == null) return;
            if (_vignetteFlashRoutine != null) StopCoroutine(_vignetteFlashRoutine);
            _vignetteFlashRoutine = StartCoroutine(BlockedVignetteRoutine());
        }

        private IEnumerator BlockedVignetteRoutine()
        {
            if (blockedVignetteOverlay == null) yield break;

            blockedVignetteOverlay.gameObject.SetActive(true);
            float fadeIn  = 0.05f;
            float fadeOut = Mathf.Max(0.05f, vignetteFlashDuration - fadeIn);
            float elapsed = 0f;

            Color c = blockedVignetteOverlay.color;

            // Fast ramp up
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeIn;
                c.a = Mathf.Lerp(0f, vignettePeakAlpha, t);
                blockedVignetteOverlay.color = c;
                yield return null;
            }

            c.a = vignettePeakAlpha;
            blockedVignetteOverlay.color = c;
            elapsed = 0f;

            // Smooth fade out
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOut;
                float eased = 1f - (t * t);
                c.a = Mathf.Lerp(0f, vignettePeakAlpha, eased);
                blockedVignetteOverlay.color = c;
                yield return null;
            }

            c.a = 0f;
            blockedVignetteOverlay.color = c;
            _vignetteFlashRoutine = null;
        }

        // ── Settings (gear) button ────────────────────────────────────

        /// <summary>Called by the gear button onClick in the Canvas prefab.</summary>
        public void OnGearButtonPressed()
        {
            AudioManager.Instance?.PlayButton();
            if (SettingsPanelUI.Instance != null)
                SettingsPanelUI.Instance.Open();
            else
                Debug.Log("[GameplayHUD] Gear tapped – SettingsPanelUI not in scene.");
        }

        // ── Back button ───────────────────────────────────────────────

        /// <summary>Called by the back button onClick in the Canvas prefab.</summary>
        public void OnBackButtonPressed()
        {
            AudioManager.Instance?.PlayButton();
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene("Map");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Map");
        }
    }
}