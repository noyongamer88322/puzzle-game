// ============================================================
// SaveManager.cs  –  Arrow Slide Puzzle
// Local JSON save system persisted in Application.persistentDataPath.
// Replaces PlayerPrefs as the single persistent source of truth.
// Handles corruption gracefully, supports version migration,
// and auto-migrates existing PlayerPrefs on first run.
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Core;

namespace ArrowPuzzle.Systems
{
    /// <summary>
    /// Serializable game state saved to save_data.json.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public const int CURRENT_VERSION = 1;

        // ── Save File Version ─────────────────────────────────
        public int version = CURRENT_VERSION;

        // ── Economy & Progression ─────────────────────────────
        public int coins = 0;
        public int totalStars = 0;
        public int currentLevel = 1;
        public int highestUnlockedLevel = 1;
        public int currentChapter = 0;

        // ── Cleared Levels & Gallery Unlocks ──────────────────
        public List<int> clearedLevels = new List<int>();
        public List<int> unlockedGallery = new List<int>();

        // ── Boosters Free-use Tracking ────────────────────────
        public bool hintFreeUsed = false;
        public bool magnetFreeUsed = false;

        // ── Settings ──────────────────────────────────────────
        public bool soundOn = true;
        public bool musicOn = true;
        public bool vibrationOn = true;

        // ── Monetization ──────────────────────────────────────
        public bool adsRemoved = false;

        // ── Tutorial Flags ────────────────────────────────────
        public bool tutorialStep1Done = false;
        public bool tutorialStep2Done = false;
        public bool tutorialStep3Done = false;
        public bool mapLevelButtonTutorialDone = false;

        // ── Map Scene Reward Animations ───────────────────────
        public List<int> mapRewardFlownLevels = new List<int>();

