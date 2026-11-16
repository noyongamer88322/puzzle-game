// ============================================================
// GameOverPopupBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Builds the GameOverPopupUI prefab matching cartoon mobile game style:
//   • Cartoon rounded panel (cream inside)
//   • 'Out of hearts' ribbon at top
//   • Broken / empty heart in center with subtitle
//   • Player current coin count
//   • Green 'Retry' button with UIButtonScale
//   • Orange-yellow 'Revive' button (ReviveCost = 300 coins) with UIButtonScale
//   • UIPanelOvershoot scale-in (0.8 -> 1.05 -> 1.0, 0.25s)
//   • NO Ads or IAP
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using ArrowPuzzle.UI;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Editor
{
    public static class GameOverPopupBuilder
    {
        private const string PrefabDir           = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath          = "Assets/_Game/Prefabs/UI/Popup_GameOver.prefab";
        private const string ExtraPrefabDir      = "Assets/Prefabs/UI";
        private const string ExtraPrefabPath     = "Assets/Prefabs/UI/Popup_GameOver.prefab";
        private const string ResourcesDir        = "Assets/Resources/UI";
        private const string ResourcesPrefabPath = "Assets/Resources/UI/Popup_GameOver.prefab";
        private const string ArtUIDir            = "Assets/_Game/Art/UI";

        [MenuItem("Arrow Puzzle/Build Game Over Popup Prefab", false, 202)]
        public static GameObject Build()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(ExtraPrefabDir)) Directory.CreateDirectory(ExtraPrefabDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);

            // Sprites
            var panelSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_panel_popup.png")
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");
            var ribbonSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_ribbon_red.png")
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_ribbon_gold.png");
            var brokenHeart   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_heart_broken.png")
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_heart_empty.png");
            var emptyHeart    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_heart_empty.png");
            var fullHeart     = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_heart_full.png");
            var coinSprite    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/icon_coin.png");
            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");

            // ── Root Holder ───────────────────────────────────────────
            var rootGO = new GameObject("Popup_GameOver");
            var rootRT = rootGO.AddComponent<RectTransform>();
            UIHelper.Stretch(rootRT);

            var ctrl = rootGO.AddComponent<GameOverPopupUI>();
            ctrl.reviveCoinCost = 300;
            ctrl.heartEmptySprite = emptyHeart;
            ctrl.heartFullSprite  = fullHeart;

            // ── Dim overlay ───────────────────────────────────────────
            var dimGO  = UIHelper.CreateImage(rootGO.transform, "DimOverlay", new Color(0f, 0f, 0f, 0.65f));
            UIHelper.Stretch(dimGO.GetComponent<RectTransform>());
            dimGO.GetComponent<Image>().raycastTarget = true;
            ctrl.dimOverlay = dimGO.GetComponent<Image>();

            // ── Card Panel ────────────────────────────────────────────
            var cardGO = new GameObject("Card");
            cardGO.transform.SetParent(rootGO.transform, false);
            var cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot     = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(660f, 840f);
            cardRT.anchoredPosition = Vector2.zero;

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

            // UIPanelOvershoot
            var overshoot = cardGO.AddComponent<UIPanelOvershoot>();
            overshoot.panelCard        = cardRT;
            overshoot.dimOverlay       = dimGO.GetComponent<Image>();
            overshoot.openDuration     = 0.25f;
            overshoot.dimTargetAlpha   = 0.65f;
            ctrl.panelOvershoot        = overshoot;

            // ── Ribbon Header: 'Out of hearts' ────────────────────────
            var ribbonGO = new GameObject("Ribbon_GameOver");
            ribbonGO.transform.SetParent(cardGO.transform, false);
            var ribbonRT = ribbonGO.AddComponent<RectTransform>();
            ribbonRT.anchorMin = new Vector2(0.5f, 1f);
            ribbonRT.anchorMax = new Vector2(0.5f, 1f);
            ribbonRT.pivot     = new Vector2(0.5f, 0.5f);
            ribbonRT.anchoredPosition = new Vector2(0f, 10f);
            ribbonRT.sizeDelta = new Vector2(500f, 100f);

            var ribbonImg = ribbonGO.AddComponent<Image>();
            if (ribbonSprite != null)
            {
                ribbonImg.sprite = ribbonSprite;
                ribbonImg.type = Image.Type.Sliced;
                ribbonImg.color = Color.white;
            }
            else
            {
                ribbonImg.color = new Color(0.92f, 0.24f, 0.32f); // Red ribbon
            }

            var ribbonTitleGO = new GameObject("Lbl_Title");
            ribbonTitleGO.transform.SetParent(ribbonGO.transform, false);
            UIHelper.Stretch(ribbonTitleGO.AddComponent<RectTransform>());
            var ribbonTMP = ribbonTitleGO.AddComponent<TextMeshProUGUI>();
            ribbonTMP.text = "Out of hearts";
            ribbonTMP.fontSize = 42f;
            ribbonTMP.fontStyle = FontStyles.Bold;
            ribbonTMP.alignment = TextAlignmentOptions.Center;
            ribbonTMP.color = Color.white;
            ctrl.titleLabel = ribbonTMP;

            // ── Layout inside card ────────────────────────────────────
            var layoutGO = new GameObject("Layout");
            layoutGO.transform.SetParent(cardGO.transform, false);
            var layoutRT = layoutGO.AddComponent<RectTransform>();
            UIHelper.Stretch(layoutRT);
            layoutRT.offsetMin = new Vector2(40f, 32f);
            layoutRT.offsetMax = new Vector2(-40f, -80f);

            var vlg = layoutGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 16, 16);

            // Broken / Empty Heart Display (Large in Center)
            var heartCenterGO = new GameObject("Icon_BrokenHeart");
            heartCenterGO.transform.SetParent(layoutGO.transform, false);
            var heartCenterLE = heartCenterGO.AddComponent<LayoutElement>();
            heartCenterLE.preferredHeight = 140f;
            heartCenterLE.preferredWidth  = 140f;
            var heartCenterImg = heartCenterGO.AddComponent<Image>();
            heartCenterImg.sprite = brokenHeart;
            heartCenterImg.preserveAspect = true;
            heartCenterImg.color = Color.white;

            // Subtitle
            var subGO = new GameObject("SubtitleLabel");
            subGO.transform.SetParent(layoutGO.transform, false);
            var subLE = subGO.AddComponent<LayoutElement>();
            subLE.preferredHeight = 44f;
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "Don't give up!";
            subTMP.fontSize = 32f;
            subTMP.fontStyle = FontStyles.Bold;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = new Color(0.48f, 0.38f, 0.30f); // Warm dark text
            ctrl.subtitleLabel = subTMP;

            // Coin row (player current coins)
            var coinRowGO = new GameObject("CoinRow");
            coinRowGO.transform.SetParent(layoutGO.transform, false);
            var coinRowLE = coinRowGO.AddComponent<LayoutElement>();
            coinRowLE.preferredHeight = 48f;
            var coinHLG = coinRowGO.AddComponent<HorizontalLayoutGroup>();
            coinHLG.spacing = 10f;
            coinHLG.childAlignment = TextAnchor.MiddleCenter;
            coinHLG.childForceExpandWidth  = false;
            coinHLG.childForceExpandHeight = false;

            var coinIconGO = UIHelper.CreateImage(coinRowGO.transform, "CoinIcon");
            var coinIconLE = coinIconGO.AddComponent<LayoutElement>();
            coinIconLE.preferredWidth = coinIconLE.preferredHeight = 40f;
            var coinIconImg = coinIconGO.GetComponent<Image>();
            coinIconImg.sprite = coinSprite;
            coinIconImg.preserveAspect = true;
            coinIconImg.color = Color.white;

            var coinLabelGO = new GameObject("CoinLabel");
            coinLabelGO.transform.SetParent(coinRowGO.transform, false);
            var coinLabelTMP = coinLabelGO.AddComponent<TextMeshProUGUI>();
            coinLabelTMP.text = "0";
            coinLabelTMP.fontSize = 32f;
            coinLabelTMP.fontStyle = FontStyles.Bold;
            coinLabelTMP.alignment = TextAlignmentOptions.Left;
            coinLabelTMP.color = new Color(0.28f, 0.18f, 0.08f);
            ctrl.playerCoinsLabel = coinLabelTMP;

            // Spacer
            UIHelper.AddFlexibleSpacer(layoutGO.transform, 0.5f);

            // ── Green Retry Button ────────────────────────────────────
            var retryGO = new GameObject("Btn_Retry");
            retryGO.transform.SetParent(layoutGO.transform, false);
            var retryRT = retryGO.AddComponent<RectTransform>();
            var retryLE = retryGO.AddComponent<LayoutElement>();
            retryLE.preferredHeight = 84f;
            retryLE.flexibleWidth   = 1f;

            var retryImg = retryGO.AddComponent<Image>();
            if (roundedSprite != null)
            {
                retryImg.sprite = roundedSprite;
                retryImg.type   = Image.Type.Sliced;
            }
            retryImg.color = new Color(0.13f, 0.77f, 0.37f); // Green #22C55E

            var retryBtn = retryGO.AddComponent<Button>();
            retryBtn.transition = Selectable.Transition.None;
            retryGO.AddComponent<UIButtonScale>();

            var retryLblGO = new GameObject("Lbl_Text");
            retryLblGO.transform.SetParent(retryGO.transform, false);
            UIHelper.Stretch(retryLblGO.AddComponent<RectTransform>());
            var retryTMP = retryLblGO.AddComponent<TextMeshProUGUI>();
            retryTMP.text = "Retry";
            retryTMP.fontSize = 38f;
            retryTMP.fontStyle = FontStyles.Bold;
            retryTMP.alignment = TextAlignmentOptions.Center;
            retryTMP.color = Color.white;
            ctrl.retryButton = retryBtn;

            // ── Orange-Yellow Revive Button (ReviveCost = 300) ────────
            var reviveGO = new GameObject("Btn_Revive");
            reviveGO.transform.SetParent(layoutGO.transform, false);
            var reviveRT = reviveGO.AddComponent<RectTransform>();
            var reviveLE = reviveGO.AddComponent<LayoutElement>();
            reviveLE.preferredHeight = 84f;
            reviveLE.flexibleWidth   = 1f;

            var reviveImg = reviveGO.AddComponent<Image>();
            if (roundedSprite != null)
            {
                reviveImg.sprite = roundedSprite;
                reviveImg.type   = Image.Type.Sliced;
            }
            reviveImg.color = new Color(0.98f, 0.58f, 0.06f); // Orange-yellow #F9960F

            var reviveBtn = reviveGO.AddComponent<Button>();
            reviveBtn.transition = Selectable.Transition.None;
            reviveGO.AddComponent<UIButtonScale>();

            var reviveHLG = reviveGO.AddComponent<HorizontalLayoutGroup>();
            reviveHLG.spacing = 12f;
            reviveHLG.childAlignment = TextAnchor.MiddleCenter;
            reviveHLG.childForceExpandWidth  = false;
            reviveHLG.childForceExpandHeight = false;

            // Coin icon inside revive button
            var rCoinGO = UIHelper.CreateImage(reviveGO.transform, "CoinIcon");
            var rCoinLE = rCoinGO.AddComponent<LayoutElement>();
            rCoinLE.preferredWidth = rCoinLE.preferredHeight = 44f;
            var rCoinImg = rCoinGO.GetComponent<Image>();
            rCoinImg.sprite = coinSprite;
            rCoinImg.preserveAspect = true;
            rCoinImg.color = Color.white;
            ctrl.reviveCoinIcon = rCoinGO;

            var reviveLblGO = new GameObject("Lbl_Text");
            reviveLblGO.transform.SetParent(reviveGO.transform, false);
            var reviveTMP = reviveLblGO.AddComponent<TextMeshProUGUI>();
            reviveTMP.text = "300";
            reviveTMP.fontSize = 38f;
            reviveTMP.fontStyle = FontStyles.Bold;
            reviveTMP.alignment = TextAlignmentOptions.Center;
            reviveTMP.color = Color.white;
            ctrl.reviveLabel  = reviveTMP;
            ctrl.reviveButton = reviveBtn;

            // Save Prefabs
            PrefabUtility.SaveAsPrefabAsset(rootGO, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ExtraPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ResourcesPrefabPath);

            Debug.Log($"[GameOverPopupBuilder] Game Over Popup Prefab saved to {PrefabPath}");
            return rootGO;
        }
    }
}
#endif
