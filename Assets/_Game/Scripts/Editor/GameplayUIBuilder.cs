// ============================================================
// GameplayUIBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Creates and configures the Gameplay HUD UI according to PRD:
//   • Canvas Scaler: 1080×1920, Match = 0.5
//   • Top-Left: Coin pill (icon + count label)
//   • Top-Center: Heart pill (3 heart image slots)
//   • Top-Right: Orange Back button + Green Gear button
//   • Right Vertical: Hint button (650 / FREE), Magnet button (1300 / FREE)
//   • Auto-converts UI textures in Art/UI to Sprites if needed
//   • Saves Prefab to Assets/_Game/Prefabs/UI/Canvas_GameplayHUD.prefab
//   • Adds/Updates Canvas in open scene (Game.unity)
//
// Menu: Arrow Puzzle -> Build Gameplay HUD Prefab & Scene UI
// PRD Section 7: Assets/_Game/Scripts/Editor/GameplayUIBuilder.cs
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
    public static class GameplayUIBuilder
    {
        private const string PrefabDir          = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath         = "Assets/_Game/Prefabs/UI/Canvas_GameplayHUD.prefab";
        private const string ResourcesDir       = "Assets/Resources/UI";
        private const string ResourcesPrefabPath = "Assets/Resources/UI/Canvas_GameplayHUD.prefab";
        private const string ArtUIDir           = "Assets/_Game/Art/UI";

        private const string GameScenePath      = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Arrow Puzzle/Add HUD To Game Scene", false, 101)]
        public static void AddHUDToGameScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GameScenePath);
            if (Object.FindObjectOfType<GameplayHUD>() == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                    Debug.Log($"[GameplayUIBuilder] Instantiated HUD in {GameScenePath}");
                }
            }

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[GameplayUIBuilder] Created EventSystem in Game scene.");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[GameplayUIBuilder] Saved scene {GameScenePath}");
        }

        [InitializeOnLoadMethod]
        private static void AutoInit()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                BuildHUD();
            };
        }

        [MenuItem("Arrow Puzzle/Build Gameplay HUD Prefab & Scene UI", false, 100)]
        public static void BuildHUD()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // 1. Ensure all UI art textures are imported as Sprites
            FixUITextureImporters();

            // 2. Load sprites
            var iconCoin       = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_coin.png");
            var iconHeartFull  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_heart_full.png");
            var iconHeartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_heart_empty.png");
            var iconBack       = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_back.png");
            var iconGear       = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_gear.png");
            var iconHint       = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_hint.png");
            var iconMagnet     = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_magnet.png");

            var pillCreamSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_pill_cream.png");
            var circleBtnSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_circle.png");
            var cardPurpleSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");
            var badgeFreeSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_badge_free.png");
            var vignetteSprite    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_overlay_vignette.png");

            // Look for custom font in Fonts/ folder or fallback
            TMP_FontAsset customFont = FindCustomTMPFont();

            // 3. Build the Canvas Hierarchy in scene
            var root = new GameObject("Canvas_GameplayHUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            var hud = root.AddComponent<GameplayHUD>();

            // ── Blocked Full-Screen Vignette Overlay (default transparent) ────
            var vignetteGO = CreateUIObject("BlockedVignetteOverlay", root.transform);
            var vigRect = vignetteGO.GetComponent<RectTransform>();
            SetAnchors(vigRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;
            var vigImg = vignetteGO.AddComponent<Image>();
            if (vignetteSprite != null) vigImg.sprite = vignetteSprite;
            vigImg.color = new Color(1f, 1f, 1f, 0f); // Default fully transparent
            vigImg.raycastTarget = false;
            hud.blockedVignetteOverlay = vigImg;

            // ── Safe Area Container (handles notch / home indicator) ──────────
            var safeAreaGO = CreateUIObject("SafeArea", root.transform);
            var safeRect = safeAreaGO.GetComponent<RectTransform>();
            SetAnchors(safeRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeAreaGO.AddComponent<ArrowPuzzle.Systems.SafeAreaFitter>();

            // ── Top-Left: Coin Pill (anchor top-left, offset (24, -24), ~270x84) ──
            var coinPillGO = CreateUIObject("CoinPill", safeAreaGO.transform);
            var coinRect = coinPillGO.GetComponent<RectTransform>();
            SetAnchors(coinRect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            coinRect.anchoredPosition = new Vector2(24, -24);
            coinRect.sizeDelta = new Vector2(270, 84);

            var coinBg = coinPillGO.AddComponent<Image>();
            if (pillCreamSprite != null)
            {
                coinBg.sprite = pillCreamSprite;
                coinBg.type = Image.Type.Sliced;
                coinBg.color = Color.white;
            }
            else
            {
                coinBg.color = new Color(0.99f, 0.97f, 0.93f); // Fallback cream
            }

            var coinBtn = coinPillGO.AddComponent<Button>();
            coinBtn.transition = Selectable.Transition.None;
            var coinPill = coinPillGO.AddComponent<CoinPillUI>();
            coinPillGO.AddComponent<UIButtonScale>();

            // Coin Icon: large round coin icon on left, protruding slightly outside the pill
            var coinIconGO = CreateUIObject("CoinIcon", coinPillGO.transform);
            var coinIconRect = coinIconGO.GetComponent<RectTransform>();
            SetAnchors(coinIconRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f));
            // Centered at x=28: radius 46 -> left edge at 28-46 = -18 (protrudes outside pill)
            coinIconRect.anchoredPosition = new Vector2(28, 0);
            coinIconRect.sizeDelta = new Vector2(92, 92);
            var coinIconImg = coinIconGO.AddComponent<Image>();
            coinIconImg.sprite = iconCoin;
            if (iconCoin == null) coinIconImg.color = new Color(1f, 0.85f, 0.15f);
            coinIconImg.raycastTarget = false;

            // Small green '+' badge at bottom-left of coin icon
            var plusBadgeGO = CreateUIObject("Badge_Plus", coinIconGO.transform);
            var plusRect = plusBadgeGO.GetComponent<RectTransform>();
            SetAnchors(plusRect, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            plusRect.anchoredPosition = new Vector2(14, 14);
            plusRect.sizeDelta = new Vector2(28, 28);
            var plusBg = plusBadgeGO.AddComponent<Image>();
            if (circleBtnSprite != null) plusBg.sprite = circleBtnSprite;
            plusBg.color = new Color(0.13f, 0.77f, 0.37f); // Green #22C55E
            plusBg.raycastTarget = false;

            var plusTextGO = CreateUIObject("Text", plusBadgeGO.transform);
            var plusTextRect = plusTextGO.GetComponent<RectTransform>();
            SetAnchors(plusTextRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            plusTextRect.offsetMin = Vector2.zero;
            plusTextRect.offsetMax = Vector2.zero;
            var plusText = plusTextGO.AddComponent<TextMeshProUGUI>();
            plusText.text = "+";
            plusText.fontSize = 20;
            plusText.fontStyle = FontStyles.Bold;
            plusText.alignment = TextAlignmentOptions.Center;
            plusText.color = Color.white;
            plusText.raycastTarget = false;
            if (customFont != null) plusText.font = customFont;

            // Coin Label: dark brown, bold rounded font
            var coinTextGO = CreateUIObject("CoinText", coinPillGO.transform);
            var coinTextRect = coinTextGO.GetComponent<RectTransform>();
            SetAnchors(coinTextRect, new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0.5f));
            coinTextRect.offsetMin = new Vector2(78, 0);
            coinTextRect.offsetMax = new Vector2(-20, 0);
            var coinText = coinTextGO.AddComponent<TextMeshProUGUI>();
            coinText.text = "0";
            coinText.fontSize = 34;
            coinText.fontStyle = FontStyles.Bold;
            coinText.alignment = TextAlignmentOptions.MidlineRight;
            coinText.color = new Color(0.29f, 0.16f, 0.06f); // Dark rich brown #4A2810
            coinText.raycastTarget = false;
            if (customFont != null) coinText.font = customFont;

            coinPill.coinIcon   = coinIconImg;
            coinPill.coinLabel  = coinText;
            coinPill.coinSprite = iconCoin;
            coinBtn.onClick.AddListener(coinPill.OnPillPressed);

            // ── Top-Center: Heart Pill (anchor top-center, offset (0, -24), ~300x80) ──
            var heartPillGO = CreateUIObject("HeartPill", safeAreaGO.transform);
            var heartRect = heartPillGO.GetComponent<RectTransform>();
            SetAnchors(heartRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            heartRect.anchoredPosition = new Vector2(0, -24);
            heartRect.sizeDelta = new Vector2(300, 80);

            var heartBg = heartPillGO.AddComponent<Image>();
            if (pillCreamSprite != null)
            {
                heartBg.sprite = pillCreamSprite;
                heartBg.type = Image.Type.Sliced;
                heartBg.color = Color.white;
            }
            else
            {
                heartBg.color = new Color(0.99f, 0.97f, 0.93f);
            }

            var heartPill = heartPillGO.AddComponent<HeartPillUI>();
            heartPill.fullHeartSprite  = iconHeartFull;
            heartPill.emptyHeartSprite = iconHeartEmpty;
            heartPill.fullHeartColor   = Color.white;
            heartPill.emptyHeartColor  = new Color(0.85f, 0.77f, 0.65f, 1f); // Pale beige/brownish

            Image[] heartSlots = new Image[3];
            float[] heartXPositions = { -72f, 0f, 72f };
            for (int i = 0; i < 3; i++)
            {
                var hGO = CreateUIObject($"Heart_{i}", heartPillGO.transform);
                var hRect = hGO.GetComponent<RectTransform>();
                SetAnchors(hRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                hRect.anchoredPosition = new Vector2(heartXPositions[i], 0);
                hRect.sizeDelta = new Vector2(52, 52);
                var hImg = hGO.AddComponent<Image>();
                hImg.sprite = iconHeartFull;
                if (iconHeartFull == null) hImg.color = new Color(0.92f, 0.20f, 0.25f);
                hImg.raycastTarget = false;
                heartSlots[i] = hImg;
            }
            heartPill.heartImages = heartSlots;

            // ── Top-Right: Gear Button (anchor top-right, offset (-24, -24), ~90x90) ──
            var gearGO = CreateUIObject("Button_Gear", safeAreaGO.transform);
            var gearRect = gearGO.GetComponent<RectTransform>();
            SetAnchors(gearRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            gearRect.anchoredPosition = new Vector2(-24, -24);
            gearRect.sizeDelta = new Vector2(90, 90);

            var gearBg = gearGO.AddComponent<Image>();
            if (circleBtnSprite != null) gearBg.sprite = circleBtnSprite;
            gearBg.color = new Color(0.13f, 0.77f, 0.37f); // Bright green #22C55E
            var gearBtn = gearGO.AddComponent<Button>();
            gearBtn.transition = Selectable.Transition.None;
            var gearHudBtn = gearGO.AddComponent<HUDButtonUI>();
            gearHudBtn.buttonType = HUDButtonType.Gear;
            gearGO.AddComponent<UIButtonScale>();
            gearBtn.onClick.AddListener(gearHudBtn.OnButtonClicked);
            gearBtn.onClick.AddListener(hud.OnGearButtonPressed);

            var gearIconGO = CreateUIObject("Icon", gearGO.transform);
            var gearIconRect = gearIconGO.GetComponent<RectTransform>();
            SetAnchors(gearIconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            gearIconRect.anchoredPosition = Vector2.zero;
            gearIconRect.sizeDelta = new Vector2(54, 54);
            var gearIconImg = gearIconGO.AddComponent<Image>();
            gearIconImg.sprite = iconGear;
            gearIconImg.color = Color.white;
            gearIconImg.raycastTarget = false;

            // ── Top-Right: Back Button (gear-এর ঠিক বামে, offset (-130, -24), ~90x90, Level 4+) ──
            var backGO = CreateUIObject("Button_Back", safeAreaGO.transform);
            var backRect = backGO.GetComponent<RectTransform>();
            SetAnchors(backRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            backRect.anchoredPosition = new Vector2(-130, -24);
            backRect.sizeDelta = new Vector2(90, 90);

            var backBg = backGO.AddComponent<Image>();
            if (circleBtnSprite != null) backBg.sprite = circleBtnSprite;
            backBg.color = new Color(0.98f, 0.45f, 0.09f); // Orange #F97316
            var backBtn = backGO.AddComponent<Button>();
            backBtn.transition = Selectable.Transition.None;
            var backHudBtn = backGO.AddComponent<HUDButtonUI>();
            backHudBtn.buttonType = HUDButtonType.Back;
            backGO.AddComponent<UIButtonScale>();
            backBtn.onClick.AddListener(backHudBtn.OnButtonClicked);
            backBtn.onClick.AddListener(hud.OnBackButtonPressed);

            var backIconGO = CreateUIObject("Icon", backGO.transform);
            var backIconRect = backIconGO.GetComponent<RectTransform>();
            SetAnchors(backIconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            backIconRect.anchoredPosition = Vector2.zero;
            backIconRect.sizeDelta = new Vector2(50, 50);
            var backIconImg = backIconGO.AddComponent<Image>();
            backIconImg.sprite = iconBack;
            backIconImg.color = Color.white;
            backIconImg.raycastTarget = false;

            // ── Right Power Buttons (anchor right-middle, offset (-24, +90) and (-24, -90), ~140x160) ──
            // Hint Button (Cost 650, Level 4+)
            var hintGO = BuildPowerButton("Button_Hint", safeAreaGO.transform,
                new Vector2(-24, 90), PowerButtonType.Hint, 650, iconHint, iconCoin, cardPurpleSprite, badgeFreeSprite, customFont);

            // Magnet Button (Cost 1300, 20px gap below Hint -> y = 90 - 160 - 20 = -90, Level 6 Heart only)
            var magnetGO = BuildPowerButton("Button_Magnet", safeAreaGO.transform,
                new Vector2(-24, -90), PowerButtonType.Magnet, 1300, iconMagnet, iconCoin, cardPurpleSprite, badgeFreeSprite, customFont);

            // ── Wire into GameplayHUD ───────────────────────────────────────
            hud.coinPill     = coinPill;
            hud.heartPill    = heartPill;
            hud.backButton   = backGO;
            hud.gearButton   = gearGO;
            hud.hintButton   = hintGO.GetComponent<PowerButtonUI>();
            hud.magnetButton = magnetGO.GetComponent<PowerButtonUI>();

            // 4. Save to Prefabs
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[GameplayUIBuilder] Saved HUD Prefab to {PrefabPath}");

            PrefabUtility.SaveAsPrefabAsset(root, ResourcesPrefabPath);
            Debug.Log($"[GameplayUIBuilder] Saved HUD Prefab to {ResourcesPrefabPath}");

            // Ensure EventSystem is present
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[GameplayUIBuilder] Created EventSystem in scene.");
            }

            // If Game scene is currently open, keep it in scene and save. Otherwise clean up the temporary root object.
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == GameScenePath && !EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

                Undo.RegisterCreatedObjectUndo(root, "Create Gameplay HUD");
                Selection.activeGameObject = root;
            }
            else if (activeScene.IsValid() && activeScene.path != GameScenePath)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildPowerButton(string name, Transform parent, Vector2 anchoredPos,
            PowerButtonType type, int cost, Sprite icon, Sprite coinSprite, Sprite cardBg, Sprite badgeBg, TMP_FontAsset font)
        {
            var btnGO = CreateUIObject(name, parent);
            var rect = btnGO.GetComponent<RectTransform>();
            SetAnchors(rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(140, 160);

            var bg = btnGO.AddComponent<Image>();
            if (cardBg != null)
            {
                bg.sprite = cardBg;
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.26f, 0.22f, 0.79f); // Blue-purple #4338CA
            }
            else
            {
                bg.color = new Color(0.26f, 0.22f, 0.79f);
            }

            var btn = btnGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            var powerUI = btnGO.AddComponent<PowerButtonUI>();
            powerUI.buttonType = type;
            powerUI.coinCost = cost;
            powerUI.iconSprite = icon;
            btnGO.AddComponent<UIButtonScale>();
            btn.onClick.AddListener(powerUI.OnButtonClicked);

            // Icon (Bulb or Magnet)
            var iconGO = CreateUIObject("Icon", btnGO.transform);
            var iconRect = iconGO.GetComponent<RectTransform>();
            SetAnchors(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            iconRect.anchoredPosition = new Vector2(0, 22);
            iconRect.sizeDelta = new Vector2(68, 68);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = icon;
            if (icon == null) iconImg.color = type == PowerButtonType.Hint ? Color.yellow : Color.cyan;
            iconImg.raycastTarget = false;
            powerUI.iconImage = iconImg;

            // Cost Container (Small Coin Icon + Number)
            var costContainerGO = CreateUIObject("CostContainer", btnGO.transform);
            var costContainerRect = costContainerGO.GetComponent<RectTransform>();
            SetAnchors(costContainerRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f));
            costContainerRect.anchoredPosition = new Vector2(0, 26);
            costContainerRect.sizeDelta = new Vector2(110, 32);

            // Small Coin Icon
            var costCoinGO = CreateUIObject("CoinIcon", costContainerGO.transform);
            var costCoinRect = costCoinGO.GetComponent<RectTransform>();
            SetAnchors(costCoinRect, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            costCoinRect.anchoredPosition = new Vector2(cost >= 1000 ? 6 : 14, 0);
            costCoinRect.sizeDelta = new Vector2(24, 24);
            var costCoinImg = costCoinGO.AddComponent<Image>();
            costCoinImg.sprite = coinSprite;
            if (coinSprite == null) costCoinImg.color = new Color(1f, 0.85f, 0.15f);
            costCoinImg.raycastTarget = false;

            // Cost Label
            var costGO = CreateUIObject("CostLabel", costContainerGO.transform);
            var costRect = costGO.GetComponent<RectTransform>();
            SetAnchors(costRect, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f));
            costRect.offsetMin = new Vector2(cost >= 1000 ? 36 : 42, 0);
            costRect.offsetMax = Vector2.zero;
            var costText = costGO.AddComponent<TextMeshProUGUI>();
            costText.text = cost.ToString("N0").Replace(",", " ");
            costText.fontSize = 22;
            costText.fontStyle = FontStyles.Bold;
            costText.alignment = TextAlignmentOptions.MidlineLeft;
            costText.color = new Color(1f, 0.91f, 0.52f); // Warm bright gold
            costText.raycastTarget = false;
            if (font != null) costText.font = font;

            powerUI.costLabel = costText;
            powerUI.costCoinIcon = costContainerGO;

            // FREE Badge Tag
            var badgeGO = CreateUIObject("Badge_FREE", btnGO.transform);
            var badgeRect = badgeGO.GetComponent<RectTransform>();
            SetAnchors(badgeRect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f));
            badgeRect.anchoredPosition = new Vector2(0, 26);
            badgeRect.sizeDelta = new Vector2(100, 30);
            var badgeBgImg = badgeGO.AddComponent<Image>();
            if (badgeBg != null)
            {
                badgeBgImg.sprite = badgeBg;
                badgeBgImg.type = Image.Type.Sliced;
                badgeBgImg.color = Color.white;
            }
            else
            {
                badgeBgImg.color = new Color(0.13f, 0.77f, 0.37f); // Green #22C55E
            }
            badgeBgImg.raycastTarget = false;

            var badgeTextGO = CreateUIObject("Text", badgeGO.transform);
            var badgeTextRect = badgeTextGO.GetComponent<RectTransform>();
            SetAnchors(badgeTextRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;
            var badgeText = badgeTextGO.AddComponent<TextMeshProUGUI>();
            badgeText.text = "FREE";
            badgeText.fontSize = 18;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.white;
            badgeText.raycastTarget = false;
            if (font != null) badgeText.font = font;

            powerUI.freeBadge = badgeGO;
            badgeGO.SetActive(false); // default off

            return btnGO;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        private static void FixUITextureImporters()
        {
            if (!Directory.Exists(ArtUIDir)) return;
            string[] files = Directory.GetFiles(ArtUIDir, "*.png");
            bool changed = false;

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
                        Debug.Log($"[GameplayUIBuilder] Converted {path} to Sprite.");
                    }

                    // Configure 9-slice borders if needed
                    if (path.EndsWith("ui_pill_cream.png") && importer.spriteBorder != new Vector4(64, 64, 64, 64))
                    {
                        importer.spriteBorder = new Vector4(64, 64, 64, 64);
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        changed = true;
                    }
                    else if (path.EndsWith("ui_button_rounded_square.png") && importer.spriteBorder != new Vector4(54, 54, 54, 54))
                    {
                        importer.spriteBorder = new Vector4(54, 54, 54, 54);
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        changed = true;
                    }
                    else if (path.EndsWith("ui_badge_free.png") && importer.spriteBorder != new Vector4(40, 30, 40, 30))
                    {
                        importer.spriteBorder = new Vector4(40, 30, 40, 30);
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        changed = true;
                    }
                }
            }
            if (changed) AssetDatabase.Refresh();
        }

        private static TMP_FontAsset FindCustomTMPFont()
        {
            // Search in Assets/_Game/Fonts/ or anywhere in Assets
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
    }
}
#endif
