// ============================================================
// ShopPanelBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Programmatically builds the Shop pop-up panel:
//
// Hierarchy added under Canvas:
//   Panel_Shop (ShopPanelUI, starts inactive)
//   ├── Img_Dim          (full-screen dark overlay, tap to close)
//   └── Panel_Card       (dark indigo rounded card, scale-in overshoot)
//       ├── Header
//       │   ├── Lbl_Title    ("SHOP", golden bold)
//       │   └── Btn_Close    (Red X close button, top-right)
//       └── Scroll_Cards (Horizontal ScrollRect)
//           └── Viewport (RectMask2D)
//               └── Content (HorizontalLayoutGroup, 6 cards)
//                   ├── Card_0_DisableAds ($3.99, +1000 Coins)
//                   ├── Card_1_Coins1500  ($2.99, Best Seller badge)
//                   ├── Card_2_Coins6250  ($9.99)
//                   ├── Card_3_Coins13500 ($19.99)
//                   ├── Card_4_Coins25000 ($29.99)
//                   └── Card_5_Coins100000 ($99.99, Save badge)
//
// Menu: Arrow Puzzle → Build Shop Panel (Map & Game)
// PRD Section 3.9 & 7: Assets/_Game/Scripts/Editor/ShopPanelBuilder.cs
// ============================================================

