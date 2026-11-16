// ============================================================
// SceneTransition.cs  –  Arrow Slide Puzzle
// Reusable Canvas overlay for scene transitions and Splash screen:
//   • Canvas Scaler: 1080×1920, Match 0.5
//   • Background: Solid light-blue (#2FB4E8) with faint repeating icon pattern
//   • 4 glossy 3D arrows in center (2×2 grid, 130×130, 30px gap):
//       Top-Left Pink (Up), Top-Right Orange (Right),
//       Bottom-Left Light-Blue (Left), Bottom-Right Purple (Down)
//   • 5 loading dots at bottom-center (~15% from bottom = 288px)
//       22px dots, 18px gap; 1 active bold dot looping left-to-right
//   • States:
//       - Arrows start at ~30% opacity, brighten to 100% in ~0.5s
//       - Overlay cross-fades over previous scene in ~0.8s to light-blue
//       - Clean fade-out (~0.5s) once target scene is loaded
//   • Static API: SceneTransition.Load("Map")
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        private const string PrefabResourcePath = "UI/SceneTransition";
        public static readonly Color SplashBlue = new Color(0.1843f, 0.7059f, 0.9098f, 1f); // #2FB4E8

        // ── Inspector References ─────────────────────────────────────
        [Header("Canvas & Groups")]
        public Canvas canvas;
        public CanvasGroup mainCanvasGroup;
        public CanvasScaler canvasScaler;
        public GraphicRaycaster raycaster;

        [Header("Background & Pattern")]
        public Image backgroundImage;
        public RawImage patternOverlay;

        [Header("Arrow Grid (Center 2x2)")]
        public CanvasGroup arrowGroup;
        public RectTransform arrowPinkUp;
        public RectTransform arrowOrangeRight;
        public RectTransform arrowCyanLeft;
        public RectTransform arrowPurpleDown;

        [Header("Loading Dots (Bottom Center)")]
        public RectTransform dotsContainer;
        public Image[] loadingDots = new Image[5];

        [Header("Timing")]
        [Tooltip("Overlay cross-fade in duration (seconds)")]
        public float crossFadeInDuration = 0.8f;

        [Tooltip("Arrows fade from 30% to 100% brightness (seconds)")]
        public float arrowFadeDuration = 0.5f;

        [Tooltip("Overlay fade-out duration after loading (seconds)")]
        public float fadeOutDuration = 0.5f;

        [Tooltip("Minimum time transition overlay remains visible")]
        public float minDisplayDuration = 1.1f;

        [Tooltip("Time interval per loading dot step")]
        public float dotStepInterval = 0.22f;

        // ── Runtime State ────────────────────────────────────────────
        private bool _isTransitioning = false;
        private Coroutine _dotsCoroutine;
        private Coroutine _transitionCoroutine;
        private int _activeDotIndex = 0;

        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureComponents();
        }

        private void Start()
        {
            // If in Splash scene, keep visible initially
            if (SceneManager.GetActiveScene().name == "Splash")
            {
                ShowInstant();
                StartDotsAnimation();
                StartCoroutine(FadeArrowsRoutine(0.3f, 1f, arrowFadeDuration));
            }
            else if (!_isTransitioning)
            {
                HideInstant();
            }
        }

        // ── Static Public API ────────────────────────────────────────
        /// <summary>
        /// Loads the target scene with the smooth light-blue cross-fade transition.
        /// Can be called from any scene: SceneTransition.Load("Map");
        /// </summary>
        public static void Load(string sceneName)
        {
            EnsureInstance();
            Instance.TransitionToScene(sceneName);
        }

        /// <summary>Instance alias for scene transition.</summary>
        public void LoadScene(string sceneName) => TransitionToScene(sceneName);

        private static void EnsureInstance()
        {
            if (Instance != null) return;

            // Try loading from Resources
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab != null)
            {
                var go = Instantiate(prefab);
                go.name = "SceneTransition";
                Instance = go.GetComponent<SceneTransition>();
                DontDestroyOnLoad(go);
            }
            else
            {
                // Fallback: create dynamically
                Debug.LogWarning($"[SceneTransition] Prefab not found at Resources/{PrefabResourcePath}. Creating fallback dynamically.");
                var go = CreateDynamicTransitionObject();
                Instance = go.GetComponent<SceneTransition>();
                DontDestroyOnLoad(go);
            }
        }

        // ── Transition Logic ─────────────────────────────────────────
        public void TransitionToScene(string sceneName)
        {
            if (_isTransitioning) return;

            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);

            _transitionCoroutine = StartCoroutine(RunTransition(sceneName));
        }

        private IEnumerator RunTransition(string sceneName)
        {
            _isTransitioning = true;
            gameObject.SetActive(true);

            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = 999; // Ensure on top of all HUDs
            }

            if (raycaster != null)
                raycaster.enabled = true;

            // 1. Prepare initial states
            // Background cross-fade starts from transparent
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            // Arrows start at ~30% opacity
            if (arrowGroup != null)
                arrowGroup.alpha = 0.3f;

            // Start looping loading dots
            StartDotsAnimation();

            // 2. Cross-fade overlay in over ~0.8s
            float elapsed = 0f;
            float arrowElapsed = 0f;
            float totalStartTime = Time.unscaledTime;

            // Async scene load in background
            AsyncOperation asyncOp = null;
            try
            {
                asyncOp = SceneManager.LoadSceneAsync(sceneName);
                if (asyncOp != null)
                    asyncOp.allowSceneActivation = false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SceneTransition] Failed to start loading scene '{sceneName}': {ex.Message}");
            }

            while (elapsed < crossFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                arrowElapsed += Time.unscaledDeltaTime;

                float tOverlay = Mathf.Clamp01(elapsed / crossFadeInDuration);
                if (mainCanvasGroup != null)
                    mainCanvasGroup.alpha = tOverlay;

                if (arrowGroup != null)
                {
                    float tArrow = Mathf.Clamp01(arrowElapsed / arrowFadeDuration);
                    arrowGroup.alpha = Mathf.Lerp(0.3f, 1f, tArrow);
                }

                yield return null;
            }

            if (mainCanvasGroup != null)
                mainCanvasGroup.alpha = 1f;

            // Continue arrow brightening if arrowFadeDuration > crossFadeInDuration (default is 0.5s <= 0.8s)
            while (arrowElapsed < arrowFadeDuration)
            {
                arrowElapsed += Time.unscaledDeltaTime;
                float tArrow = Mathf.Clamp01(arrowElapsed / arrowFadeDuration);
                if (arrowGroup != null)
                    arrowGroup.alpha = Mathf.Lerp(0.3f, 1f, tArrow);
                yield return null;
            }

            if (arrowGroup != null)
                arrowGroup.alpha = 1f;

            // 3. Wait until scene is loaded and minimum display duration met
            while (asyncOp != null && asyncOp.progress < 0.9f)
            {
                yield return null;
            }

            while (Time.unscaledTime - totalStartTime < minDisplayDuration)
            {
                yield return null;
            }

            // 4. Activate new scene
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = true;
                while (!asyncOp.isDone)
                    yield return null;
            }

            // Wait 1 frame for new scene camera and UI to initialize
            yield return null;

            // 5. Cross-fade overlay out over ~0.5s
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                if (mainCanvasGroup != null)
                    mainCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            HideInstant();
            _isTransitioning = false;
            _transitionCoroutine = null;
        }

        // ── Splash Sequence Helper ───────────────────────────────────
        /// <summary>
        /// Plays the Splash intro sequence (for initial game launch).
        /// Arrows brighten from 30% to 100% in 0.5s, dots animate, then calls onComplete.
        /// </summary>
        public IEnumerator PlaySplashIntro(float holdTime, System.Action onComplete)
        {
            ShowInstant();
            if (arrowGroup != null)
                arrowGroup.alpha = 0.3f;

            StartDotsAnimation();

            yield return StartCoroutine(FadeArrowsRoutine(0.3f, 1f, arrowFadeDuration));

            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            onComplete?.Invoke();
        }

        private IEnumerator FadeArrowsRoutine(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            if (arrowGroup != null) arrowGroup.alpha = fromAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (arrowGroup != null)
                    arrowGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            if (arrowGroup != null)
                arrowGroup.alpha = toAlpha;
        }

        // ── Loading Dots Loop Animation ──────────────────────────────
        public void StartDotsAnimation()
        {
            if (_dotsCoroutine != null)
                StopCoroutine(_dotsCoroutine);

            _dotsCoroutine = StartCoroutine(LoopDotsAnimation());
        }

        public void StopDotsAnimation()
        {
            if (_dotsCoroutine != null)
            {
                StopCoroutine(_dotsCoroutine);
                _dotsCoroutine = null;
            }
        }

        private IEnumerator LoopDotsAnimation()
        {
            _activeDotIndex = 0;
            while (true)
            {
                UpdateDotsVisuals(_activeDotIndex);
                yield return new WaitForSecondsRealtime(dotStepInterval);
                _activeDotIndex = (_activeDotIndex + 1) % 5;
            }
        }

        private void UpdateDotsVisuals(int activeIndex)
        {
            if (loadingDots == null) return;

            for (int i = 0; i < loadingDots.Length; i++)
            {
                var dot = loadingDots[i];
                if (dot == null) continue;

                bool isActive = (i == activeIndex);
                // Active dot: bold, full opacity (1.0), larger scale (~1.35x)
                // Inactive dots: smaller (1.0x), semi-transparent (0.35 alpha)
                float targetScale = isActive ? 1.35f : 1.0f;
                float targetAlpha = isActive ? 1.0f : 0.35f;

                dot.transform.localScale = Vector3.one * targetScale;
                Color c = Color.white;
                c.a = targetAlpha;
                dot.color = c;
            }
        }

        // ── Utility Visibility Helpers ───────────────────────────────
        public void ShowInstant()
        {
            gameObject.SetActive(true);
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.sortingOrder = 999;
            }
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }
            if (raycaster != null)
                raycaster.enabled = true;
        }

        public void HideInstant()
        {
            StopDotsAnimation();
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }
            if (raycaster != null)
                raycaster.enabled = false;
            if (canvas != null)
                canvas.enabled = false;
        }

        private void EnsureComponents()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvasScaler == null) canvasScaler = GetComponent<CanvasScaler>();
            if (mainCanvasGroup == null) mainCanvasGroup = GetComponent<CanvasGroup>();
            if (raycaster == null) raycaster = GetComponent<GraphicRaycaster>();

            if (backgroundImage != null)
                backgroundImage.color = SplashBlue;
        }

        // ── Dynamic Fallback Construction ────────────────────────────
        private static GameObject CreateDynamicTransitionObject()
        {
            var go = new GameObject("SceneTransition");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = go.AddComponent<GraphicRaycaster>();
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var comp = go.AddComponent<SceneTransition>();
            comp.canvas = canvas;
            comp.canvasScaler = scaler;
            comp.mainCanvasGroup = cg;
            comp.raycaster = raycaster;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            Stretch(bgRT);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = SplashBlue;
            comp.backgroundImage = bgImg;

            // Arrow Grid
            var gridGO = new GameObject("ArrowGrid");
            gridGO.transform.SetParent(go.transform, false);
            var gridRT = gridGO.AddComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridRT.anchorMax = new Vector2(0.5f, 0.5f);
            gridRT.pivot     = new Vector2(0.5f, 0.5f);
            gridRT.sizeDelta = new Vector2(290f, 290f);
            gridRT.anchoredPosition = Vector2.zero;

            var arrowCG = gridGO.AddComponent<CanvasGroup>();
            arrowCG.alpha = 0.3f;
            comp.arrowGroup = arrowCG;

            comp.arrowPinkUp      = CreateDynamicArrow(gridGO.transform, "Arrow_Pink_Up", new Vector2(-80, 80));
            comp.arrowOrangeRight = CreateDynamicArrow(gridGO.transform, "Arrow_Orange_Right", new Vector2(80, 80));
            comp.arrowCyanLeft    = CreateDynamicArrow(gridGO.transform, "Arrow_Cyan_Left", new Vector2(-80, -80));
            comp.arrowPurpleDown  = CreateDynamicArrow(gridGO.transform, "Arrow_Purple_Down", new Vector2(80, -80));

            // Loading dots
            var dotsGO = new GameObject("LoadingDots");
            dotsGO.transform.SetParent(go.transform, false);
            var dotsRT = dotsGO.AddComponent<RectTransform>();
            dotsRT.anchorMin = new Vector2(0.5f, 0f);
            dotsRT.anchorMax = new Vector2(0.5f, 0f);
            dotsRT.pivot     = new Vector2(0.5f, 0.5f);
            dotsRT.anchoredPosition = new Vector2(0f, 288f);
            dotsRT.sizeDelta = new Vector2(200f, 40f);
            comp.dotsContainer = dotsRT;

            comp.loadingDots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var dGO = new GameObject($"Dot_{i}");
                dGO.transform.SetParent(dotsRT, false);
                var dRT = dGO.AddComponent<RectTransform>();
                dRT.anchorMin = new Vector2(0.5f, 0.5f);
                dRT.anchorMax = new Vector2(0.5f, 0.5f);
                dRT.pivot     = new Vector2(0.5f, 0.5f);
                dRT.sizeDelta = new Vector2(22f, 22f);
                dRT.anchoredPosition = new Vector2(-80f + i * 40f, 0f);
                var dImg = dGO.AddComponent<Image>();
                dImg.color = (i == 0) ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                dRT.localScale = (i == 0) ? Vector3.one * 1.35f : Vector3.one;
                comp.loadingDots[i] = dImg;
            }

            return go;
        }

        private static RectTransform CreateDynamicArrow(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(130f, 130f);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        // ── Context Menu Test Methods ────────────────────────────────
        [ContextMenu("Test CrossFade In (0.8s)")]
        private void ContextTestFadeIn()
        {
            StopAllCoroutines();
            StartCoroutine(RunTransition(SceneManager.GetActiveScene().name));
        }

        [ContextMenu("Test Arrow Brighten (30% -> 100%)")]
        private void ContextTestArrowBrighten()
        {
            StopAllCoroutines();
            StartCoroutine(FadeArrowsRoutine(0.3f, 1f, arrowFadeDuration));
        }

        [ContextMenu("Test Loading Dots Loop")]
        private void ContextTestDots()
        {
            StartDotsAnimation();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureComponents();
        }
#endif
    }
}
