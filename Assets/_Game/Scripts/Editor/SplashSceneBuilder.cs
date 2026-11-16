// ============================================================
// SplashSceneBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Builds the Splash scene UI and the SceneTransition Prefab:
//   • Canvas Scaler: 1080×1920, Match = 0.5
//   • Full-screen light-blue (#2FB4E8) background Image
//   • Faint icon-pattern overlay (low-contrast repeating gift, star, puzzle, dice)
//   • 2×2 arrow grid:
//       Top-Left   Pink  ↑  (arrowPinkUp)      [-80,  80], 130×130
//       Top-Right  Orange → (arrowOrangeRight) [ 80,  80], 130×130
//       Bottom-Left Cyan  ← (arrowCyanLeft)    [-80, -80], 130×130
//       Bottom-Right Purple ↓(arrowPurpleDown) [ 80, -80], 130×130
//     Each arrow is styled with a glossy 3D look
//   • Loading dots:
//       Anchor bottom-center, ~15% up (y = 288)
//       5 round white dots (22×22, 18px gap), 1 active bold dot looping left-to-right
//   • Saves Prefab to Assets/Resources/UI/SceneTransition.prefab
//     and Assets/_Game/Prefabs/UI/SceneTransition.prefab
//   • Updates Splash.unity
//
// Menu: Arrow Puzzle → Build Splash Scene & Transition Prefab
// PRD Section 7: Assets/_Game/Scripts/Editor/SplashSceneBuilder.cs
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ArrowPuzzle.UI;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.Editor
{
    public static class SplashSceneBuilder
    {
        private const string SplashScenePath     = "Assets/_Game/Scenes/Splash.unity";
        private const string ArtUIDir            = "Assets/_Game/Art/UI";
        private const string PrefabDir           = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath          = "Assets/_Game/Prefabs/UI/SceneTransition.prefab";
        private const string ResourcesDir        = "Assets/Resources/UI";
        private const string ResourcesPrefabPath = "Assets/Resources/UI/SceneTransition.prefab";

        public static readonly Color SplashBlue = new Color(0.1843f, 0.7059f, 0.9098f, 1f); // #2FB4E8

        [MenuItem("Arrow Puzzle/Build Splash Scene & Transition Prefab", false, 100)]
        [MenuItem("Arrow Puzzle/Build Splash Screen", false, 101)]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[SplashSceneBuilder] Skipping build – cannot run EditorSceneManager during Play Mode.");
                return;
            }

            EnsureDirectories();
            EnsureSprites();

            // 3. Open Splash.unity FIRST so canvasGO is created in the correct scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SplashScenePath);

            // Remove existing Canvas in Splash
            var existingCanvases = Object.FindObjectsOfType<Canvas>();
            foreach (var c in existingCanvases)
            {
                if (c.gameObject.name.Contains("Splash") || c.gameObject.name.Contains("Transition"))
                {
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Create EventSystem if missing
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 1. Build hierarchy in memory (now created inside Splash scene)
            var canvasGO = BuildTransitionHierarchy("Canvas_Splash");

            // 2. Save as Prefabs
            SaveAsPrefab(canvasGO, PrefabPath);
            SaveAsPrefab(canvasGO, ResourcesPrefabPath);

            // Move the created canvas into the open scene
            canvasGO.transform.SetParent(null);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log("[SplashSceneBuilder] Splash scene and SceneTransition prefabs built successfully!");
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PrefabPath) || !File.Exists(ResourcesPrefabPath))
                {
                    Build();
                }
            };
        }

        public static GameObject BuildTransitionHierarchy(string rootName = "Canvas_SceneTransition")
        {
            // ── Root Canvas ──────────────────────────────────────────
            var canvasGO = new GameObject(rootName);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = canvasGO.AddComponent<GraphicRaycaster>();
            var mainCG = canvasGO.AddComponent<CanvasGroup>();
            mainCG.alpha = 1f;

            // ── SceneTransition Component ─────────────────────────────
            var transition = canvasGO.AddComponent<SceneTransition>();
            transition.canvas = canvas;
            transition.canvasScaler = scaler;
            transition.mainCanvasGroup = mainCG;
            transition.raycaster = raycaster;

            // ── SplashScreenController Component ──────────────────────
            var splashCtrl = canvasGO.AddComponent<SplashScreenController>();

            // ── Background Image (#2FB4E8) ───────────────────────────
            var bgGO = UIHelper.CreateImage(canvasGO.transform, "Background");
            var bgRT = bgGO.GetComponent<RectTransform>();
            UIHelper.Stretch(bgRT);
            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = SplashBlue;
            bgImg.raycastTarget = true;
            transition.backgroundImage = bgImg;
            splashCtrl.backgroundImage = bgImg;

            // ── Pattern Overlay (Repeating low-contrast icons) ───────
            var patternGO = new GameObject("IconPatternOverlay");
            patternGO.transform.SetParent(canvasGO.transform, false);
            var patternRT = patternGO.AddComponent<RectTransform>();
            UIHelper.Stretch(patternRT);

            var patternTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(ArtUIDir, "bg_pattern_splash.png"));
            var patternRaw = patternGO.AddComponent<RawImage>();
            patternRaw.texture = patternTex;
            // UV Rect tiling (1080/256 x 1920/256)
            patternRaw.uvRect = new Rect(0, 0, 1080f / 256f, 1920f / 256f);
            patternRaw.color = new Color(1f, 1f, 1f, 0.12f);
            patternRaw.raycastTarget = false;
            transition.patternOverlay = patternRaw;
            splashCtrl.rawPatternOverlay = patternRaw;

            // ── SafeArea Container ────────────────────────────────────
            var safeGO = new GameObject("SafeArea");
            safeGO.transform.SetParent(canvasGO.transform, false);
            var safeRT = safeGO.AddComponent<RectTransform>();
            UIHelper.Stretch(safeRT);
            safeGO.AddComponent<SafeAreaFitter>();

            // ── Arrow Grid Container (Center 50% height) ─────────────
            var gridGO = new GameObject("ArrowGrid");
            gridGO.transform.SetParent(safeRT, false);
            var gridRT = gridGO.AddComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridRT.anchorMax = new Vector2(0.5f, 0.5f);
            gridRT.pivot     = new Vector2(0.5f, 0.5f);
            gridRT.sizeDelta = new Vector2(290f, 290f);
            gridRT.anchoredPosition = Vector2.zero;

            var arrowCG = gridGO.AddComponent<CanvasGroup>();
            arrowCG.alpha = 0.3f; // Starts at ~30% opacity
            transition.arrowGroup = arrowCG;
            splashCtrl.arrowGroup = arrowCG;

            // 4 Arrows: 130x130, 30px gap -> distance = 160 -> half = 80
            float offset = 80f;
            float arrowSize = 130f;

            var pinkUpSprite    = LoadSprite("arrow_glossy_pink_up");
            var orangeRtSprite  = LoadSprite("arrow_glossy_orange_right");
            var cyanLtSprite    = LoadSprite("arrow_glossy_cyan_left");
            var purpleDnSprite  = LoadSprite("arrow_glossy_purple_down");

            var pinkUp = CreateArrow(gridGO.transform, "Arrow_Pink_Up",
                new Vector2(-offset, offset), arrowSize, pinkUpSprite);
            var orangeRt = CreateArrow(gridGO.transform, "Arrow_Orange_Right",
                new Vector2(offset, offset), arrowSize, orangeRtSprite);
            var cyanLt = CreateArrow(gridGO.transform, "Arrow_Cyan_Left",
                new Vector2(-offset, -offset), arrowSize, cyanLtSprite);
            var purpleDn = CreateArrow(gridGO.transform, "Arrow_Purple_Down",
                new Vector2(offset, -offset), arrowSize, purpleDnSprite);

            transition.arrowPinkUp       = pinkUp;
            transition.arrowOrangeRight  = orangeRt;
            transition.arrowCyanLeft     = cyanLt;
            transition.arrowPurpleDown   = purpleDn;

            splashCtrl.arrowPinkUp       = pinkUp;
            splashCtrl.arrowOrangeRight  = orangeRt;
            splashCtrl.arrowCyanLeft     = cyanLt;
            splashCtrl.arrowPurpleDown   = purpleDn;

            // ── Loading Dots Container (Bottom Center, ~15% up) ──────
            var dotsGO = new GameObject("LoadingDots");
            dotsGO.transform.SetParent(safeRT, false);
            var dotsRT = dotsGO.AddComponent<RectTransform>();
            dotsRT.anchorMin = new Vector2(0.5f, 0f);
            dotsRT.anchorMax = new Vector2(0.5f, 0f);
            dotsRT.pivot     = new Vector2(0.5f, 0.5f);
            // 15% of 1920 = 288px
            dotsRT.anchoredPosition = new Vector2(0f, 288f);
            dotsRT.sizeDelta = new Vector2(200f, 40f);

            transition.dotsContainer = dotsRT;
            splashCtrl.dotsContainer = dotsRT;

            // 5 round white dots (22px diameter, 18px gap -> step = 40px)
            var circleSprite = LoadSprite("dot_white_circle");
            Image[] dots = new Image[5];
            float dotStep = 40f;
            float startX = -2f * dotStep; // -80, -40, 0, 40, 80

            for (int i = 0; i < 5; i++)
            {
                var dotGO = new GameObject($"Dot_{i}");
                dotGO.transform.SetParent(dotsRT, false);
                var dotRT = dotGO.AddComponent<RectTransform>();
                dotRT.anchorMin = new Vector2(0.5f, 0.5f);
                dotRT.anchorMax = new Vector2(0.5f, 0.5f);
                dotRT.pivot     = new Vector2(0.5f, 0.5f);
                dotRT.sizeDelta = new Vector2(22f, 22f);
                dotRT.anchoredPosition = new Vector2(startX + i * dotStep, 0f);

                var img = dotGO.AddComponent<Image>();
                img.sprite = circleSprite;
                img.raycastTarget = false;

                // Active dot starts bold and large, others dim and small
                if (i == 0)
                {
                    dotRT.localScale = Vector3.one * 1.35f;
                    img.color = Color.white;
                }
                else
                {
                    dotRT.localScale = Vector3.one;
                    img.color = new Color(1f, 1f, 1f, 0.35f);
                }

                dots[i] = img;
            }

            transition.loadingDots = dots;
            splashCtrl.loadingDots = dots;

            // ── Fade Overlay Image ───────────────────────────────────
            var fadeGO = UIHelper.CreateImage(canvasGO.transform, "FadeOverlay");
            var fadeRT = fadeGO.GetComponent<RectTransform>();
            UIHelper.Stretch(fadeRT);
            var fadeImg = fadeGO.GetComponent<Image>();
            fadeImg.color = new Color(SplashBlue.r, SplashBlue.g, SplashBlue.b, 0f);
            fadeImg.raycastTarget = false;
            splashCtrl.fadeOverlay = fadeImg;

            return canvasGO;
        }

        private static RectTransform CreateArrow(Transform parent, string name,
            Vector2 pos, float size, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;

            return rt;
        }

        private static Sprite LoadSprite(string nameWithoutExt)
        {
            string path = Path.Combine(ArtUIDir, nameWithoutExt + ".png").Replace('\\', '/');
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);
        }

        private static void EnsureSprites()
        {
            SplashAssetGenerator.GenerateAllSprites();
        }

        private static void SaveAsPrefab(GameObject go, string path)
        {
            path = path.Replace('\\', '/');
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
