// ============================================================
// TutorialUIBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Creates and configures the Grandma-style Tutorial UI Prefab & Scene UI:
//   • Canvas Scaler: 1080×1920, Match = 0.5
//   • Canvas sortingOrder: 30 (Above HUD at 10, below Win at 50)
//   • Full-screen dark semi-transparent DimOverlay (#08051A, alpha ~0.82)
//   • Guide Mascot (Dida poses: guide_talk, guide_point)
//   • Speech Bubble with blue border (#2563EB), white background, TMP label
//   • Hand Pointer animated tap loop icon
//   • Saves Prefab to Assets/_Game/Prefabs/UI/Canvas_Tutorial.prefab
//   • Adds / Updates Canvas in Game.unity scene
//
// Menu: Arrow Puzzle -> Build Tutorial UI Prefab & Scene UI
// PRD Section 7: Assets/_Game/Scripts/Editor/TutorialUIBuilder.cs
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
    public static class TutorialUIBuilder
    {
        private const string PrefabDir           = "Assets/_Game/Prefabs/UI";
        private const string PrefabPath          = "Assets/_Game/Prefabs/UI/Canvas_Tutorial.prefab";
        private const string ResourcesDir        = "Assets/Resources/UI";
        private const string ResourcesPrefabPath = "Assets/Resources/UI/Canvas_Tutorial.prefab";
        private const string ArtCharDir          = "Assets/_Game/Art/Characters";
        private const string ArtUIDir            = "Assets/_Game/Art/UI";
        private const string GameScenePath       = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Arrow Puzzle/Build Tutorial UI Prefab & Scene UI", false, 105)]
        public static void BuildTutorialUI()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[TutorialUIBuilder] Skipping build – cannot run EditorSceneManager during Play Mode.");
                return;
            }
            Debug.Log("[TutorialUIBuilder] Building Tutorial UI Prefab & Scene UI...");

            // 1. Load sprites
            var spriteGuideTalk   = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtCharDir}/guide_talk.png");
            var spriteGuidePoint  = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtCharDir}/guide_point.png");
            var spriteHandPointer = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/hand_pointer.png");
            var spriteBubble      = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtUIDir}/speech_bubble.png");

            // Look for custom TMP Font if available
            TMP_FontAsset customFont = FindCustomTMPFont();

            // 2. Build Root Canvas
            var root = new GameObject("Canvas_Tutorial");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // Above HUD (10), below Win Screen (50)

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            var canvasGroup = root.AddComponent<CanvasGroup>();
            var controller = root.AddComponent<TutorialController>();

            controller.tutorialCanvas = canvas;
            controller.canvasGroup = canvasGroup;
            controller.guideTalkSprite = spriteGuideTalk;
            controller.guidePointSprite = spriteGuidePoint;
            controller.handPointerSprite = spriteHandPointer;

            // 3. Dim Overlay
            var dimGO = CreateUIObject("DimOverlay", root.transform);
            var dimRect = dimGO.GetComponent<RectTransform>();
            SetStretchAnchors(dimRect);
            var dimImage = dimGO.AddComponent<Image>();
            dimImage.color = new Color(0.04f, 0.02f, 0.09f, 0.82f);
            dimImage.raycastTarget = true;
            controller.dimOverlay = dimImage;

            // 4. Guide Mascot (Dida)
            var mascotGO = CreateUIObject("GuideMascot", root.transform);
            var mascotRect = mascotGO.GetComponent<RectTransform>();
            mascotRect.anchorMin = new Vector2(1, 1);
            mascotRect.anchorMax = new Vector2(1, 1);
            mascotRect.pivot = new Vector2(1, 1);
            mascotRect.anchoredPosition = new Vector2(-40, -180);
            mascotRect.sizeDelta = new Vector2(260, 325);

            var mascotImage = mascotGO.AddComponent<Image>();
            mascotImage.sprite = spriteGuidePoint != null ? spriteGuidePoint : spriteGuideTalk;
            mascotImage.preserveAspect = true;
            mascotImage.raycastTarget = false;
            controller.guideMascotRect = mascotRect;
            controller.guideMascotImage = mascotImage;

            // 5. Speech Bubble
            var bubbleGO = CreateUIObject("SpeechBubble", root.transform);
            var bubbleRect = bubbleGO.GetComponent<RectTransform>();
            bubbleRect.anchorMin = new Vector2(1, 1);
            bubbleRect.anchorMax = new Vector2(1, 1);
            bubbleRect.pivot = new Vector2(1, 1);
            bubbleRect.anchoredPosition = new Vector2(-310, -220);
            bubbleRect.sizeDelta = new Vector2(490, 200);

            var bubbleImage = bubbleGO.AddComponent<Image>();
            if (spriteBubble != null)
            {
                bubbleImage.sprite = spriteBubble;
                bubbleImage.type = Image.Type.Sliced;
            }
            bubbleImage.color = Color.white;
            bubbleImage.raycastTarget = false;
            controller.speechBubbleRect = bubbleRect;
            controller.speechBubbleImage = bubbleImage;

            // Speech Bubble Pointer Tail (Notch pointing towards Dida)
            var tailGO = CreateUIObject("Tail", bubbleGO.transform);
            var tailRect = tailGO.GetComponent<RectTransform>();
            tailRect.anchorMin = new Vector2(1, 0.5f);
            tailRect.anchorMax = new Vector2(1, 0.5f);
            tailRect.pivot = new Vector2(0, 0.5f);
            tailRect.anchoredPosition = new Vector2(-3, 0);
            tailRect.sizeDelta = new Vector2(28, 28);
            var tailImage = tailGO.AddComponent<Image>();
            tailImage.color = new Color(0.145f, 0.388f, 0.921f, 1f); // #2563EB Blue
            tailImage.raycastTarget = false;
            controller.speechBubbleTail = tailImage;

            // Speech Bubble Text
            var textGO = CreateUIObject("Text", bubbleGO.transform);
            var textRect = textGO.GetComponent<RectTransform>();
            SetStretchAnchors(textRect);
            textRect.offsetMin = new Vector2(24, 18);
            textRect.offsetMax = new Vector2(-24, -18);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            if (customFont != null) tmp.font = customFont;
            tmp.text = "Tap the <color=#9333EA>arrow</color> to make it <color=#9333EA>fly away!</color>";
            tmp.fontSize = 32;
            tmp.fontSizeMin = 20;
            tmp.fontSizeMax = 36;
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.12f, 0.10f, 0.28f, 1f); // Deep charcoal
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            tmp.richText = true;
            controller.speechBubbleText = tmp;

            // 6. Hand Pointer
            var handGO = CreateUIObject("HandPointer", root.transform);
            var handRect = handGO.GetComponent<RectTransform>();
            handRect.pivot = new Vector2(0.586f, 0.8125f);
            handRect.sizeDelta = new Vector2(110, 110);
            handRect.anchoredPosition = new Vector2(0, 0);

            var handImage = handGO.AddComponent<Image>();
            handImage.sprite = spriteHandPointer;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;
            controller.handPointerRect = handRect;
            controller.handPointerImage = handImage;

            // 7. Save Prefab
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            if (!Directory.Exists(ResourcesDir)) Directory.CreateDirectory(ResourcesDir);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, ResourcesPrefabPath);
            Debug.Log($"[TutorialUIBuilder] Saved prefab to {PrefabPath} and {ResourcesPrefabPath}");

            // 8. Add to Game Scene if currently open or load & add
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            bool isGameScene = activeScene.path == GameScenePath;
            if (!isGameScene)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GameScenePath);
                EnsureTutorialInScene(prefab);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
            else
            {
                EnsureTutorialInScene(prefab);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            }

            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TutorialUIBuilder] Tutorial UI Build Complete!");
        }

        private static void EnsureTutorialInScene(GameObject prefab)
        {
            var existing = Object.FindObjectsOfType<TutorialController>();
            foreach (var e in existing)
            {
                Object.DestroyImmediate(e.gameObject);
            }
            PrefabUtility.InstantiatePrefab(prefab);
            Debug.Log("[TutorialUIBuilder] Instantiated Tutorial Canvas in Game scene.");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void SetStretchAnchors(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TMP_FontAsset FindCustomTMPFont()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/_Game/Fonts", "Assets/Art/Fonts" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            return null;
        }
    }
}
#endif

