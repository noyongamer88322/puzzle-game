// ============================================================
// SafeAreaFitter.cs
// Resizes a RectTransform to stay inside the device safe area
// (notch / home-bar / rounded corners on mobile).
// Attach to the root Canvas RectTransform of every Scene.
// ============================================================

using UnityEngine;

namespace ArrowPuzzle.Systems
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            ApplySafeArea();
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Re-apply in Editor so preview works when you resize the Game window.
            if (Screen.safeArea != _lastSafeArea)
                ApplySafeArea();
        }
#endif

        private void ApplySafeArea()
        {
            Rect sa     = Screen.safeArea;
            _lastSafeArea = sa;

            Vector2 anchorMin = sa.position;
            Vector2 anchorMax = sa.position + sa.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
        }
    }
}