#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.UI;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Editor
{
    public static class ShopPanelBuilder
    {
        private const string ArtUIDir       = "Assets/_Game/Art/UI";
        private const string PrefabDir      = "Assets/_Game/Prefabs/UI";
        private const string ResourcesDir   = "Assets/Resources/UI";
        private const string CardPrefabPath = PrefabDir + "/Pfb_ShopCard.prefab";
        private const string ShopPrefabPath = PrefabDir + "/Panel_Shop.prefab";
        private const string ResourcesPrefabPath = ResourcesDir + "/Panel_Shop.prefab";
        private const string MapScenePath   = "Assets/_Game/Scenes/Map.unity";
        private const string GameScenePath  = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Arrow Puzzle/Build Shop Panel (Map & Game)", false, 210)]
        public static void BuildShopPanelAll()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[ShopPanelBuilder] Skipping build – cannot run EditorSceneManager during Play Mode.");
                return;
            }
            // 1. Generate & import textures if needed
            GenerateShopTextures();

            // 2. Build Card Prefab
            var cardPrefab = BuildOrGetCardPrefab();

            // 3. Build Panel Prefab
            var shopPrefab = BuildShopPanelPrefab(cardPrefab);

            // 4. Attach to Game Scene
            AttachShopToScene(GameScenePath, shopPrefab);

            // 5. Attach to Map Scene
            AttachShopToScene(MapScenePath, shopPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShopPanelBuilder] Shop Panel successfully built and wired into Game and Map scenes!");
        }

        private static void AttachShopToScene(string scenePath, GameObject shopPrefab)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning($"[ShopPanelBuilder] No Canvas found in {scenePath}.");
                return;
            }

            // Remove existing Panel_Shop if present
            var existing = canvas.transform.Find("Panel_Shop");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
                Debug.Log($"[ShopPanelBuilder] Removed existing Panel_Shop from {scenePath}.");
            }

            // Instantiate prefab under canvas
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, canvas.transform);
            instance.name = "Panel_Shop";
            instance.SetActive(false);

            var shopUI = instance.GetComponent<ShopPanelUI>();

            // Wire to GameplayHUD if in Game scene
            var hud = Object.FindFirstObjectByType<GameplayHUD>();
            if (hud != null)
            {
                hud.shopPanel = shopUI;
                EditorUtility.SetDirty(hud);
                Debug.Log($"[ShopPanelBuilder] Linked ShopPanelUI to GameplayHUD in {scenePath}.");
            }

            // Wire to MapSceneController if in Map scene
            var mapCtrl = Object.FindFirstObjectByType<MapSceneController>();
            if (mapCtrl != null)
            {
                mapCtrl.shopPanel = shopUI;
                EditorUtility.SetDirty(mapCtrl);
                Debug.Log($"[ShopPanelBuilder] Linked ShopPanelUI to MapSceneController in {scenePath}.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static GameObject BuildShopPanelPrefab(ShopCardUI cardPrefab)
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            // Root Holder
            var rootGO = new GameObject("Panel_Shop", typeof(RectTransform));
            var rootRect = rootGO.GetComponent<RectTransform>();
            Stretch(rootRect);

            // Dim Overlay
            var dimGO = CreateImage(rootGO.transform, "Img_Dim", new Color(0f, 0f, 0f, 0.72f));
            Stretch(dimGO.GetComponent<RectTransform>());
            var dimImg = dimGO.GetComponent<Image>();
            dimImg.raycastTarget = true;
            var dimBtn = dimGO.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;

            // Central Card Panel
            var cardGO = new GameObject("Panel_Card", typeof(RectTransform));
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardGO.transform.SetParent(rootGO.transform, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(1000f, 700f);

            var cardImg = cardGO.AddComponent<Image>();
            cardImg.color = new Color(0.13f, 0.08f, 0.28f, 0.98f); // Deep dark purple panel
            var cardCG = cardGO.AddComponent<CanvasGroup>();

            // Header Container
            var headerGO = new GameObject("Header", typeof(RectTransform));
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerGO.transform.SetParent(cardGO.transform, false);
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 90f);

            // Title Label ("SHOP")
            var titleGO = new GameObject("Lbl_Title", typeof(RectTransform));
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleGO.transform.SetParent(headerGO.transform, false);
            Stretch(titleRect);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "SHOP";
            titleTMP.fontSize = 54f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.85f, 0.15f); // Gold #FFD700
            titleTMP.raycastTarget = false;

            // Red X Close Button (Top-Right)
            var closeGO = new GameObject("Btn_Close", typeof(RectTransform));
            var closeRect = closeGO.GetComponent<RectTransform>();
            closeGO.transform.SetParent(headerGO.transform, false);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);
            closeRect.sizeDelta = new Vector2(62f, 62f);

            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = new Color(0.92f, 0.22f, 0.22f); // Vibrant red #EF4444
            var closeBtn = closeGO.AddComponent<Button>();

            var closeLblGO = new GameObject("Lbl_X", typeof(RectTransform));
            var closeLblRect = closeLblGO.AddComponent<RectTransform>();
            closeLblGO.transform.SetParent(closeGO.transform, false);
            Stretch(closeLblRect);
            var closeTMP = closeLblGO.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 36f;
            closeTMP.fontStyle = FontStyles.Bold;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.color = Color.white;
            closeTMP.raycastTarget = false;

            // Scroll Area
            var scrollGO = new GameObject("Scroll_Shop", typeof(RectTransform));
            var scrollRect = scrollGO.GetComponent<RectTransform>();
            scrollGO.transform.SetParent(cardGO.transform, false);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(16f, 20f);
            scrollRect.offsetMax = new Vector2(-16f, -94f);

            var scrollComp = scrollGO.AddComponent<ScrollRect>();
            scrollComp.horizontal = true;
            scrollComp.vertical = false;
            scrollComp.movementType = ScrollRect.MovementType.Clamped;
            scrollComp.scrollSensitivity = 25f;

            // Viewport
            var vpGO = new GameObject("Viewport", typeof(RectTransform));
            var vpRect = vpGO.AddComponent<RectTransform>();
            vpGO.transform.SetParent(scrollGO.transform, false);
            Stretch(vpRect);
            vpGO.AddComponent<RectMask2D>();

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform));
            var contentRect = contentGO.GetComponent<RectTransform>();
            contentGO.transform.SetParent(vpGO.transform, false);
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.spacing = 18f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollComp.viewport = vpRect;
            scrollComp.content = contentRect;

            // Load Sprites
            var spNoAds = LoadSprite(ArtUIDir, "shop_noads");
            var spPack1 = LoadSprite(ArtUIDir, "shop_pack_1");
            var spPack2 = LoadSprite(ArtUIDir, "shop_pack_2");
            var spPack3 = LoadSprite(ArtUIDir, "shop_pack_3");
            var spPack4 = LoadSprite(ArtUIDir, "shop_pack_4");
            var spPack5 = LoadSprite(ArtUIDir, "shop_pack_5");

            // Instantiate Cards in Exact PRD Order:
            // 1. Disable Ads ($3.99, সাথে 1000 coin বোনাস)
            // 2. 1500 coin ($2.99, Best Seller badge)
            // 3. 6250 ($9.99)
            // 4. 13500 ($19.99)
            // 5. 25000 ($29.99)
            // 6. 100000 ($99.99, Save badge)
            var cardList = new List<ShopCardUI>();

            cardList.Add(CreateCard(contentGO.transform, cardPrefab, ShopProductIds.DisableAds,
                "Disable Ads", "$3.99", spNoAds, null, true, 1000, "+1 000 Coins"));

            cardList.Add(CreateCard(contentGO.transform, cardPrefab, ShopProductIds.Coins1500,
                "1 500 Coins", "$2.99", spPack1, "BEST SELLER", false, 1500));

            cardList.Add(CreateCard(contentGO.transform, cardPrefab, ShopProductIds.Coins6250,
                "6 250 Coins", "$9.99", spPack2, null, false, 6250));

            cardList.Add(CreateCard(contentGO.transform, cardPrefab, ShopProductIds.Coins13500,
                "13 500 Coins", "$19.99", spPack3, null, false, 13500));

            cardList.Add(CreateCard(contentGO.transform, cardPrefab, ShopProductIds.Coins25000,
                "25 000 Coins", "$29.99", spPack4, null, false, 25000));

            cardList.Add(CreateCard(contentGO.transform, cardPrefab, ShopProductIds.Coins100000,
                "100 000 Coins", "$99.99", spPack5, "SAVE 50%", false, 100000));

            // ShopPanelUI Component
            var shopUI = rootGO.AddComponent<ShopPanelUI>();
            shopUI.panelCard = cardRect;
            shopUI.dimOverlay = dimImg;
            shopUI.panelCanvasGroup = cardCG;
            shopUI.closeButton = closeBtn;
            shopUI.titleLabel = titleTMP;
            shopUI.scrollRect = scrollComp;
            shopUI.cardsContainer = contentRect;
            shopUI.cards = cardList;

            shopUI.iconNoAds = spNoAds;
            shopUI.iconPack1 = spPack1;
            shopUI.iconPack2 = spPack2;
            shopUI.iconPack3 = spPack3;
            shopUI.iconPack4 = spPack4;
            shopUI.iconPack5 = spPack5;

            // Save Prefab
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ResourcesPrefabPath);

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootGO, ShopPrefabPath);
            Object.DestroyImmediate(rootGO);

            Debug.Log($"[ShopPanelBuilder] Saved Shop Panel Prefab to {ShopPrefabPath} and {ResourcesPrefabPath}");
            return prefabAsset;
        }

        private static ShopCardUI CreateCard(Transform parent, ShopCardUI cardPrefab, string id, string title,
            string price, Sprite icon, string badge, bool disableAds, int coins, string sub = null)
        {
            var cardInst = Object.Instantiate(cardPrefab, parent);
            cardInst.name = $"Card_{id}";
            cardInst.Setup(id, title, price, icon, badge, disableAds, coins, sub);
            return cardInst;
        }

        public static ShopCardUI BuildOrGetCardPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShopCardUI>(CardPrefabPath);
            if (existing != null) return existing;

            var cardGO = new GameObject("Pfb_ShopCard", typeof(RectTransform));
            var cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(250f, 530f);

            var bgImg = cardGO.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.12f, 0.38f, 0.95f); // Medium purple card background

            // Top Badge
            var badgeGO = new GameObject("Badge", typeof(RectTransform));
            var badgeRect = badgeGO.GetComponent<RectTransform>();
            badgeGO.transform.SetParent(cardGO.transform, false);
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -8f);
            badgeRect.sizeDelta = new Vector2(180f, 32f);

            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.color = new Color(0.96f, 0.58f, 0.12f); // Orange badge #F59E0B

            var badgeLblGO = new GameObject("Lbl_Badge", typeof(RectTransform));
            var badgeLblRect = badgeLblGO.GetComponent<RectTransform>();
            badgeLblGO.transform.SetParent(badgeGO.transform, false);
            Stretch(badgeLblRect);
            var badgeTMP = badgeLblGO.AddComponent<TextMeshProUGUI>();
            badgeTMP.text = "BEST SELLER";
            badgeTMP.fontSize = 18f;
            badgeTMP.fontStyle = FontStyles.Bold;
            badgeTMP.alignment = TextAlignmentOptions.Center;
            badgeTMP.color = Color.white;
            badgeTMP.raycastTarget = false;

            // Title
            var titleGO = new GameObject("Lbl_Title", typeof(RectTransform));
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleGO.transform.SetParent(cardGO.transform, false);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -48f);
            titleRect.sizeDelta = new Vector2(230f, 44f);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "1 500 Coins";
            titleTMP.fontSize = 26f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;
            titleTMP.raycastTarget = false;

            // Subtitle
            var subGO = new GameObject("Lbl_Subtitle", typeof(RectTransform));
            var subRect = subGO.GetComponent<RectTransform>();
            subGO.transform.SetParent(cardGO.transform, false);
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -92f);
            subRect.sizeDelta = new Vector2(230f, 26f);
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "+1 000 Coins";
            subTMP.fontSize = 18f;
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = new Color(0.35f, 0.90f, 0.50f); // Light green #4ADE80
            subTMP.raycastTarget = false;
            subGO.SetActive(false); // default off unless set

            // Artwork Icon Image
            var imgGO = new GameObject("Img_Item", typeof(RectTransform));
            var imgRect = imgGO.GetComponent<RectTransform>();
            imgGO.transform.SetParent(cardGO.transform, false);
            imgRect.anchorMin = new Vector2(0.5f, 0.5f);
            imgRect.anchorMax = new Vector2(0.5f, 0.5f);
            imgRect.pivot = new Vector2(0.5f, 0.5f);
            imgRect.anchoredPosition = new Vector2(0f, 20f);
            imgRect.sizeDelta = new Vector2(170f, 170f);

            var itemImg = imgGO.AddComponent<Image>();
            itemImg.preserveAspect = true;
            itemImg.raycastTarget = false;

            // Green Price Button
            var btnGO = new GameObject("Btn_Price", typeof(RectTransform));
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnGO.transform.SetParent(cardGO.transform, false);
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, 24f);
            btnRect.sizeDelta = new Vector2(204f, 68f);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.13f, 0.77f, 0.37f); // Vibrant game green #22C55E
            var btnComp = btnGO.AddComponent<Button>();
            btnComp.transition = Selectable.Transition.None;

            var priceLblGO = new GameObject("Lbl_Price", typeof(RectTransform));
            var priceLblRect = priceLblGO.GetComponent<RectTransform>();
            priceLblGO.transform.SetParent(btnGO.transform, false);
            Stretch(priceLblRect);
            var priceTMP = priceLblGO.AddComponent<TextMeshProUGUI>();
            priceTMP.text = "$2.99";
            priceTMP.fontSize = 28f;
            priceTMP.fontStyle = FontStyles.Bold;
            priceTMP.alignment = TextAlignmentOptions.Center;
            priceTMP.color = Color.white;
            priceTMP.raycastTarget = false;

            // ShopCardUI Component
            var cardUI = cardGO.AddComponent<ShopCardUI>();
            cardUI.titleLabel = titleTMP;
            cardUI.subtitleLabel = subTMP;
            cardUI.itemImage = itemImg;
            cardUI.priceButton = btnComp;
            cardUI.priceLabel = priceTMP;
            cardUI.badgeRoot = badgeGO;
            cardUI.badgeLabel = badgeTMP;

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(cardGO, CardPrefabPath);
            Object.DestroyImmediate(cardGO);

            Debug.Log($"[ShopPanelBuilder] Created Shop Card Prefab at {CardPrefabPath}");
            return prefabAsset.GetComponent<ShopCardUI>();
        }

        // ── Texture Generation ────────────────────────────────

        public static void GenerateShopTextures()
        {
            if (!Directory.Exists(ArtUIDir)) Directory.CreateDirectory(ArtUIDir);

            CreateOrUpdateTexture("shop_noads.png", DrawNoAdsTexture);
            CreateOrUpdateTexture("shop_pack_1.png", DrawCoinPack1Texture);
            CreateOrUpdateTexture("shop_pack_2.png", DrawCoinPack2Texture);
            CreateOrUpdateTexture("shop_pack_3.png", DrawCoinPack3Texture);
            CreateOrUpdateTexture("shop_pack_4.png", DrawCoinPack4Texture);
            CreateOrUpdateTexture("shop_pack_5.png", DrawCoinPack5Texture);

            AssetDatabase.Refresh();
        }

        private static void CreateOrUpdateTexture(string fileName, System.Func<Texture2D> drawer)
        {
            string path = $"{ArtUIDir}/{fileName}";
            if (!File.Exists(path))
            {
                var tex = drawer();
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                Object.DestroyImmediate(tex);
                Debug.Log($"[ShopPanelBuilder] Generated {fileName}");
            }

            // Ensure Sprite import settings
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
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
                    importer.SaveAndReimport();
                }
            }
        }

        // Texture 1: Disable Ads (Red circle with white AD crossed out)
        private static Texture2D DrawNoAdsTexture()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.44f;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= r)
                    {
                        // Red badge with 3D gloss
                        float normY = (float)y / s;
                        float gloss = Mathf.Clamp01(1f - (d / r));
                        Color red = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(1f, 0.32f, 0.32f), normY * 0.7f + gloss * 0.3f);

                        // White ring
                        if (d >= r * 0.78f && d <= r * 0.92f)
                            red = Color.white;

                        // Diagonal strike line
                        float lineDist = Mathf.Abs((x - y) / 1.414f);
                        if (lineDist < 12f && d < r * 0.78f)
                            red = Color.white;

                        // "AD" block in center
                        if (d < r * 0.55f && lineDist >= 12f)
                        {
                            // A letter (left side)
                            if (x >= 75 && x <= 115 && y >= 95 && y <= 165)
                            {
                                if (x <= 87 || x >= 103 || (y >= 125 && y <= 137) || y >= 153)
                                    red = Color.white;
                            }
                            // D letter (right side)
                            if (x >= 135 && x <= 180 && y >= 95 && y <= 165)
                            {
                                if (x <= 147 || y <= 107 || y >= 153 || (x >= 168 && y >= 107 && y <= 153))
                                    red = Color.white;
                            }
                        }

                        // Antialiased circle edge
                        float edge = Mathf.Clamp01((r - d) * 1.5f);
                        red.a = edge;
                        cols[y * s + x] = red;
                    }
                    else
                    {
                        cols[y * s + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // Texture 2: Pack 1 (Small pile of shiny gold coins)
        private static Texture2D DrawCoinPack1Texture()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];

            // Draw 3-4 overlapping gold coins
            Vector2[] coinCenters = {
                new Vector2(100, 110),
                new Vector2(156, 110),
                new Vector2(128, 150)
            };
            float cr = 50f;

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color pixel = Color.clear;
                    for (int i = 0; i < coinCenters.Length; i++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), coinCenters[i]);
                        if (d <= cr)
                        {
                            float norm = (float)(y - coinCenters[i].y + cr) / (cr * 2f);
                            Color gold = Color.Lerp(new Color(0.85f, 0.55f, 0.05f), new Color(1f, 0.92f, 0.28f), norm);
                            if (d > cr * 0.75f && d < cr * 0.88f) gold = new Color(1f, 0.98f, 0.6f); // Inner rim
                            if (d < cr * 0.25f) gold = Color.Lerp(gold, Color.white, 0.5f); // Star center
                            pixel = gold;
                        }
                    }
                    cols[y * s + x] = pixel;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // Texture 3: Pack 2 (Purple pouch overflowing with gold coins)
        private static Texture2D DrawCoinPack2Texture()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];
            Vector2 pouchCenter = new Vector2(128, 100);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color pixel = Color.clear;
                    // Top coins
                    float dCoin1 = Vector2.Distance(new Vector2(x, y), new Vector2(110, 165));
                    float dCoin2 = Vector2.Distance(new Vector2(x, y), new Vector2(146, 165));
                    if (dCoin1 <= 32f || dCoin2 <= 32f)
                    {
                        pixel = new Color(1f, 0.88f, 0.18f);
                    }

                    // Pouch body
                    float dx = (x - pouchCenter.x) / 65f;
                    float dy = (y - pouchCenter.y) / 75f;
                    if (dx * dx + dy * dy <= 1f && y < 165)
                    {
                        float normY = (float)y / 165f;
                        Color purple = Color.Lerp(new Color(0.42f, 0.12f, 0.68f), new Color(0.68f, 0.22f, 0.95f), normY);
                        // Gold tie ribbon
                        if (y >= 135 && y <= 145) purple = new Color(1f, 0.82f, 0.1f);
                        pixel = purple;
                    }
                    cols[y * s + x] = pixel;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // Texture 4: Pack 3 (Bag full of gold coins)
        private static Texture2D DrawCoinPack3Texture()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];
            Vector2 bagCenter = new Vector2(128, 110);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color pixel = Color.clear;
                    // Shiny coins top
                    float dCoin1 = Vector2.Distance(new Vector2(x, y), new Vector2(98, 175));
                    float dCoin2 = Vector2.Distance(new Vector2(x, y), new Vector2(128, 185));
                    float dCoin3 = Vector2.Distance(new Vector2(x, y), new Vector2(158, 175));
                    if (dCoin1 <= 28f || dCoin2 <= 30f || dCoin3 <= 28f)
                    {
                        pixel = new Color(1f, 0.88f, 0.2f);
                    }

                    // Plump coin sack
                    float dx = (x - bagCenter.x) / 78f;
                    float dy = (y - bagCenter.y) / 82f;
                    if (dx * dx + dy * dy <= 1f && y < 175)
                    {
                        float normY = (float)y / 175f;
                        Color red = Color.Lerp(new Color(0.68f, 0.15f, 0.25f), new Color(0.92f, 0.25f, 0.35f), normY);
                        if (y >= 148 && y <= 158) red = new Color(1f, 0.84f, 0.15f); // Tie cord
                        pixel = red;
                    }
                    cols[y * s + x] = pixel;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // Texture 5: Pack 4 (Wooden treasure chest full of coins and gems)
        private static Texture2D DrawCoinPack4Texture()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color pixel = Color.clear;
                    // Lower chest body
                    if (x >= 45 && x <= 211 && y >= 50 && y <= 130)
                    {
                        Color wood = new Color(0.48f, 0.26f, 0.10f); // Rich brown wood
                        if (x <= 58 || x >= 198 || y <= 62 || (x >= 120 && x <= 136))
                            wood = new Color(0.95f, 0.78f, 0.22f); // Gold metal brackets
                        pixel = wood;
                    }

                    // Open chest lid
                    if (x >= 40 && x <= 216 && y >= 145 && y <= 190)
                    {
                        Color lid = new Color(0.55f, 0.30f, 0.12f);
                        if (x <= 54 || x >= 202 || y >= 180) lid = new Color(0.95f, 0.78f, 0.22f);
                        pixel = lid;
                    }

                    // Glowing coins and gems inside chest cavity
                    if (x >= 55 && x <= 201 && y >= 125 && y <= 155)
                    {
                        float t = (x + y) % 18;
                        if (t < 5) pixel = new Color(0.2f, 0.8f, 1f); // Diamond cyan
                        else if (t < 10) pixel = new Color(1f, 0.25f, 0.35f); // Ruby red
                        else pixel = new Color(1f, 0.90f, 0.25f); // Gold coins
                    }
                    cols[y * s + x] = pixel;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // Texture 6: Pack 5 (Big treasure vault full of gold bars and coins)
        private static Texture2D DrawCoinPack5Texture()
        {
            int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] cols = new Color[s * s];
            Vector2 c = new Vector2(128, 128);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color pixel = Color.clear;
                    // Steel vault frame
                    if (x >= 40 && x <= 216 && y >= 40 && y <= 216)
                    {
                        pixel = new Color(0.22f, 0.25f, 0.35f); // Dark steel vault safe
                    }

                    // Heavy round vault door with gold wheel
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= 75f)
                    {
                        Color door = Color.Lerp(new Color(0.35f, 0.40f, 0.52f), new Color(0.52f, 0.58f, 0.72f), (float)y / 256f);
                        if (d >= 64f && d <= 72f) door = new Color(0.95f, 0.80f, 0.20f); // Gold outer ring
                        // Turn wheel spokes
                        if (Mathf.Abs(x - 128) < 6 || Mathf.Abs(y - 128) < 6) door = new Color(1f, 0.88f, 0.25f);
                        if (d <= 20f) door = new Color(1f, 0.88f, 0.25f); // Center wheel hub
                        pixel = door;
                    }
                    cols[y * s + x] = pixel;
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // ── Helpers ───────────────────────────────────────────

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string dir, string baseName)
        {
            foreach (var ext in new[] { ".png", ".jpg" })
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{baseName}{ext}");
                if (sp != null) return sp;
            }
            return null;
        }
    }
}
#endif
