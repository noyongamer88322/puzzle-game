// ============================================================
// TutorialController.cs  –  Arrow Slide Puzzle
// Master controller for Grandma-style (Dida) tutorial system:
//   • Each tutorial step triggers only ONCE (saved via SaveManager JSON flags).
//   • Step 1 (Level 1):
//       - Hand pointer loop tapping on the free arrow.
//       - Speech bubble: 'Tap the <color=#9333EA>arrow</color> to make it <color=#9333EA>fly away!</color>'.
//       - Blocks taps on non-target arrows.
//   • Step 2 (Level 4, Hint button appears for the first time):
//       - Screen dimmed almost black (#08051A, alpha ~0.82).
//       - Hint button highlighted above dim.
//       - Guide mascot at top-right, speech bubble to the left:
//         'Stuck on your next move? Try a <color=#9333EA>Hint</color>.'.
//       - Hand pointer loop on Hint button.
//       - Only Hint button can be clicked.
//   • Step 3 (Level 6, Magnet button appears for the first time):
//       - Screen dimmed almost black.
//       - Magnet button highlighted above dim.
//       - Guide mascot at top-right, speech bubble to the left:
//         'Use the <color=#9333EA>Magnet</color> to fly <color=#9333EA>3 arrows</color> away at once'.
//       - Hand pointer loop on Magnet button.
//       - Only Magnet button can be clicked.
//   • Speech Bubble: White background, vibrant blue border (#2563EB),
//     purple highlighted words (#9333EA), smooth pop-in animation (0 -> 1.15 -> 1.0).
//   • Guide mascot: Dida poses (guide_talk, guide_point) with gentle breathing idle.
//
// PRD Sections 3.4, 5, 7 & Video Correction 8: Assets/_Game/Scripts/UI/TutorialController.cs
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Gameplay;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class TutorialController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static TutorialController Instance { get; private set; }

        // ── Events ────────────────────────────────────────────
        public static event Action<int> OnTutorialStarted;
        public static event Action<int> OnTutorialCompleted;

        // ── Inspector UI References ───────────────────────────
        [Header("Root Canvas & CanvasGroup")]
        [Tooltip("Root Canvas of the Tutorial overlay.")]
        public Canvas tutorialCanvas;

        [Tooltip("CanvasGroup controlling fade / visibility.")]
        public CanvasGroup canvasGroup;

        [Header("Dim Overlay")]
        [Tooltip("Dark semi-transparent fullscreen image blocking wrong taps.")]
        public Image dimOverlay;

        [Header("Guide Mascot (Dida)")]
        [Tooltip("Mascot RectTransform container.")]
        public RectTransform guideMascotRect;

        [Tooltip("Mascot Image component.")]
        public Image guideMascotImage;

        [Tooltip("Sprite for talking pose.")]
        public Sprite guideTalkSprite;

        [Tooltip("Sprite for pointing pose.")]
        public Sprite guidePointSprite;

        [Header("Speech Bubble")]
        [Tooltip("Speech bubble RectTransform container.")]
        public RectTransform speechBubbleRect;

        [Tooltip("Speech bubble background image.")]
        public Image speechBubbleImage;

        [Tooltip("Speech bubble speech pointer tail (points towards Dida).")]
        public Image speechBubbleTail;

        [Tooltip("Speech bubble text label (TMP).")]
        public TextMeshProUGUI speechBubbleText;

        [Header("Hand Pointer")]
        [Tooltip("Hand pointer RectTransform container.")]
        public RectTransform handPointerRect;

        [Tooltip("Hand pointer Image component.")]
        public Image handPointerImage;

        [Tooltip("Hand pointer sprite.")]
        public Sprite handPointerSprite;

        // ── Tunables ──────────────────────────────────────────
        [Header("Visual Tunables")]
        [Tooltip("Dim overlay color for steps 2 & 3 (almost black).")]
        public Color dimColor = new Color(0.04f, 0.02f, 0.09f, 0.82f);

        [Tooltip("Highlight purple color for keywords in bubble.")]
        public string highlightColorHex = "#9333EA";

        // ── Runtime State ─────────────────────────────────────
        public int CurrentStep { get; private set; } = 0; // 0=None, 1=ArrowTap, 2=Hint, 3=Magnet
        public bool IsTutorialActive => CurrentStep > 0;
        public ArrowView TargetArrow { get; private set; }

        private Coroutine _handPointerCoroutine;
        private Coroutine _bubblePopCoroutine;
        private Coroutine _guideIdleCoroutine;
        private Canvas _highlightedButtonCanvas;
        private GraphicRaycaster _highlightedButtonRaycaster;
        private GameObject _currentlyHighlightedGO;
        private Camera _cam;

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = Camera.main;
            TryAutoLoadAssets();
            EnsureUIHierarchy();
            HideAllImmediate();
        }

        private void Start()
        {
            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevelId > 0)
            {
                CheckAndTriggerTutorial(LevelLoader.Instance.CurrentLevelId);
            }
        }

        private void Update()
        {
            // Debug shortcut: T resets tutorial flags and reloads current level
            if (Input.GetKeyDown(KeyCode.T))
            {
                ResetAllTutorials();
                Debug.Log("[Tutorial] Debug: All tutorial flags reset! Reloading level...");
                if (LevelLoader.Instance != null)
                {
                    LevelLoader.Instance.ExecuteLoadLevel(LevelLoader.Instance.CurrentLevelId);
                }
            }

            // In Step 1: Keep hand pointer tracking the target arrow on board
            if (CurrentStep == 1 && TargetArrow != null && handPointerRect != null && handPointerRect.gameObject.activeSelf)
            {
                UpdateHandPointerWorldTarget(TargetArrow.transform.position);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CleanUpHighlightedButton();
        }

        // ── Public Trigger & Progression API ──────────────────

        /// <summary>
        /// Checks if a tutorial is required for the loaded level and starts it if not already done.
        /// </summary>
        public void OnLevelLoaded(int levelId)
        {
            StartCoroutine(WaitAndTrigger(levelId));
        }

        private IEnumerator WaitAndTrigger(int levelId)
        {
            // Wait 1 frame so grid spawning & HUD apply finishes
            yield return null;
            CheckAndTriggerTutorial(levelId);
        }

        public void CheckAndTriggerTutorial(int levelId)
        {
            CleanUpHighlightedButton();
            HideAllImmediate();

            // Step 1: Level 1 (first arrow tap)
            if (levelId == 1 && !SaveManager.Instance.IsTutorialDone(1))
            {
                StartStep1();
                return;
            }

            // Step 2: Level 4 (Hint button first appears)
            if (levelId == 4 && !SaveManager.Instance.IsTutorialDone(2))
            {
                StartStep2();
                return;
            }

            // Step 3: Level 6 (Magnet button first appears)
            if (levelId == 6 && !SaveManager.Instance.IsTutorialDone(3))
            {
                StartStep3();
                return;
            }

            // No tutorial needed
            CurrentStep = 0;
        }

        /// <summary>
        /// Called when the player executes the tutorial action (tap arrow, tap hint, tap magnet).
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (CurrentStep == 0) return;

            int completedStep = CurrentStep;
            Debug.Log($"[Tutorial] Completed Step {completedStep}!");

            if (completedStep == 1)
            {
                SaveManager.Instance.SetTutorialDone(1);
            }
            else if (completedStep == 2)
            {
                SaveManager.Instance.SetTutorialDone(2);
            }
            else if (completedStep == 3)
            {
                SaveManager.Instance.SetTutorialDone(3);
            }

            CurrentStep = 0;
            TargetArrow = null;

            CleanUpHighlightedButton();
            StartCoroutine(AnimateHide());
            OnTutorialCompleted?.Invoke(completedStep);
        }

        public void ResetAllTutorials()
        {
            SaveManager.Instance.ResetAllTutorials();
            Debug.Log("[Tutorial] SaveManager flags reset (Step 1, Step 2, Step 3).");
        }

        public bool IsStepDone(int step)
        {
            return SaveManager.Instance.IsTutorialDone(step);
        }

        // ── Step 1: Level 1 Arrow Tap ─────────────────────────

        private void StartStep1()
        {
            StartCoroutine(RoutineStartStep1());
        }

        private IEnumerator RoutineStartStep1()
        {
            // Give arrow views time to instantiate
            yield return new WaitForSeconds(0.2f);

            var freeArrows = GameplayController.Instance != null
                ? GameplayController.Instance.GetFreeArrowsInSolveOrder()
                : null;

            if (freeArrows == null || freeArrows.Count == 0)
            {
                Debug.LogWarning("[Tutorial] Step 1: No free arrow found!");
                yield break;
            }

            // Prefer an arrow near the bottom (like Arrow 11 at row 0) so the hand points to a prominently visible arrow
            ArrowView bestArrow = freeArrows[0];
            foreach (var a in freeArrows)
            {
                if (a != null && a.Data != null)
                {
                    if (a.Data.Head.y == 0 || (bestArrow.Data.Head.y > 0 && a.Data.Head.y < bestArrow.Data.Head.y))
                    {
                        bestArrow = a;
                    }
                }
            }

            TargetArrow = bestArrow;
            CurrentStep = 1;
            Debug.Log($"[Tutorial] Starting Step 1: Arrow Tap on Arrow at Head {TargetArrow.Data.Head}");

            // Show UI container
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false; // Arrow needs to be tapped directly in world
                canvasGroup.interactable = false;
            }

            // In Step 1, subtle dim overlay - raycastTarget MUST be false so world clicks pass through!
            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(true);
                dimOverlay.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0.35f);
                dimOverlay.raycastTarget = false;
            }

            if (handPointerImage != null) handPointerImage.raycastTarget = false;
            if (speechBubbleImage != null) speechBubbleImage.raycastTarget = false;
            if (speechBubbleText != null) speechBubbleText.raycastTarget = false;
            if (speechBubbleTail != null) speechBubbleTail.raycastTarget = false;
            if (guideMascotImage != null) guideMascotImage.raycastTarget = false;

            // Guide Mascot: Top-Right corner (গাইড ডান উপরে)
            if (guideMascotRect != null)
            {
                guideMascotRect.gameObject.SetActive(true);
                guideMascotRect.anchorMin = new Vector2(1, 1);
                guideMascotRect.anchorMax = new Vector2(1, 1);
                guideMascotRect.pivot = new Vector2(1, 1);
                guideMascotRect.anchoredPosition = new Vector2(-40, -180);
                guideMascotRect.sizeDelta = new Vector2(260, 325);

                if (guideMascotImage != null && guideTalkSprite != null)
                    guideMascotImage.sprite = guideTalkSprite;
            }

            // Speech Bubble: Left of Guide (সাদা speech bubble নীল বর্ডার, বামে)
            string step1Msg = $"Tap the <color={highlightColorHex}>arrow</color> to make it <color={highlightColorHex}>fly away!</color>";
            ShowSpeechBubble(
                text: step1Msg,
                anchorMin: new Vector2(1, 1),
                anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                anchoredPos: new Vector2(-310, -220),
                size: new Vector2(490, 200),
                tailPointsRight: true
            );

            // Hand Pointer: Taps on target arrow
            if (handPointerRect != null)
            {
                handPointerRect.gameObject.SetActive(true);
                UpdateHandPointerWorldTarget(TargetArrow.transform.position);
                StartHandPointerTapLoop();
            }

            StartGuideIdleAnimation();
            OnTutorialStarted?.Invoke(1);
        }

        // ── Step 2: Level 4 Hint Button ───────────────────────

        private void StartStep2()
        {
            CurrentStep = 2;
            Debug.Log("[Tutorial] Starting Step 2: Hint Button Introduction");

            // 1. Show Canvas with full dim
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(true);
                dimOverlay.color = dimColor;
            }

            // 2. Highlight Hint Button above the dim overlay
            var hud = GameplayHUD.Instance;
            if (hud != null && hud.hintButton != null)
            {
                HighlightButton(hud.hintButton.gameObject);
            }

            // 3. Guide Mascot: Top-Right (গাইড ডান উপরে)
            if (guideMascotRect != null)
            {
                guideMascotRect.gameObject.SetActive(true);
                guideMascotRect.anchorMin = new Vector2(1, 1);
                guideMascotRect.anchorMax = new Vector2(1, 1);
                guideMascotRect.pivot = new Vector2(1, 1);
                guideMascotRect.anchoredPosition = new Vector2(-40, -180);
                guideMascotRect.sizeDelta = new Vector2(260, 325);

                if (guideMascotImage != null && guidePointSprite != null)
                    guideMascotImage.sprite = guidePointSprite;
            }

            // 4. Speech Bubble: Left of Guide (bubble বামে)
            string step2Msg = $"Stuck on your next move?\nTry a <color={highlightColorHex}>Hint</color>.";
            ShowSpeechBubble(
                text: step2Msg,
                anchorMin: new Vector2(1, 1),
                anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                anchoredPos: new Vector2(-310, -220),
                size: new Vector2(490, 200),
                tailPointsRight: true
            );

            // 5. Hand Pointer: Positioned on Hint Button
            if (hud != null && hud.hintButton != null)
            {
                PositionHandPointerOnScreenTarget(hud.hintButton.transform.position);
                StartHandPointerTapLoop();
            }

            StartGuideIdleAnimation();
            OnTutorialStarted?.Invoke(2);
        }

        // ── Step 3: Level 6 Magnet Button ─────────────────────

        private void StartStep3()
        {
            CurrentStep = 3;
            Debug.Log("[Tutorial] Starting Step 3: Magnet Button Introduction");

            // 1. Show Canvas with full dim
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(true);
                dimOverlay.color = dimColor;
            }

            // 2. Highlight Magnet Button above the dim overlay
            var hud = GameplayHUD.Instance;
            if (hud != null && hud.magnetButton != null)
            {
                HighlightButton(hud.magnetButton.gameObject);
            }

            // 3. Guide Mascot: Top-Right
            if (guideMascotRect != null)
            {
                guideMascotRect.gameObject.SetActive(true);
                guideMascotRect.anchorMin = new Vector2(1, 1);
                guideMascotRect.anchorMax = new Vector2(1, 1);
                guideMascotRect.pivot = new Vector2(1, 1);
                guideMascotRect.anchoredPosition = new Vector2(-40, -180);
                guideMascotRect.sizeDelta = new Vector2(260, 325);

                if (guideMascotImage != null && guideTalkSprite != null)
                    guideMascotImage.sprite = guideTalkSprite;
            }

            // 4. Speech Bubble: Left of Guide
            string step3Msg = $"Use the <color={highlightColorHex}>Magnet</color> to fly\n<color={highlightColorHex}>3 arrows</color> away at once";
            ShowSpeechBubble(
                text: step3Msg,
                anchorMin: new Vector2(1, 1),
                anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                anchoredPos: new Vector2(-310, -220),
                size: new Vector2(510, 210),
                tailPointsRight: true
            );

            // 5. Hand Pointer: Positioned on Magnet Button
            if (hud != null && hud.magnetButton != null)
            {
                PositionHandPointerOnScreenTarget(hud.magnetButton.transform.position);
                StartHandPointerTapLoop();
            }

            StartGuideIdleAnimation();
            OnTutorialStarted?.Invoke(3);
        }

        // ── Speech Bubble Display & Animation ─────────────────

        private void ShowSpeechBubble(string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, bool tailPointsRight)
        {
            if (speechBubbleRect == null) return;

            speechBubbleRect.gameObject.SetActive(true);
            speechBubbleRect.anchorMin = anchorMin;
            speechBubbleRect.anchorMax = anchorMax;
            speechBubbleRect.pivot = pivot;
            speechBubbleRect.anchoredPosition = anchoredPos;
            speechBubbleRect.sizeDelta = size;

            if (speechBubbleText != null)
            {
                speechBubbleText.text = text;
            }

            if (speechBubbleTail != null)
            {
                speechBubbleTail.gameObject.SetActive(true);
                var tailRect = speechBubbleTail.rectTransform;
                if (tailPointsRight)
                {
                    tailRect.anchorMin = new Vector2(1, 0.5f);
                    tailRect.anchorMax = new Vector2(1, 0.5f);
                    tailRect.pivot = new Vector2(0, 0.5f);
                    tailRect.anchoredPosition = new Vector2(-2, 0);
                    tailRect.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    tailRect.anchorMin = new Vector2(0, 0.5f);
                    tailRect.anchorMax = new Vector2(0, 0.5f);
                    tailRect.pivot = new Vector2(1, 0.5f);
                    tailRect.anchoredPosition = new Vector2(2, 0);
                    tailRect.localRotation = Quaternion.Euler(0, 180, 0);
                }
            }

            if (_bubblePopCoroutine != null) StopCoroutine(_bubblePopCoroutine);
            _bubblePopCoroutine = StartCoroutine(AnimateBubblePopIn());
        }

        private IEnumerator AnimateBubblePopIn()
        {
            if (speechBubbleRect == null) yield break;

            speechBubbleRect.localScale = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.32f;

            // Scale from 0 to 1.15 overshoot then ease back to 1.0
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Elastic/Overshoot curve
                float s;
                if (t < 0.65f)
                {
                    float subT = t / 0.65f;
                    s = Mathf.SmoothStep(0f, 1.15f, subT);
                }
                else
                {
                    float subT = (t - 0.65f) / 0.35f;
                    s = Mathf.Lerp(1.15f, 1.0f, subT);
                }
                speechBubbleRect.localScale = Vector3.one * s;
                yield return null;
            }

            speechBubbleRect.localScale = Vector3.one;
            _bubblePopCoroutine = null;
        }

        // ── Hand Pointer Tap Loop Animation ───────────────────

        private void PositionHandPointerOnScreenTarget(Vector3 targetWorldOrScreenPos)
        {
            if (handPointerRect == null) return;
            handPointerRect.gameObject.SetActive(true);
            handPointerRect.position = targetWorldOrScreenPos;
        }

        private void UpdateHandPointerWorldTarget(Vector3 worldPos)
        {
            if (handPointerRect == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
            screenPos.z = 0;
            // Pointing finger tip lands directly on target
            handPointerRect.position = screenPos;
        }

        private void StartHandPointerTapLoop()
        {
            if (_handPointerCoroutine != null) StopCoroutine(_handPointerCoroutine);
            _handPointerCoroutine = StartCoroutine(HandPointerTapRoutine());
        }

        private IEnumerator HandPointerTapRoutine()
        {
            if (handPointerRect == null) yield break;

            Vector3 baseScale = Vector3.one * 1.1f;
            handPointerRect.localScale = baseScale;

            while (true)
            {
                // Press down tap
                float elapsed = 0f;
                float pressTime = 0.16f;
                while (elapsed < pressTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / pressTime;
                    handPointerRect.localScale = Vector3.Lerp(baseScale, baseScale * 0.82f, t);
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.06f);

                // Release up
                elapsed = 0f;
                float releaseTime = 0.14f;
                while (elapsed < releaseTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / releaseTime;
                    handPointerRect.localScale = Vector3.Lerp(baseScale * 0.82f, baseScale, t);
                    yield return null;
                }

                handPointerRect.localScale = baseScale;
                yield return new WaitForSecondsRealtime(0.45f);
            }
        }

        // ── Guide Idle Animation ──────────────────────────────

        private void StartGuideIdleAnimation()
        {
            if (_guideIdleCoroutine != null) StopCoroutine(_guideIdleCoroutine);
            _guideIdleCoroutine = StartCoroutine(GuideIdleRoutine());
        }

        private IEnumerator GuideIdleRoutine()
        {
            if (guideMascotRect == null) yield break;

            Vector3 baseScale = Vector3.one;
            guideMascotRect.localScale = baseScale;
            float timer = 0f;

            while (true)
            {
                timer += Time.unscaledDeltaTime * 2.8f;
                float wave = Mathf.Sin(timer);
                float sy = 1f + wave * 0.025f;
                float sx = 1f - wave * 0.015f;
                guideMascotRect.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, 1f);
                yield return null;
            }
        }

        // ── Button Highlighting ───────────────────────────────

        private void HighlightButton(GameObject buttonGO)
        {
            CleanUpHighlightedButton();
            if (buttonGO == null) return;

            _currentlyHighlightedGO = buttonGO;

            // Add or retrieve Canvas on the button object so it renders above dim overlay (order 35 vs dim order 30)
            _highlightedButtonCanvas = buttonGO.GetComponent<Canvas>();
            if (_highlightedButtonCanvas == null)
            {
                _highlightedButtonCanvas = buttonGO.AddComponent<Canvas>();
            }
            _highlightedButtonCanvas.overrideSorting = true;
            _highlightedButtonCanvas.sortingOrder = 35; // Above dimOverlay (30)

            _highlightedButtonRaycaster = buttonGO.GetComponent<GraphicRaycaster>();
            if (_highlightedButtonRaycaster == null)
            {
                _highlightedButtonRaycaster = buttonGO.AddComponent<GraphicRaycaster>();
            }
        }

        private void CleanUpHighlightedButton()
        {
            if (_currentlyHighlightedGO != null)
            {
                if (_highlightedButtonRaycaster != null)
                {
                    Destroy(_highlightedButtonRaycaster);
                    _highlightedButtonRaycaster = null;
                }
                if (_highlightedButtonCanvas != null)
                {
                    Destroy(_highlightedButtonCanvas);
                    _highlightedButtonCanvas = null;
                }
                _currentlyHighlightedGO = null;
            }
        }

        // ── Hide & Clean Up Animations ────────────────────────

        private IEnumerator AnimateHide()
        {
            if (_bubblePopCoroutine != null) StopCoroutine(_bubblePopCoroutine);
            if (_handPointerCoroutine != null) StopCoroutine(_handPointerCoroutine);
            if (_guideIdleCoroutine != null) StopCoroutine(_guideIdleCoroutine);

            if (handPointerRect != null) handPointerRect.gameObject.SetActive(false);

            float elapsed = 0f;
            float duration = 0.2f;
            Vector3 initScale = speechBubbleRect != null ? speechBubbleRect.localScale : Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                if (speechBubbleRect != null) speechBubbleRect.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            HideAllImmediate();
        }

        private void HideAllImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);
            if (guideMascotRect != null) guideMascotRect.gameObject.SetActive(false);
            if (speechBubbleRect != null) speechBubbleRect.gameObject.SetActive(false);
            if (handPointerRect != null) handPointerRect.gameObject.SetActive(false);
        }

        // ── Auto Initialization & Hierarchy Builder ───────────

        private void TryAutoLoadAssets()
        {
#if UNITY_EDITOR
            if (guideTalkSprite == null)
            {
                guideTalkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/Characters/guide_talk.png");
            }
            if (guidePointSprite == null)
            {
                guidePointSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/Characters/guide_point.png");
            }
            if (handPointerSprite == null)
            {
                handPointerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/hand_pointer.png");
            }
#endif
        }

        private void EnsureUIHierarchy()
        {
            if (tutorialCanvas == null)
            {
                tutorialCanvas = GetComponent<Canvas>();
                if (tutorialCanvas == null) tutorialCanvas = gameObject.AddComponent<Canvas>();
            }
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = 30; // Above HUD (10) and Board, below Win (50)

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 1. Dim Overlay
            if (dimOverlay == null)
            {
                var dimGO = transform.Find("DimOverlay");
                if (dimGO == null)
                {
                    var go = new GameObject("DimOverlay");
                    go.transform.SetParent(transform, false);
                    dimOverlay = go.AddComponent<Image>();
                    var r = go.GetComponent<RectTransform>();
                    r.anchorMin = Vector2.zero;
                    r.anchorMax = Vector2.one;
                    r.sizeDelta = Vector2.zero;
                }
                else
                {
                    dimOverlay = dimGO.GetComponent<Image>();
                }
            }
            if (dimOverlay != null)
            {
                dimOverlay.color = dimColor;
                dimOverlay.raycastTarget = true;
            }

            // 2. Guide Mascot
            if (guideMascotRect == null)
            {
                var mascotGO = transform.Find("GuideMascot");
                if (mascotGO == null)
                {
                    var go = new GameObject("GuideMascot");
                    go.transform.SetParent(transform, false);
                    guideMascotRect = go.AddComponent<RectTransform>();
                    guideMascotImage = go.AddComponent<Image>();
                }
                else
                {
                    guideMascotRect = mascotGO.GetComponent<RectTransform>();
                    guideMascotImage = mascotGO.GetComponent<Image>();
                }
            }
            if (guideMascotImage != null)
            {
                guideMascotImage.preserveAspect = true;
                guideMascotImage.raycastTarget = false;
                if (guideMascotImage.sprite == null)
                    guideMascotImage.sprite = guideTalkSprite;
            }

            // 3. Speech Bubble
            if (speechBubbleRect == null)
            {
                var bubbleGO = transform.Find("SpeechBubble");
                if (bubbleGO == null)
                {
                    var go = new GameObject("SpeechBubble");
                    go.transform.SetParent(transform, false);
                    speechBubbleRect = go.AddComponent<RectTransform>();
                    speechBubbleImage = go.AddComponent<Image>();
                }
                else
                {
                    speechBubbleRect = bubbleGO.GetComponent<RectTransform>();
                    speechBubbleImage = bubbleGO.GetComponent<Image>();
                }
            }

            // Style Speech Bubble: White background, vibrant blue border
            if (speechBubbleImage != null)
            {
#if UNITY_EDITOR
                if (speechBubbleImage.sprite == null)
                {
                    speechBubbleImage.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                        "Assets/_Game/Art/UI/speech_bubble.png");
                }
#endif
                speechBubbleImage.type = Image.Type.Sliced;
                speechBubbleImage.color = Color.white;
                speechBubbleImage.raycastTarget = false;
            }

            // Bubble Tail
            if (speechBubbleTail == null && speechBubbleRect != null)
            {
                var tailTr = speechBubbleRect.Find("Tail");
                if (tailTr != null)
                {
                    speechBubbleTail = tailTr.GetComponent<Image>();
                }
                else
                {
                    var tailGO = new GameObject("Tail");
                    tailGO.transform.SetParent(speechBubbleRect, false);
                    speechBubbleTail = tailGO.AddComponent<Image>();
                    var tr = tailGO.GetComponent<RectTransform>();
                    tr.sizeDelta = new Vector2(28, 28);
                    speechBubbleTail.color = new Color(0.145f, 0.388f, 0.921f, 1f); // #2563EB Blue
                    speechBubbleTail.raycastTarget = false;
                }
            }

            // Bubble Text (TMP)
            if (speechBubbleText == null && speechBubbleRect != null)
            {
                var textTr = speechBubbleRect.Find("Text");
                if (textTr != null)
                {
                    speechBubbleText = textTr.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(speechBubbleRect, false);
                    speechBubbleText = textGO.AddComponent<TextMeshProUGUI>();
                    var tr = textGO.GetComponent<RectTransform>();
                    tr.anchorMin = Vector2.zero;
                    tr.anchorMax = Vector2.one;
                    tr.offsetMin = new Vector2(24, 16);
                    tr.offsetMax = new Vector2(-24, -16);
                }
            }
            if (speechBubbleText != null)
            {
                speechBubbleText.fontSize = 32;
                speechBubbleText.fontSizeMin = 20;
                speechBubbleText.fontSizeMax = 36;
                speechBubbleText.enableAutoSizing = true;
                speechBubbleText.alignment = TextAlignmentOptions.Center;
                speechBubbleText.color = new Color(0.12f, 0.10f, 0.28f, 1f); // Deep charcoal #1E1B4B
                speechBubbleText.fontStyle = FontStyles.Bold;
                speechBubbleText.raycastTarget = false;
                speechBubbleText.richText = true;
            }

            // 4. Hand Pointer
            if (handPointerRect == null)
            {
                var handGO = transform.Find("HandPointer");
                if (handGO == null)
                {
                    var go = new GameObject("HandPointer");
                    go.transform.SetParent(transform, false);
                    handPointerRect = go.AddComponent<RectTransform>();
                    handPointerImage = go.AddComponent<Image>();
                }
                else
                {
                    handPointerRect = handGO.GetComponent<RectTransform>();
                    handPointerImage = handGO.GetComponent<Image>();
                }
            }
            if (handPointerImage != null)
            {
                handPointerImage.preserveAspect = true;
                handPointerImage.raycastTarget = false;
                if (handPointerImage.sprite == null && handPointerSprite != null)
                    handPointerImage.sprite = handPointerSprite;
                handPointerRect.sizeDelta = new Vector2(110, 110);
                handPointerRect.pivot = new Vector2(0.586f, 0.8125f); // Finger pointing tip
            }
        }
    }
}

