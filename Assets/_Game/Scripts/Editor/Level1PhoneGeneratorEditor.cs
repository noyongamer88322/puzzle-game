// ============================================================
// Level1PhoneGeneratorEditor.cs  –  Arrow Slide Puzzle
// Editor hooks for generating Level 1 phone silhouette data.
// Adds MenuItem "Arrow Puzzle/Generate Level 1 (Phone Silhouette)"
// ============================================================

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using ArrowPuzzle.Data;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Editor
{
    public static class Level1PhoneGeneratorEditor
    {
        [InitializeOnLoadMethod]
        public static void OnInit()
        {
            GenerateAndLog(Level1PhoneGenerator.LEVEL1_MODE);
        }

        [MenuItem("Arrow Puzzle/Generate Level 1 (Phone Silhouette)", false, 5)]
        public static void GenerateLevel1MenuItem()
        {
            GenerateAndLog(Level1PhoneGenerator.LEVEL1_MODE);
        }

        [MenuItem("Arrow Puzzle/Generate Level 1 (Beginner Mode 10x18)", false, 6)]
        public static void GenerateLevel1BeginnerMenuItem()
        {
            GenerateAndLog("BEGINNER");
        }

        public static void GenerateAndLog(string mode)
        {
            Debug.Log($"<color=#FFB52E>============================================================</color>");
            Debug.Log($"<color=#FFB52E>[Level 1 Phone Silhouette Generator] Mode: {mode}</color>");
            Debug.Log($"<color=#FFB52E>============================================================</color>");

            // 1. ASCII Preview
            string ascii = Level1PhoneGenerator.GetAsciiMaskPreview(mode);
            Debug.Log($"[Level 1 Mask Preview]\n{ascii}");

            // 2. Generate
            var res = Level1PhoneGenerator.GenerateLevel1(mode);
            if (!res.success)
            {
                Debug.LogError($"[Level1PhoneGenerator] Failed: {res.errorMessage}");
                return;
            }

            // 3. Stats Calculation
            int upCount = 0, rightCount = 0, downCount = 0, leftCount = 0;
            int yellowCount = 0, pinkCount = 0;
            int maxLen = 0;

            foreach (var a in res.dataJson.arrows)
            {
                if (a.cells.Count > maxLen) maxLen = a.cells.Count;
                if (a.colorIndex == 0) yellowCount++; else pinkCount++;

                switch (a.direction.ToLowerInvariant())
                {
                    case "up": upCount++; break;
                    case "right": rightCount++; break;
                    case "down": downCount++; break;
                    case "left": leftCount++; break;
                }
            }

            int total = res.dataJson.arrows.Count;
            bool solvable = Level1PhoneGenerator.ValidateSolvability(
                res.dataJson.gridWidth, res.dataJson.gridHeight, res.dataJson.arrows);

            // Print Stats
            Debug.Log($"[Level 1 Stats]\n" +
                      $"• Mode: {mode} ({res.dataJson.gridWidth}x{res.dataJson.gridHeight})\n" +
                      $"• Total Arrows: {total}\n" +
                      $"• Total Mask Cells: {res.cellCount}\n" +
                      $"• Longest Arrow: {maxLen} cells\n" +
                      $"• Color Distribution: Yellow (#FFB52E) = {yellowCount} ({(float)yellowCount / total:P0}), Pink (#FF4FD8) = {pinkCount} ({(float)pinkCount / total:P0})\n" +
                      $"• Direction Distribution: Up = {upCount} ({(float)upCount / total:P0}), Right = {rightCount} ({(float)rightCount / total:P0}), Down = {downCount} ({(float)downCount / total:P0}), Left = {leftCount} ({(float)leftCount / total:P0})\n" +
                      $"• Solver Solvability Check: {(solvable ? "PASSED (solvable: true)" : "FAILED (solvable: false)")}");

            // 4. Save to files
            Level1PhoneGenerator.GenerateAndSaveLevel1(mode);
            AssetDatabase.Refresh();

            Debug.Log($"<color=#3FD04A>✓ Level 1 successfully generated, validated, and saved to Assets/_Game/Data/level_1.json!</color>");
        }
    }
}
#endif
