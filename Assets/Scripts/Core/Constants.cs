// ============================================================
// Constants.cs
// Central place for all magic strings, scene names, legacy PlayerPrefs
// keys, layer names, and tunable numeric constants.
// ============================================================

namespace ArrowPuzzle.Core
{
    public static class SceneNames
    {
        public const string Splash   = "Splash";
        public const string Map      = "Map";
        public const string Gameplay = "Game";
    }

    public static class PrefKeys
    {
        // Progress
        public const string HighestUnlockedLevel = "highest_unlocked_level";
        public const string TotalCoins           = "total_coins";
        public const string TotalStars           = "total_stars";
        public const string AdsRemoved           = "ads_removed";

        // Boosters (first time free tracking)
        public const string HintFreeUsed         = "hint_free_used";
        public const string MagnetFreeUsed       = "magnet_free_used";

        // Per-level gallery unlock  (key = "gallery_N")
        public static string GalleryUnlocked(int level) => $"gallery_{level}";

        // Settings
        public const string SoundOn     = "sound_on";
        public const string MusicOn     = "music_on";
        public const string VibrationOn = "vibration_on";

        // Tutorial flags (Grandma-style guide, shown once per milestone)
        public const string TutorialStep1Done = "tutorial_step1_done";
        public const string TutorialStep2Done = "tutorial_step2_done";
        public const string TutorialStep3Done = "tutorial_step3_done";

        // Map scene tutorial (Step 10 – hand pointer on Level button, first time only)
        public const string MapLevelButtonTutorialDone = "map_level_btn_tutorial_done";

        // Map scene: rewards-fly animation already played for a cleared level
        public static string MapRewardFlown(int level) => $"map_reward_flown_{level}";
    }

    public static class Tags
    {
        public const string Arrow = "Arrow";
    }

    public static class Layers
    {
        public const string UI       = "UI";
        public const string Gameplay = "Gameplay";
    }

    /// <summary>
    /// Game-wide tunable numbers (tweak here, not scattered across scripts).
    /// </summary>
    public static class GameConfig
    {
        // Economy
        public const int CoinsPerLevel  = 30;
        public const int StarsPerLevel  = 1;
        public const int HintCostCoins  = 650;
        public const int MagnetCostCoins = 1300;

        // UI unlock thresholds (1-based level index)
        public const int HeartPillUnlockLevel  = 2;   // heart life-bar appears from L2
        public const int BackButtonUnlockLevel = 4;
        public const int HintUnlockLevel       = 4;
        public const int MagnetLevelIndex      = 6;   // only on the heart-shape level

        // Hearts / lives
        public const int MaxHearts = 5;

        // Animation durations (seconds)
        public const float ArrowSlideDuration    = 0.18f;
        public const float HintOrbTravelDuration = 0.55f;
        public const float LevelClearDelay       = 0.6f;   // pause before showing clear screen
        public const float DidaSlideInDuration   = 0.4f;

        // Splash
        public const float SplashFadeInDuration = 0.5f;
        public const float SplashPopDuration     = 0.25f;

        // Magnet
        public const float MagnetArrowFlyDuration = 0.35f;

        // Blocked-tap
        public const float BlockedRedDuration     = 0.3f;
        public const float VignetteFadeDuration   = 0.4f;

        // Map scene
        public const int   LevelsPerChapter          = 5;      // PRD: every 5 levels = 1 chapter
        public const float MapFadeDuration           = 0.35f;  // blue fade in/out for scene transition
        public const float BreathingPulseDuration    = 1.8f;   // Level button breathing cycle
        public const float BreathingPulseScale       = 1.05f;  // max scale of breathing pulse
        public const float RewardFlyDuration         = 0.55f;  // coin/star fly to counter duration
        public const float ProgressBarFillDuration   = 0.5f;   // chapter progress bar fill duration
    }
}

