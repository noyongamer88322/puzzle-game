// ============================================================
// AllScreensBuilder.cs  –  Arrow Slide Puzzle Editor Tool
// Master builder that builds and updates all 8 UI screens, prefabs,
// and scenes in one automated pass:
//   1. Generates UI Sprite placeholders / assets
//   2. Builds SceneTransition prefab & Splash.unity
//   3. Builds Settings popup prefab
//   4. Builds Gallery popup & cell prefabs
//   5. Builds Game Over popup prefab
//   6. Builds Tutorial UI prefab
//   7. Builds Level Cleared UI prefab
//   8. Builds Gameplay HUD prefab & sets up Game.unity
//   9. Builds Map scene UI & sets up Map.unity
//  10. Copies prefabs to Assets/Prefabs/UI/
//  11. Ensures Build Settings contains Splash, Map, Game
// ============================================================

#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Editor
{
    public static class AllScreensBuilder
    {
        private const string PrefabsSrcDir = "Assets/_Game/Prefabs/UI";
        private const string PrefabsDstDir = "Assets/Prefabs/UI";
        private const string ResourcesDir  = "Assets/Resources/UI";

        private const string SplashScenePath = "Assets/_Game/Scenes/Splash.unity";
        private const string MapScenePath    = "Assets/_Game/Scenes/Map.unity";
        private const string GameScenePath   = "Assets/_Game/Scenes/Game.unity";

        [InitializeOnLoadMethod]
        private static void AutoRunOnce()
        {
            if (Application.isPlaying) return;

            string marker = "Temp/AllScreensBuilt.marker";
            if (!File.Exists(marker))
            {
                EditorApplication.delayCall += () =>
                {
                    if (Application.isPlaying) return;
                    if (File.Exists(marker)) return;
                    try
                    {
                        File.WriteAllText(marker, System.DateTime.UtcNow.ToString());
                    }
                    catch {}
                    BuildAllScreens();
                };
            }
        }

        [MenuItem("Arrow Puzzle/Build All 8 UI Screens & Scenes", false, 50)]
        public static void BuildAllScreens()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[AllScreensBuilder] Skipping build – cannot run EditorSceneManager during Play Mode. Use the menu item in Edit Mode instead.");
                return;
            }
            Debug.Log("<color=#22C55E><b>[AllScreensBuilder] Starting Full UI System Build for 8 Screens...</b></color>");

            // 1. Generate UI assets
            UIAssetGenerator.GenerateAll();
            SplashAssetGenerator.GenerateAllSprites();

            // 2. Build Settings Popup Prefab
            SettingsPanelBuilder.Build();

            // 3. Build Gallery Popup & Cell Prefabs
            GalleryPanelBuilder.BuildGalleryPanel();

            // 4. Build Game Over Popup Prefab
            GameOverPopupBuilder.Build();

            // 5. Build Tutorial UI Prefab
            TutorialUIBuilder.BuildTutorialUI();

            // 6. Build Level Cleared UI Prefab
            LevelClearedUIBuilder.BuildLevelClearedUI();

            // 7. Build SceneTransition Prefab & Splash Scene
            SplashSceneBuilder.Build();

            // 8. Build Gameplay HUD Prefab & Configure Game.unity
            ConfigureGameScene();

            // 9. Build Map Scene UI & Configure Map.unity
            MapSceneBuilder.BuildMapUI();

            // 10. Copy prefabs to Assets/Prefabs/UI for convenience
            CopyPrefabsToAssetsPrefabsDir();

            // 11. Ensure Build Settings has Splash -> Map -> Game
            EnsureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#22C55E><b>[AllScreensBuilder] ALL 8 SCREENS & POPUPS BUILT SUCCESSFULLY!</b></color>");
        }

        private static void ConfigureGameScene()
        {
            // First build the HUD prefab
            GameplayUIBuilder.BuildHUD();

            // Open Game Scene
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            // Clean any duplicate canvases
            var existingHUD = Object.FindObjectsOfType<GameplayHUD>();
            foreach (var h in existingHUD)
            {
                Object.DestroyImmediate(h.gameObject);
            }
            var existingWin = Object.FindObjectsOfType<LevelClearedUI>();
            foreach (var w in existingWin)
            {
                Object.DestroyImmediate(w.gameObject);
            }
            var existingTut = Object.FindObjectsOfType<TutorialController>();
            foreach (var t in existingTut)
            {
                Object.DestroyImmediate(t.gameObject);
            }

            // 1. Add Canvas_GameplayHUD
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Canvas_GameplayHUD.prefab");
            GameObject hudGO = null;
            if (hudPrefab != null)
            {
                hudGO = PrefabUtility.InstantiatePrefab(hudPrefab) as GameObject;
                hudGO.name = "Canvas_GameplayHUD";
            }
            else
            {
                hudGO = new GameObject("Canvas_GameplayHUD");
                hudGO.AddComponent<GameplayHUD>();
            }

            // 2. Add Panel_Settings under Gameplay HUD
            var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Panel_Settings.prefab");
            if (settingsPrefab != null && hudGO != null)
            {
                var settingsGO = PrefabUtility.InstantiatePrefab(settingsPrefab, hudGO.transform) as GameObject;
                settingsGO.name = "Panel_Settings";
                settingsGO.SetActive(false);
            }

            // 3. Add Popup_GameOver under Gameplay HUD
            var gameOverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Popup_GameOver.prefab");
            if (gameOverPrefab != null && hudGO != null)
            {
                var goGO = PrefabUtility.InstantiatePrefab(gameOverPrefab, hudGO.transform) as GameObject;
                goGO.name = "Popup_GameOver";
                goGO.SetActive(false);
            }

            // 4. Add Canvas_Tutorial
            var tutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Canvas_Tutorial.prefab");
            if (tutPrefab != null)
            {
                var tutGO = PrefabUtility.InstantiatePrefab(tutPrefab) as GameObject;
                tutGO.name = "Canvas_Tutorial";
            }

            // 5. Add Canvas_LevelCleared
            var winPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/Canvas_LevelCleared.prefab");
            if (winPrefab != null)
            {
                var winGO = PrefabUtility.InstantiatePrefab(winPrefab) as GameObject;
                winGO.name = "Canvas_LevelCleared";
            }

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AllScreensBuilder] Game scene configured with HUD, Settings, GameOver, Tutorial, LevelCleared.");
        }

        private static void CopyPrefabsToAssetsPrefabsDir()
        {
            if (!Directory.Exists(PrefabsDstDir))
                Directory.CreateDirectory(PrefabsDstDir);

            if (!Directory.Exists(PrefabsSrcDir)) return;

            string[] prefabs = Directory.GetFiles(PrefabsSrcDir, "*.prefab");
            foreach (var src in prefabs)
            {
                string filename = Path.GetFileName(src);
                string dst = Path.Combine(PrefabsDstDir, filename);
                AssetDatabase.CopyAsset(src, dst);
            }
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(SplashScenePath, true),
                new EditorBuildSettingsScene(MapScenePath,    true),
                new EditorBuildSettingsScene(GameScenePath,   true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[AllScreensBuilder] EditorBuildSettings updated: Splash -> Map -> Game.");
        }
    }
}
#endif
