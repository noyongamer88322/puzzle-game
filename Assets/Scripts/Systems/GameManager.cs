// ============================================================
// GameManager.cs
// Persistent singleton.  Bootstraps the app, manages scene
// transitions, and exposes the active level context.
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle
{
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Active session data ───────────────────────────────
        /// <summary>Level that is currently being played (set before Gameplay scene loads).</summary>
        public LevelData ActiveLevel { get; private set; }

        /// <summary>Number of wrong taps in the current level session.</summary>
        public int WrongTapsThisLevel { get; private set; }

        /// <summary>Whether the hint was used (free) this level.</summary>
        public bool HintUsedFreeThisLevel { get; private set; }

        /// <summary>Whether the magnet was used (free) this level.</summary>
        public bool MagnetUsedFreeThisLevel { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure GameState is loaded (creates singleton on first access).
            _ = GameState.Instance;
        }

        // ── Navigation ────────────────────────────────────────

        public void GoToSplash()
        {
            SceneManager.LoadScene(SceneNames.Splash);
        }

        public void GoToMap()
        {
            SceneManager.LoadScene(SceneNames.Map);
        }

        /// <summary>Sets active level data without loading scene immediately.</summary>
        public void SetActiveLevel(int levelIndex)
        {
            ActiveLevel              = LevelDatabase.Get(levelIndex);
            WrongTapsThisLevel       = 0;
            HintUsedFreeThisLevel    = false;
            MagnetUsedFreeThisLevel  = false;
        }

        /// <summary>Start a gameplay session for the given level.</summary>
        public void StartLevel(int levelIndex)
        {
            SetActiveLevel(levelIndex);

            if (ActiveLevel == null)
            {
                Debug.LogError($"[GameManager] Level {levelIndex} not found.");
                return;
            }

            SceneManager.LoadScene(SceneNames.Gameplay);
        }

        // ── Level events (called by gameplay systems) ─────────

        /// <summary>Call when the player taps a blocked (non-free) arrow.</summary>
        public void OnBlockedTap()
        {
            WrongTapsThisLevel++;
            GameState.Instance.LoseHeart();
        }

        /// <summary>Call when the player successfully clears the level.</summary>
        public void OnLevelCleared()
        {
            var state = GameState.Instance;
            state.AddCoins(GameConfig.CoinsPerLevel);
            state.AddStars(GameConfig.StarsPerLevel);
            state.UnlockLevel(ActiveLevel.levelIndex);

            if (ActiveLevel.hasGalleryReward)
                state.SetGalleryUnlocked(ActiveLevel.levelIndex);
        }

        /// <summary>Returns whether hint is free this level (first time AND level supports it).</summary>
        public bool CanUseHintFree()
        {
            return ActiveLevel != null
                && ActiveLevel.hintIsFree
                && !HintUsedFreeThisLevel;
        }

        /// <summary>Call when hint is consumed this level.</summary>
        public bool TryConsumeHint()
        {
            if (CanUseHintFree())
            {
                HintUsedFreeThisLevel = true;
                return true;
            }
            // Paid hint
            return GameState.Instance.SpendCoins(GameConfig.HintCostCoins);
        }

        /// <summary>Returns whether magnet is free this level.</summary>
        public bool CanUseMagnetFree()
        {
            return ActiveLevel != null
                && ActiveLevel.magnetIsFree
                && !MagnetUsedFreeThisLevel;
        }

        /// <summary>Call when magnet is consumed.</summary>
        public bool TryConsumeMagnet()
        {
            if (CanUseMagnetFree())
            {
                MagnetUsedFreeThisLevel = true;
                return true;
            }
            return GameState.Instance.SpendCoins(GameConfig.MagnetCostCoins);
        }
    }
}

