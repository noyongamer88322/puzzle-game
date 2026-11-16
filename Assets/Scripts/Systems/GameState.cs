// ============================================================
// GameState.cs
// Global runtime state: coins, stars, hearts, level progress.
// Persisted via SaveManager (JSON). Single source of truth.
// ============================================================

using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Systems
{
    /// <summary>
    /// Runtime facade for game state. Delegates persistent state to SaveManager.
    /// Access via GameState.Instance.
    /// </summary>
    public class GameState
    {
        // ── Singleton ─────────────────────────────────────────
        private static GameState _instance;
        public static GameState Instance => _instance ??= new GameState();

        // ── Properties (backed by SaveManager) ────────────────
        public int  Coins                 => SaveManager.Instance.Coins;
        public int  TotalStars            => SaveManager.Instance.TotalStars;
        public int  HighestUnlockedLevel  => SaveManager.Instance.HighestUnlockedLevel;
        public bool AdsRemoved            => SaveManager.Instance.AdsRemoved;

        // Current hearts (refills over time in a full implementation).
        public int  Hearts                { get; private set; }

        // Settings
        public bool SoundOn     => SaveManager.Instance.SoundOn;
        public bool MusicOn     => SaveManager.Instance.MusicOn;
        public bool VibrationOn => SaveManager.Instance.VibrationOn;

        // ── Init ──────────────────────────────────────────────
        private GameState() => Load();

        public void Load()
        {
            Hearts = GameConfig.MaxHearts;   // always full at app start for now
            // SaveManager is already loaded on its own singleton construction;
            // calling this ensures the runtime Hearts field is refreshed.
        }

        // ── Mutations ─────────────────────────────────────────

        public void AddCoins(int amount)
        {
            SaveManager.Instance.AddCoins(amount);
        }

        public bool SpendCoins(int amount)
        {
            return SaveManager.Instance.SpendCoins(amount);
        }

        public void AddStars(int amount)
        {
            SaveManager.Instance.AddStars(amount);
        }

        // ── Booster Free-Use Tracking ─────────────────────────
        public bool IsHintFree => SaveManager.Instance.IsHintFree;

        public void ConsumeFreeHint()
        {
            SaveManager.Instance.ConsumeFreeHint();
        }

        public bool IsMagnetFree => SaveManager.Instance.IsMagnetFree;

        public void ConsumeFreeMagnet()
        {
            SaveManager.Instance.ConsumeFreeMagnet();
        }

        /// <summary>Call when a level is successfully cleared.</summary>
        public void UnlockLevel(int clearedLevelIndex)
        {
            SaveManager.Instance.UnlockLevel(clearedLevelIndex);
        }

        /// <summary>Returns true if the gallery image for this level is unlocked.</summary>
        public bool IsGalleryUnlocked(int levelIndex)
        {
            return SaveManager.Instance.IsGalleryUnlocked(levelIndex);
        }

        public void SetGalleryUnlocked(int levelIndex)
        {
            SaveManager.Instance.UnlockGallery(levelIndex);
        }

        public void LoseHeart()
        {
            Hearts = Mathf.Max(0, Hearts - 1);
        }

        public void RemoveAds()
        {
            SaveManager.Instance.SetAdsRemoved(true);
        }

        // Settings setters
        public void SetSound(bool on)
        {
            SaveManager.Instance.SetSound(on);
            ArrowPuzzle.AudioManager.Instance?.ApplySettings();
        }

        public void SetMusic(bool on)
        {
            SaveManager.Instance.SetMusic(on);
            ArrowPuzzle.AudioManager.Instance?.ApplySettings();
        }

        public void SetVibration(bool on)
        {
            SaveManager.Instance.SetVibration(on);
        }

        /// <summary>Debug: wipe all saved progress.</summary>
        public void ResetAll()
        {
            SaveManager.Instance.ResetAll();
            Hearts = GameConfig.MaxHearts;
        }
    }
}

