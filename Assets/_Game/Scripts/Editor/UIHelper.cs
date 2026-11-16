// ============================================================
// UIHelper.cs  –  Arrow Slide Puzzle Editor Utility
// Shared helper methods used by all Scene/Prefab builder scripts.
// Keeps builder code concise by wrapping common RectTransform
// and UI-component creation patterns.
//
// PRD Section 7: Assets/_Game/Scripts/Editor/UIHelper.cs
// ============================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArrowPuzzle.Editor
{
    public static class UIHelper
    {
        // ── GameObject creation ──────────────────────────────────────

        /// <summary>Creates a child GameObject with a RectTransform.</summary>
        public static GameObject CreateRectGO(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        /// <summary>Creates a child Image GameObject.</summary>
        public static GameObject CreateImage(Transform parent, string name, Color? color = null)
        {
            var go = CreateRectGO(parent, name);
            var img = go.AddComponent<Image>();
            img.color = color ?? Color.white;
            return go;
        }

        /// <summary>
        /// Creates a simple Button with an Image background and a TextMeshProUGUI label.
        /// </summary>
        public static GameObject CreateButton(
            Transform parent, string name,
            float height      = 80f,
            Color? bgColor    = null,
            string labelText  = "Button",
            Color? labelColor = null,
            float fontSize    = 36f)
        {
            var go = CreateRectGO(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth   = 1f;

            var img = go.AddComponent<Image>();
            img.color = bgColor ?? Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            UIHelper.Stretch(labelGO.AddComponent<RectTransform>());
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = labelColor ?? Color.black;
            tmp.raycastTarget = false;

            return go;
        }

        // ── RectTransform helpers ────────────────────────────────────

        /// <summary>Makes a RectTransform fill its parent (stretch all).</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Adds a flexible-height LayoutElement (spacer).</summary>
        public static void AddFlexibleSpacer(Transform parent, float flexHeight = 1f)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            var le = spacer.AddComponent<LayoutElement>();
            le.flexibleHeight = flexHeight;
        }
    }
}
#endif

