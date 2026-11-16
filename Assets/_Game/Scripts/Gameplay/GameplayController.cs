// ============================================================
// GameplayController.cs  –  Arrow Slide Puzzle
// Core gameplay controller for active level sessions:
//   • Tap detection and filtering
//   • Free-path raycasting check
//   • Snake slide coordination (concurrent sliding supported)
//   • Blocked tap feedback (red color, shake, vignette, vibration, heart -1)
//   • Hearts management (3 hearts, per-level reset, full/empty slots)
//   • Events: OnArrowCleared, OnBlocked, OnHeartChanged, OnLevelWon, OnLevelLost
//   • Win / Lose checks and logging
// PRD Section 7: _Game/Scripts/Gameplay
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    public class GameplayController : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static GameplayController Instance { get; private set; }

        // ── Events ────────────────────────────────────────────
        public static event Action<ArrowData> OnArrowCleared;
        public static event Action<ArrowData> OnBlocked;
        public static event Action<int, int>   OnHeartChanged; // (currentHearts, maxHearts)
        public static event Action             OnLevelWon;
        public static event Action             OnLevelLost;

        /// <summary>Raises OnArrowCleared from outside GameplayController (e.g. BoosterController).</summary>
        public static void RaiseArrowCleared(ArrowData data) => OnArrowCleared?.Invoke(data);

        // ── Inspector tunables ────────────────────────────────
        [Header("Hearts")]
        [Tooltip("Max and starting hearts per level (PRD: 3, resets per level).")]
        public int maxHearts = 3;

        [Tooltip("Full heart sprite slot.")]
        public Sprite fullHeartSprite;

        [Tooltip("Empty heart (faded beige/brownish) sprite slot.")]
        public Sprite emptyHeartSprite;

        [Header("Concurrency & Tap Rules")]
        [Tooltip("Whether separate free arrows can slide concurrently.")]
        public bool allowConcurrentSliding = true;

        [Tooltip("Minimum time (seconds) between processing taps to prevent spam.")]
        public float tapDebounceTime = 0.08f;

        // ── Runtime state ─────────────────────────────────────
        public int  CurrentHearts         { get; private set; }
        public bool IsLevelEnded          => _isWon || _isLost;
        public bool IsBoosterRunning      { get; set; }
        public int  RemainingArrowsCount  => _activeArrows.Count;

        private readonly List<ArrowView> _activeArrows = new List<ArrowView>();
        private bool  _isWon;
        private bool  _isLost;
        private float _lastTapTime;
        private Camera _cam;

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _cam = Camera.main;
            InitHearts();
            TryLoadDefaultHeartSprites();

            // Ensure ScreenVignetteFlash is present in scene
            if (ScreenVignetteFlash.Instance == null)
            {
                var flashGO = new GameObject("ScreenVignetteFlash");
                flashGO.AddComponent<ScreenVignetteFlash>();
            }
        }

        private void Start()
        {
            // Initial notification for listeners
            OnHeartChanged?.Invoke(CurrentHearts, maxHearts);
        }

        private void Update()
        {
            if (IsLevelEnded || IsBoosterRunning) return;

            // Tutorial input gating: in steps 2 & 3, board taps are disabled
            if (TutorialController.Instance != null && TutorialController.Instance.IsTutorialActive)
            {
                if (TutorialController.Instance.CurrentStep > 1)
                    return;
            }

            // Handle touch or mouse tap
            if (Input.GetMouseButtonDown(0))
            {
                ProcessTapAtScreenPosition(Input.mousePosition);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1.0f; // Safety reset
        }

        // ── Heart management ──────────────────────────────────

        /// <summary>Resets hearts to maxHearts (called on level start/reset).</summary>
        public void InitHearts()
        {
            CurrentHearts = maxHearts;
            _isWon  = false;
            _isLost = false;
        }

        private void TryLoadDefaultHeartSprites()
        {
#if UNITY_EDITOR
            if (fullHeartSprite == null)
            {
                fullHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_heart_full.png");
            }
            if (emptyHeartSprite == null)
            {
                emptyHeartSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Game/Art/UI/icon_heart_empty.png");
            }
#endif
        }

        // ── Arrow registration ────────────────────────────────

        public void RegisterArrow(ArrowView view)
        {
            if (view != null && !_activeArrows.Contains(view))
            {
                _activeArrows.Add(view);
            }
        }

        public void UnregisterArrow(ArrowView view)
        {
            if (view != null)
            {
                _activeArrows.Remove(view);
            }
        }

        public void ClearRegisteredArrows()
        {
            _activeArrows.Clear();
        }

        // ── Free Check Logic ──────────────────────────────────

        /// <summary>
        /// Checks if the given arrow has a clear ray from its head in its direction to the grid edge.
        /// Returns true if all cells along the ray to the boundary are empty.
        /// </summary>
        public bool CheckIsFree(ArrowData arrow)
        {
            if (arrow == null || arrow.cells == null || arrow.cells.Count == 0)
                return false;

            var grid = GridManager.Instance;
            if (grid == null) return false;

            // Build set of currently occupied cells from all active, non-sliding arrows
            var occupied = new HashSet<Vector2Int>();
            foreach (var a in _activeArrows)
            {
                if (a == null || a.IsCleared) continue;
                // If concurrent sliding is active, arrows already sliding are leaving the board
                if (a.IsSliding) continue;

                var data = a.Data;
                if (data == null || data.cells == null) continue;

                foreach (var c in data.cells)
                    occupied.Add(c);
            }

            Vector2Int step = arrow.direction.ToVector();
            Vector2Int cur  = arrow.Head + step;

            // Traverse along ray until outside grid bounds
            while (grid.IsInBounds(cur.x, cur.y))
            {
                // If any occupied cell is encountered, path is blocked
                if (occupied.Contains(cur))
                    return false;

                cur += step;
            }

            // Ray reached grid edge without hitting any arrow -> FREE!
            return true;
        }

        // ── Tap Processing ────────────────────────────────────

        private void ProcessTapAtScreenPosition(Vector3 screenPos)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector2 worldPos = _cam.ScreenToWorldPoint(screenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            // Fallback: slight radius around tap point so touches on thin/dense arrows register reliably
            if (hit == null && GridManager.Instance != null)
            {
                hit = Physics2D.OverlapCircle(worldPos, GridManager.Instance.CellSize * 0.35f);
            }

            if (hit != null)
            {
                var arrowView = hit.GetComponentInParent<ArrowView>();
                if (arrowView != null)
                {
                    HandleArrowTap(arrowView);
                }
            }
        }

        /// <summary>
        /// Handles a tap on a specific ArrowView.
        /// Enforces slide, blocked, concurrency, and win/lose rules.
        /// </summary>
        public void HandleArrowTap(ArrowView view)
        {
            if (IsLevelEnded || IsBoosterRunning) return;
            if (view == null || view.IsSliding || view.IsBlockedAnim || view.IsCleared) return;

            // Tutorial input gating: in step 1, player must tap a free arrow to learn how it flies away
            if (TutorialController.Instance != null && TutorialController.Instance.IsTutorialActive)
            {
                if (TutorialController.Instance.CurrentStep == 1)
                {
                    bool isFreeArrow = CheckIsFree(view.Data);
                    if (isFreeArrow)
                    {
                        // Any free arrow tapped during tutorial step 1 successfully completes the tutorial!
                        TutorialController.Instance.CompleteCurrentStep();
                    }
                    else
                    {
                        // Wrong (blocked) arrow tapped during tutorial step 1 -> provide clear visual/audio feedback!
                        AudioManager.Instance?.PlayBlockedTap();
                        view.PlayBlocked();
                        if (ScreenVignetteFlash.Instance != null)
                            ScreenVignetteFlash.Instance.TriggerFlash();
                        return;
                    }
                }
                else
                {
                    return; // Ignore arrow taps during booster tutorials (step 2 Hint, step 3 Magnet)
                }
            }

            // Debounce rapid tap jitter
            if (Time.time < _lastTapTime + tapDebounceTime) return;
            _lastTapTime = Time.time;

            bool isFree = CheckIsFree(view.Data);

            if (isFree)
            {
                // ── FREE ARROW: Slide Out ──────────────────────
                // If concurrent sliding is disabled and another arrow is sliding, ignore
                if (!allowConcurrentSliding && HasAnySlidingArrow())
                    return;

                // PRD 5: Last arrow slow-mo
                if (_activeArrows.Count <= 1)
                {
                    StartCoroutine(LastArrowSlowMoRoutine());
                }

                AudioManager.Instance?.PlayArrowSlide();

                view.SlideOut(() =>
                {
                    OnArrowSlideComplete(view);
                });
            }
            else
            {
                // ── BLOCKED ARROW: Feedback & Penalty ──────────
                AudioManager.Instance?.PlayBlockedTap();

                // 1. Arrow turns red (#FF1A1A) and shakes (0.25s)
                view.PlayBlocked();

                // 2. Screen-edge pink/red vignette flash & vibration
                if (ScreenVignetteFlash.Instance != null)
                    ScreenVignetteFlash.Instance.TriggerFlash();

                // 3. Lose 1 heart
                CurrentHearts = Mathf.Max(0, CurrentHearts - 1);
                AudioManager.Instance?.PlayHeartBreak();
                OnHeartChanged?.Invoke(CurrentHearts, maxHearts);
                OnBlocked?.Invoke(view.Data);

                // 4. Check Lose
                if (CurrentHearts <= 0 && !_isLost)
                {
                    _isLost = true;
                    Debug.Log("[Gameplay] Level Lost: Out of hearts (0)!");
                    OnLevelLost?.Invoke();
                }
            }
        }

        public bool HasAnySlidingArrow()
        {
            foreach (var a in _activeArrows)
            {
                if (a != null && a.IsSliding) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all currently free arrows ordered by solve order (PRD 4.6).
        /// </summary>
        public List<ArrowView> GetFreeArrowsInSolveOrder()
        {
            var freeList = new List<ArrowView>();
            if (_activeArrows == null || _activeArrows.Count == 0) return freeList;

            for (int i = 0; i < _activeArrows.Count; i++)
            {
                var a = _activeArrows[i];
                if (a == null || a.IsCleared || a.IsSliding || a.IsBlockedAnim) continue;
                if (CheckIsFree(a.Data))
                {
                    freeList.Add(a);
                }
            }

            if (freeList.Count <= 1) return freeList;

            // Sort freeList according to solve order.
            // In LevelGenerator (PRD 4.6), arrows were placed in reverse of solve order (last placed = exits first).
            var levelArrows = LevelLoader.Instance?.CurrentLevelData?.arrows;
            if (levelArrows != null)
            {
                freeList.Sort((a, b) =>
                {
                    int idxA = levelArrows.IndexOf(a.Data);
                    int idxB = levelArrows.IndexOf(b.Data);
                    if (idxA >= 0 && idxB >= 0)
                        return idxB.CompareTo(idxA); // descending
                    return 0; // no id field on ArrowData; keep original order
                });
            }

            return freeList;
        }

        /// <summary>
        /// Checks if all remaining arrows are cleared to trigger level win.
        /// </summary>
        public void CheckWinCondition()
        {
            if (_activeArrows.Count == 0 && !_isLost && !_isWon)
            {
                _isWon = true;
                Debug.Log("[Gameplay] Level Won: All arrows cleared!");
                AudioManager.Instance?.PlayLevelClear();
                OnLevelWon?.Invoke();
            }
        }

        private void OnArrowSlideComplete(ArrowView view)
        {
            _activeArrows.Remove(view);

            if (view != null && view.Data != null)
                OnArrowCleared?.Invoke(view.Data);

            // Check Win: all arrows cleared and not lost
            if (_activeArrows.Count == 0 && !_isLost && !_isWon)
            {
                _isWon = true;
                Debug.Log("[Gameplay] Level Won: All arrows cleared!");
                AudioManager.Instance?.PlayLevelClear();
                OnLevelWon?.Invoke();
            }
        }

        private System.Collections.IEnumerator LastArrowSlowMoRoutine()
        {
            Time.timeScale = 0.35f;
            yield return new WaitForSecondsRealtime(0.55f);
            Time.timeScale = 1.0f;
        }

        // ── Public Helpers ────────────────────────────────────

        /// <summary>Returns true if the arrow at the given view is free right now.</summary>
        public bool IsArrowFree(ArrowView view) => view != null && CheckIsFree(view.Data);

        // ── Revive ────────────────────────────────────────────────────

        /// <summary>
        /// Restores 1 heart and un-flags the lost state so the level can continue.
        /// Called by GameOverPopupUI after a successful coin-spend or reward-ad revive.
        /// </summary>
        public void ReviveWithOneHeart()
        {
            CurrentHearts = Mathf.Clamp(CurrentHearts + 1, 0, maxHearts);
            _isLost = false;
            OnHeartChanged?.Invoke(CurrentHearts, maxHearts);
            Debug.Log($"[GameplayController] Revived – hearts restored to {CurrentHearts}/{maxHearts}.");
        }
    }
}
