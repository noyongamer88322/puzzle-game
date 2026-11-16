// ============================================================
// LevelLoader.cs  –  Arrow Slide Puzzle
// Responsible for loading puzzle levels from shape masks and JSON data:
//   • Loads LevelData from Assets/_Game/Data/level_{id}.json
//   • Loads active shape mask via MaskLoader
//   • Reconfigures GridManager and DotGrid to shape bounds
//   • Spawns ArrowView instances with staggered spawn animations
//   • Sets arrow colours based on colorMode (mixed, orangeMagenta, gradient)
//   • Provides Debug switching via Keys (1, 2, 3, R) and OnGUI buttons
// PRD Section 7: Assets/_Game/Scripts/Gameplay
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ArrowPuzzle.Core;
using ArrowPuzzle.Data;

namespace ArrowPuzzle.Gameplay
{
    [DisallowMultipleComponent]
    public class LevelLoader : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static LevelLoader Instance { get; private set; }

        // ── Inspector tunables ────────────────────────────────
        [Header("Startup Level")]
        [Tooltip("Level ID to load on scene start.")]
        public int initialLevelId = 1;

        [Header("Visual Tunables")]
        [Tooltip("Delay between each arrow's spawn animation (seconds).")]
        public float spawnStaggerDelay = 0.08f;

        [Tooltip("Optional ArrowView prefab (if null, created at runtime).")]
        public ArrowView arrowViewPrefab;

        [Header("Debug Controls")]
        [Tooltip("Show on-screen debug buttons for switching levels.")]
        public bool showDebugUI = false;

        // ── Runtime State ─────────────────────────────────────
        public int CurrentLevelId { get; private set; }
        public LevelData CurrentLevelData { get; private set; }
        public HashSet<Vector2Int> CurrentShapeCells { get; private set; }