        // ── Session Transit States ────────────────────────────
        public int mapPendingClearedLevel = 0;
        public int pendingLevel = 1;
    }

    /// <summary>
    /// Singleton manager for persistent local game saves in JSON format.
    /// </summary>
    public class SaveManager
    {
        // ── Singleton ─────────────────────────────────────────
        private static SaveManager _instance;
        public static SaveManager Instance => _instance ??= new SaveManager();

        public const string SaveFileName = "save_data.json";

        public static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        public SaveData Data { get; private set; }

        // ── Constructor ───────────────────────────────────────
        private SaveManager()
        {
            Load();
            Application.quitting += OnApplicationQuitting;
        }

        private void OnApplicationQuitting()
        {
            Save();
        }

        // ── Load & Save ───────────────────────────────────────

        /// <summary>
        /// Loads the save data from JSON file. If file is corrupted or unreadable,
        /// resets to default values without crashing.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException("Save file was empty.");
                    }

                    Data = JsonUtility.FromJson<SaveData>(json);
                    if (Data == null)
                    {
                        throw new InvalidDataException("JsonUtility returned null when parsing save file.");
                    }

                    EnsureDefaults();

                    if (Data.version < SaveData.CURRENT_VERSION)
                    {
                        MigrateVersion(Data.version, SaveData.CURRENT_VERSION);
                    }

                    Debug.Log($"[SaveManager] Save loaded successfully from {SaveFilePath} (v{Data.version}, Coins={Data.coins}, Stars={Data.totalStars}, Level={Data.highestUnlockedLevel}).");
                }
                else
                {
                    // No save file found: initialize defaults and check if legacy PlayerPrefs exist to migrate
                    Data = new SaveData();
                    EnsureDefaults();

                    if (!MigrateFromPlayerPrefsIfNeeded())
                    {
                        Save();
                        Debug.Log($"[SaveManager] Created new default save file at {SaveFilePath}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Save file at '{SaveFilePath}' was corrupt or unreadable: {ex.Message}. Recovering with fresh default data without crashing.");
                Data = new SaveData();
                EnsureDefaults();
                Save();
            }
        }

        /// <summary>
        /// Saves current game data to JSON file atomically.
        /// </summary>
        public void Save()
        {
            try
            {
                if (Data == null)
                {
                    Data = new SaveData();
                }

                EnsureDefaults();

                string json = JsonUtility.ToJson(Data, true);
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write atomically via temporary file
                string tempPath = SaveFilePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
                File.Move(tempPath, SaveFilePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to write save file: {ex.Message}");
            }
        }

        private void EnsureDefaults()
        {
            if (Data.clearedLevels == null) Data.clearedLevels = new List<int>();
            if (Data.unlockedGallery == null) Data.unlockedGallery = new List<int>();
            if (Data.mapRewardFlownLevels == null) Data.mapRewardFlownLevels = new List<int>();
            if (Data.highestUnlockedLevel < 1) Data.highestUnlockedLevel = 1;
            if (Data.currentLevel < 1) Data.currentLevel = 1;
        }

        private void MigrateVersion(int oldVersion, int newVersion)
        {
            Debug.Log($"[SaveManager] Migrating save data from v{oldVersion} to v{newVersion}...");
            // Future migration steps can be added here
            Data.version = newVersion;
            Save();
        }

        private bool MigrateFromPlayerPrefsIfNeeded()
        {
            bool hasLegacyData = PlayerPrefs.HasKey(PrefKeys.TotalCoins) ||
                                 PlayerPrefs.HasKey(PrefKeys.HighestUnlockedLevel) ||
                                 PlayerPrefs.HasKey(PrefKeys.TotalStars) ||
                                 PlayerPrefs.HasKey(PrefKeys.AdsRemoved);

            if (!hasLegacyData) return false;

            Debug.Log("[SaveManager] Detected legacy PlayerPrefs data. Migrating to JSON save file...");

            Data.coins                = PlayerPrefs.GetInt(PrefKeys.TotalCoins, 0);
            Data.totalStars           = PlayerPrefs.GetInt(PrefKeys.TotalStars, 0);
            Data.highestUnlockedLevel = PlayerPrefs.GetInt(PrefKeys.HighestUnlockedLevel, 1);
            Data.adsRemoved           = PlayerPrefs.GetInt(PrefKeys.AdsRemoved, 0) == 1;

            Data.soundOn              = PlayerPrefs.GetInt(PrefKeys.SoundOn, 1) == 1;
            Data.musicOn              = PlayerPrefs.GetInt(PrefKeys.MusicOn, 1) == 1;
            Data.vibrationOn          = PlayerPrefs.GetInt(PrefKeys.VibrationOn, 1) == 1;

            Data.hintFreeUsed         = PlayerPrefs.GetInt(PrefKeys.HintFreeUsed, 0) == 1;
            Data.magnetFreeUsed       = PlayerPrefs.GetInt(PrefKeys.MagnetFreeUsed, 0) == 1;

            Data.tutorialStep1Done    = PlayerPrefs.GetInt(PrefKeys.TutorialStep1Done, 0) == 1;
            Data.tutorialStep2Done    = PlayerPrefs.GetInt(PrefKeys.TutorialStep2Done, 0) == 1;
            Data.tutorialStep3Done    = PlayerPrefs.GetInt(PrefKeys.TutorialStep3Done, 0) == 1;
            Data.mapLevelButtonTutorialDone = PlayerPrefs.GetInt(PrefKeys.MapLevelButtonTutorialDone, 0) == 1;

            for (int i = 1; i <= 100; i++)
            {
                if (PlayerPrefs.GetInt(PrefKeys.GalleryUnlocked(i), 0) == 1)
                {
                    if (!Data.unlockedGallery.Contains(i))
                        Data.unlockedGallery.Add(i);
                }

                if (PlayerPrefs.GetInt($"level_{i}_cleared", 0) == 1)
                {
                    if (!Data.clearedLevels.Contains(i))
                        Data.clearedLevels.Add(i);
                }

                if (PlayerPrefs.GetInt(PrefKeys.MapRewardFlown(i), 0) == 1)
                {
                    if (!Data.mapRewardFlownLevels.Contains(i))
                        Data.mapRewardFlownLevels.Add(i);
                }
            }

            Data.mapPendingClearedLevel = PlayerPrefs.GetInt("map_pending_cleared_level", 0);
            Data.pendingLevel           = PlayerPrefs.GetInt("pending_level", 1);

            Save();

            // Clear legacy PlayerPrefs now that data is safely saved to JSON
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] Legacy PlayerPrefs successfully migrated to JSON and cleared.");
            return true;
        }

        // ── Economy Helpers ───────────────────────────────────

        public int Coins => Data.coins;
        public int TotalStars => Data.totalStars;

        public void AddCoins(int amount)
        {
            Data.coins += amount;
            Save();
        }

        public bool SpendCoins(int amount)
        {
            if (Data.coins < amount) return false;
            Data.coins -= amount;
            Save();
            return true;
        }

        public void AddStars(int amount)
        {
            Data.totalStars += amount;
            Save();
        }

        // ── Level & Chapter Progression ───────────────────────

        public int HighestUnlockedLevel => Data.highestUnlockedLevel;

        public int CurrentLevel
        {
            get => Data.currentLevel;
            set
            {
                Data.currentLevel = value;
                Save();
            }
        }

        public int CurrentChapter => Data.currentChapter;

        public void UnlockLevel(int clearedLevelIndex)
        {
            if (clearedLevelIndex >= Data.highestUnlockedLevel)
            {
                Data.highestUnlockedLevel = clearedLevelIndex + 1;
                Save();
            }
        }

        public bool IsLevelCleared(int levelIndex)
        {
            return Data.clearedLevels != null && Data.clearedLevels.Contains(levelIndex);
        }

        public void MarkLevelCleared(int levelIndex)
        {
            EnsureDefaults();
            if (!Data.clearedLevels.Contains(levelIndex))
            {
                Data.clearedLevels.Add(levelIndex);
            }
            UnlockLevel(levelIndex);
            Save();
        }

        public int GetClearedCountInChapter(int chapterIndex, int levelsPerChapter)
        {
            EnsureDefaults();
            int start = chapterIndex * levelsPerChapter + 1;
            int end = start + levelsPerChapter;
            int count = 0;
            for (int i = start; i < end; i++)
            {
                if (Data.clearedLevels.Contains(i))
                    count++;
            }
            return count;
        }

        // ── Gallery ───────────────────────────────────────────

        public bool IsGalleryUnlocked(int levelIndex)
        {
            EnsureDefaults();
            return Data.unlockedGallery.Contains(levelIndex);
        }

        public void UnlockGallery(int levelIndex)
        {
            EnsureDefaults();
            if (!Data.unlockedGallery.Contains(levelIndex))
            {
                Data.unlockedGallery.Add(levelIndex);
                Save();
            }
        }

        public IReadOnlyList<int> UnlockedGallery => Data.unlockedGallery;

        // ── Boosters Free-use Tracking ────────────────────────

        public bool IsHintFree => !Data.hintFreeUsed;

        public void ConsumeFreeHint()
        {
            Data.hintFreeUsed = true;
            Save();
        }

        public bool IsMagnetFree => !Data.magnetFreeUsed;

        public void ConsumeFreeMagnet()
        {
            Data.magnetFreeUsed = true;
            Save();
        }

        // ── Settings ──────────────────────────────────────────

        public bool SoundOn => Data.soundOn;
        public bool MusicOn => Data.musicOn;
        public bool VibrationOn => Data.vibrationOn;

        public void SetSound(bool on)
        {
            Data.soundOn = on;
            Save();
        }

        public void SetMusic(bool on)
        {
            Data.musicOn = on;
            Save();
        }

        public void SetVibration(bool on)
        {
            Data.vibrationOn = on;
            Save();
        }

        // ── Ads ───────────────────────────────────────────────

        public bool AdsRemoved => Data.adsRemoved;

        public void SetAdsRemoved(bool removed = true)
        {
            Data.adsRemoved = removed;
            Save();
        }

        // ── Tutorials ─────────────────────────────────────────

        public bool IsTutorialDone(int step)
        {
            return step switch
            {
                1 => Data.tutorialStep1Done,
                2 => Data.tutorialStep2Done,
                3 => Data.tutorialStep3Done,
                _ => false
            };
        }

        public void SetTutorialDone(int step, bool done = true)
        {
            switch (step)
            {
                case 1: Data.tutorialStep1Done = done; break;
                case 2: Data.tutorialStep2Done = done; break;
                case 3: Data.tutorialStep3Done = done; break;
            }
            Save();
        }

        public bool IsMapLevelButtonTutorialDone => Data.mapLevelButtonTutorialDone;

        public void SetMapLevelButtonTutorialDone(bool done = true)
        {
            Data.mapLevelButtonTutorialDone = done;
            Save();
        }

        public void ResetAllTutorials()
        {
            Data.tutorialStep1Done = false;
            Data.tutorialStep2Done = false;
            Data.tutorialStep3Done = false;
            Save();
        }

        // ── Map Scene Reward & Pending States ─────────────────

        public bool IsRewardFlown(int level)
        {
            EnsureDefaults();
            return Data.mapRewardFlownLevels.Contains(level);
        }

        public void SetRewardFlown(int level, bool flown = true)
        {
            EnsureDefaults();
            if (flown && !Data.mapRewardFlownLevels.Contains(level))
            {
                Data.mapRewardFlownLevels.Add(level);
                Save();
            }
            else if (!flown && Data.mapRewardFlownLevels.Contains(level))
            {
                Data.mapRewardFlownLevels.Remove(level);
                Save();
            }
        }

        public int PendingClearedLevel
        {
            get => Data.mapPendingClearedLevel;
            set
            {
                Data.mapPendingClearedLevel = value;
                Save();
            }
        }

        public int PendingLevel
        {
            get => Data.pendingLevel;
            set
            {
                Data.pendingLevel = value;
                Save();
            }
        }

        // ── Debug / Reset ─────────────────────────────────────

        /// <summary>
        /// Wipes all save data and creates a fresh default save file.
        /// </summary>
        public void ResetAll()
        {
            Data = new SaveData();
            EnsureDefaults();
            Save();
            Debug.Log("[SaveManager] All save data has been wiped and reset to default.");
        }
    }
}

