#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using ArrowPuzzle.Data;

namespace ArrowPuzzle.Editor
{
    public static class GenerateTargetLevels
    {
        [MenuItem("Arrow Puzzle/Generate Target Levels (Phone, Book, Love, Dog)", false, 10)]
        public static void GenerateLevels1To4()
        {
            string outFolder = Path.Combine(Application.dataPath, "_Game/Data");
            if (!Directory.Exists(outFolder)) Directory.CreateDirectory(outFolder);

            int[] ids = { 1, 2, 3, 4 };
            int generated = 0;

            foreach (int id in ids)
            {
                var cfg = LevelGenerator.GetConfigForLevel(id);
                LevelGenerationResult bestResult = null;

                for (int s = 1; s <= 100; s++)
                {
                    var res = LevelGenerator.GenerateLevel(cfg, id * 10007 + s * 37);
                    if (res != null && res.success)
                    {
                        bestResult = res;
                        break;
                    }
                }

                if (bestResult != null && bestResult.success)
                {
                    string path = Path.Combine(outFolder, $"level_{id}.json");
                    File.WriteAllText(path, bestResult.jsonString);
                    generated++;
                    Debug.Log($"[GenerateTargetLevels] Generated & Saved Level {id} ({cfg.maskFileName}, reward={cfg.rewardImageName}, arrows={bestResult.arrowCount}, cells={bestResult.cellCount})");
                }
                else
                {
                    Debug.LogError($"[GenerateTargetLevels] Failed to generate solvable level for ID {id} ({cfg.maskFileName})");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[GenerateTargetLevels] Target level generation complete ({generated}/4).");
        }

        [MenuItem("Arrow Puzzle/Levels/Jump to Level 1 (Mobile)", false, 1)]
        public static void SetLevel1() => SetLevel(1);

        [MenuItem("Arrow Puzzle/Levels/Jump to Level 2 (Book)", false, 2)]
        public static void SetLevel2() => SetLevel(2);

        [MenuItem("Arrow Puzzle/Levels/Jump to Level 3 (Love)", false, 3)]
        public static void SetLevel3() => SetLevel(3);

        [MenuItem("Arrow Puzzle/Levels/Jump to Level 4 (Dog)", false, 4)]
        public static void SetLevel4() => SetLevel(4);

        private static void SetLevel(int lvl)
        {
            var save = ArrowPuzzle.Systems.SaveManager.Instance;
            if (save != null)
            {
                save.PendingLevel = lvl;
                save.CurrentLevel = lvl;
                if (save.Data != null)
                {
                    save.Data.highestUnlockedLevel = Mathf.Max(save.Data.highestUnlockedLevel, lvl);
                    save.Save();
                }
            }
            if (Application.isPlaying && ArrowPuzzle.Gameplay.LevelLoader.Instance != null)
            {
                ArrowPuzzle.Gameplay.LevelLoader.LoadLevel(lvl);
            }
            Debug.Log($"[Arrow Puzzle] Set to Level {lvl}!");
        }

        [MenuItem("Arrow Puzzle/Levels/Reset Progress (Start Fresh from Level 1)", false, 20)]
        public static void ResetAllProgress()
        {
            ArrowPuzzle.Systems.SaveManager.Instance?.ResetAll();
            Debug.Log("[Arrow Puzzle] Save data completely wiped! You are now back to Level 1 with 0 stars and locked gallery.");
        }
    }
}
#endif
