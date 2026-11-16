// ============================================================
// AdService.cs  –  Arrow Slide Puzzle
// Singleton MonoBehaviour that wraps Google Mobile Ads (AdMob):
//   • Banner      – shown at bottom unless AdsRemoved
//   • Interstitial – shown every AdConfig.InterstitialEveryNLevels levels cleared
//   • Rewarded     – always available (revive / extra coin)
//
// Uses Google's official TEST IDs at startup.
// Real IDs live in GameConstants.AdMobIds — swap before shipping.
//
// Setup:
//   1. Import Google Mobile Ads Unity Plugin (.unitypackage) from
//      https://github.com/googleads/googleads-mobile-unity/releases
//   2. Set App ID in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset
//   3. Place AdServiceBootstrapper prefab in Splash scene (auto-creates AdService)
//
// PRD Section 7: Assets/_Game/Scripts/Core/AdService.cs
// ============================================================

using System;
using UnityEngine;
using ArrowPuzzle.Systems;

// Conditional compilation: compiles only when Google Mobile Ads SDK is imported
#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace ArrowPuzzle.Core
{
    [DisallowMultipleComponent]
    public class AdService : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────
        public static AdService Instance { get; private set; }

        // ── State ────────────────────────────────────────────────────────────
        private bool   _initialized;
        private int    _levelsSinceLastInterstitial;

        // ── Rewarded callback storage ─────────────────────────────────────────
        private Action<bool> _rewardedCallback;

        // ─────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeAds();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Show banner ad at the bottom of the screen.
        /// No-op if AdsRemoved is true.
        /// </summary>
        public void ShowBanner()
        {
            if (IsAdsRemoved()) return;

#if GOOGLE_MOBILE_ADS
            LoadAndShowBanner();
#else
            Debug.Log("[AdService] ShowBanner called (SDK not imported yet).");
#endif
        }

        /// <summary>
        /// Hide and destroy the banner.
        /// </summary>
        public void HideBanner()
        {
#if GOOGLE_MOBILE_ADS
            DestroyBanner();
#else
            Debug.Log("[AdService] HideBanner called (SDK not imported yet).");
#endif
        }

        /// <summary>
        /// Called by LevelClearedUI after each level.
        /// Shows interstitial every AdConfig.InterstitialEveryNLevels levels.
        /// No-op if AdsRemoved.
        /// </summary>
        public void NotifyLevelCleared(Action onAdClosed = null)
        {
            if (IsAdsRemoved())
            {
                onAdClosed?.Invoke();
                return;
            }

            _levelsSinceLastInterstitial++;
            if (_levelsSinceLastInterstitial >= AdConfig.InterstitialEveryNLevels)
            {
                _levelsSinceLastInterstitial = 0;
#if GOOGLE_MOBILE_ADS
                ShowInterstitial(onAdClosed);
#else
                // No SDK: skip interstitial, proceed immediately
                Debug.Log("[AdService] Interstitial skipped (SDK not imported).");
                onAdClosed?.Invoke();
#endif
            }
            else
            {
                onAdClosed?.Invoke();
            }
        }

        /// <summary>
        /// Show a rewarded ad.
        /// onRewardGranted(true) = user earned reward; (false) = ad failed or skipped.
        /// Rewarded ads are always available even when AdsRemoved.
        /// </summary>
        public void ShowRewarded(Action<bool> onRewardGranted)
        {
            _rewardedCallback = onRewardGranted;

#if GOOGLE_MOBILE_ADS
            LoadAndShowRewarded();
#else
            // Editor/no-SDK fallback: simulate successful reward after 1 frame
            Debug.Log("[AdService] ShowRewarded — SDK not imported. Granting reward immediately (dev mode).");
            StartCoroutine(SimulateRewardCoroutine());
#endif
        }

        // ─────────────────────────────────────────────────────────────────────
        // Initialization
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeAds()
        {
#if GOOGLE_MOBILE_ADS
            MobileAds.Initialize(initStatus =>
            {
                _initialized = true;
                Debug.Log("[AdService] Google Mobile Ads initialized.");

                // Pre-load interstitial & rewarded for fast display
                LoadInterstitial();
                LoadRewarded();

                // Show banner unless ads are removed
                if (!IsAdsRemoved())
                    LoadAndShowBanner();
            });
#else
            Debug.Log("[AdService] Google Mobile Ads SDK not imported. " +
                      "Download from https://github.com/googleads/googleads-mobile-unity/releases");
            _initialized = true;
#endif
        }

        private static bool IsAdsRemoved() =>
            GameState.Instance != null && GameState.Instance.AdsRemoved;

        // ─────────────────────────────────────────────────────────────────────
        // Banner
        // ─────────────────────────────────────────────────────────────────────

#if GOOGLE_MOBILE_ADS
        private BannerView _bannerView;

        private void LoadAndShowBanner()
        {
            DestroyBanner();

            _bannerView = new BannerView(
                AdMobIds.BannerAndroid,
                AdSize.Banner,
                AdPosition.Bottom);

            _bannerView.OnBannerAdLoadFailed += (_, args) =>
                Debug.LogWarning($"[AdService] Banner load failed: {args.GetMessage()}");

            _bannerView.LoadAd(new AdRequest());
            Debug.Log("[AdService] Banner loaded.");
        }

        private void DestroyBanner()
        {
            _bannerView?.Destroy();
            _bannerView = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Interstitial
        // ─────────────────────────────────────────────────────────────────────

        private InterstitialAd _interstitial;
        private Action         _interstitialClosedCallback;

        private void LoadInterstitial()
        {
            InterstitialAd.Load(AdMobIds.InterstitialAndroid, new AdRequest(),
                (ad, loadError) =>
                {
                    if (loadError != null)
                    {
                        Debug.LogWarning($"[AdService] Interstitial load failed: {loadError.GetMessage()}");
                        return;
                    }
                    _interstitial = ad;
                    _interstitial.OnAdFullScreenContentClosed += OnInterstitialClosed;
                    _interstitial.OnAdFullScreenContentFailed += (_, _) => OnInterstitialClosed();
                    Debug.Log("[AdService] Interstitial loaded.");
                });
        }

        private void ShowInterstitial(Action onClosed)
        {
            _interstitialClosedCallback = onClosed;

            if (_interstitial != null && _interstitial.CanShowAd())
            {
                _interstitial.Show();
            }
            else
            {
                Debug.Log("[AdService] Interstitial not ready — skipping.");
                _interstitialClosedCallback?.Invoke();
                _interstitialClosedCallback = null;
                LoadInterstitial(); // pre-load for next time
            }
        }

        private void OnInterstitialClosed()
        {
            _interstitial?.Destroy();
            _interstitial = null;
            LoadInterstitial(); // pre-load next one

            var cb = _interstitialClosedCallback;
            _interstitialClosedCallback = null;
            cb?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rewarded
        // ─────────────────────────────────────────────────────────────────────

        private RewardedAd _rewardedAd;
        private bool       _rewardEarned;

        private void LoadRewarded()
        {
            RewardedAd.Load(AdMobIds.RewardedAndroid, new AdRequest(),
                (ad, loadError) =>
                {
                    if (loadError != null)
                    {
                        Debug.LogWarning($"[AdService] Rewarded load failed: {loadError.GetMessage()}");
                        return;
                    }
                    _rewardedAd = ad;
                    _rewardedAd.OnAdFullScreenContentClosed += OnRewardedClosed;
                    _rewardedAd.OnAdFullScreenContentFailed += (_, _) => OnRewardedFailed();
                    Debug.Log("[AdService] Rewarded ad loaded.");
                });
        }

        private void LoadAndShowRewarded()
        {
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardEarned = false;
                _rewardedAd.Show(reward =>
                {
                    _rewardEarned = true;
                    Debug.Log($"[AdService] Reward earned: {reward.Type} × {reward.Amount}");
                });
            }
            else
            {
                Debug.Log("[AdService] Rewarded ad not ready — loading and granting reward anyway (dev fallback).");
                LoadRewarded();
                // In a real build, show a "Loading…" indicator and retry.
                // For now, fail gracefully:
                _rewardedCallback?.Invoke(false);
                _rewardedCallback = null;
            }
        }

        private void OnRewardedClosed()
        {
            bool earned = _rewardEarned;
            _rewardEarned = false;
            _rewardedAd?.Destroy();
            _rewardedAd = null;
            LoadRewarded();

            _rewardedCallback?.Invoke(earned);
            _rewardedCallback = null;
        }

        private void OnRewardedFailed()
        {
            _rewardedAd?.Destroy();
            _rewardedAd = null;
            LoadRewarded();

            _rewardedCallback?.Invoke(false);
            _rewardedCallback = null;
        }
#endif

        // ─────────────────────────────────────────────────────────────────────
        // Editor/No-SDK fallback coroutines
        // ─────────────────────────────────────────────────────────────────────

#if !GOOGLE_MOBILE_ADS
        private System.Collections.IEnumerator SimulateRewardCoroutine()
        {
            yield return null; // one frame
            Debug.Log("[AdService] Simulated reward granted (no SDK).");
            _rewardedCallback?.Invoke(true);
            _rewardedCallback = null;
        }
#endif

        private void OnDestroy()
        {
            if (Instance == this)
            {
#if GOOGLE_MOBILE_ADS
                DestroyBanner();
                _interstitial?.Destroy();
                _rewardedAd?.Destroy();
#endif
                Instance = null;
            }
        }
    }
}

