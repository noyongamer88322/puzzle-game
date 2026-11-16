// ============================================================
// SettingsPanelBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Builds the SettingsPanelUI prefab matching cartoon mobile game style:
//   • Blue rounded panel with cream inside
//   • Orange-yellow 'Settings' ribbon at top
//   • Close (✕) button top-right with UIButtonScale
//   • Sound / Music / Vibration toggles (ON green #22C55E, OFF grey #9CA3AF)
//   • Green cartoon 'Privacy Policy' button with UIButtonScale
//   • Small version number at bottom (v1.0.0)
//   • UIPanelOvershoot (0.8 -> 1.05 -> 1.0, 0.25s)
//   • NO Ads or IAP / Restore Purchases
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
    public static class SettingsPanelBuilder
    {
        private const string PrefabDir           = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath          = "Assets/_Game/Prefabs/UI/Panel_Settings.prefab";
        private const string ExtraPrefabDir      = "Assets/Prefabs/UI";
        private const string ExtraPrefabPath     = "Assets/Prefabs/UI/Panel_Settings.prefab";
        private const string ResourcesDir        = "Assets/Resources/UI";
        private const string ResourcesPrefabPath = "Assets/Resources/UI/Panel_Settings.prefab";
        private const string ArtUIDir            = "Assets/_Game/Art/UI";

        [MenuItem("Arrow Puzzle/Build Settings Panel Prefab", false, 201)]
        public static GameObject Build()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(ExtraPrefabDir)) Directory.CreateDirectory(ExtraPrefabDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);

            // Sprites
            var panelSprite   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_panel_popup.png")
                                ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");
            var ribbonSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_ribbon_gold.png");
            var circleSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_circle.png");
            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/ui_button_rounded_square.png");

            // Root holder (inactive)
            var rootGO  = new GameObject("Panel_Settings");
            var rootRT  = rootGO.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

            var ctrl = rootGO.AddComponent<SettingsPanelUI>();

            // Dim overlay
            var dimGO  = UIHelper.CreateImage(rootGO.transform, "DimOverlay");
            UIHelper.Stretch(dimGO.GetComponent<RectTransform>());
            var dimImg = dimGO.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.65f);
            dimImg.raycastTarget = true;

            // Card
            var cardGO = new GameObject("Card");
            cardGO.transform.SetParent(rootGO.transform, false);
            var cardRT = cardGO.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot     = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(680f, 880f);
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
                cardImg.color = new Color(0.18f, 0.44f, 0.88f, 1f); // Blue cartoon panel
            }
            cardImg.raycastTarget = true;

            // UIPanelOvershoot
            var overshoot = cardGO.AddComponent<UIPanelOvershoot>();
            overshoot.panelCard        = cardRT;
            overshoot.dimOverlay       = dimImg;
            overshoot.openDuration     = 0.25f;
            overshoot.dimTargetAlpha   = 0.65f;
            ctrl.panelOvershoot        = overshoot;

            // Ribbon header at top
            var ribbonGO = new GameObject("Ribbon_Settings");
            ribbonGO.transform.SetParent(cardGO.transform, false);
            var ribbonRT = ribbonGO.AddComponent<RectTransform>();
            ribbonRT.anchorMin = new Vector2(0.5f, 1f);
            ribbonRT.anchorMax = new Vector2(0.5f, 1f);
            ribbonRT.pivot     = new Vector2(0.5f, 0.5f);
            ribbonRT.anchoredPosition = new Vector2(0f, 10f);
            ribbonRT.sizeDelta = new Vector2(460f, 100f);

            var ribbonImg = ribbonGO.AddComponent<Image>();
            if (ribbonSprite != null)
            {
                ribbonImg.sprite = ribbonSprite;
                ribbonImg.type = Image.Type.Sliced;
                ribbonImg.color = Color.white;
            }
            else
            {
                ribbonImg.color = new Color(1f, 0.74f, 0.12f);
            }

            var ribbonTitleGO = new GameObject("Lbl_Title");
            ribbonTitleGO.transform.SetParent(ribbonGO.transform, false);
            UIHelper.Stretch(ribbonTitleGO.AddComponent<RectTransform>());
            var ribbonTMP = ribbonTitleGO.AddComponent<TextMeshProUGUI>();
            ribbonTMP.text = "Settings";
            ribbonTMP.fontSize = 46f;
            ribbonTMP.fontStyle = FontStyles.Bold;
            ribbonTMP.alignment = TextAlignmentOptions.Center;
            ribbonTMP.color = new Color(0.18f, 0.08f, 0.02f); // Dark rich brown

            // Close button (✕) top-right of card
            var closeGO = new GameObject("Btn_Close");
            closeGO.transform.SetParent(cardGO.transform, false);
            var closeRT = closeGO.AddComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot     = new Vector2(0.5f, 0.5f);
            closeRT.sizeDelta = new Vector2(68f, 68f);
            closeRT.anchoredPosition = new Vector2(-12f, 10f);

            var closeImg = closeGO.AddComponent<Image>();
            if (circleSprite != null) closeImg.sprite = circleSprite;
            closeImg.color = new Color(0.18f, 0.44f, 0.88f); // Blue circle
            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.transition = Selectable.Transition.None;
            closeGO.AddComponent<UIButtonScale>();

            var closeLblGO = new GameObject("Lbl_X");
            closeLblGO.transform.SetParent(closeGO.transform, false);
            UIHelper.Stretch(closeLblGO.AddComponent<RectTransform>());
            var closeTMP = closeLblGO.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "✕";
            closeTMP.fontSize = 32f;
            closeTMP.fontStyle = FontStyles.Bold;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.color = Color.white;
            ctrl.closeButton = closeBtn;

            // Layout inside card
            var layoutGO = new GameObject("Layout");
            layoutGO.transform.SetParent(cardGO.transform, false);
            var layoutRT = layoutGO.AddComponent<RectTransform>();
            UIHelper.Stretch(layoutRT);
            layoutRT.offsetMin = new Vector2(40f, 32f);
            layoutRT.offsetMax = new Vector2(-40f, -80f); // Space for top ribbon

            var vlg = layoutGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 16, 16);

            // Three toggle rows: Sound, Music, Vibration
            (ctrl.soundToggleButton, ctrl.soundLabel, ctrl.soundIcon)
                = AddCartoonToggleRow(layoutGO.transform, "Sound", "🔊", roundedSprite, circleSprite);
            (ctrl.musicToggleButton, ctrl.musicLabel, ctrl.musicIcon)
                = AddCartoonToggleRow(layoutGO.transform, "Music", "🎵", roundedSprite, circleSprite);
            (ctrl.vibrationToggleButton, ctrl.vibrationLabel, ctrl.vibrationIcon)
                = AddCartoonToggleRow(layoutGO.transform, "Vibration", "📳", roundedSprite, circleSprite);

            // Divider
            AddDivider(layoutGO.transform, 2f);

            // Privacy Policy button (Green cartoon button)
            var ppGO = new GameObject("Btn_PrivacyPolicy");
            ppGO.transform.SetParent(layoutGO.transform, false);
            var ppRT = ppGO.AddComponent<RectTransform>();
            var ppLE = ppGO.AddComponent<LayoutElement>();
            ppLE.preferredHeight = 76f;
            ppLE.flexibleWidth   = 1f;

            var ppImg = ppGO.AddComponent<Image>();
            if (roundedSprite != null)
            {
                ppImg.sprite = roundedSprite;
                ppImg.type = Image.Type.Sliced;
            }
            ppImg.color = new Color(0.13f, 0.77f, 0.37f); // Green #22C55E

            var ppBtn = ppGO.AddComponent<Button>();
            ppBtn.transition = Selectable.Transition.None;
            ppGO.AddComponent<UIButtonScale>();

            var ppLblGO = new GameObject("Lbl_Text");
            ppLblGO.transform.SetParent(ppGO.transform, false);
            UIHelper.Stretch(ppLblGO.AddComponent<RectTransform>());
            var ppTMP = ppLblGO.AddComponent<TextMeshProUGUI>();
            ppTMP.text = "Privacy Policy";
            ppTMP.fontSize = 34f;
            ppTMP.fontStyle = FontStyles.Bold;
            ppTMP.alignment = TextAlignmentOptions.Center;
            ppTMP.color = Color.white;
            ctrl.privacyPolicyButton = ppBtn;

            // Version Label at bottom
            var verGO = new GameObject("Lbl_Version");
            verGO.transform.SetParent(layoutGO.transform, false);
            var verLE = verGO.AddComponent<LayoutElement>();
            verLE.preferredHeight = 36f;
            var verTMP = verGO.AddComponent<TextMeshProUGUI>();
            verTMP.text = "v1.0.0";
            verTMP.fontSize = 24f;
            verTMP.alignment = TextAlignmentOptions.Center;
            verTMP.color = new Color(0.45f, 0.38f, 0.32f, 0.8f);
            ctrl.versionLabel = verTMP;

            // Save Prefabs
            PrefabUtility.SaveAsPrefabAsset(rootGO, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ExtraPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rootGO, ResourcesPrefabPath);

            Debug.Log($"[SettingsPanelBuilder] Settings Panel Prefab saved to {PrefabPath}");
            return rootGO;
        }

        private static void AddDivider(Transform parent, float height)
        {
            var divGO = UIHelper.CreateImage(parent, "Divider");
            var divRT = divGO.GetComponent<RectTransform>();
            divRT.sizeDelta = new Vector2(0f, height);
            var img = divGO.GetComponent<Image>();
            img.color = new Color(0.85f, 0.78f, 0.70f, 0.8f);
            img.raycastTarget = false;
            var le = divGO.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth   = 1f;
        }

        private static (Button btn, TextMeshProUGUI label, Image icon)
            AddCartoonToggleRow(Transform parent, string settingName, string iconEmoji, Sprite cardSprite, Sprite circleSprite)
        {
            var rowGO = new GameObject($"Row_{settingName}");
            rowGO.transform.SetParent(parent, false);
            var rowRT = rowGO.AddComponent<RectTransform>();
            var le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.flexibleWidth   = 1f;

            var bgImg = rowGO.AddComponent<Image>();
            if (cardSprite != null)
            {
                bgImg.sprite = cardSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.95f, 0.91f, 0.84f, 0.9f); // Soft warm pill card

            var btn = rowGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            rowGO.AddComponent<UIButtonScale>();

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(20, 20, 10, 10);

            // Icon
            var iconGO = UIHelper.CreateImage(rowGO.transform, "Icon");
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth  = 54f;
            iconLE.preferredHeight = 54f;
            var iconImg = iconGO.GetComponent<Image>();
            if (circleSprite != null) iconImg.sprite = circleSprite;
            iconImg.color = new Color(0.13f, 0.77f, 0.37f); // Start ON (green)
            iconImg.raycastTarget = false;

            var emojiGO = new GameObject("Emoji");
            emojiGO.transform.SetParent(iconGO.transform, false);
            UIHelper.Stretch(emojiGO.AddComponent<RectTransform>());
            var emojiTMP = emojiGO.AddComponent<TextMeshProUGUI>();
            emojiTMP.text = iconEmoji;
            emojiTMP.fontSize = 28f;
            emojiTMP.alignment = TextAlignmentOptions.Center;
            emojiTMP.color = Color.white;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.flexibleWidth = 1f;
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = $"{settingName}: ON";
            labelTMP.fontSize = 32f;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.Left;
            labelTMP.color = new Color(0.25f, 0.16f, 0.08f); // Dark rich brown
            labelTMP.raycastTarget = false;

            return (btn, labelTMP, iconImg);
        }
    }
}
#endif
