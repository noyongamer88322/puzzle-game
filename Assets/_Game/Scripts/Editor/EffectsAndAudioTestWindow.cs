// ============================================================
// EffectsAndAudioTestWindow.cs  –  Arrow Slide Puzzle Editor Tool
// Interactive test dashboard for PRD Section 5 VFX & Audio:
//   • Particle Systems: Sparkle, Fireworks, Confetti, Coin Burst, Star Burst, Magnet Dust
//   • Audio SFX & Music playback controls
//   • Settings toggles: Sound, Music, Vibration
//   • Gameplay effects: Flash, Slow-mo, Coin Roll-up
//
// Menu: Arrow Puzzle -> Effects & Audio Test Window
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ArrowPuzzle.Gameplay;
using ArrowPuzzle.Systems;
using ArrowPuzzle.UI;

namespace ArrowPuzzle.Editor
{
    public class EffectsAndAudioTestWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("Arrow Puzzle/Effects & Audio Test Window", false, 300)]
        public static void ShowWindow()
        {
            var window = GetWindow<EffectsAndAudioTestWindow>("VFX & Audio Tester");
            window.minSize = new Vector2(380f, 600f);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("PRD Section 5: Effects & Audio Tester", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Press Play in Game or Map scene to test runtime VFX and Audio directly.", MessageType.Info);

            EditorGUILayout.Space(10);
            DrawSettingsSection();

            EditorGUILayout.Space(10);
            DrawVFXSection();

            EditorGUILayout.Space(10);
            DrawAudioSection();

            EditorGUILayout.Space(10);
            DrawGameplayEffectsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("1. Settings Toggles (Sound, Music, Vibration)", EditorStyles.boldLabel);
            var state = GameState.Instance;
            if (state == null)
            {
                EditorGUILayout.HelpBox("GameState not initialized (Enter Play Mode to test).", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical("box");

            bool sound = EditorGUILayout.Toggle("Sound FX Enabled", state.SoundOn);
            if (sound != state.SoundOn) state.SetSound(sound);

            bool music = EditorGUILayout.Toggle("Music Enabled", state.MusicOn);
            if (music != state.MusicOn) state.SetMusic(music);

            bool vib = EditorGUILayout.Toggle("Vibration Enabled", state.VibrationOn);
            if (vib != state.VibrationOn) state.SetVibration(vib);

            EditorGUILayout.EndVertical();
        }

        private void DrawVFXSection()
        {
            EditorGUILayout.LabelField("2. Particle System VFX (PRD Section 5)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to spawn particle bursts in Game/Scene view.", MessageType.Warning);
            }
            else
            {
                Vector3 spawnPos = Camera.main != null ? (Vector3)(Vector2)Camera.main.transform.position : Vector3.zero;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Sparkle Burst", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnSparkleBurst(spawnPos, new Color(1f, 0.9f, 0.3f), 14);
                }
                if (GUILayout.Button("Exit Sparkle", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnExitSparkle(spawnPos, new Color(0.3f, 0.7f, 1f), 10);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Firework Burst (4-5 Colors)", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnFireworkBurst(spawnPos, 22);
                }
                if (GUILayout.Button("Confetti Burst", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnConfettiBurst(spawnPos, 25);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Coin Burst (Radial)", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnCoinBurst(spawnPos, 14);
                }
                if (GUILayout.Button("Star Burst", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnStarBurst(spawnPos, 12);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Magnet Pull Dust", GUILayout.Height(28)))
                {
                    VFXManager.Instance?.SpawnMagnetPullDust(spawnPos, spawnPos + new Vector3(-2f, -1.5f, 0f), 10);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAudioSection()
        {
            EditorGUILayout.LabelField("3. AudioManager (SFX & Music)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to play audio via AudioSource.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Tap SFX", GUILayout.Height(24))) AudioManager.Instance?.PlayTap();
                if (GUILayout.Button("Whoosh SFX", GUILayout.Height(24))) AudioManager.Instance?.PlayWhoosh();
                if (GUILayout.Button("Error SFX", GUILayout.Height(24))) AudioManager.Instance?.PlayError();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Heart Break", GUILayout.Height(24))) AudioManager.Instance?.PlayHeartBreak();
                if (GUILayout.Button("Hint Orb", GUILayout.Height(24))) AudioManager.Instance?.PlayHintOrb();
                if (GUILayout.Button("Magnet SFX", GUILayout.Height(24))) AudioManager.Instance?.PlayMagnet();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Coin Collect", GUILayout.Height(24))) AudioManager.Instance?.PlayCoinCollect();
                if (GUILayout.Button("Star SFX", GUILayout.Height(24))) AudioManager.Instance?.PlayStar();
                if (GUILayout.Button("Level Clear Jingle", GUILayout.Height(24))) AudioManager.Instance?.PlayLevelClearJingle();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Button Click", GUILayout.Height(24))) AudioManager.Instance?.PlayButtonClick();
                if (GUILayout.Button("Popup Open", GUILayout.Height(24))) AudioManager.Instance?.PlayPopupOpen();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Soft Music Loop", GUILayout.Height(26))) AudioManager.Instance?.PlayMusic();
                if (GUILayout.Button("Stop Music", GUILayout.Height(26))) AudioManager.Instance?.StopMusic();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGameplayEffectsSection()
        {
            EditorGUILayout.LabelField("4. Gameplay & UI Effects", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Trigger Blocked Vignette Flash & Shake", GUILayout.Height(28)))
                {
                    ScreenVignetteFlash.Instance?.TriggerFlash();
                    AudioManager.Instance?.PlayError();
                }

                if (GUILayout.Button("Trigger Last Arrow Slow-Mo (0.55s)", GUILayout.Height(28)))
                {
                    if (GameplayController.Instance != null)
                    {
                        GameplayController.Instance.StartCoroutine(SlowMoTestRoutine());
                    }
                }

                if (GUILayout.Button("Trigger Coin Roll-Up (+50 Coins)", GUILayout.Height(28)))
                {
                    var coinPill = Object.FindFirstObjectByType<CoinPillUI>();
                    if (coinPill != null) coinPill.AddCoins(50);
                }

                if (GUILayout.Button("Trigger Win Screen (Level Cleared UI)", GUILayout.Height(28)))
                {
                    LevelClearedUI.Instance?.ShowLevelCleared();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to trigger live gameplay animations.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private static System.Collections.IEnumerator SlowMoTestRoutine()
        {
            Time.timeScale = 0.35f;
            Debug.Log("[Tester] Slow-Mo Active (Time.timeScale = 0.35)");
            yield return new WaitForSecondsRealtime(0.55f);
            Time.timeScale = 1.0f;
            Debug.Log("[Tester] Slow-Mo Finished (Time.timeScale = 1.0)");
        }
    }
}
#endif

