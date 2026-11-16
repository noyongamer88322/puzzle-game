// ============================================================
// MapSceneController.cs  –  Arrow Slide Puzzle
// Home / Map screen controller (Step 11 of PRD).
//
// Layout (PRD Section 3.3 – Map / Home):
//   • Full-screen painted background (bg_map_hills.png or green placeholder)
//   • Top-left : Coin pill (CoinPillUI) – tap opens Shop (event only, Step 13)
//   • Below coin: 'Remove Ads' red label pill ($3.99)
//   • Top-right: Green Gear button (settings, placeholder)
//   • Centre-bottom: Chapter pill (name + progress bar + star x/5)
//   • Below chapter pill: big green "Level N" button with breathing pulse
//   • Bottom-centre: small gallery icon button
//
// Transitions:
//   • From LevelCleared: coin & star icons fly to their counters, progress bar fills.
//   • "Level N" tap → Game scene with light-blue fade.
//   • First time opening (Step 10 hook): hand pointer on Level button.
//
// PRD Section 7: Assets/_Game/Scripts/UI/MapSceneController.cs
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;
using ArrowPuzzle.Gameplay;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class MapSceneController : MonoBehaviour
    {
        // ── Inspector: Background ────────────────────────────────
        [Header("Background")]
        [Tooltip("Full-screen background Image (bg_map_hills or placeholder).")]
        public Image backgroundImage;

        [Tooltip("Hills chapter background sprite. If null, a green placeholder colour is used.")]
        public Sprite bg_HillsSprite;

        // ── Inspector: Top-Left HUD ──────────────────────────────
        [Header("Top-Left HUD")]
        [Tooltip("CoinPillUI component (top-left).")]
        public CoinPillUI coinPill;

        [Tooltip("'Remove Ads' pill button (red, below coin pill).")]
        public Button removeAdsButton;

        [Tooltip("'Remove Ads' label text.")]
        public TextMeshProUGUI removeAdsLabel;

        // ── Inspector: Top-Right HUD ─────────────────────────────
        [Header("Top-Right HUD")]
        [Tooltip("Gear / settings button (top-right, green).")]
        public Button gearButton;

        // ── Inspector: Chapter Pill ──────────────────────────────
        [Header("Chapter Pill")]
        [Tooltip("Chapter name label (e.g. 'Hills').")]
        public TextMeshProUGUI chapterNameLabel;

        [Tooltip("Progress bar fill image (fill amount = levels cleared in chapter / levels per chapter).")]
        public Image chapterProgressFill;

        [Tooltip("Star count label (e.g. '3/5').")]
        public TextMeshProUGUI chapterStarLabel;

        [Tooltip("Star icon images in the pill (up to 5 slots).")]
        public Image[] chapterStarImages;

        [Tooltip("Filled star sprite.")]
        public Sprite starFilledSprite;

        [Tooltip("Empty star sprite.")]
        public Sprite starEmptySprite;

        // ── Inspector: Level Button ──────────────────────────────
        [Header("Level Button")]
        [Tooltip("The big green 'Level N' play button.")]
        public Button levelButton;

        [Tooltip("Label on the level button (e.g. 'Level 4').")]
        public TextMeshProUGUI levelButtonLabel;

        [Tooltip("RectTransform of the level button (used for breathing pulse).")]
        public RectTransform levelButtonRect;

        // ── Inspector: Gallery Button ────────────────────────────
        [Header("Gallery Button")]
        [Tooltip("Bottom-centre small gallery icon button.")]
        public Button galleryButton;

        // ── Inspector: Gallery Panel ─────────────────────────────
        [Header("Gallery Panel")]
        [Tooltip("GalleryPanelUI component (the pop-up gallery panel).")]
        public GalleryPanelUI galleryPanel;

        // ── Inspector: Settings Panel ────────────────────────────
        [Header("Settings Panel")]
        [Tooltip("SettingsPanelUI component (the pop-up settings panel).")]
        public SettingsPanelUI settingsPanel;

        // ── Inspector: Shop Panel ────────────────────────────────
        [Header("Shop Panel")]
        [Tooltip("ShopPanelUI component (the pop-up shop panel).")]
        public ShopPanelUI shopPanel;

        // ── Inspector: Hand Pointer Tutorial ────────────────────
        [Header("Tutorial – Hand Pointer (Step 10)")]
        [Tooltip("Hand pointer image shown on first visit (Step 10 hook).")]
        public RectTransform handPointer;

        // ── Inspector: Reward Fly VFX ────────────────────────────
        [Header("Reward Fly VFX")]
        [Tooltip("Flying coin icon prefab (small, used for the fly-to-counter animation).")]
        public RectTransform flyingCoinIconPrefab;

        [Tooltip("Flying star icon prefab (small, used for the fly-to-counter animation).")]
        public RectTransform flyingStarIconPrefab;

        [Tooltip("Canvas used as parent for flying icons.")]
        public Canvas rootCanvas;

        // ── Inspector: Scene Transition Overlay ─────────────────
        [Header("Scene Transition")]
        [Tooltip("Full-screen overlay Image for the light-blue fade transition.")]
        public Image fadeOverlay;

        // ── Runtime ───────────────────────────────────────────────
        private int _currentLevel;
        private int _chapterIndex;        // 0-based
        private int _levelsIntoChapter;   // 0-4 (cleared within chapter)
        private Coroutine _breathCoroutine;

        // ── Chapter names (index-matched, Hills = 0) ─────────────
        private static readonly string[] ChapterNames =
        {
            "Hills",
            "Forest",
            "Ocean",
            "Desert",
            "Sky",
            "Snow"
        };

        // ─────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Apply background
            ApplyBackground();

            // Auto-resolve popup panels if not assigned
            if (galleryPanel == null)  galleryPanel  = FindObjectOfType<GalleryPanelUI>(true);
            if (settingsPanel == null) settingsPanel = FindObjectOfType<SettingsPanelUI>(true);
            if (shopPanel == null)     shopPanel     = FindObjectOfType<ShopPanelUI>(true);

            // Wire up buttons
            if (levelButton   != null) levelButton.onClick.AddListener(OnLevelButtonPressed);
            if (gearButton    != null) gearButton.onClick.AddListener(OnGearPressed);
            if (galleryButton != null) galleryButton.onClick.AddListener(OnGalleryPressed);
            if (removeAdsButton != null) removeAdsButton.onClick.AddListener(OnRemoveAdsPressed);

            // Start fade in (clear overlay)
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.color = new Color(0.53f, 0.81f, 0.98f, 1f); // light blue
            }
        }

        private void Start()
        {
            if (removeAdsButton != null)
                removeAdsButton.gameObject.SetActive(false);

            RefreshFromGameState();
            StartBreathingPulse();
            StartCoroutine(FadeIn());

            // Check if we arrived from a Level Cleared event
            int clearedLevel = SaveManager.Instance.PendingClearedLevel;
            if (clearedLevel > 0)
            {
                SaveManager.Instance.PendingClearedLevel = 0;

                // Only play fly animation once per cleared level
                if (!SaveManager.Instance.IsRewardFlown(clearedLevel))
                {
                    SaveManager.Instance.SetRewardFlown(clearedLevel, true);
                    StartCoroutine(PlayRewardFlyAnimation(clearedLevel));
                }
                else
                {
                    // Already played – just fill progress bar instantly
                    SetProgressBar(_levelsIntoChapter, false);
                }
            }
            else
            {
                SetProgressBar(_levelsIntoChapter, false);
            }

            // Hand-pointer tutorial hook (Step 10 – first time on Level button)
            bool tutorialDone = SaveManager.Instance.IsMapLevelButtonTutorialDone;
            if (!tutorialDone && handPointer != null)
            {
                StartCoroutine(ShowHandPointer());
            }
            else if (handPointer != null)
            {
                handPointer.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (levelButton   != null) levelButton.onClick.RemoveListener(OnLevelButtonPressed);
            if (gearButton    != null) gearButton.onClick.RemoveListener(OnGearPressed);
            if (galleryButton != null) galleryButton.onClick.RemoveListener(OnGalleryPressed);
            if (removeAdsButton != null) removeAdsButton.onClick.RemoveListener(OnRemoveAdsPressed);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                SetSelectedLevel(1);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                SetSelectedLevel(2);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                SetSelectedLevel(3);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                SetSelectedLevel(4);
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                SetSelectedLevel(5);
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
                SetSelectedLevel(6);
        }

        private void SetSelectedLevel(int lvl)
        {
            _currentLevel = lvl;
            SaveManager.Instance.PendingLevel = lvl;
            SaveManager.Instance.CurrentLevel = lvl;
            if (levelButtonLabel != null)
                levelButtonLabel.text = $"Level {_currentLevel}";
            Debug.Log($"[MapSceneController] Selected Level set to {_currentLevel}");
        }
#endif

        // ─────────────────────────────────────────────────────────
        // State refresh
        // ─────────────────────────────────────────────────────────

        private void RefreshFromGameState()
        {
            var state = GameState.Instance;

            // Which level to show on the button
            _currentLevel = Mathf.Max(1, state.HighestUnlockedLevel);

            // Chapter computation (0-based, every LevelsPerChapter levels)
            int levelsPerChapter = GameConfig.LevelsPerChapter;
            _chapterIndex       = (_currentLevel - 1) / levelsPerChapter;
            _levelsIntoChapter  = (_currentLevel - 1) % levelsPerChapter;
            // Count how many levels in current chapter are actually cleared
            int chapterStart = _chapterIndex * levelsPerChapter + 1;
            int clearedInChapter = 0;
            for (int i = chapterStart; i < chapterStart + levelsPerChapter; i++)
            {
                if (SaveManager.Instance.IsLevelCleared(i))
                    clearedInChapter++;
            }
            _levelsIntoChapter = clearedInChapter;

            // Update chapter pill
            string chapterName = _chapterIndex < ChapterNames.Length
                ? ChapterNames[_chapterIndex]
                : $"Chapter {_chapterIndex + 1}";
            if (chapterNameLabel != null) chapterNameLabel.text = chapterName;

            // Star label
            int starsTotal = Mathf.Min(state.TotalStars, GameConfig.LevelsPerChapter);
            int starsInChapter = Mathf.Min(clearedInChapter, GameConfig.LevelsPerChapter);
            if (chapterStarLabel != null)
                chapterStarLabel.text = $"{starsInChapter}/{GameConfig.LevelsPerChapter}";

            // Star icon fill
            if (chapterStarImages != null)
            {
                for (int i = 0; i < chapterStarImages.Length; i++)
                {
                    if (chapterStarImages[i] == null) continue;
                    bool filled = (i < starsInChapter);
                    if (starFilledSprite != null && starEmptySprite != null)
                        chapterStarImages[i].sprite = filled ? starFilledSprite : starEmptySprite;
                    else
                        chapterStarImages[i].color = filled
                            ? new Color(1f, 0.85f, 0f)   // gold
                            : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                }
            }

            // Level button label
            if (levelButtonLabel != null)
                levelButtonLabel.text = $"Level {_currentLevel}";

            // Coin pill
            if (coinPill != null)
                coinPill.SetCoins(state.Coins);
        }

        // ─────────────────────────────────────────────────────────
        // Background
        // ─────────────────────────────────────────────────────────

        private void ApplyBackground()
        {
            if (backgroundImage == null) return;

            if (bg_HillsSprite != null)
            {
                backgroundImage.sprite = bg_HillsSprite;
                backgroundImage.color  = Color.white;
            }
            else
            {
                // Placeholder: warm sage-green painted look
                backgroundImage.sprite = null;
                backgroundImage.color  = new Color(0.46f, 0.72f, 0.44f);
            }
        }

        // ─────────────────────────────────────────────────────────
        // Progress bar
        // ─────────────────────────────────────────────────────────

        private void SetProgressBar(int cleared, bool animate)
        {
            if (chapterProgressFill == null) return;
            float target = Mathf.Clamp01((float)cleared / GameConfig.LevelsPerChapter);
            if (animate)
                StartCoroutine(AnimateProgressBar(target));
            else
                chapterProgressFill.fillAmount = target;
        }

        private IEnumerator AnimateProgressBar(float target)
        {
            float start   = chapterProgressFill.fillAmount;
            float elapsed = 0f;
            float dur     = GameConfig.ProgressBarFillDuration;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                // Ease-out cubic
                t = 1f - Mathf.Pow(1f - t, 3f);
                chapterProgressFill.fillAmount = Mathf.Lerp(start, target, t);
                yield return null;
            }
            chapterProgressFill.fillAmount = target;
        }

        // ─────────────────────────────────────────────────────────
        // Breathing pulse on Level button
        // ─────────────────────────────────────────────────────────

        private void StartBreathingPulse()
        {
            if (levelButtonRect == null) return;
            if (_breathCoroutine != null) StopCoroutine(_breathCoroutine);
            _breathCoroutine = StartCoroutine(BreathingLoop());
        }

        private IEnumerator BreathingLoop()
        {
            float half = GameConfig.BreathingPulseDuration * 0.5f;
            float maxS = GameConfig.BreathingPulseScale;
            while (true)
            {
                // Expand
                float elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
                    levelButtonRect.localScale = Vector3.one * Mathf.Lerp(1f, maxS, t);
                    yield return null;
                }
                // Contract
                elapsed = 0f;
                while (elapsed < half)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
                    levelButtonRect.localScale = Vector3.one * Mathf.Lerp(maxS, 1f, t);
                    yield return null;
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // Hand pointer tutorial (Step 10 hook)
        // ─────────────────────────────────────────────────────────

        private IEnumerator ShowHandPointer()
        {
            if (handPointer == null) yield break;
            handPointer.gameObject.SetActive(true);

            // Position hand pointer over the level button
            if (levelButtonRect != null)
                handPointer.position = levelButtonRect.position + new Vector3(30f, -30f, 0f);

            // Loop a small tap animation until the user taps the button
            while (true)
            {
                float t = 0f;
                while (t < 0.4f)
                {
                    t += Time.deltaTime;
                    handPointer.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t / 0.4f);
                    yield return null;
                }
                t = 0f;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    handPointer.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t / 0.3f);
                    yield return null;
                }
                yield return new WaitForSeconds(0.6f);
            }
        }

        private void DismissHandPointer()
        {
            if (handPointer == null) return;
            handPointer.gameObject.SetActive(false);
            StopCoroutine(ShowHandPointer()); // safe no-op if not running
            SaveManager.Instance.SetMapLevelButtonTutorialDone(true);
        }

        // ─────────────────────────────────────────────────────────
        // Reward fly animation (coin & star → counter)
        // ─────────────────────────────────────────────────────────

        private IEnumerator PlayRewardFlyAnimation(int clearedLevel)
        {
            // Slight delay so the scene has fully faded in
            yield return new WaitForSeconds(0.6f);

            // Fly a coin icon
            if (flyingCoinIconPrefab != null && coinPill != null && rootCanvas != null)
            {
                var coinFly = Instantiate(flyingCoinIconPrefab, rootCanvas.transform);
                // Start from screen centre-bottom (approximate cleared screen position)
                coinFly.anchoredPosition = new Vector2(0f, -300f);
                yield return StartCoroutine(FlyIcon(coinFly, coinPill.GetComponent<RectTransform>()));
                Destroy(coinFly.gameObject);
            }

            // Animate coin pill count-up
            if (coinPill != null)
            {
                coinPill.AddCoins(GameConfig.CoinsPerLevel);
                if (VFXManager.Instance != null)
                    VFXManager.Instance.SpawnCoinBurst(coinPill.transform.position, 10);
            }

            yield return new WaitForSeconds(0.15f);

            // Fly a star icon
            if (flyingStarIconPrefab != null && chapterProgressFill != null && rootCanvas != null)
            {
                var starFly = Instantiate(flyingStarIconPrefab, rootCanvas.transform);
                starFly.anchoredPosition = new Vector2(0f, -300f);
                var pillRect = chapterProgressFill.rectTransform.parent as RectTransform
                               ?? chapterProgressFill.rectTransform;
                yield return StartCoroutine(FlyIcon(starFly, pillRect));
                Destroy(starFly.gameObject);
            }

            // Pop star + fill progress bar
            yield return StartCoroutine(PopStarAndFillBar());
        }

        private IEnumerator FlyIcon(RectTransform icon, RectTransform target)
        {
            float dur = GameConfig.RewardFlyDuration;
            Vector2 start = icon.anchoredPosition;
            // Convert target world pos to canvas local
            Vector2 end = GetCanvasAnchoredPos(target);

            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                // Ease-in-out + slight arc
                float easedT = t * t * (3f - 2f * t);
                float arcY   = Mathf.Sin(t * Mathf.PI) * 80f;
                icon.anchoredPosition = Vector2.Lerp(start, end, easedT) + new Vector2(0f, arcY);
                // Scale down as it arrives
                icon.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, easedT);
                yield return null;
            }
        }

        private Vector2 GetCanvasAnchoredPos(RectTransform target)
        {
            if (rootCanvas == null || target == null) return Vector2.zero;
            // Convert world position → canvas local anchored position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.GetComponent<RectTransform>(),
                RectTransformUtility.WorldToScreenPoint(null, target.position),
                rootCanvas.worldCamera,
                out Vector2 local);
            return local;
        }

        private IEnumerator PopStarAndFillBar()
        {
            // Refresh state so starsInChapter is up to date
            var state = GameState.Instance;
            int chapterStart    = _chapterIndex * GameConfig.LevelsPerChapter + 1;
            int clearedInChapter = 0;
            for (int i = chapterStart; i < chapterStart + GameConfig.LevelsPerChapter; i++)
            {
                if (SaveManager.Instance.IsLevelCleared(i))
                    clearedInChapter++;
            }
            _levelsIntoChapter = clearedInChapter;

            // Update star label
            if (chapterStarLabel != null)
                chapterStarLabel.text = $"{_levelsIntoChapter}/{GameConfig.LevelsPerChapter}";

            // Pop the newly-earned star
            int starIdx = _levelsIntoChapter - 1;
            if (chapterStarImages != null && starIdx >= 0 && starIdx < chapterStarImages.Length)
            {
                var starImg = chapterStarImages[starIdx];
                if (starImg != null)
                {
                    if (starFilledSprite != null) starImg.sprite = starFilledSprite;
                    else starImg.color = new Color(1f, 0.85f, 0f);

                    // Pop scale  0 → 1.3 → 1
                    AudioManager.Instance?.PlayStar();
                    if (VFXManager.Instance != null)
                        VFXManager.Instance.SpawnStarBurst(starImg.transform.position, 10);

                    starImg.rectTransform.localScale = Vector3.zero;
                    float t = 0f;
                    while (t < 0.2f)
                    {
                        t += Time.deltaTime;
                        starImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(0f, 1.3f, t / 0.2f);
                        yield return null;
                    }
                    t = 0f;
                    while (t < 0.1f)
                    {
                        t += Time.deltaTime;
                        starImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, t / 0.1f);
                        yield return null;
                    }
                    starImg.rectTransform.localScale = Vector3.one;
                }
            }

            // Smooth fill progress bar
            SetProgressBar(_levelsIntoChapter, true);
        }

        // ─────────────────────────────────────────────────────────
        // Scene fade in / out
        // ─────────────────────────────────────────────────────────

        private IEnumerator FadeIn()
        {
            if (fadeOverlay == null) yield break;
            float dur     = GameConfig.MapFadeDuration;
            float elapsed = 0f;
            Color startCol = fadeOverlay.color;
            Color endCol   = new Color(startCol.r, startCol.g, startCol.b, 0f);
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.color = Color.Lerp(startCol, endCol, elapsed / dur);
                yield return null;
            }
            fadeOverlay.color = endCol;
            fadeOverlay.gameObject.SetActive(false);
        }

        private IEnumerator FadeOutThenLoad(string sceneName)
        {
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                float dur     = GameConfig.MapFadeDuration;
                float elapsed = 0f;
                Color startCol = new Color(0.53f, 0.81f, 0.98f, 0f);  // light blue, transparent
                Color endCol   = new Color(0.53f, 0.81f, 0.98f, 1f);  // opaque
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    fadeOverlay.color = Color.Lerp(startCol, endCol, elapsed / dur);
                    yield return null;
                }
            }

            // Stop breathing pulse before unloading
            if (_breathCoroutine != null) StopCoroutine(_breathCoroutine);

            SceneManager.LoadScene(sceneName);
        }

        // ─────────────────────────────────────────────────────────
        // Button handlers
        // ─────────────────────────────────────────────────────────

        private void OnLevelButtonPressed()
        {
            // Dismiss hand-pointer tutorial if shown
            DismissHandPointer();

            AudioManager.Instance?.PlayButton();

            // Store which level to load (GameManager will read it, or we pass via LevelLoader)
            SaveManager.Instance.PendingLevel = _currentLevel;

            // Let GameManager set the active level
            if (GameManager.Instance != null)
                GameManager.Instance.SetActiveLevel(_currentLevel);

            if (SceneTransition.Instance != null)
                SceneTransition.Load(SceneNames.Gameplay);
            else
                StartCoroutine(FadeOutThenLoad(SceneNames.Gameplay));
        }

        private void OnGearPressed()
        {
            AudioManager.Instance?.PlayButton();
            var panel = settingsPanel != null ? settingsPanel : SettingsPanelUI.Instance;
            if (panel != null)
                panel.Open();
            else
                Debug.LogWarning("[MapSceneController] Gear/Settings tapped – SettingsPanelUI not found.");
        }

        private void OnGalleryPressed()
        {
            AudioManager.Instance?.PlayButton();
            if (galleryPanel == null)
                galleryPanel = FindObjectOfType<GalleryPanelUI>(true);

            if (galleryPanel != null)
                galleryPanel.Open();
            else
                Debug.LogWarning("[MapSceneController] Gallery tapped – GalleryPanelUI not found.");
        }

        private void OnRemoveAdsPressed()
        {
            AudioManager.Instance?.PlayButton();
            if (shopPanel != null)
                shopPanel.Open();
            else
                CoinPillUI.RaiseShopRequested();
            Debug.Log("[MapSceneController] Remove Ads tapped – Shop opened.");
        }

        // ─────────────────────────────────────────────────────────
        // Editor helper: auto-load sprites from AssetDatabase
        // ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void Reset()
        {
            AutoLoadSprites();
        }

        [ContextMenu("Auto-load sprites from AssetDatabase")]
        private void AutoLoadSprites()
        {
            if (bg_HillsSprite == null)
                bg_HillsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/Backgrounds/bg_map_hills.png");

            if (starFilledSprite == null)
                starFilledSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_star.png");
        }
#endif
    }
}

