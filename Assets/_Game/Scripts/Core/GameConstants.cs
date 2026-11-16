// ============================================================
// GameConstants.cs  –  Arrow Slide Puzzle
// Single source of truth for all colours, prices, rewards,
// and tunable numbers referenced throughout the codebase.
// PRD Section 7 – do NOT scatter magic values in other files.
// ============================================================

using UnityEngine;

namespace ArrowPuzzle.Core
{
    /// <summary>Arrow tint colours (5 gameplay colours + blocked state).</summary>
    public static class ArrowColors
    {
        // ── Reference-image palette ───────────────────────────────────────────
        // Primary two colours from the reference: warm golden-yellow and hot pink/magenta.
        // The remaining slots fall back to variations of these two for variety.
        public static readonly Color Orange  = HexToColor("FFB52E");   // warm golden-yellow (#FFB52E)
        public static readonly Color Pink    = HexToColor("FF4FD8");   // hot pink / magenta  (#FF4FD8)
        public static readonly Color Purple  = HexToColor("FFB52E");   // mapped → golden-yellow (variety)
        public static readonly Color Blue    = HexToColor("FF4FD8");   // mapped → hot pink     (variety)
        public static readonly Color Cyan    = HexToColor("FFB52E");   // mapped → golden-yellow (variety)

        // Blocked / invalid tap colour
        public static readonly Color Blocked = HexToColor("FF1A1A");

        // Convenience array – index matches ArrowColorId enum
        public static readonly Color[] All = { Orange, Pink, Purple, Blue, Cyan };

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }

    // ── Arrow visual style constants ─────────────────────────────────────────
    // All geometry and glow values are defined here so they are easy to edit
    // in one place. ArrowView and ArrowMeshBuilder read from these.
    //
    // KEY DESIGN: Arrow thickness is derived from a clamped "visual unit" U,
    // NOT raw cellSize. This keeps arrows the same elegant thickness on both
    // sparse (few, large cells) and dense (many, small cells) levels.
    // ── Easy tuning constants (edit here to change the arrow look) ───────────
    public static class ArrowVisuals
    {
        // ── Primary Visual Constants ──────────────────────────────────────────
        public const float  UNIT_MIN             = 0.20f;     // Min visual unit in world units (~38 ref-px)
        public const float  UNIT_MAX             = 0.35f;     // Max visual unit in world units (~67 ref-px)
        public const float  STROKE_RATIO         = 0.30f;     // Stroke width = 30% of U (slimmer, elegant)
        public const float  HEAD_WIDTH_RATIO     = 2.4f;      // Head width = 2.4x stroke width (clearly wider than body)
        public const float  HEAD_LENGTH_RATIO    = 1.6f;      // Head length = 1.6x stroke width (pointier, not squat)
        public const float  CORNER_RADIUS_RATIO  = 0.9f;      // Corner radius = 0.9x stroke width (max 0.45 * cellSize)
        public const float  HEAD_CORNER_ROUNDING = 0.12f;     // Head corners rounded by 12% of head width
        public const string COLOR_YELLOW         = "FFB52E";  // Golden yellow (#FFB52E)
        public const string COLOR_PINK           = "FF4FD8";  // Hot pink / magenta (#FF4FD8)
        public const bool   HIGHLIGHT_ENABLED    = false;     // Optional upper-left highlight stripe
        public const bool   GLOW_ENABLED         = false;     // Optional soft same-color glow (disabled = no halo)
        public const float  GLOW_BLUR            = 5.0f;      // Conceptual glow blur radius
        public const float  GLOW_OPACITY         = 0.25f;     // Glow opacity ~25%

        // ── Secondary Layout & Rendering Tunables ─────────────────────────────
        /// <summary>
        /// CellSize cap multiplier: GridManager.CellSize is capped at UNIT_MAX * this value
        /// so the puzzle stays compact and centered on sparse / few-arrow levels.
        /// </summary>
        public const float CELL_MAX_RATIO = 1.4f;

        /// <summary>Number of Bezier arc segments per 90° bend (8 = smooth, 12 = silky).</summary>
        public const int CornerSteps = 10;

        /// <summary>Effective glow alpha (0 when disabled, GLOW_OPACITY when enabled).</summary>
        public static float GlowAlpha => GLOW_ENABLED ? GLOW_OPACITY : 0.0f;

        /// <summary>Glow mesh halfWidth multiplier (how much bigger than the body half-width).</summary>
        public const float GlowWidthMultiplier = 1.20f;

        /// <summary>Highlight stripe width as fraction of strokeWidth.</summary>
        public const float HIGHLIGHT_WIDTH_RATIO = 0.20f;
        /// <summary>Highlight stripe opacity.</summary>
        public const float HIGHLIGHT_ALPHA       = 0.50f;

        // ── Legacy compat constants (mapped to new ratios) ───────────────────
        public const float WidthFraction         = 0.155f;
        public const float HeadLengthFraction    = HEAD_LENGTH_RATIO / 2f;    // 0.80f
        public const float HeadHalfWidthFraction = HEAD_WIDTH_RATIO / 2f;     // 1.20f
        public const float CornerRadiusFraction  = CORNER_RADIUS_RATIO * 2f;  // 1.80f

