// ============================================================
// LevelClearedUI.cs  –  Arrow Slide Puzzle
// Master controller for the Level Cleared (Win) Screen:
//   • Triggers on GameplayController.OnLevelWon event
//   • Slow-mo / pause on last arrow slide, followed by dark purple fade
//   • Multi-color firework bursts (4-5 colors) & continuous floating sparkles
//   • Left guide mascot (Dida, clap pose) slides in from bottom, loops clap animation
//   • Polaroid card with tape and slight tilt drops from top with overshoot bounce
//   • Displays LevelData reward picture (or colorful placeholder fallback)
//   • Large yellow 'Level cleared!' banner with pop animation (0 -> 1.2 -> 1) & yellow glow
//   • Rewards row: Coin +30 and Star +1 with number count-up animation
//   • Pulsing 'Tap to continue' prompt at bottom
//   • On tap: saves Coin (+30) and Star (+1) to SaveManager, loads Map scene
//
// PRD Section 3.8 & Section 7: Assets/_Game/Scripts/UI/LevelClearedUI.cs
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
    public class LevelClearedUI : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static LevelClearedUI Instance { get; private set; }

        // ── Inspector References ──────────────────────────────
        [Header("Root & Overlay")]
        [Tooltip("Root CanvasGroup of the win screen for fading and interaction.")]
        public CanvasGroup rootCanvasGroup;

        [Tooltip("Dark purple background overlay image.")]
        public Image darkPurpleOverlay;

        [Tooltip("Content container holding all UI elements.")]
        public GameObject contentRoot;

        [Header("Banner (Top)")]
        [Tooltip("Title banner RectTransform for scale-pop animation.")]
        public RectTransform titleBannerRect;

        [Tooltip("Main 'Level cleared!' TextMeshPro label.")]
        public TextMeshProUGUI titleText;

        [Tooltip("Optional glow underlay label for the title.")]
        public TextMeshProUGUI titleGlowText;

        [Header("Polaroid Card (Center)")]
        [Tooltip("Center polaroid card container.")]
        public RectTransform polaroidCardRect;

        [Tooltip("Polaroid frame image.")]
        public Image polaroidFrameImage;

        [Tooltip("Tape strip image attached to polaroid.")]
        public Image tapeImage;

        [Tooltip("Inner reward image component.")]
        public Image rewardPhotoImage;

        [Tooltip("Default placeholder sprite if LevelData has no image.")]
        public Sprite defaultPlaceholderPhoto;

        [Tooltip("Card tilt angle in degrees (PRD: slight tilt).")]
        public float polaroidTiltAngle = -4.5f;

        [Header("Guide Mascot (Dida Clapping - Left)")]
        [Tooltip("Guide mascot RectTransform.")]
        public RectTransform guideMascotRect;

        [Tooltip("Guide mascot image component.")]
        public Image guideMascotImage;

        [Tooltip("Dida clapping sprite.")]
        public Sprite guideClapSprite;

        [Header("Reward Badges (Coins & Star)")]
        [Tooltip("Row holding coin and star reward badges.")]
        public RectTransform rewardsRowRect;

        [Tooltip("Coin icon image.")]
        public Image coinIcon;

        [Tooltip("Coin count TextMeshPro label.")]
        public TextMeshProUGUI coinCountText;

        [Tooltip("Star icon image.")]
        public Image starIcon;

        [Tooltip("Star count TextMeshPro label.")]
        public TextMeshProUGUI starCountText;

        [Header("Tap To Continue (Bottom)")]
        [Tooltip("Bottom prompt RectTransform.")]
        public RectTransform tapToContinueRect;

        [Tooltip("Tap to continue TextMeshPro label.")]
        public TextMeshProUGUI tapToContinueText;

        [Tooltip("Full-screen button to detect user tap to continue.")]
        public Button fullScreenTapButton;

        [Header("VFX & Particles")]
        [Tooltip("Canvas UI Particle emitter for fireworks & floating sparkles.")]
        public UIParticleEmitter uiParticleEmitter;

        [Tooltip("Optional standard 3D/world ParticleSystem.")]
        public ParticleSystem worldParticleSystem;

        [Header("Animation Tunables")]
        [Tooltip("Brief slow-mo timescale on win.")]
        public float slowMoTimeScale = 0.2f;

        [Tooltip("Duration of the slow-mo pause (realtime seconds).")]
        public float slowMoDuration = 0.45f;

        [Tooltip("Duration of the dark purple fade-in.")]
        public float fadeDuration = 0.45f;

        [Tooltip("Amount of coins awarded per level clear.")]
        public int rewardCoins = 30;

        [Tooltip("Amount of stars awarded per level clear.")]
        public int rewardStars = 1;

        // ── Runtime State ─────────────────────────────────────
        private bool _isShowing;
        private bool _canTapToContinue;
        private bool _hasTransitioned;
        private Coroutine _sequenceCoroutine;
        private Coroutine _guideClapCoroutine;
        private Coroutine _tapPulseCoroutine;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoLoadDefaultSprites();

            // Initial hidden state
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;
            }

            if (contentRoot != null)
                contentRoot.SetActive(false);

            if (fullScreenTapButton != null)
            {
                fullScreenTapButton.onClick.RemoveAllListeners();
                fullScreenTapButton.onClick.AddListener(OnTapToContinueClicked);
                fullScreenTapButton.interactable = false;
            }
        }

        private void OnEnable()
        {
            GameplayController.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            GameplayController.OnLevelWon -= HandleLevelWon;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1.0f; // Safety reset
        }

        private void Update()
        {
            // Debug key to test win screen directly in Editor or build
            if (Input.GetKeyDown(KeyCode.W) && !_isShowing)
            {
                Debug.Log("[LevelClearedUI] Debug Win Triggered via 'W' key!");
                ShowLevelCleared();
            }
        }

        // ── Event Handlers ────────────────────────────────────

        private void HandleLevelWon()
        {
            if (_isShowing) return;
            ShowLevelCleared();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Initiates the full Level Cleared celebration sequence.
        /// </summary>
        public void ShowLevelCleared()
        {
            if (_isShowing) return;
            _isShowing = true;
            _canTapToContinue = false;
            _hasTransitioned = false;

            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = StartCoroutine(WinCelebrationSequence());
        }

        // ── Celebration Sequence Coroutine ────────────────────

        private IEnumerator WinCelebrationSequence()
        {
            Debug.Log("[LevelClearedUI] Starting Win Celebration Sequence...");

            // ── Phase 1: Slow-mo / Pause on last arrow ────────
            Time.timeScale = Mathf.Clamp(slowMoTimeScale, 0.05f, 1f);
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = 1.0f;

            // Prepare UI hierarchy
            if (contentRoot != null) contentRoot.SetActive(true);
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            // Setup level reward image
            SetupRewardImage();

            // Set initial transforms
            if (titleBannerRect != null) titleBannerRect.localScale = Vector3.zero;
            if (polaroidCardRect != null)
            {
                polaroidCardRect.anchoredPosition = new Vector2(0f, 1200f); // Above screen
                polaroidCardRect.localEulerAngles = new Vector3(0f, 0f, polaroidTiltAngle);
            }
            if (guideMascotRect != null)
            {
                guideMascotRect.anchoredPosition = new Vector2(-280f, -800f); // Below screen
            }
            if (rewardsRowRect != null)
            {
                rewardsRowRect.localScale = Vector3.zero;
            }
            if (tapToContinueRect != null)
            {
                tapToContinueRect.gameObject.SetActive(false);
            }

            // Reset counters
            if (coinCountText != null) coinCountText.text = "+0";
            if (starCountText != null) starCountText.text = "+0";

            // ── Phase 2: Dark Purple Fade ─────────────────────
            Color targetBgColor = new Color(0.12f, 0.06f, 0.21f, 0.95f); // Deep rich purple
            if (darkPurpleOverlay != null)
            {
                darkPurpleOverlay.color = new Color(targetBgColor.r, targetBgColor.g, targetBgColor.b, 0f);
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeElapsed / fadeDuration);

                if (rootCanvasGroup != null) rootCanvasGroup.alpha = t;
                if (darkPurpleOverlay != null)
                {
                    darkPurpleOverlay.color = new Color(targetBgColor.r, targetBgColor.g, targetBgColor.b, t * targetBgColor.a);
                }
                yield return null;
            }

            if (rootCanvasGroup != null) rootCanvasGroup.alpha = 1f;
            if (darkPurpleOverlay != null) darkPurpleOverlay.color = targetBgColor;

            // ── Phase 3: Start Fireworks & Sparkle Float ──────
            AudioManager.Instance?.PlayLevelClearJingle();

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnFireworkBurst(new Vector3(-1.5f, 2f, 0f), 20);
                VFXManager.Instance.SpawnFireworkBurst(new Vector3(1.5f, 2.5f, 0f), 20);
                VFXManager.Instance.SpawnConfettiBurst(new Vector3(0f, 3f, 0f), 25);
            }

            if (worldParticleSystem != null)
            {
                worldParticleSystem.Play();
            }

            if (uiParticleEmitter != null)
            {
                uiParticleEmitter.StartSparkleFloat();
                // Initial firework burst pair
                uiParticleEmitter.EmitBurst(new Vector2(-200f, 280f), 30);
                uiParticleEmitter.EmitBurst(new Vector2(200f, 320f), 30);
            }

            // ── Phase 4: Title Pop Animation (0 -> 1.2 -> 1.0) ─
            yield return StartCoroutine(AnimateTitleBannerPop());

            // ── Phase 5: Polaroid Card Drop & Overshoot Bounce ─
            if (uiParticleEmitter != null)
            {
                uiParticleEmitter.EmitBurst(new Vector2(0f, 450f), 25);
            }
            yield return StartCoroutine(AnimatePolaroidDrop());

            // ── Phase 6: Guide Mascot (Dida) Slide-In ──────────
            yield return StartCoroutine(AnimateGuideSlideIn());

            // Start guide clapping loop animation
            if (_guideClapCoroutine != null) StopCoroutine(_guideClapCoroutine);
            _guideClapCoroutine = StartCoroutine(GuideClapLoop());

            // Extra firework burst for celebration
            if (uiParticleEmitter != null)
            {
                uiParticleEmitter.EmitBurst(new Vector2(-220f, -40f), 24);
                uiParticleEmitter.EmitBurst(new Vector2(220f, 40f), 24);
            }

            // ── Phase 7: Rewards Count-Up (Coins & Stars) ─────
            yield return StartCoroutine(AnimateRewardsCountUp());

            // ── Phase 8: Tap to Continue Prompt ───────────────
            if (tapToContinueRect != null)
            {
                tapToContinueRect.gameObject.SetActive(true);
            }
            if (_tapPulseCoroutine != null) StopCoroutine(_tapPulseCoroutine);
            _tapPulseCoroutine = StartCoroutine(TapToContinuePulseLoop());

            // Enable full-screen tap
            _canTapToContinue = true;
            if (fullScreenTapButton != null)
            {
                fullScreenTapButton.interactable = true;
            }

            // Background ambient firework bursts loop
            StartCoroutine(AmbientFireworksLoop());
        }

        // ── Animation Sub-routines ────────────────────────────

        private IEnumerator AnimateTitleBannerPop()
        {
            if (titleBannerRect == null) yield break;

            float duration = 0.38f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Overshoot curve: 0 -> 1.2 -> 1.0
                float scale;
                if (t < 0.65f)
                {
                    float subT = t / 0.65f;
                    // Ease-out to 1.22
                    scale = Mathf.Sin(subT * Mathf.PI * 0.5f) * 1.22f;
                }
                else
                {
                    float subT = (t - 0.65f) / 0.35f;
                    scale = Mathf.Lerp(1.22f, 1.0f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }

                titleBannerRect.localScale = Vector3.one * scale;
                yield return null;
            }
            titleBannerRect.localScale = Vector3.one;
        }

        private IEnumerator AnimatePolaroidDrop()
        {
            if (polaroidCardRect == null) yield break;

            float startY = 1200f;
            float targetY = 40f;
            float duration = 0.52f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Elastic / Overshoot bounce curve
                // Drops fast past target to -18, bounces up to 60, settles at targetY
                float currentY;
                if (t < 0.6f)
                {
                    float subT = t / 0.6f;
                    // EaseInQuad dropping fast
                    currentY = Mathf.Lerp(startY, targetY - 20f, subT * subT);
                }
                else if (t < 0.82f)
                {
                    float subT = (t - 0.6f) / 0.22f;
                    // Bounce up
                    currentY = Mathf.Lerp(targetY - 20f, targetY + 16f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else
                {
                    float subT = (t - 0.82f) / 0.18f;
                    currentY = Mathf.Lerp(targetY + 16f, targetY, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }

                polaroidCardRect.anchoredPosition = new Vector2(0f, currentY);
                yield return null;
            }

            polaroidCardRect.anchoredPosition = new Vector2(0f, targetY);
            polaroidCardRect.localEulerAngles = new Vector3(0f, 0f, polaroidTiltAngle);
        }

        private IEnumerator AnimateGuideSlideIn()
        {
            if (guideMascotRect == null) yield break;

            float startY = -750f;
            float targetY = -150f;
            float targetX = -270f;
            float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                float currentY = Mathf.Lerp(startY, targetY, ease);
                guideMascotRect.anchoredPosition = new Vector2(targetX, currentY);
                yield return null;
            }

            guideMascotRect.anchoredPosition = new Vector2(targetX, targetY);
        }

        private IEnumerator GuideClapLoop()
        {
            if (guideMascotRect == null) yield break;

            Vector2 basePos = guideMascotRect.anchoredPosition;

            while (_isShowing)
            {
                // Clapping rhythm: 2 claps per second
                float clapTime = Time.unscaledTime * 5.5f;
                float wobble = Mathf.Sin(clapTime);
                float scale = 1.0f + Mathf.Max(0f, wobble) * 0.07f;
                float rot = Mathf.Sin(Time.unscaledTime * 2.8f) * 2.5f;

                guideMascotRect.localScale = new Vector3(scale, scale, 1f);
                guideMascotRect.localEulerAngles = new Vector3(0f, 0f, rot);

                yield return null;
            }
        }

        private IEnumerator AnimateRewardsCountUp()
        {
            if (rewardsRowRect != null)
            {
                // Pop rewards row in
                float popElapsed = 0f;
                while (popElapsed < 0.22f)
                {
                    popElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(popElapsed / 0.22f);
                    rewardsRowRect.localScale = Vector3.one * Mathf.Sin(t * Mathf.PI * 0.5f);
                    yield return null;
                }
                rewardsRowRect.localScale = Vector3.one;
            }

            // Coin count-up from 0 to rewardCoins (30)
            float countDuration = 0.65f;
            float elapsed = 0f;
            int lastSoundStep = -1;

            while (elapsed < countDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / countDuration);
                int currentCoins = Mathf.RoundToInt(Mathf.Lerp(0, rewardCoins, t));

                if (coinCountText != null)
                    coinCountText.text = $"+{currentCoins}";

                // Occasional coin tick audio
                int step = currentCoins / 6;
                if (step != lastSoundStep)
                {
                    lastSoundStep = step;
                    AudioManager.Instance?.PlayCoin();
                }

                yield return null;
            }

            if (coinCountText != null)
                coinCountText.text = $"+{rewardCoins}";

            // Star count-up from 0 to rewardStars (1) with pop
            yield return new WaitForSecondsRealtime(0.12f);

            if (starCountText != null)
                starCountText.text = $"+{rewardStars}";

            AudioManager.Instance?.PlayStar();
            if (VFXManager.Instance != null && starIcon != null)
                VFXManager.Instance.SpawnStarBurst(starIcon.transform.position, 10);

            if (starIcon != null)
            {
                // Star bounce animation
                float starElapsed = 0f;
                while (starElapsed < 0.25f)
                {
                    starElapsed += Time.unscaledDeltaTime;
                    float st = starElapsed / 0.25f;
                    float sScale = 1.0f + Mathf.Sin(st * Mathf.PI) * 0.35f;
                    starIcon.rectTransform.localScale = Vector3.one * sScale;
                    yield return null;
                }
                starIcon.rectTransform.localScale = Vector3.one;
            }

            AudioManager.Instance?.PlayCoin();
        }

        private IEnumerator TapToContinuePulseLoop()
        {
            if (tapToContinueText == null) yield break;

            Color baseCol = tapToContinueText.color;

            while (_isShowing)
            {
                // Gentle breathing pulse between 0.4 and 1.0 alpha
                float wave = Mathf.PingPong(Time.unscaledTime * 1.8f, 1f);
                float alpha = Mathf.Lerp(0.38f, 1.0f, wave);

                Color c = baseCol;
                c.a = alpha;
                tapToContinueText.color = c;

                if (tapToContinueRect != null)
                {
                    float s = 1.0f + (wave - 0.5f) * 0.04f;
                    tapToContinueRect.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }
        }

        private IEnumerator AmbientFireworksLoop()
        {
            while (_isShowing && uiParticleEmitter != null)
            {
                yield return new WaitForSecondsRealtime(Random.Range(0.85f, 1.4f));
                if (!_isShowing) break;

                float rx = Random.Range(-350f, 350f);
                float ry = Random.Range(100f, 550f);
                uiParticleEmitter.EmitBurst(new Vector2(rx, ry), Random.Range(18, 28));
            }
        }

        // ── Reward Image Setup ────────────────────────────────

        private void SetupRewardImage()
        {
            if (rewardPhotoImage == null) return;

            Sprite loadedReward = null;
            LevelData activeData = null;

            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelData != null)
            {
                activeData = LevelLoader.Instance.CurrentLevelData;
            }
            else if (GameManager.Instance != null && GameManager.Instance.ActiveLevel != null)
            {
                activeData = GameManager.Instance.ActiveLevel;
            }

            if (activeData != null)
            {
                // 1. Try rewardImageName
                if (!string.IsNullOrEmpty(activeData.rewardImageName))
                {
                    loadedReward = LoadGallerySprite(activeData.rewardImageName);
                }

                // 2. Try galleryKey or reward enum
                if (loadedReward == null && !string.IsNullOrEmpty(activeData.galleryKey))
                {
                    loadedReward = LoadGallerySprite(activeData.galleryKey);
                }

                // 3. Try reward enum name (e.g. phone.png, book.png, love.png, dog.png)
                if (loadedReward == null && activeData.reward != RewardType.None)
                {
                    string enumName = activeData.reward.ToString().ToLower();
                    loadedReward = LoadGallerySprite($"{enumName}.png") ?? LoadGallerySprite($"pic_{enumName}.png");
                }
            }

            // Fallback to placeholder if no reward found
            if (loadedReward == null)
            {
                loadedReward = defaultPlaceholderPhoto;
            }

            rewardPhotoImage.sprite = loadedReward;
            rewardPhotoImage.color = Color.white;
            rewardPhotoImage.preserveAspect = true;
        }

        private Sprite LoadGallerySprite(string filenameOrKey)
        {
            if (string.IsNullOrEmpty(filenameOrKey)) return null;

            // Remove extension if any for Resource/Asset lookup
            string cleanName = filenameOrKey.EndsWith(".png")
                ? filenameOrKey.Substring(0, filenameOrKey.Length - 4)
                : filenameOrKey;

#if UNITY_EDITOR
            // Try direct AssetDatabase load
            string pathWithExt = $"Assets/_Game/Art/Gallery/{cleanName}.png";
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(pathWithExt);
            if (sprite != null) return sprite;

            string pathArrows = $"Assets/_Game/Art/Arrows/{cleanName}.png";
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(pathArrows);
            if (sprite != null) return sprite;
#endif

            // Try Resources load
            var res = Resources.Load<Sprite>($"Gallery/{cleanName}");
            if (res != null) return res;

            return null;
        }

        private void AutoLoadDefaultSprites()
        {
#if UNITY_EDITOR
            if (polaroidFrameImage != null && polaroidFrameImage.sprite == null)
            {
                polaroidFrameImage.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/polaroid_frame.png");
            }

            if (tapeImage != null && tapeImage.sprite == null)
            {
                tapeImage.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/tape.png");
            }

            if (guideClapSprite == null)
            {
                guideClapSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/Characters/guide_clap.png");
            }

            if (guideMascotImage != null && guideMascotImage.sprite == null)
            {
                guideMascotImage.sprite = guideClapSprite;
            }

            if (defaultPlaceholderPhoto == null)
            {
                defaultPlaceholderPhoto = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/Gallery/pic_placeholder.png");
            }

            if (coinIcon != null && coinIcon.sprite == null)
            {
                coinIcon.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_coin.png");
            }

            if (starIcon != null && starIcon.sprite == null)
            {
                starIcon.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_star.png");
            }
#endif
        }

        // ── Tap To Continue & Progression ─────────────────────

        public void OnTapToContinueClicked()
        {
            if (!_canTapToContinue || _hasTransitioned) return;
            _hasTransitioned = true;
            _canTapToContinue = false;

            Debug.Log("[LevelClearedUI] Tap To Continue clicked!");
            AudioManager.Instance?.PlayButton();

            // 1. Save Economy (+30 coins, +1 star)
            SaveManager.Instance.AddCoins(rewardCoins);
            SaveManager.Instance.AddStars(rewardStars);

            // 2. Mark Level Cleared
            int clearedLevel = 1;
            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelId > 0)
                clearedLevel = LevelLoader.Instance.CurrentLevelId;
            else if (GameManager.Instance != null && GameManager.Instance.ActiveLevel != null)
                clearedLevel = GameManager.Instance.ActiveLevel.levelIndex;

            SaveManager.Instance.MarkLevelCleared(clearedLevel);
            // Unlock gallery picture for this level
            SaveManager.Instance.UnlockGallery(clearedLevel);
            // Signal to Map scene that it should play the reward-fly animation
            SaveManager.Instance.PendingClearedLevel = clearedLevel;

            Debug.Log($"[LevelClearedUI] Progression Saved via SaveManager: Coins={SaveManager.Instance.Coins}, Stars={SaveManager.Instance.TotalStars}, Level={clearedLevel} Cleared.");

            // 3. Load Map scene
            StartCoroutine(TransitionToMapScene());
        }

        private IEnumerator TransitionToMapScene()
        {
            // Clean up VFX & Coroutines
            if (uiParticleEmitter != null) uiParticleEmitter.ClearAll();
            Time.timeScale = 1.0f;

            // Small 0.15s beat before loading scene
            yield return new WaitForSecondsRealtime(0.15f);

            // ── Load Map scene (via SceneTransition with smooth light-blue cross fade) ──
            string targetScene = SceneNames.Map;
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Load(targetScene);
            }
            else if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                Debug.LogWarning($"[LevelClearedUI] Scene '{targetScene}' not found in Build Settings! Reloading current scene as fallback.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}

