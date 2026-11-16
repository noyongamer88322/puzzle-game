// ============================================================
// IAPService.cs  –  Arrow Slide Puzzle
// Interface for In-App Purchase service (mocked now, replaceable
// with Google Play Billing later).
//
// PRD Section 7: Assets/_Game/Scripts/Core/IAPService.cs
// ============================================================

using System;

namespace ArrowPuzzle.Core
{
    /// <summary>
    /// Contract for in-app purchase operations.
    /// Fulfilled by MockIAPService (dev) or GooglePlayIAPService (production).
    /// </summary>
    public interface IAPService
    {
        bool IsInitialized { get; }
        void Initialize(Action<bool> onComplete = null);
        void PurchaseProduct(string productId, Action<bool, string> onComplete);
        bool IsProductOwned(string productId);
        /// <summary>Restore non-consumable purchases (e.g. Disable Ads).</summary>
        void RestorePurchases(Action<bool, string> onComplete);
    }

    /// <summary>Alias with standard interface naming prefix.</summary>
    public interface IIAPService : IAPService { }

    /// <summary>
    /// Service locator / manager for active IAP provider.
    /// </summary>
    public static class IAPManager
    {
        private static IAPService _service;
        public static IAPService Service
        {
            get => _service ??= MockIAPService.Instance;
            set => _service = value;
        }
    }
}