        // ── Computed helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Computes the clamped visual unit U from the raw cellSize.
        /// U = Clamp(cellSize, UNIT_MIN, UNIT_MAX).
        /// All arrow visual dimensions scale from U, not raw cellSize.
        /// </summary>
        public static float ComputeVisualUnit(float cellSize)
            => Mathf.Clamp(cellSize, UNIT_MIN, UNIT_MAX);

        /// <summary>
        /// Computes halfWidth (half the stroke in world units) from cellSize.
        /// halfWidth = (STROKE_RATIO × U) × 0.5
        /// </summary>
        public static float ComputeHalfWidth(float cellSize)
            => ComputeVisualUnit(cellSize) * STROKE_RATIO * 0.5f;

        /// <summary>
        /// Computes the corner arc radius from halfWidth and cellSize.
        /// r = CORNER_RADIUS_RATIO × fullStrokeWidth, clamped to 0.45 × cellSize.
        /// </summary>
        public static float ComputeCornerRadius(float halfWidth, float cellSize)
        {
            float strokeWidth = halfWidth * 2f;
            float r = CORNER_RADIUS_RATIO * strokeWidth;
            return Mathf.Min(r, 0.45f * cellSize);
        }
    }

    /// <summary>Background gradient colours.</summary>
    public static class BackgroundColors
    {
        public static readonly Color GradientBottom = HexToColor("2A0A9E");
        public static readonly Color GradientTop    = HexToColor("3B14C8");

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }

    /// <summary>Economy – coin costs and level rewards.</summary>
    public static class Economy
    {
        public const int HintCost       = 650;   // coins per hint use
        public const int MagnetCost     = 1300;  // coins per magnet use
        public const int LevelRewardCoin = 30;   // coins awarded on level clear
        public const int LevelRewardStar = 1;    // stars awarded on level clear
        public const int StartHearts     = 3;    // hearts given on first launch
    }

    // NOTE: SceneNames is defined in ArrowPuzzle.Runtime (Constants.cs).
    // Use ArrowPuzzle.Core.SceneNames from that assembly; do not duplicate here.
    // The Game scene file is "Game.unity" but the scene name in Build Settings is "Game".

    /// <summary>Grid and background animation timings.</summary>
    public static class GridConfig
    {
        public const float DotFadeInDuration  = 0.3f;   // dot grid fade-in on level start
        public const float TopPadding         = 1.8f;   // world units reserved for HUD
        public const float BottomPadding      = 0.4f;
        public const float SidePadding        = 0.5f;
        public const float DotRadiusFraction  = 0.08f;  // dot radius as fraction of CellSize
        public const float DotAlpha           = 0.30f;  // 30 % opacity
    }

    /// <summary>Shop product identifiers and pack configs.</summary>
    public static class ShopProductIds
    {
        // ── Google Play Billing Product IDs (must match Play Console exactly) ──
        public const string DisableAds  = "arrow_puzzle_disable_ads";
        public const string Coins1500   = "arrow_puzzle_coins_1500";
        public const string Coins6250   = "arrow_puzzle_coins_6250";
        public const string Coins13500  = "arrow_puzzle_coins_13500";
        public const string Coins25000  = "arrow_puzzle_coins_25000";
        public const string Coins100000 = "arrow_puzzle_coins_100000";
    }

    /// <summary>
    /// AdMob Ad Unit IDs.
    /// Using Google's official TEST IDs — replace with real IDs before shipping.
    /// Real IDs from: https://admob.google.com → Your app → Ad units
    /// </summary>
    public static class AdMobIds
    {
        // ── App ID (set in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset) ──
        // Test App ID (Android) — replace with your real App ID before publishing
        public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";

        // ── Banner Ad Unit ──
        // Test ID: ca-app-pub-3940256099942544/6300978111
        public const string BannerAndroid      = "ca-app-pub-3940256099942544/6300978111";
        // Replace with real: public const string BannerAndroid = "ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN";

        // ── Interstitial Ad Unit (shown every 3–4 levels at Level Cleared screen) ──
        // Test ID: ca-app-pub-3940256099942544/1033173712
        public const string InterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        // Replace with real: public const string InterstitialAndroid = "ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN";

        // ── Rewarded Ad Unit (revive / extra coins) ──
        // Test ID: ca-app-pub-3940256099942544/5224354917
        public const string RewardedAndroid     = "ca-app-pub-3940256099942544/5224354917";
        // Replace with real: public const string RewardedAndroid = "ca-app-pub-XXXXXXXXXXXXXXXX/NNNNNNNNNN";
    }

    /// <summary>
    /// App-wide URLs used in Settings and Play Store listing.
    /// Replace placeholder with your actual hosted URL before publishing.
    /// </summary>
    public static class AppLinks
    {
        // ── Replace this with your real Privacy Policy URL ──
        // E.g. from GitHub Pages, Notion, or a simple HTML host.
        public const string PrivacyPolicyUrl = "https://YOUR_DOMAIN/privacy-policy";

        // ── Android build info ──
        public const string PackageName = "com.yourstudio.arrowpuzzle"; // change to your real package name
    }

    /// <summary>Interstitial ad frequency: show every N levels cleared.</summary>
    public static class AdConfig
    {
        public const int InterstitialEveryNLevels = 3; // show interstitial after every 3rd level
    }
}

