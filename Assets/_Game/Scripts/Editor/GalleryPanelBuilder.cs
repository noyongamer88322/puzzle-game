// ============================================================
// GalleryPanelBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Builds the Gallery pop-up panel matching cartoon mobile game style:
//   • Blue rounded panel with cream inside
//   • Orange-yellow 'Gallery' ribbon at top
//   • Small blue X button top-right with UIButtonScale
//   • 3-column polaroid-style grid (unlocked = green checkmark, locked = grey + lock)
//   • Vertical scroll with ScrollRect
//   • UIPanelOvershoot scale-in (0.8 -> 1.05 -> 1.0, 0.25s)
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Editor
{
    public static class GalleryPanelBuilder
    {
        private const string PrefabDir           = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath          = "Assets/_Game/Prefabs/UI/Panel_Gallery.prefab";
        private const string ExtraPrefabDir      = "Assets/Prefabs/UI";
        private const string ExtraPrefabPath     = "Assets/Prefabs/UI/Panel_Gallery.prefab";
        private const string ResourcesDir        = "Assets/Resources/UI";
        private const string ResourcesPrefabPath = "Assets/Resources/UI/Panel_Gallery.prefab";
        private const string CellPrefabPath      = "Assets/_Game/Prefabs/UI/Pfb_GalleryCell.prefab";
        private const string ArtUIDir            = "Assets/_Game/Art/UI";
        private const string ArtGalleryDir       = "Assets/_Game/Art/Gallery";

        [MenuItem("Arrow Puzzle/Build Gallery Panel", false, 205)]
        public static GameObject BuildGalleryPanel()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(ExtraPrefabDir)) Directory.CreateDirectory(ExtraPrefabDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);

            // Sprites
            var panelSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_panel_popup.png")
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");
            var ribbonSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_ribbon_gold.png");
            var circleSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_circle.png");
            var lockSprite    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_lock.png");
            var tickSprite    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_check.png")
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_star.png");
            var placeholderSp = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtGalleryDir}/pic_placeholder.png");

            // Build or get Cell Prefab
            var cellPrefab = BuildCellPrefab(lockSprite, tickSprite, placeholderSp);

            // ── Root Holder ───────────────────────────────────────────
            var rootGO   = new GameObject("Panel_Gallery");
            var rootRect = rootGO.AddComponent<RectTransform>();
            UIHelper.Stretch(rootRect);

            // ── Dim overlay ───────────────────────────────────────────
            var dimGO   = UIHelper.CreateImage(rootGO.transform, "Img_Dim", new Color(0f, 0f, 0f, 0.65f));
            UIHelper.Stretch(dimGO.GetComponent<RectTransform>());
            dimGO.GetComponent<Image>().raycastTarget = true;

            // ── Card Panel (Blue rounded frame with cream center) ─────
            var cardGO   = new GameObject("Panel_Card");
            var cardRect = cardGO.AddComponent<RectTransform>();
            cardGO.transform.SetParent(rootGO.transform, false);
            cardRect.anchorMin        = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax        = new Vector2(0.5f, 0.5f);
            cardRect.pivot            = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta        = new Vector2(700f, 960f);

            var cardImg = cardGO.AddComponent<Image>();
            if (panelSprite != null)
            {
                cardImg.sprite = panelSprite;
                cardImg.type   = Image.Type.Sliced;
                cardImg.color  = Color.white;
            }
            else
            {
                cardImg.color = new Color(0.18f, 0.44f, 0.88f, 1f);
            }
            cardImg.raycastTarget = true;

            var cardCG = cardGO.AddComponent<CanvasGroup>();

            // ── Top Orange-Yellow Ribbon ──────────────────────────────
            var ribbonGO   = new GameObject("Ribbon_Gallery");
            var ribbonRect = ribbonGO.AddComponent<RectTransform>();
            ribbonGO.transform.SetParent(cardGO.transform, false);
            ribbonRect.anchorMin        = new Vector2(0.5f, 1f);
            ribbonRect.anchorMax        = new Vector2(0.5f, 1f);
            ribbonRect.pivot            = new Vector2(0.5f, 0.5f);
            ribbonRect.anchoredPosition = new Vector2(0f, 10f);
            ribbonRect.sizeDelta        = new Vector2(460f, 100f);

            var ribbonImg = ribbonGO.AddComponent<Image>();
            if (ribbonSprite != null)
            {
                ribbonImg.sprite = ribbonSprite;
                ribbonImg.type   = Image.Type.Sliced;
                ribbonImg.color  = Color.white;
            }
            else
            {
                ribbonImg.color = new Color(1f, 0.74f, 0.12f);
            }

            var titleGO   = new GameObject("Lbl_Title");
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleGO.transform.SetParent(ribbonGO.transform, false);
            UIHelper.Stretch(titleRect);

            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text      = "Gallery";
            titleTMP.fontSize  = 46f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color     = new Color(0.18f, 0.08f, 0.02f);
            titleTMP.alignment = TextAlignmentOptions.Center;

            // ── Close button (small blue X top-right) ─────────────────
            var closeGO   = new GameObject("Btn_Close");
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeGO.transform.SetParent(cardGO.transform, false);
            closeRect.anchorMin        = new Vector2(1f, 1f);
            closeRect.anchorMax        = new Vector2(1f, 1f);
            closeRect.pivot            = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-12f, 10f);
            closeRect.sizeDelta        = new Vector2(68f, 68f);

            var closeImg = closeGO.AddComponent<Image>();
            if (circleSprite != null) closeImg.sprite = circleSprite;
            closeImg.color = new Color(0.18f, 0.44f, 0.88f);
            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeGO.AddComponent<UIButtonScale>();

            var closeLblGO   = new GameObject("Lbl_X");
            closeLblGO.transform.SetParent(closeGO.transform, false);
            UIHelper.Stretch(closeLblGO.AddComponent<RectTransform>());
            var closeTMP = closeLblGO.AddComponent<TextMeshProUGUI>();
            closeTMP.text      = "✕";
            closeTMP.fontSize  = 32f;
            closeTMP.fontStyle = FontStyles.Bold;
            closeTMP.color     = Color.white;
            closeTMP.alignment = TextAlignmentOptions.Center;

            // ── ScrollRect container ──────────────────────────────────
            var scrollGO   = new GameObject("Scroll_Gallery");
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollGO.transform.SetParent(cardGO.transform, false);
            scrollRect.anchorMin        = new Vector2(0f, 0f);
            scrollRect.anchorMax        = new Vector2(1f, 1f);
            scrollRect.offsetMin        = new Vector2(32f, 32f);
            scrollRect.offsetMax        = new Vector2(-32f, -80f); // Space for top ribbon

            var scrollComp = scrollGO.AddComponent<ScrollRect>();
            scrollComp.horizontal = false;
            scrollComp.vertical   = true;

            // Viewport with Mask
            var vpGO   = new GameObject("Viewport");
            var vpRect = vpGO.AddComponent<RectTransform>();
            vpGO.transform.SetParent(scrollGO.transform, false);
            UIHelper.Stretch(vpRect);
            var vpMask = vpGO.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;
            var vpImg = vpGO.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0.01f);

            // Content Grid (3 columns)
            var contentGO   = new GameObject("Content");
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentGO.transform.SetParent(vpGO.transform, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot     = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var grid        = contentGO.AddComponent<GridLayoutGroup>();
            grid.padding    = new RectOffset(10, 10, 12, 12);
            grid.spacing    = new Vector2(14f, 14f);
            grid.cellSize   = new Vector2(190f, 210f); // Polaroid aspect ratio
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollComp.viewport  = vpRect;
            scrollComp.content   = contentRect;
            scrollComp.movementType = ScrollRect.MovementType.Clamped;
            scrollComp.scrollSensitivity = 35f;

            // ── GalleryPanelUI component ──────────────────────────────
            var ctrl = rootGO.AddComponent<GalleryPanelUI>();
            ctrl.panelRect        = cardRect;
            ctrl.dimOverlay       = dimGO.GetComponent<Image>();
            ctrl.panelCanvasGroup = cardCG;
            ctrl.titleLabel       = titleTMP;
            ctrl.closeButton      = closeBtn;
            ctrl.scrollRect       = scrollComp;
            ctrl.gridContent      = contentRect;
            ctrl.cellPrefab       = cellPrefab;
            ctrl.lockSprite       = lockSprite;
            ctrl.tickSprite       = tickSprite;
            ctrl.placeholderSprite = placeholderSp;

            ctrl.openDuration     = 0.25f;
            ctrl.overshootScale   = 1.05f;
            ctrl.dimAlpha         = 0.65f;

            // Save Prefabs
            PrefabUtility.SaveAsPrefabAsset(rootGO, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ExtraPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ResourcesPrefabPath);

            Debug.Log($"[GalleryPanelBuilder] Gallery Panel Prefab saved to {PrefabPath}");
            return rootGO;
        }

        private static GalleryCellUI BuildCellPrefab(Sprite lockSprite, Sprite tickSprite, Sprite placeholderSprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CellPrefabPath);
            if (existing != null)
                return existing.GetComponent<GalleryCellUI>();

            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/polaroid_frame.png")
                              ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");

            var cellGO   = new GameObject("Pfb_GalleryCell");
            var cellRect = cellGO.AddComponent<RectTransform>();
            cellRect.sizeDelta = new Vector2(190f, 210f);

            // Polaroid frame background (white/cream card with bottom chin)
            var frameGO = new GameObject("Img_PolaroidFrame");
            frameGO.transform.SetParent(cellGO.transform, false);
            UIHelper.Stretch(frameGO.AddComponent<RectTransform>());
            var frameImg = frameGO.AddComponent<Image>();
            if (frameSprite != null)
            {
                frameImg.sprite = frameSprite;
                frameImg.type = Image.Type.Sliced;
            }
            frameImg.color = Color.white;

            // Loading background
            var bgGO = new GameObject("Img_LoadingBg");
            bgGO.transform.SetParent(cellGO.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.08f, 0.18f);
            bgRT.anchorMax = new Vector2(0.92f, 0.92f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.46f, 0.78f, 1f);

            // Photo image
            var photoGO = new GameObject("Img_Photo");
            photoGO.transform.SetParent(cellGO.transform, false);
            var photoRT = photoGO.AddComponent<RectTransform>();
            photoRT.anchorMin = new Vector2(0.08f, 0.18f);
            photoRT.anchorMax = new Vector2(0.92f, 0.92f);
            photoRT.offsetMin = photoRT.offsetMax = Vector2.zero;
            var photoImg = photoGO.AddComponent<Image>();
            photoImg.preserveAspect = true;
            photoGO.SetActive(false);

            // Locked overlay (grey tint)
            var overlayGO = new GameObject("Img_LockedOverlay");
            overlayGO.transform.SetParent(cellGO.transform, false);
            var overRT = overlayGO.AddComponent<RectTransform>();
            overRT.anchorMin = new Vector2(0.08f, 0.18f);
            overRT.anchorMax = new Vector2(0.92f, 0.92f);
            overRT.offsetMin = overRT.offsetMax = Vector2.zero;
            var overImg = overlayGO.AddComponent<Image>();
            overImg.color = new Color(0f, 0f, 0f, 0.45f);
            overlayGO.SetActive(false);

            // Lock icon (centre of photo)
            var lockGO   = new GameObject("Img_Lock");
            lockGO.transform.SetParent(cellGO.transform, false);
            var lockRect = lockGO.AddComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = lockRect.pivot = new Vector2(0.5f, 0.55f);
            lockRect.sizeDelta = new Vector2(52f, 52f);
            var lockImg = lockGO.AddComponent<Image>();
            lockImg.sprite = lockSprite;
            lockImg.preserveAspect = true;
            lockGO.SetActive(false);

            // Tick badge (green circle with white checkmark)
            var tickGO   = new GameObject("Img_Tick");
            tickGO.transform.SetParent(cellGO.transform, false);
            var tickRect = tickGO.AddComponent<RectTransform>();
            tickRect.anchorMin = tickRect.anchorMax = tickRect.pivot = new Vector2(1f, 0.08f);
            tickRect.anchoredPosition = new Vector2(-12f, 12f);
            tickRect.sizeDelta = new Vector2(40f, 40f);
            var tickImg = tickGO.AddComponent<Image>();
            tickImg.sprite = tickSprite;
            tickImg.preserveAspect = true;
            tickGO.SetActive(false);

            var cellUI           = cellGO.AddComponent<GalleryCellUI>();
            cellUI.photoImage    = photoImg;
            cellUI.loadingBg     = bgImg;
            cellUI.lockIcon      = lockImg;
            cellUI.tickIcon      = tickImg;
            cellUI.lockedOverlay = overImg;

            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(cellGO, CellPrefabPath);
            Object.DestroyImmediate(cellGO);
            return prefabAsset.GetComponent<GalleryCellUI>();
        }
    }
}
#endif
