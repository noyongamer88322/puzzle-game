// ============================================================
// LevelGeneratorWindow.cs  –  Arrow Slide Puzzle
// Editor Window accessible via 'Tools > Generate Levels'
// Provides controls for:
//   • Batch generating all 30 PRD-compliant solvable levels
//   • Generating custom levels with chosen shape, count, dimensions,
//     color mode, and length range (up to 15-25 cells)
//   • Validating existing generated levels via solver simulation
// PRD Section 7: Assets/_Game/Scripts/Editor/LevelGeneratorWindow.cs
// ============================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ArrowPuzzle.Core;
using ArrowPuzzle.Data;

namespace ArrowPuzzle.Editor
{
    public class LevelGeneratorWindow : EditorWindow
    {
        private static readonly string[] ShapeNames = new string[]
        {
            "Rectangle (Full Grid)",
            "Heart (mask_heart.png)",
            "Phone (mask_phone.png)",
            "Book (mask_book.png)",
            "Diamond (mask_diamond.png)",
            "Star (mask_star.png)",
            "Circle (mask_circle.png)",
            "Shield (mask_shield.png)",
            "Custom Mask File"
        };

        private static readonly string[] ShapeMaskFiles = new string[]
        {
            "",
            "mask_heart.png",
            "mask_phone.png",
            "mask_book.png",
            "mask_diamond.png",
            "mask_star.png",
            "mask_circle.png",
            "mask_shield.png",
            ""
        };

        private static readonly string[] ColorModes = new string[]
        {
            "mixed",
            "orangeMagenta",
            "gradient"
        };

        // ── Window tunables ───────────────────────────────────
        [SerializeField] private int _batchLevelCount = 30;
        [SerializeField] private string _outputFolder = "Assets/_Game/Data";

        // Custom level parameters
        [SerializeField] private int _customLevelId = 1;
        [SerializeField] private string _customChapter = "Hills";
        [SerializeField] private int _selectedShapeIdx = 1; // Heart
        [SerializeField] private string _customMaskFile = "mask_heart.png";
        [SerializeField] private int _customGridWidth = 16;
        [SerializeField] private int _customGridHeight = 16;
        [SerializeField] private int _customMinLength = 2;
        [SerializeField] private int _customMaxLength = 4;
        [SerializeField] private int _selectedColorModeIdx = 2; // gradient
        [SerializeField] private string _customRewardPic = "pic_heart.png";

        // UI state
        private Vector2 _scrollPos;
        private string _logOutput = "Ready to generate solvable levels.\n";
        private int _tab = 0;