        private readonly List<ArrowView> _spawnedViews = new List<ArrowView>();

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            int lvl = initialLevelId;
            if (ArrowPuzzle.Systems.SaveManager.Instance != null && ArrowPuzzle.Systems.SaveManager.Instance.PendingLevel > 0)
            {
                lvl = ArrowPuzzle.Systems.SaveManager.Instance.PendingLevel;
            }
            else if (ArrowPuzzle.GameManager.Instance != null && ArrowPuzzle.GameManager.Instance.ActiveLevel != null)
            {
                lvl = ArrowPuzzle.GameManager.Instance.ActiveLevel.levelIndex;
            }
            LoadLevel(lvl);
        }

        private void Update()
        {
            // Debug key switching: 1-9 for levels 1-9, [ and ] or Left/Right arrows for Prev/Next, R to reload
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                LoadLevel(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                LoadLevel(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                LoadLevel(3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                LoadLevel(4);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                LoadLevel(5);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                LoadLevel(6);
            }
            else if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                LoadLevel(Mathf.Max(1, CurrentLevelId - 1));
            }
            else if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                LoadLevel(Mathf.Min(30, CurrentLevelId + 1));
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                LoadLevel(CurrentLevelId);
            }
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Loads and instantiates the specified level by ID in the Game scene.
        /// </summary>
        public static void LoadLevel(int levelId)
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<LevelLoader>();
                if (Instance == null)
                {
                    var go = new GameObject("LevelLoader");
                    Instance = go.AddComponent<LevelLoader>();
                }
            }

            Instance.ExecuteLoadLevel(levelId);
        }

        // ── Level Loading Execution ───────────────────────────

        public void ExecuteLoadLevel(int levelId)
        {
            CurrentLevelId = levelId;
            Debug.Log($"[LevelLoader] Loading Level {levelId}...");

            // 1. Locate and read JSON file
            string json = LoadLevelJsonText(levelId);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[LevelLoader] Could not find JSON for level {levelId}!");
                return;
            }

            var levelData = LevelData.FromJson(json);
            if (levelData == null)
            {
                Debug.LogError($"[LevelLoader] Failed to deserialize LevelData for level {levelId}!");
                return;
            }
            CurrentLevelData = levelData;

            // 2. Load shape mask from Masks/ folder
            CurrentShapeCells = MaskLoader.LoadMask(levelData.maskFileName, levelData.gridWidth, levelData.gridHeight);
            Debug.Log($"[LevelLoader] Shape mask '{levelData.maskFileName}' loaded with {CurrentShapeCells.Count} active cells.");

            // 3. Configure GridManager and DotGrid
            var grid = GridManager.Instance;
            if (grid == null)
            {
                Debug.LogError("[LevelLoader] GridManager.Instance is null!");
                return;
            }
            grid.SetGridDimensions(levelData.gridWidth, levelData.gridHeight, CurrentShapeCells);

            // 4. Clean up existing arrows in scene
            ClearExistingArrows();

            // 5. Reset GameplayController state and hearts
            if (GameplayController.Instance != null)
            {
                GameplayController.Instance.ClearRegisteredArrows();
                GameplayController.Instance.InitHearts();
                GameplayController.Instance.IsBoosterRunning = false;
            }

            // 6. Spawn arrows from LevelData
            SpawnArrows(levelData, grid);

            // 7. Update Gameplay UI HUD
            if (ArrowPuzzle.UI.GameplayHUD.Instance == null)
            {
                var hudPrefab = Resources.Load<GameObject>("UI/Canvas_GameplayHUD");
                if (hudPrefab != null)
                {
                    Instantiate(hudPrefab);
                }
            }
            ArrowPuzzle.UI.GameplayHUD.Instance?.ApplyLevelData(levelData);

            // 8. Update Grandma Tutorial
            if (ArrowPuzzle.UI.TutorialController.Instance == null)
            {
                var tutorialPrefab = Resources.Load<GameObject>("UI/Canvas_Tutorial");
                if (tutorialPrefab != null)
                {
                    Instantiate(tutorialPrefab);
                }
                else
                {
                    var tutorialGO = new GameObject("Canvas_Tutorial");
                    tutorialGO.AddComponent<ArrowPuzzle.UI.TutorialController>();
                }
            }
            ArrowPuzzle.UI.TutorialController.Instance?.OnLevelLoaded(levelId);

            Debug.Log($"[LevelLoader] Successfully loaded Level {levelId} ({levelData.chapterName}, {levelData.arrows.Count} arrows, mode={levelData.colorMode}).");
        }

        private string LoadLevelJsonText(int levelId)
        {
            // Primary path: Assets/_Game/Data/level_{id}.json
            string p1 = Path.Combine(Application.dataPath, "_Game/Data", $"level_{levelId}.json");
            if (File.Exists(p1)) return File.ReadAllText(p1);

            // Alternative path: Assets/Data/level_{id}.json
            string p2 = Path.Combine(Application.dataPath, "Data", $"level_{levelId}.json");
            if (File.Exists(p2)) return File.ReadAllText(p2);

            // Resources fallback
            TextAsset ta = Resources.Load<TextAsset>($"Levels/level_{levelId}");
            if (ta != null) return ta.text;

            return null;
        }

        private void ClearExistingArrows()
        {
            // Destroy tracked views
            foreach (var view in _spawnedViews)
            {
                if (view != null && view.gameObject != null)
                    Destroy(view.gameObject);
            }
            _spawnedViews.Clear();

            // Destroy any other ArrowView instances in scene
            var remainingViews = FindObjectsOfType<ArrowView>();
            foreach (var v in remainingViews)
            {
                if (v != null && v.gameObject != null)
                    Destroy(v.gameObject);
            }
        }

        private void SpawnArrows(LevelData levelData, GridManager grid)
        {
            var arrows = levelData.arrows;
            if (arrows == null || arrows.Count == 0) return;

            // Configure gradient values if colorMode is gradient
            bool isGradient = string.Equals(levelData.colorMode, "gradient", StringComparison.OrdinalIgnoreCase);
            if (isGradient)
            {
                for (int i = 0; i < arrows.Count; i++)
                {
                    arrows[i].useGradient = true;
                    arrows[i].gradientT   = arrows.Count > 1 ? (float)i / (arrows.Count - 1) : 0f;
                }
            }

            for (int i = 0; i < arrows.Count; i++)
            {
                var arrowData = arrows[i];
                var view = CreateArrowView(arrowData);
                view.Init(arrowData, grid);
                view.PlaySpawnAnim(i * spawnStaggerDelay);

                _spawnedViews.Add(view);

                // Register with GameplayController
                GameplayController.Instance?.RegisterArrow(view);
            }
        }

        private ArrowView CreateArrowView(ArrowData data)
        {
            if (arrowViewPrefab != null)
            {
                return Instantiate(arrowViewPrefab, transform);
            }

            var go = new GameObject($"Arrow_{data.direction}_{data.Head.x}_{data.Head.y}");
            go.transform.SetParent(transform, false);
            return go.AddComponent<ArrowView>();
        }

    }
}

