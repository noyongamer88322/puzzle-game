// ============================================================
// AdServiceBootstrapper.cs  –  Arrow Slide Puzzle
// Attach to a persistent GameObject in the Splash scene.
// Initializes AdService and GooglePlayIAPService at app start,
// then stays alive for the full session (DontDestroyOnLoad).
//
// Also switches IAPManager.Service from MockIAPService to
// GooglePlayIAPService in production builds.
//
// PRD Section 7: Assets/_Game/Scripts/Core/AdServiceBootstrapper.cs
// ============================================================

using UnityEngine;

namespace ArrowPuzzle.Core
{
    [DisallowMultipleComponent]
    public class AdServiceBootstrapper : MonoBehaviour
    {
        [Header("IAP")]
        [Tooltip("If true, use GooglePlayIAPService. If false, keep MockIAPService (Editor/QA).")]
        public bool useRealIAP = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // ── Initialize IAP ───────────────────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
            if (useRealIAP)
            {
                var iapService = GooglePlayIAPService.Instance;
                IAPManager.Service = iapService;
                iapService.Initialize(success =>
                {
                    Debug.Log($"[AdServiceBootstrapper] IAP Init: {(success ? "OK" : "FAILED")}");
                });
            }
#else
            Debug.Log("[AdServiceBootstrapper] Using MockIAPService (Editor or non-Android build).");
#endif

            // ── Initialize AdService (adds component to this GO) ─────────────────
            if (AdService.Instance == null)
                gameObject.AddComponent<AdService>();
        }
    }
}

