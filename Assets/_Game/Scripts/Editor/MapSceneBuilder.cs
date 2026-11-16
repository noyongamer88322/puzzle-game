// ============================================================
// MapSceneBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Builds the Map (Home) scene UI matching cartoon mobile game style:
//   • Canvas (1080×1920, Match 0.5)
//   • Painted background (bg_map_hills.png or painted gradient)
//   • Img_FadeOverlay (light-blue fullscreen for cross-fade)
//   • SafeArea:
//       ├─ Group_TopLeft: Pill_Coin (cream pill, golden border, coin icon + count)
//       ├─ Btn_Gear: Green gear button (opens Settings popup)
//       ├─ Group_ChapterPill: (~66% height, blue, 'Hills', progress bar + star x/5)
//       ├─ Btn_Level: Big green 'Level N' button with breathing pulse
//       └─ Btn_Gallery: Bottom-centre small gallery icon button
//   • Popups:
//       ├─ Panel_Settings (SettingsPanelUI)
//       └─ Panel_Gallery  (GalleryPanelUI)
//   • Hand Pointer Tutorial hook
//   • NO Ads or Shop!
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Editor
{
    public static class MapSceneBuilder
    {
        private const string MapScenePath  = "Assets/_Game/Scenes/Map.unity";
        private const string ArtUIDir      = "Assets/_Game/Art/UI";
        private const string ArtBgDir      = "Assets/_Game/Art/Backgrounds";

        [MenuItem("Arrow Puzzle/Build Map Scene UI", false, 200)]
        public static void BuildMapUI()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[MapSceneBuilder] Skipping build – cannot run EditorSceneManager during Play Mode.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

            // Destroy old canvases
            var existingCanvases = Object.FindObjectsOfType<Canvas>();
            foreach (var c in existingCanvases)
            {
                Object.DestroyImmediate(c.gameObject);
            }

            EnsureSpriteImportSettings(ArtUIDir);

            // ── Root Canvas ───────────────────────────────────────
            var canvasGO = new GameObject("Canvas_Map");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight   = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ── Background ────────────────────────────────────────
            var bgGO  = CreateImage(canvasGO.transform, "Img_Background", Color.white);
            Stretch(bgGO.GetComponent<RectTransform>());
            var bgImg = bgGO.GetComponent<Image>();
            var hillsSprite = LoadSprite(ArtBgDir, "bg_map_hills");
            if (hillsSprite != null)
            {
                bgImg.sprite = hillsSprite;
                bgImg.color  = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.46f, 0.72f, 0.44f); // Green painted look
            }

            // ── Fade Overlay (light blue for cross-fade) ──────────
            var fadeGO  = CreateImage(canvasGO.transform, "Img_FadeOverlay",
                                      new Color(0.1843f, 0.7059f, 0.9098f, 0f));
            Stretch(fadeGO.GetComponent<RectTransform>());
            var fadeImg = fadeGO.GetComponent<Image>();
            fadeImg.raycastTarget = false;

            // ── SafeArea container ────────────────────────────────
            var safeGO   = new GameObject("SafeArea");
            var safeRect = safeGO.AddComponent<RectTransform>();
            safeGO.transform.SetParent(canvasGO.transform, false);
            Stretch(safeRect);
            safeGO.AddComponent<ArrowPuzzle.Systems.SafeAreaFitter>();

            // ── Top-Left: Coin Pill ───────────────────────────────
            var coinPillGO = BuildCoinPill(safeRect);

            // ── Top-Right: Green Gear button ──────────────────────
            var gearGO = BuildGearButton(safeRect);

            // ── Chapter Pill (~66% height = anchored ~ y = 0.35) ──
            var chapterPillGO = BuildChapterPill(safeRect);

            // ── Big Green Level Button (below chapter pill) ───────
            var levelBtnGO = BuildLevelButton(safeRect);

            // ── Bottom-Centre: Small Gallery Button ───────────────
            var galleryBtnGO = BuildGalleryButton(safeRect);

            // ── Hand Pointer (Tutorial) ───────────────────────────
            var handGO   = new GameObject("Ptr_Hand");
            var handRect = handGO.AddComponent<RectTransform>();
            handGO.transform.SetParent(canvasGO.transform, false);
            handRect.sizeDelta        = new Vector2(90f, 90f);
            handRect.anchorMin        = new Vector2(0.5f, 0.5f);
            handRect.anchorMax        = new Vector2(0.5f, 0.5f);
            handRect.pivot            = new Vector2(0.3f, 0.9f);
            handRect.anchoredPosition = new Vector2(50f, -30f);
            var handImg = handGO.AddComponent<Image>();
            handImg.sprite = LoadSprite(ArtUIDir, "hand_pointer");
            handImg.raycastTarget = false;
            handGO.SetActive(false);

            // ── Popups: Panel_Settings and Panel_Gallery ──────────
            var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Panel_Settings.prefab");
            GameObject settingsGO = null;
            if (settingsPrefab != null)
            {
                settingsGO = PrefabUtility.InstantiatePrefab(settingsPrefab, canvasGO.transform) as GameObject;
            }
            else
            {
                settingsGO = SettingsPanelBuilder.Build();
                settingsGO.transform.SetParent(canvasGO.transform, false);
            }
            settingsGO.name = "Panel_Settings";
            settingsGO.SetActive(false);

            var galleryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Panel_Gallery.prefab");
            GameObject galleryGO = null;
            if (galleryPrefab != null)
            {
                galleryGO = PrefabUtility.InstantiatePrefab(galleryPrefab, canvasGO.transform) as GameObject;
            }
            else
            {
                galleryGO = GalleryPanelBuilder.BuildGalleryPanel();
                galleryGO.transform.SetParent(canvasGO.transform, false);
            }
            galleryGO.name = "Panel_Gallery";
            galleryGO.SetActive(false);

            // ── Wire up MapSceneController ────────────────────────
            var ctrl = canvasGO.AddComponent<MapSceneController>();
            ctrl.backgroundImage   = bgImg;
            ctrl.bg_HillsSprite    = hillsSprite;
            ctrl.fadeOverlay       = fadeImg;
            ctrl.rootCanvas        = canvas;
            ctrl.handPointer       = handRect;

            ctrl.coinPill          = coinPillGO.GetComponent<CoinPillUI>();
            ctrl.gearButton        = gearGO.GetComponent<Button>();
            ctrl.galleryButton     = galleryBtnGO.GetComponent<Button>();
            ctrl.galleryPanel      = galleryGO.GetComponent<GalleryPanelUI>();
            ctrl.settingsPanel     = settingsGO.GetComponent<SettingsPanelUI>();

            ctrl.levelButton       = levelBtnGO.GetComponent<Button>();
            ctrl.levelButtonLabel  = levelBtnGO.GetComponentInChildren<TextMeshProUGUI>();
            ctrl.levelButtonRect   = levelBtnGO.GetComponent<RectTransform>();

            // Chapter pill children
            ctrl.chapterNameLabel   = chapterPillGO.transform.Find("Lbl_ChapterName")?.GetComponent<TextMeshProUGUI>();
            ctrl.chapterProgressFill = chapterPillGO.transform.Find("Bar_Progress/Fill")?.GetComponent<Image>();
            ctrl.chapterStarLabel   = chapterPillGO.transform.Find("Row_Stars/Lbl_StarCount")?.GetComponent<TextMeshProUGUI>();
            ctrl.starFilledSprite   = LoadSprite(ArtUIDir, "icon_star");

            var starsRow = chapterPillGO.transform.Find("Row_Stars");
            if (starsRow != null)
            {
                var imgs = new System.Collections.Generic.List<Image>();
                for (int i = 0; i < 5; i++)
                {
                    var t = starsRow.Find($"Star_{i + 1}");
                    if (t != null) imgs.Add(t.GetComponent<Image>());
                }
                ctrl.chapterStarImages = imgs.ToArray();
            }

            // Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MapSceneBuilder] Map scene UI built successfully.");
        }

        // ── 1. Coin Pill ──────────────────────────────────────────────
        private static GameObject BuildCoinPill(Transform parent)
        {
            var pillCreamSprite = LoadSprite(ArtUIDir, "ui_pill_cream");
            var iconCoin        = LoadSprite(ArtUIDir, "icon_coin");

            var pillGO   = new GameObject("Pill_Coin");
            var pillRect = pillGO.AddComponent<RectTransform>();
            pillGO.transform.SetParent(parent, false);
            pillRect.anchorMin = new Vector2(0f, 1f);
            pillRect.anchorMax = new Vector2(0f, 1f);
            pillRect.pivot     = new Vector2(0f, 1f);
            pillRect.anchoredPosition = new Vector2(28f, -28f);
            pillRect.sizeDelta = new Vector2(260f, 80f);

            var pillImg = pillGO.AddComponent<Image>();
            if (pillCreamSprite != null)
            {
                pillImg.sprite = pillCreamSprite;
                pillImg.type = Image.Type.Sliced;
                pillImg.color = Color.white;
            }
            else
            {
                pillImg.color = new Color(0.99f, 0.97f, 0.93f);
            }

            var pillBtn = pillGO.AddComponent<Button>();
            pillBtn.transition = Selectable.Transition.None;
            pillGO.AddComponent<UIButtonScale>();

            // Coin Icon on left
            var iconGO = CreateImage(pillGO.transform, "Img_CoinIcon", Color.white);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(28f, 0f);
            iconRect.sizeDelta = new Vector2(88f, 88f);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = iconCoin;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Coin Count Label
            var lblGO = new GameObject("Lbl_CoinCount");
            lblGO.transform.SetParent(pillGO.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(1f, 1f);
            lblRect.offsetMin = new Vector2(76f, 0f);
            lblRect.offsetMax = new Vector2(-20f, 0f);

            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "0";
            tmp.fontSize = 34f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.28f, 0.16f, 0.06f); // Dark rich brown
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.raycastTarget = false;

            var cpu = pillGO.AddComponent<CoinPillUI>();
            cpu.coinIcon  = iconImg;
            cpu.coinLabel = tmp;
            cpu.coinSprite = iconCoin;
            pillBtn.onClick.AddListener(cpu.OnPillPressed);

            return pillGO;
        }

        // ── 2. Green Gear Button (Top-Right) ──────────────────────────
        private static GameObject BuildGearButton(Transform parent)
        {
            var circleSprite = LoadSprite(ArtUIDir, "ui_button_circle");
            var iconGear     = LoadSprite(ArtUIDir, "icon_gear");

            var gearGO = new GameObject("Btn_Gear");
            var gearRT = gearGO.AddComponent<RectTransform>();
            gearGO.transform.SetParent(parent, false);
            gearRT.anchorMin = gearRT.anchorMax = gearRT.pivot = new Vector2(1f, 1f);
            gearRT.anchoredPosition = new Vector2(-28f, -28f);
            gearRT.sizeDelta = new Vector2(88f, 88f);

            var gearImg = gearGO.AddComponent<Image>();
            if (circleSprite != null) gearImg.sprite = circleSprite;
            gearImg.color = new Color(0.13f, 0.77f, 0.37f); // Vibrant green #22C55E

            var gearBtn = gearGO.AddComponent<Button>();
            gearBtn.transition = Selectable.Transition.None;
            gearGO.AddComponent<UIButtonScale>();

            var iconGO = CreateImage(gearGO.transform, "Icon", Color.white);
            var iconRT = iconGO.GetComponent<RectTransform>();
            UIHelper.Stretch(iconRT);
            iconRT.offsetMin = new Vector2(16f, 16f);
            iconRT.offsetMax = new Vector2(-16f, -16f);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = iconGear;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            return gearGO;
        }

        // ── 3. Chapter Pill (Mid-Bottom, ~66% height) ─────────────────
        private static GameObject BuildChapterPill(Transform parent)
        {
            var cardSprite = LoadSprite(ArtUIDir, "ui_button_rounded_square");

            var pillGO   = new GameObject("Group_ChapterPill");
            var pillRect = pillGO.AddComponent<RectTransform>();
            pillGO.transform.SetParent(parent, false);
            // ~66% height from top = anchored at y = 0.35 from bottom
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(0.5f, 0.34f);
            pillRect.anchoredPosition = new Vector2(0f, 60f);
            pillRect.sizeDelta        = new Vector2(560f, 160f);

            var pillImg = pillGO.AddComponent<Image>();
            if (cardSprite != null)
            {
                pillImg.sprite = cardSprite;
                pillImg.type = Image.Type.Sliced;
            }
            pillImg.color = new Color(0.12f, 0.32f, 0.78f, 0.95f); // Rich cartoon blue

            var vg = pillGO.AddComponent<VerticalLayoutGroup>();
            vg.padding   = new RectOffset(24, 24, 14, 14);
            vg.spacing   = 10f;
            vg.childAlignment         = TextAnchor.UpperCenter;
            vg.childControlWidth      = true;
            vg.childControlHeight     = false;
            vg.childForceExpandWidth  = true;
            vg.childForceExpandHeight = false;

            // Chapter Name
            var nameGO = new GameObject("Lbl_ChapterName");
            nameGO.transform.SetParent(pillGO.transform, false);
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 40f;
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text      = "Hills";
            nameTMP.fontSize  = 34f;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.color     = Color.white;
            nameTMP.alignment = TextAlignmentOptions.Center;

            // Progress Bar
            var barContGO   = new GameObject("Bar_Progress");
            barContGO.transform.SetParent(pillGO.transform, false);
            var barLE = barContGO.AddComponent<LayoutElement>();
            barLE.preferredHeight = 22f;

            var barBg = barContGO.AddComponent<Image>();
            if (cardSprite != null)
            {
                barBg.sprite = cardSprite;
                barBg.type = Image.Type.Sliced;
            }
            barBg.color = new Color(0.08f, 0.20f, 0.52f, 0.85f); // Dark inner blue

            var fillGO   = new GameObject("Fill");
            fillGO.transform.SetParent(barContGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            UIHelper.Stretch(fillRect);
            var fillImg  = fillGO.AddComponent<Image>();
            if (cardSprite != null)
            {
                fillImg.sprite = cardSprite;
                fillImg.type = Image.Type.Sliced;
            }
            fillImg.color      = new Color(0.18f, 0.88f, 0.42f); // Vibrant green fill
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.4f;

            // Stars Row
            var starsRowGO   = new GameObject("Row_Stars");
            starsRowGO.transform.SetParent(pillGO.transform, false);
            var starsLE = starsRowGO.AddComponent<LayoutElement>();
            starsLE.preferredHeight = 36f;

            var hg = starsRowGO.AddComponent<HorizontalLayoutGroup>();
            hg.spacing  = 10f;
            hg.childAlignment = TextAnchor.MiddleCenter;
            hg.childControlWidth    = false;
            hg.childControlHeight   = false;
            hg.childForceExpandWidth  = false;
            hg.childForceExpandHeight = false;

            Sprite starSprite = LoadSprite(ArtUIDir, "icon_star");
            for (int i = 1; i <= 5; i++)
            {
                var sGO   = new GameObject($"Star_{i}");
                sGO.transform.SetParent(starsRowGO.transform, false);
                var sRect = sGO.AddComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(32f, 32f);
                var sImg  = sGO.AddComponent<Image>();
                sImg.sprite        = starSprite;
                sImg.color         = new Color(1f, 0.85f, 0.15f);
                sImg.preserveAspect = true;
                sImg.raycastTarget = false;
            }

            var cntGO = new GameObject("Lbl_StarCount");
            cntGO.transform.SetParent(starsRowGO.transform, false);
            var cntTMP = cntGO.AddComponent<TextMeshProUGUI>();
            cntTMP.text      = "2/5";
            cntTMP.fontSize  = 26f;
            cntTMP.fontStyle = FontStyles.Bold;
            cntTMP.color     = new Color(1f, 0.90f, 0.20f);
            cntTMP.alignment = TextAlignmentOptions.MidlineLeft;

            return pillGO;
        }

        // ── 4. Big Green 'Level N' Button ─────────────────────────────
        private static GameObject BuildLevelButton(Transform parent)
        {
            var roundedSprite = LoadSprite(ArtUIDir, "ui_button_rounded_square");

            var btnGO   = new GameObject("Btn_Level");
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnGO.transform.SetParent(parent, false);
            btnRect.anchorMin = btnRect.anchorMax = btnRect.pivot = new Vector2(0.5f, 0.34f);
            btnRect.anchoredPosition = new Vector2(0f, -80f);
            btnRect.sizeDelta        = new Vector2(440f, 110f);

            var btnImg = btnGO.AddComponent<Image>();
            if (roundedSprite != null)
            {
                btnImg.sprite = roundedSprite;
                btnImg.type   = Image.Type.Sliced;
            }
            btnImg.color = new Color(0.13f, 0.77f, 0.37f); // Vibrant green #22C55E

            var btn = btnGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btnGO.AddComponent<UIButtonScale>();

            var lblGO = new GameObject("Lbl_Level");
            lblGO.transform.SetParent(btnGO.transform, false);
            UIHelper.Stretch(lblGO.AddComponent<RectTransform>());
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Level 1";
            tmp.fontSize  = 48f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btnGO;
        }

        // ── 5. Small Gallery Button (Bottom-Centre) ───────────────────
        private static GameObject BuildGalleryButton(Transform parent)
        {
            var circleSprite = LoadSprite(ArtUIDir, "ui_button_circle");
            var iconGallery  = LoadSprite(ArtUIDir, "icon_gallery");

            var galGO = new GameObject("Btn_Gallery");
            var galRT = galGO.AddComponent<RectTransform>();
            galGO.transform.SetParent(parent, false);
            galRT.anchorMin = galRT.anchorMax = galRT.pivot = new Vector2(0.5f, 0f);
            galRT.anchoredPosition = new Vector2(0f, 60f);
            galRT.sizeDelta        = new Vector2(84f, 84f);

            var galImg = galGO.AddComponent<Image>();
            if (circleSprite != null) galImg.sprite = circleSprite;
            galImg.color = new Color(0.98f, 0.58f, 0.06f); // Warm cartoon gold-orange

            var galBtn = galGO.AddComponent<Button>();
            galBtn.transition = Selectable.Transition.None;
            galGO.AddComponent<UIButtonScale>();

            var iconGO = CreateImage(galGO.transform, "Icon", Color.white);
            var iconRT = iconGO.GetComponent<RectTransform>();
            UIHelper.Stretch(iconRT);
            iconRT.offsetMin = new Vector2(16f, 16f);
            iconRT.offsetMax = new Vector2(-16f, -16f);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = iconGallery;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            return galGO;
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin  = Vector2.zero;
            rect.anchorMax  = Vector2.one;
            rect.offsetMin  = Vector2.zero;
            rect.offsetMax  = Vector2.zero;
        }

        private static Sprite LoadSprite(string dir, string baseName)
        {
            foreach (var ext in new[] { ".png", ".jpg" })
            {
                string path = $"{dir}/{baseName}{ext}";
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
            }
            return null;
        }

        private static void EnsureSpriteImportSettings(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.GetFiles(dir, "*.png"))
            {
                string assetPath = file.Replace("\\", "/");
                int idx = assetPath.IndexOf("Assets/");
                if (idx < 0) continue;
                assetPath = assetPath.Substring(idx);

                var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (imp == null || imp.textureType == TextureImporterType.Sprite) continue;
                imp.textureType      = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.SaveAndReimport();
            }
        }
    }
}
#endif
