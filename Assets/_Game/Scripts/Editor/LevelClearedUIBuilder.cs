// ============================================================
// LevelClearedUIBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Creates and configures the Level Cleared UI according to PRD:
//   • Canvas Scaler: 1080×1920, Match = 0.5
//   • Full-screen dark purple fade overlay (#1F1035)
//   • Fireworks & Floating Sparkles (UIParticleEmitter & ParticleSystem)
//   • Clapping Dida Mascot on left side
//   • Center Polaroid Card with tape & slight tilt
//   • Yellow 'Level cleared!' banner with pop animation & glow
//   • Coin (+30) & Star (+1) count-up badges
//   • Pulsing 'Tap to continue' button
//   • Saves Prefab to Assets/_Game/Prefabs/UI/Canvas_LevelCleared.prefab
//   • Adds/Updates Canvas in Game.unity scene
//
// Menu: Arrow Puzzle -> Build Level Cleared UI Prefab & Scene UI
// PRD Section 7: Assets/_Game/Scripts/Editor/LevelClearedUIBuilder.cs
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Editor
{
    public static class LevelClearedUIBuilder
    {
        private const string PrefabDir          = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath         = "Assets/_Game/Prefabs/UI/Canvas_LevelCleared.prefab";
        private const string VFXPrefabDir       = "Assets/_Game/Prefabs/VFX";
        private const string VFXPrefabPath      = "Assets/_Game/Prefabs/VFX/VFX_FireworkSparkles.prefab";
        private const string ArtUIDir           = "Assets/_Game/Art/UI";
        private const string ArtCharDir         = "Assets/_Game/Art/Characters";
        private const string ArtVFXDir          = "Assets/_Game/Art/VFX";
        private const string ArtGalleryDir      = "Assets/_Game/Art/Gallery";
        private const string GameScenePath      = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Arrow Puzzle/Build Level Cleared UI Prefab & Scene UI", false, 110)]
        public static void BuildLevelClearedUI()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[LevelClearedUIBuilder] Skipping build – cannot run EditorSceneManager during Play Mode.");
                return;
            }
            Debug.Log("[LevelClearedUIBuilder] Starting build of Level Cleared Screen...");

            // 1. Ensure all relevant textures are imported as Sprites
            FixSpriteImporters();

            // 2. Load sprites
            var spritePolaroidFrame = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/polaroid_frame.png");
            var spriteTape          = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/tape.png");
            var spriteCoin          = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_coin.png");
            var spriteStar          = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_star.png");
            var spriteSparkle       = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtVFXDir}/particle_sparkle.png");
            var spriteGuideClap     = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtCharDir}/guide_clap.png");
            var spritePlaceholder   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtGalleryDir}/pic_placeholder.png");
            var spriteHeartPhoto    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtGalleryDir}/pic_heart.png");

            TMP_FontAsset customFont = FindCustomTMPFont();

            // 3. Build the Canvas Hierarchy
            var root = new GameObject("Canvas_LevelCleared");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Above HUD and game board

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            var canvasGroup = root.AddComponent<CanvasGroup>();
            var levelClearedUI = root.AddComponent<LevelClearedUI>();
            levelClearedUI.rootCanvasGroup = canvasGroup;

            // 4. Dark Purple Overlay
            var overlayGO = CreateUIObject("DarkPurpleOverlay", root.transform);
            var overlayRect = overlayGO.GetComponent<RectTransform>();
            SetStretchAnchors(overlayRect);
            var overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = new Color(0.12f, 0.06f, 0.21f, 0.95f);
            overlayImage.raycastTarget = false;
            levelClearedUI.darkPurpleOverlay = overlayImage;

            // 5. Canvas UI Particle Emitter
            var particleEmitterGO = CreateUIObject("UIParticleEmitter", root.transform);
            var emitterRect = particleEmitterGO.GetComponent<RectTransform>();
            SetStretchAnchors(emitterRect);
            var uiEmitter = particleEmitterGO.AddComponent<UIParticleEmitter>();
            uiEmitter.sparkleSprite = spriteSparkle;
            levelClearedUI.uiParticleEmitter = uiEmitter;

            // 6. Content Root Container
            var contentRoot = CreateUIObject("ContentRoot", root.transform);
            var contentRect = contentRoot.GetComponent<RectTransform>();
            SetStretchAnchors(contentRect);
            levelClearedUI.contentRoot = contentRoot;

            // ── A. Banner: 'Level cleared!' ──────────────────────
            var bannerGO = CreateUIObject("Banner_LevelCleared", contentRoot.transform);
            var bannerRect = bannerGO.GetComponent<RectTransform>();
            SetAnchors(bannerRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            bannerRect.anchoredPosition = new Vector2(0f, -170f);
            bannerRect.sizeDelta = new Vector2(800f, 160f);
            levelClearedUI.titleBannerRect = bannerRect;

            // Glow Text Underlay
            var glowGO = CreateUIObject("Title_Glow", bannerGO.transform);
            var glowRect = glowGO.GetComponent<RectTransform>();
            SetStretchAnchors(glowRect);
            var glowTmp = glowGO.AddComponent<TextMeshProUGUI>();
            glowTmp.text = "Level cleared!";
            glowTmp.fontSize = 76;
            glowTmp.fontStyle = FontStyles.Bold;
            glowTmp.alignment = TextAlignmentOptions.Center;
            glowTmp.color = new Color(1.0f, 0.65f, 0.0f, 0.65f); // Rich golden glow
            if (customFont != null) glowTmp.font = customFont;
            levelClearedUI.titleGlowText = glowTmp;

            // Main Title Text
            var titleGO = CreateUIObject("Title_Text", bannerGO.transform);
            var titleRect = titleGO.GetComponent<RectTransform>();
            SetStretchAnchors(titleRect);
            var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Level cleared!";
            titleTmp.fontSize = 72;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1.0f, 0.91f, 0.2f); // Vibrant bright yellow (#FFE833)
            titleTmp.outlineColor = new Color32(110, 50, 0, 255);
            titleTmp.outlineWidth = 0.22f;
            if (customFont != null) titleTmp.font = customFont;
            levelClearedUI.titleText = titleTmp;

            // ── B. Polaroid Photo Card (Center) ──────────────────
            var polaroidGO = CreateUIObject("PolaroidCard", contentRoot.transform);
            var polaroidRect = polaroidGO.GetComponent<RectTransform>();
            SetAnchors(polaroidRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            polaroidRect.anchoredPosition = new Vector2(0f, 40f);
            polaroidRect.sizeDelta = new Vector2(490f, 560f);
            polaroidRect.localEulerAngles = new Vector3(0f, 0f, -4.5f); // PRD: slight tilt
            levelClearedUI.polaroidCardRect = polaroidRect;

            // Polaroid Background Frame
            var frameImage = polaroidGO.AddComponent<Image>();
            frameImage.sprite = spritePolaroidFrame;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;
            levelClearedUI.polaroidFrameImage = frameImage;

            // Inner Photo Container
            var photoGO = CreateUIObject("RewardPhoto", polaroidGO.transform);
            var photoRect = photoGO.GetComponent<RectTransform>();
            SetAnchors(photoRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            photoRect.anchoredPosition = new Vector2(0f, 42f);
            photoRect.sizeDelta = new Vector2(395f, 390f);
            var photoImage = photoGO.AddComponent<Image>();
            photoImage.sprite = spriteHeartPhoto != null ? spriteHeartPhoto : spritePlaceholder;
            photoImage.preserveAspect = true;
            photoImage.raycastTarget = false;
            levelClearedUI.rewardPhotoImage = photoImage;
            levelClearedUI.defaultPlaceholderPhoto = spritePlaceholder;

            // Polaroid Top Tape
            var tapeGO = CreateUIObject("Tape", polaroidGO.transform);
            var tapeRect = tapeGO.GetComponent<RectTransform>();
            SetAnchors(tapeRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f));
            tapeRect.anchoredPosition = new Vector2(0f, -12f);
            tapeRect.sizeDelta = new Vector2(230f, 65f);
            tapeRect.localEulerAngles = new Vector3(0f, 0f, 7.5f); // Natural tape angle
            var tapeImg = tapeGO.AddComponent<Image>();
            tapeImg.sprite = spriteTape;
            tapeImg.color = Color.white;
            tapeImg.raycastTarget = false;
            levelClearedUI.tapeImage = tapeImg;

            // ── C. Guide Mascot: Dida Clapping (Left) ────────────
            var guideGO = CreateUIObject("Guide_Dida", contentRoot.transform);
            var guideRect = guideGO.GetComponent<RectTransform>();
            SetAnchors(guideRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
            guideRect.anchoredPosition = new Vector2(175f, -150f);
            guideRect.sizeDelta = new Vector2(360f, 450f);
            var guideImg = guideGO.AddComponent<Image>();
            guideImg.sprite = spriteGuideClap;
            guideImg.preserveAspect = true;
            guideImg.raycastTarget = false;
            levelClearedUI.guideMascotRect = guideRect;
            levelClearedUI.guideMascotImage = guideImg;
            levelClearedUI.guideClapSprite = spriteGuideClap;

            // ── D. Rewards Row: Coins +30, Star +1 ────────────────
            var rewardsRowGO = CreateUIObject("RewardsRow", contentRoot.transform);
            var rewardsRect = rewardsRowGO.GetComponent<RectTransform>();
            SetAnchors(rewardsRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rewardsRect.anchoredPosition = new Vector2(0f, -320f);
            rewardsRect.sizeDelta = new Vector2(460f, 100f);
            levelClearedUI.rewardsRowRect = rewardsRect;

            // Coin Badge (Left)
            var coinBadgeGO = CreateUIObject("CoinBadge", rewardsRowGO.transform);
            var coinBadgeRect = coinBadgeGO.GetComponent<RectTransform>();
            SetAnchors(coinBadgeRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            coinBadgeRect.anchoredPosition = new Vector2(-115f, 0f);
            coinBadgeRect.sizeDelta = new Vector2(190f, 72f);

            var coinIconGO = CreateUIObject("CoinIcon", coinBadgeGO.transform);
            var coinIconRect = coinIconGO.GetComponent<RectTransform>();
            SetAnchors(coinIconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            coinIconRect.anchoredPosition = new Vector2(0f, 0f);
            coinIconRect.sizeDelta = new Vector2(64f, 64f);
            var coinIconImg = coinIconGO.AddComponent<Image>();
            coinIconImg.sprite = spriteCoin;
            coinIconImg.preserveAspect = true;
            levelClearedUI.coinIcon = coinIconImg;

            var coinTextGO = CreateUIObject("CoinText", coinBadgeGO.transform);
            var coinTextRect = coinTextGO.GetComponent<RectTransform>();
            SetAnchors(coinTextRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f));
            coinTextRect.offsetMin = new Vector2(72f, 0f);
            coinTextRect.offsetMax = new Vector2(0f, 0f);
            var coinTmp = coinTextGO.AddComponent<TextMeshProUGUI>();
            coinTmp.text = "+30";
            coinTmp.fontSize = 44;
            coinTmp.fontStyle = FontStyles.Bold;
            coinTmp.alignment = TextAlignmentOptions.MidlineLeft;
            coinTmp.color = new Color(1.0f, 0.92f, 0.22f); // Golden yellow
            if (customFont != null) coinTmp.font = customFont;
            levelClearedUI.coinCountText = coinTmp;

            // Star Badge (Right)
            var starBadgeGO = CreateUIObject("StarBadge", rewardsRowGO.transform);
            var starBadgeRect = starBadgeGO.GetComponent<RectTransform>();
            SetAnchors(starBadgeRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            starBadgeRect.anchoredPosition = new Vector2(115f, 0f);
            starBadgeRect.sizeDelta = new Vector2(170f, 72f);

            var starIconGO = CreateUIObject("StarIcon", starBadgeGO.transform);
            var starIconRect = starIconGO.GetComponent<RectTransform>();
            SetAnchors(starIconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            starIconRect.anchoredPosition = new Vector2(0f, 0f);
            starIconRect.sizeDelta = new Vector2(64f, 64f);
            var starIconImg = starIconGO.AddComponent<Image>();
            starIconImg.sprite = spriteStar;
            starIconImg.preserveAspect = true;
            levelClearedUI.starIcon = starIconImg;

            var starTextGO = CreateUIObject("StarText", starBadgeGO.transform);
            var starTextRect = starTextGO.GetComponent<RectTransform>();
            SetAnchors(starTextRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f));
            starTextRect.offsetMin = new Vector2(72f, 0f);
            starTextRect.offsetMax = new Vector2(0f, 0f);
            var starTmp = starTextGO.AddComponent<TextMeshProUGUI>();
            starTmp.text = "+1";
            starTmp.fontSize = 44;
            starTmp.fontStyle = FontStyles.Bold;
            starTmp.alignment = TextAlignmentOptions.MidlineLeft;
            starTmp.color = new Color(1.0f, 0.85f, 0.15f); // Bright star gold
            if (customFont != null) starTmp.font = customFont;
            levelClearedUI.starCountText = starTmp;

            // ── E. Tap To Continue Prompt (Bottom) ───────────────
            var tapGO = CreateUIObject("TapToContinue", contentRoot.transform);
            var tapRect = tapGO.GetComponent<RectTransform>();
            SetAnchors(tapRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            tapRect.anchoredPosition = new Vector2(0f, 170f);
            tapRect.sizeDelta = new Vector2(600f, 70f);
            levelClearedUI.tapToContinueRect = tapRect;

            var tapTmp = tapGO.AddComponent<TextMeshProUGUI>();
            tapTmp.text = "Tap to continue";
            tapTmp.fontSize = 36;
            tapTmp.fontStyle = FontStyles.Bold;
            tapTmp.alignment = TextAlignmentOptions.Center;
            tapTmp.color = Color.white;
            if (customFont != null) tapTmp.font = customFont;
            levelClearedUI.tapToContinueText = tapTmp;

            // ── F. Full-Screen Invisible Tap Button ──────────────
            var btnGO = CreateUIObject("FullScreenTapButton", root.transform);
            var btnRect = btnGO.GetComponent<RectTransform>();
            SetStretchAnchors(btnRect);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0, 0, 0, 0); // Transparent
            btnImg.raycastTarget = true;
            var tapBtn = btnGO.AddComponent<Button>();
            tapBtn.transition = Selectable.Transition.None;
            levelClearedUI.fullScreenTapButton = tapBtn;

            // Move Tap Button right behind content so elements on top or entire screen can receive click
            btnGO.transform.SetAsLastSibling();

            // 7. Save Prefab
            EnsureDirectory(PrefabDir);
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[LevelClearedUIBuilder] Successfully created and saved prefab to {PrefabPath}");

            // 8. Create optional VFX ParticleSystem prefab as well
            CreateParticleSystemVFXPrefab(spriteSparkle);

            // 9. Add to Game Scene
            AddPrefabToGameScene(prefab);

            // Clean up temporary scene object if not playing
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Arrow Puzzle/Add Level Cleared UI to Game Scene", false, 111)]
        public static void AddToGameSceneMenu()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                BuildLevelClearedUI();
                return;
            }
            AddPrefabToGameScene(prefab);
        }

        private static void AddPrefabToGameScene(GameObject prefab)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GameScenePath);

            var existing = Object.FindObjectOfType<LevelClearedUI>();
            if (existing != null)
            {
                Debug.Log($"[LevelClearedUIBuilder] LevelClearedUI already exists in {GameScenePath}.");
            }
            else
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "Canvas_LevelCleared";
                Debug.Log($"[LevelClearedUIBuilder] Instantiated {prefab.name} in {GameScenePath}.");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LevelClearedUIBuilder] Saved scene {GameScenePath}");
        }

        private static void CreateParticleSystemVFXPrefab(Sprite sparkleSprite)
        {
            EnsureDirectory(VFXPrefabDir);
            var vfxGO = new GameObject("VFX_FireworkSparkles");
            var ps = vfxGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // 5 festive colors
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.84f, 0f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.16f, 0.52f), 0.25f),
                    new GradientColorKey(new Color(0f, 0.9f, 1f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.48f, 0f), 0.75f),
                    new GradientColorKey(new Color(0.71f, 0.21f, 1f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(grad);

            var emission = ps.emission;
            emission.rateOverTime = 12f;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 25),
                new ParticleSystem.Burst(0.8f, 30),
                new ParticleSystem.Burst(1.6f, 25)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            PrefabUtility.SaveAsPrefabAsset(vfxGO, VFXPrefabPath);
            Object.DestroyImmediate(vfxGO);
            Debug.Log($"[LevelClearedUIBuilder] Saved VFX prefab to {VFXPrefabPath}");
        }

        // ── Texture Importer & Font Helpers ───────────────────

        private static void FixSpriteImporters()
        {
            string[] dirs = new string[] { ArtUIDir, ArtCharDir, ArtVFXDir, ArtGalleryDir };
            bool changed = false;

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                string[] files = Directory.GetFiles(dir, "*.png");
                foreach (var path in files)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null)
                    {
                        if (importer.textureType != TextureImporterType.Sprite ||
                            importer.spriteImportMode != SpriteImportMode.Single)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.alphaIsTransparency = true;
                            importer.mipmapEnabled = false;
                            EditorUtility.SetDirty(importer);
                            importer.SaveAndReimport();
                            changed = true;
                            Debug.Log($"[LevelClearedUIBuilder] Converted {path} to Sprite.");
                        }
                    }
                }
            }
            if (changed) AssetDatabase.Refresh();
        }

        private static TMP_FontAsset FindCustomTMPFont()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_Game/Fonts") || path.Contains("Lilita") || path.Contains("Fredoka") || path.Contains("Baloo"))
                {
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (font != null) return font;
                }
            }
            return null;
        }

        // ── Hierarchy Creation Helpers ────────────────────────

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot     = pivot;
        }

        private static void SetStretchAnchors(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot     = new Vector2(0.5f, 0.5f);
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
#endif

