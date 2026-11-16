// ============================================================
// GalleryPanelUI.cs  –  Arrow Slide Puzzle
// Gallery pop-up panel shown when the gallery icon is tapped
// on the Map screen.
//
// Layout (PRD Gallery step):
//   • Blue rounded panel, scale-in with overshoot, dimmed backdrop
//   • Yellow 'Gallery' ribbon at the top
//   • 3-column scrollable grid of picture slots
//   • Unlocked slots show the picture + green tick
//   • Locked slots show grey tint + lock icon (icon_lock.png)
//   • Loading: empty blue cell, then image fades in
//   • Close button (top-right X)
//
// Unlock data: Saved in SaveManager. IsGalleryUnlocked(N).
// Gallery order matches level reward order (level 1, 2, 3 …).
//
// PRD Section 7: Assets/_Game/Scripts/UI/GalleryPanelUI.cs
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class GalleryPanelUI : MonoBehaviour
    {
        // ── Inspector: Panel Root ─────────────────────────────
        [Header("Panel Root")]
        [Tooltip("The blue rounded panel RectTransform (scale-in target).")]
        public RectTransform panelRect;

        [Tooltip("Dark semi-transparent dim overlay behind the panel.")]
        public Image dimOverlay;

        [Tooltip("CanvasGroup on the panel for alpha fade.")]
        public CanvasGroup panelCanvasGroup;

        // ── Inspector: Header ─────────────────────────────────
        [Header("Header")]
        [Tooltip("Yellow 'Gallery' ribbon label.")]
        public TextMeshProUGUI titleLabel;

        [Tooltip("Close button (X).")]
        public Button closeButton;

        // ── Inspector: Grid ───────────────────────────────────
        [Header("Grid")]
        [Tooltip("ScrollRect that contains the grid content.")]
        public ScrollRect scrollRect;

        [Tooltip("Grid layout group parent (content of ScrollRect).")]
        public RectTransform gridContent;

        [Tooltip("Prefab for a single gallery cell.")]
        public GalleryCellUI cellPrefab;

        // ── Inspector: Sprites ───────────────────────────────
        [Header("Sprites")]
        [Tooltip("Lock icon sprite (icon_lock.png).")]
        public Sprite lockSprite;

        [Tooltip("Green tick / checkmark sprite for unlocked cells.")]
        public Sprite tickSprite;

        [Tooltip("Placeholder sprite shown while image loads.")]
        public Sprite placeholderSprite;

        // ── Inspector: Tunables ──────────────────────────────
        [Header("Tunables")]
        [Tooltip("Scale-in animation duration (seconds). PRD: 0.25s")]
        public float openDuration = 0.25f;

        [Tooltip("Overshoot scale before settling to 1 (PRD: 1.05).")]
        public float overshootScale = 1.05f;

        [Tooltip("Duration of the dim overlay fade.")]
        public float dimFadeDuration = 0.18f;

        [Tooltip("Max alpha of the dim overlay.")]
        public float dimAlpha = 0.65f;

        // ── Runtime ──────────────────────────────────────────
        private bool _isOpen;
        public bool IsOpen => _isOpen;
        private Coroutine _openCoroutine;
        private readonly List<GalleryCellUI> _cells = new List<GalleryCellUI>();

        // Gallery entries in level-reward order (level index → image file name)
        // L1: phone (Mobile), L2: book, L3: love (Heart), L4: dog
        private static readonly string[] GalleryImages =
        {
            "phone",
            "book",
            "love",
            "dog",
        };

        // ── Unity Lifecycle ───────────────────────────────────

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // If the scene started with this panel active in the editor (and Open was not called), hide it.
            if (!_isOpen)
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Opens the gallery panel with scale-in animation.</summary>
        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            gameObject.SetActive(true);
            AudioManager.Instance?.PlayPopupOpen();

            // Reset scroll to top
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

            // Build / refresh cells
            BuildGrid();

            // Animate
            if (_openCoroutine != null) StopCoroutine(_openCoroutine);
            _openCoroutine = StartCoroutine(OpenAnimation());
        }

        /// <summary>Closes the gallery panel.</summary>
        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            if (_openCoroutine != null) StopCoroutine(_openCoroutine);
            if (gameObject.activeInHierarchy)
            {
                _openCoroutine = StartCoroutine(CloseAnimation());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // ── Grid Population ───────────────────────────────────

        private void BuildGrid()
        {
            if (gridContent == null || cellPrefab == null) return;

            // Clear old cells
            foreach (var c in _cells)
                if (c != null) Destroy(c.gameObject);
            _cells.Clear();

            int totalLevels = LevelDatabase.All.Count;
            int totalSlots  = Mathf.Max(totalLevels, GalleryImages.Length);

            for (int i = 0; i < totalSlots; i++)
            {
                int levelIndex = i + 1; // 1-based
                bool unlocked  = SaveManager.Instance.IsGalleryUnlocked(levelIndex);

                string imageName = i < GalleryImages.Length ? GalleryImages[i] : null;
                if (string.IsNullOrEmpty(imageName) && levelIndex <= totalLevels)
                {
                    var lvl = LevelDatabase.Get(levelIndex);
                    if (lvl != null && !string.IsNullOrEmpty(lvl.rewardImageName))
                        imageName = lvl.rewardImageName;
                }

                var cell = Instantiate(cellPrefab, gridContent);
                cell.Setup(imageName, unlocked, lockSprite, tickSprite, placeholderSprite);
                _cells.Add(cell);

                // Load image directly
                if (unlocked && !string.IsNullOrEmpty(imageName))
                {
                    Sprite sprite = LoadGallerySprite(imageName);
                    cell.ShowImage(sprite);
                }
            }
        }



        private Sprite LoadGallerySprite(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return placeholderSprite;

            if (baseName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                baseName = baseName.Substring(0, baseName.Length - 4);

#if UNITY_EDITOR
            string path = $"Assets/_Game/Art/Gallery/{baseName}.png";
            var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            string pathArrows = $"Assets/_Game/Art/Arrows/{baseName}.png";
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(pathArrows);
            if (sp != null) return sp;
#endif
            var res = Resources.Load<Sprite>($"Gallery/{baseName}");
            if (res != null) return res;

            return placeholderSprite;
        }

        // ── Animations ────────────────────────────────────────

        private IEnumerator OpenAnimation()
        {
            if (panelRect == null) yield break;

            // Dim overlay
            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(true);
                dimOverlay.color = new Color(0f, 0f, 0f, 0f);
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            panelRect.localScale = Vector3.one * 0.8f;

            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);

                // Scale: 0.8 → 1.05 → 1.0 (spring curve)
                float scale;
                if (t < 0.65f)
                {
                    float sub = t / 0.65f;
                    scale = Mathf.Lerp(0.8f, overshootScale, Mathf.Sin(sub * Mathf.PI * 0.5f));
                }
                else
                {
                    float sub = (t - 0.65f) / 0.35f;
                    scale = Mathf.Lerp(overshootScale, 1f, Mathf.Sin(sub * Mathf.PI * 0.5f));
                }

                panelRect.localScale = Vector3.one * scale;

                // Fade in dim overlay
                if (dimOverlay != null)
                    dimOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, dimAlpha, t));

                // Fade in panel
                if (panelCanvasGroup != null)
                    panelCanvasGroup.alpha = Mathf.Clamp01(t * 2f); // alpha reaches 1 at t=0.5

                yield return null;
            }

            panelRect.localScale = Vector3.one;
            if (dimOverlay != null) dimOverlay.color = new Color(0f, 0f, 0f, dimAlpha);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
            _openCoroutine = null;
        }

        private IEnumerator CloseAnimation()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            float elapsed = 0f;
            float closeDur = openDuration * 0.6f;

            while (elapsed < closeDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDur);
                float inv = 1f - t;

                if (panelRect != null)
                    panelRect.localScale = Vector3.one * Mathf.Lerp(1f, 0f, t * t);

                if (dimOverlay != null)
                    dimOverlay.color = new Color(0f, 0f, 0f, dimAlpha * inv);

                if (panelCanvasGroup != null)
                    panelCanvasGroup.alpha = inv;

                yield return null;
            }

            gameObject.SetActive(false);
            _openCoroutine = null;
        }
    }
}