        [MenuItem("Tools/Generate Levels")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelGeneratorWindow>("Level Generator");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎯 Solvable Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("PRD 4.6 (Solvable Guarantee) & PRD 4.7 (Difficulty Progression)", EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            _tab = GUILayout.Toolbar(_tab, new string[] { "Batch 30 Levels", "Custom Level", "Validate Levels" });
            EditorGUILayout.Space(8);

            switch (_tab)
            {
                case 0:
                    DrawBatchTab();
                    break;
                case 1:
                    DrawCustomTab();
                    break;
                case 2:
                    DrawValidateTab();
                    break;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generation & Validation Log:", EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(180));
            EditorGUILayout.TextArea(_logOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log"))
            {
                _logOutput = "";
            }
        }

        // ── Batch Generation Tab ──────────────────────────────

        private void DrawBatchTab()
        {
            EditorGUILayout.HelpBox(
                "Generates levels according to PRD Sections 4.6, 4.7, and 4.8:\n" +
                "• L1: 1-2 arrows (small grid)\n" +
                "• L2-3: 3-5 arrows\n" +
                "• L4-5: ~10-25 arrows (Orange + Magenta)\n" +
                "• L6: 40+ arrows on Heart mask (Gradient)\n" +
                "• L7+: Shape levels (40+ arrows, up to 15-25 len, more bends)\n" +
                "• Rewards: L1-3 placeholder, L4 phone, L5 book, L6 heart\n" +
                "• Chapters: 5 levels per chapter (Hills, Forest, Meadow, etc.)\n" +
                "• Every level validated by puzzle simulation before save.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            _batchLevelCount = EditorGUILayout.IntSlider("Number of Levels", _batchLevelCount, 1, 30);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.Space(8);
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.35f);
            if (GUILayout.Button($"🚀 Generate & Save All {_batchLevelCount} Levels", GUILayout.Height(36)))
            {
                ExecuteBatchGenerate();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ExecuteBatchGenerate()
        {
            string absFolder = Path.Combine(Application.dataPath, "_Game/Data");
            if (!Directory.Exists(absFolder))
                Directory.CreateDirectory(absFolder);

            _logOutput += $"\n[{DateTime.Now:HH:mm:ss}] Starting batch generation of {_batchLevelCount} levels...\n";

            int success = 0;
            for (int id = 1; id <= _batchLevelCount; id++)
            {
                var cfg = LevelGenerator.GetConfigForLevel(id);
                var res = LevelGenerator.GenerateLevel(cfg, id * 1009 + 42);

                if (res.success)
                {
                    string path = Path.Combine(absFolder, $"level_{id}.json");
                    File.WriteAllText(path, res.jsonString);
                    success++;
                    _logOutput += $"✓ Level {id} [{cfg.chapter}] shape={cfg.maskFileName} ({cfg.gridWidth}x{cfg.gridHeight}): {res.arrowCount} arrows ({res.cellCount} cells, mode={cfg.colorMode}) -> SOLVED\n";
                }
                else
                {
                    _logOutput += $"✗ Level {id} FAILED: {res.errorMessage}\n";
                }
            }

            AssetDatabase.Refresh();
            _logOutput += $"Batch generation finished: {success}/{_batchLevelCount} levels generated and saved to {absFolder}!\n";
            Debug.Log($"[LevelGeneratorWindow] Batch complete: {success}/{_batchLevelCount} levels generated.");
        }

        // ── Custom Level Tab ──────────────────────────────────

        private void DrawCustomTab()
        {
            EditorGUILayout.HelpBox("Configure and generate an individual solvable level with custom shape, size, and length settings.", MessageType.None);

            _customLevelId = EditorGUILayout.IntField("Level ID", _customLevelId);
            _customChapter = EditorGUILayout.TextField("Chapter Name", _customChapter);

            EditorGUILayout.Space(4);
            int prevShapeIdx = _selectedShapeIdx;
            _selectedShapeIdx = EditorGUILayout.Popup("Board Shape", _selectedShapeIdx, ShapeNames);
            if (_selectedShapeIdx != prevShapeIdx && _selectedShapeIdx < ShapeMaskFiles.Length - 1)
            {
                _customMaskFile = ShapeMaskFiles[_selectedShapeIdx];
                if (_customMaskFile.Contains("heart") || _customMaskFile.Contains("phone") || _customMaskFile.Contains("book"))
                {
                    _customRewardPic = _customMaskFile.Replace("mask_", "pic_");
                }
            }

            if (_selectedShapeIdx == ShapeNames.Length - 1)
            {
                _customMaskFile = EditorGUILayout.TextField("Custom Mask File", _customMaskFile);
            }
            else if (!string.IsNullOrEmpty(_customMaskFile))
            {
                EditorGUILayout.LabelField("Mask File:", _customMaskFile, EditorStyles.miniLabel);
            }

            _customGridWidth = EditorGUILayout.IntSlider("Grid Width", _customGridWidth, 3, 30);
            _customGridHeight = EditorGUILayout.IntSlider("Grid Height", _customGridHeight, 3, 30);

            EditorGUILayout.Space(4);
            _customMinLength = EditorGUILayout.IntSlider("Min Arrow Length", _customMinLength, 2, 8);
            _customMaxLength = EditorGUILayout.IntSlider("Max Arrow Length", _customMaxLength, _customMinLength, 25);

            EditorGUILayout.Space(4);
            _selectedColorModeIdx = EditorGUILayout.Popup("Color Mode", _selectedColorModeIdx, ColorModes);
            _customRewardPic = EditorGUILayout.TextField("Reward Image", _customRewardPic);

            EditorGUILayout.Space(8);
            GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
            if (GUILayout.Button($"🎯 Generate Custom Level {_customLevelId}", GUILayout.Height(34)))
            {
                ExecuteCustomGenerate();
            }
            GUI.backgroundColor = Color.white;
        }

        private void ExecuteCustomGenerate()
        {
            string absFolder = Path.Combine(Application.dataPath, "_Game/Data");
            if (!Directory.Exists(absFolder))
                Directory.CreateDirectory(absFolder);

            string mask = string.IsNullOrEmpty(_customMaskFile) ? null : _customMaskFile;
            string colorMode = ColorModes[_selectedColorModeIdx];

            var cfg = new LevelGenerationConfig(
                _customLevelId,
                _customChapter,
                mask,
                _customGridWidth,
                _customGridHeight,
                _customMinLength,
                _customMaxLength,
                colorMode,
                _customRewardPic
            );

            _logOutput += $"\n[{DateTime.Now:HH:mm:ss}] Generating Custom Level {_customLevelId} ({_customGridWidth}x{_customGridHeight}, mask={mask})...\n";

            var res = LevelGenerator.GenerateLevel(cfg);
            if (res.success)
            {
                string path = Path.Combine(absFolder, $"level_{_customLevelId}.json");
                File.WriteAllText(path, res.jsonString);
                AssetDatabase.Refresh();
                _logOutput += $"✓ Custom Level {_customLevelId} SUCCESS: {res.arrowCount} arrows ({res.cellCount} cells, mode={colorMode}) saved to level_{_customLevelId}.json!\n";
            }
            else
            {
                _logOutput += $"✗ Custom Level {_customLevelId} FAILED: {res.errorMessage}\n";
            }
        }

        // ── Validation Tab ────────────────────────────────────

        private void DrawValidateTab()
        {
            EditorGUILayout.HelpBox("Verifies all level JSON files currently in Assets/_Game/Data/ by executing the puzzle solver simulation on each.", MessageType.Info);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("🔍 Validate All Levels In Data Folder", GUILayout.Height(34)))
            {
                ExecuteValidateAll();
            }
        }

        private void ExecuteValidateAll()
        {
            string absFolder = Path.Combine(Application.dataPath, "_Game/Data");
            if (!Directory.Exists(absFolder))
            {
                _logOutput += $"Folder not found: {absFolder}\n";
                return;
            }

            var files = Directory.GetFiles(absFolder, "level_*.json");
            _logOutput += $"\n[{DateTime.Now:HH:mm:ss}] Validating {files.Length} levels...\n";

            int validCount = 0;
            foreach (var f in files)
            {
                try
                {
                    string json = File.ReadAllText(f);
                    var dto = JsonUtility.FromJson<LevelDataJson>(json);
                    if (dto == null)
                    {
                        _logOutput += $"✗ {Path.GetFileName(f)}: JSON deserialize returned null!\n";
                        continue;
                    }

                    // Load mask
                    HashSet<Vector2Int> maskCells = null;
                    if (!string.IsNullOrEmpty(dto.maskFileName))
                    {
                        maskCells = MaskLoader.LoadMask(dto.maskFileName, dto.gridWidth, dto.gridHeight);
                    }
                    if (maskCells == null || maskCells.Count == 0)
                    {
                        maskCells = new HashSet<Vector2Int>();
                        for (int y = 0; y < dto.gridHeight; y++)
                            for (int x = 0; x < dto.gridWidth; x++)
                                maskCells.Add(new Vector2Int(x, y));
                    }

                    bool solvable = LevelGenerator.ValidateAndSolveLevel(dto.gridWidth, dto.gridHeight, maskCells, dto.arrows);
                    if (solvable)
                    {
                        validCount++;
                        _logOutput += $"✓ Level {dto.id} ({dto.arrows.Count} arrows, {dto.gridWidth}x{dto.gridHeight}, {dto.colorMode}): SOLVABLE ✓\n";
                    }
                    else
                    {
                        _logOutput += $"✗ Level {dto.id}: Solver deadlock or incomplete mask coverage!\n";
                    }
                }
                catch (Exception ex)
                {
                    _logOutput += $"✗ Error checking {Path.GetFileName(f)}: {ex.Message}\n";
                }
            }

            _logOutput += $"Validation complete: {validCount}/{files.Length} levels fully verified and solvable!\n";
        }
    }
}
#endif

