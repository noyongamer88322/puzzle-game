// ============================================================
// UIScreenShowcaseWindow.cs  –  Arrow Slide Puzzle Editor Tool
// Interactive test dashboard for all 8 UI screens and popups.
// Allows developer/tester to preview and trigger each screen
// independently in Edit Mode or Play Mode.
//
// Menu: Arrow Puzzle → UI Screen Showcase / Tester
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArrowPuzzle.UI;
using ArrowPuzzle.Systems;
using ArrowPuzzle.Gameplay;

namespace ArrowPuzzle.Editor
{
    public class UIScreenShowcaseWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("Arrow Puzzle/UI Screen Showcase & Tester", false, 60)]
        public static void ShowWindow()
        {
            var win = GetWindow<UIScreenShowcaseWindow>("UI Showcase");
            win.minSize = new Vector2(420f, 720f);
            win.Show();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(8);
            DrawHeader();

            EditorGUILayout.Space(10);
            DrawBuildAndScenesSection();

            EditorGUILayout.Space(12);
            DrawScreen1SplashSection();

            EditorGUILayout.Space(10);
            DrawScreen2MapSection();

            EditorGUILayout.Space(10);
            DrawScreen3HUDSection();

            EditorGUILayout.Space(10);
            DrawScreen4TutorialSection();

            EditorGUILayout.Space(10);
            DrawScreen5LevelClearedSection();

            EditorGUILayout.Space(10);
            DrawScreen6GallerySection();

            EditorGUILayout.Space(10);
            DrawScreen7SettingsSection();

            EditorGUILayout.Space(10);
            DrawScreen8GameOverSection();

            EditorGUILayout.Space(14);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎮 Arrow Slide Puzzle: 8 UI Screens", headerStyle);

            GUIStyle subStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField("Portrait 1080x1920 • Mobile Cartoon UI • Safe Area Anchored", subStyle);
        }

        // ── 0. Build & Scene Loading ─────────────────────────────────
        private void DrawBuildAndScenesSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUIStyle btnBuildStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            GUI.backgroundColor = new Color(0.25f, 0.85f, 0.45f);
            if (GUILayout.Button("🚀 Build / Rebuild All 8 Screens & Prefabs", btnBuildStyle, GUILayout.Height(36)))
            {
                AllScreensBuilder.BuildAllScreens();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Jump to Scene:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Splash", GUILayout.Height(26)))
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Splash.unity");
            }
            if (GUILayout.Button("2. Map (Home)", GUILayout.Height(26)))
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Map.unity");
            }
            if (GUILayout.Button("3. Game Scene", GUILayout.Height(26)))
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── 1. Splash & Scene Transition ─────────────────────────────
        private void DrawScreen1SplashSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("1. Splash Screen & Scene Transition", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Light blue (#2FB4E8), 2x2 four glossy arrows (pink up, orange right, cyan left, purple down), 5 loading dots, cross-fade ~0.8s.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Splash Scene"))
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Splash.unity");
            }
            if (GUILayout.Button("Trigger Cross-Fade to Map"))
            {
                if (Application.isPlaying)
                    SceneTransition.Load("Map");
                else
                    EditorSceneManager.OpenScene("Assets/_Game/Scenes/Map.unity");
            }
            if (GUILayout.Button("Trigger Cross-Fade to Game"))
            {
                if (Application.isPlaying)
                    SceneTransition.Load("Game");
                else
                    EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ── 2. Map (Home) ────────────────────────────────────────────
        private void DrawScreen2MapSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("2. Map (Home) Screen", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Painted background, cream coin pill with gold border, green gear, chapter pill 'Hills' (progress bar + star x/5) at ~66% height, big green 'Level N' button with breathing pulse, small gallery button.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Map Scene"))
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Map.unity");
            }
            if (GUILayout.Button("Simulate Add Coins (+100)"))
            {
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.AddCoins(100);
                    var coinPill = Object.FindObjectOfType<CoinPillUI>();
                    if (coinPill != null) coinPill.AddCoins(100);
                }
            }
            if (GUILayout.Button("Add Stars (+1)"))
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.AddStars(1);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ── 3. Game HUD ──────────────────────────────────────────────
        private void DrawScreen3HUDSection()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("3. Game HUD", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Deep indigo gradient (#2A0A9E -> #3B14C8) + vignette, cream coin pill, 3-heart pill (red/pale beige), green gear, orange back, Hint (650/FREE), Magnet (1300/FREE), pink/red blocked overlay flash.", MessageType.None);

                EditorGUILayout.LabelField("Progressive Reveal Test:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("L1: Coin + Gear"))
                {
                    GameplayHUD.Instance?.ApplyLevelIndex(1);
                }
                if (GUILayout.Button("L2-3: + Heart"))
                {
                    GameplayHUD.Instance?.ApplyLevelIndex(2);
                }
                if (GUILayout.Button("L4-5: + Back & Hint"))
                {
                    GameplayHUD.Instance?.ApplyLevelIndex(4);
                }
                if (GUILayout.Button("L6: + Magnet"))
                {
                    GameplayHUD.Instance?.ApplyLevelIndex(6);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Goal: Level 1"))
                {
                    GameplayHUD.Instance?.levelGoalText?.SetText("Level 1");
                }
                if (GUILayout.Button("Goal: Level 6"))
                {
                    GameplayHUD.Instance?.levelGoalText?.SetText("Level 6");
                }
                if (GUILayout.Button("Goal: Level 10"))
                {
                    GameplayHUD.Instance?.levelGoalText?.SetText("Level 10");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(1f, 0.45f, 0.55f);
                if (GUILayout.Button("Trigger Blocked Glow Flash"))
                {
                    GameplayHUD.Instance?.TriggerBlockedVignette();
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Lose 1 Heart"))
                {
                    if (GameplayHUD.Instance?.heartPill != null)
                        GameplayHUD.Instance.heartPill.LoseHeart(2, 3);
                }
                if (GUILayout.Button("Refill 3 Hearts"))
                {
                    GameplayHUD.Instance?.heartPill?.SetHearts(3, 3);
                }
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        // ── 4. Tutorial Overlay ──────────────────────────────────────
        private void DrawScreen4TutorialSection()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("4. Tutorial Overlay", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Near-black dim, top-right Dida avatar circle, white speech bubble with blue border to the left, hand pointer tap loop. 1: Arrow Tap, 2: Hint Button, 3: Magnet Button.", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Step 1: Arrow Tap"))
                {
                    TutorialController.Instance?.CheckAndTriggerTutorial(1);
                }
                if (GUILayout.Button("Step 2: Hint Button"))
                {
                    TutorialController.Instance?.CheckAndTriggerTutorial(4);
                }
                if (GUILayout.Button("Step 3: Magnet Button"))
                {
                    TutorialController.Instance?.CheckAndTriggerTutorial(6);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Complete Step"))
                {
                    TutorialController.Instance?.CompleteCurrentStep();
                }
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
                if (GUILayout.Button("Reset All Tutorial Flags"))
                {
                    TutorialController.Instance?.ResetAllTutorials();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        // ── 5. Level Cleared ─────────────────────────────────────────
        private void DrawScreen5LevelClearedSection()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("5. Level Cleared (Win Screen)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Deep purple background, yellow 'Level cleared!' pop banner with glow, tilting polaroid with tape & reward photo, coin +30 & star +1 count-up, clapping Dida, pulsing 'Tap to continue', fireworks & sparkles.", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.95f, 0.85f, 0.15f);
                if (GUILayout.Button("🎉 Show Level Cleared Celebration", GUILayout.Height(30)))
                {
                    LevelClearedUI.Instance?.ShowLevelCleared();
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Tap To Continue"))
                {
                    LevelClearedUI.Instance?.OnTapToContinueClicked();
                }
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        // ── 6. Gallery Popup ─────────────────────────────────────────
        private void DrawScreen6GallerySection()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("6. Gallery Popup", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Blue rounded panel (cream inside), orange-yellow 'Gallery' ribbon, small blue X top-right, 3-column polaroids (unlocked = green checkmark, locked = grey + lock), vertical scroll.", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                var gal = Object.FindObjectOfType<GalleryPanelUI>(true);
                if (GUILayout.Button("Open Gallery Popup"))
                {
                    gal?.Open();
                }
                if (GUILayout.Button("Close Gallery Popup"))
                {
                    gal?.Close();
                }
                if (GUILayout.Button("Unlock All Photos"))
                {
                    for (int i = 1; i <= 10; i++)
                        SaveManager.Instance?.UnlockGallery(i);
                    gal?.Open();
                }
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        // ── 7. Settings Popup ────────────────────────────────────────
        private void DrawScreen7SettingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("7. Settings Popup", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Blue rounded panel (cream inside), 'Settings' ribbon, Sound / Music / Vibration toggles (green ON / grey OFF), green 'Privacy Policy' button, version number (v1.0.0), close X button.", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                var set = Object.FindObjectOfType<SettingsPanelUI>(true) ?? SettingsPanelUI.Instance;
                if (GUILayout.Button("Open Settings Popup"))
                {
                    set?.Open();
                }
                if (GUILayout.Button("Close Settings Popup"))
                {
                    set?.Close();
                }
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        // ── 8. Game Over Popup ───────────────────────────────────────
        private void DrawScreen8GameOverSection()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField("8. Game Over Popup", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Same cartoon style, 'Out of hearts' ribbon, large broken/empty heart, subtitle 'Don't give up!', player coins, green 'Retry' and orange 'Revive' (300 coins). NO Ads.", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                var go = Object.FindObjectOfType<GameOverPopupUI>(true) ?? GameOverPopupUI.Instance;
                if (GUILayout.Button("Open Game Over Popup"))
                {
                    if (go != null)
                    {
                        go.gameObject.SetActive(true);
                        go.SendMessage("ShowPopup", UnityEngine.SendMessageOptions.DontRequireReceiver);
                    }
                }
                if (GUILayout.Button("Add 500 Coins"))
                {
                    SaveManager.Instance?.AddCoins(500);
                }
                if (GUILayout.Button("Reset Coins to 0"))
                {
                    int current = SaveManager.Instance?.Coins ?? 0;
                    if (current > 0) SaveManager.Instance?.SpendCoins(current);
                }
                EditorGUILayout.EndHorizontal();
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif

