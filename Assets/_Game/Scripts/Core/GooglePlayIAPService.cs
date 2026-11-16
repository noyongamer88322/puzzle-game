// ============================================================
// GooglePlayIAPService.cs  –  Arrow Slide Puzzle
// Real Unity IAP implementation for Google Play Billing.
// Replaces MockIAPService in production builds.
//
// Setup steps (see also: Publishing Checklist artifact):
//   1. Install Unity IAP via Window → Package Manager → Unity IAP
//   2. Enable In-App Purchasing in Services window
//   3. Add product IDs in Play Console matching ShopProductIds constants
//   4. Call GooglePlayIAPService.Instance.Initialize() from a boot MonoBehaviour
//
// PRD Section 7: Assets/_Game/Scripts/Core/GooglePlayIAPService.cs
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using ArrowPuzzle.Systems;

// Unity IAP namespace — requires com.unity.purchasing package
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace ArrowPuzzle.Core
{
#if UNITY_PURCHASING
    /// <summary>
    /// Production IAP service using Unity IAP / Google Play Billing.
    /// Register as IAPManager.Service during app boot (e.g. in SplashScreenController).
    /// </summary>
    public class GooglePlayIAPService : IAPService,
        IDetailedStoreListener
    {
        // ── Singleton ───────────────────────────────────────────────────────
        private static GooglePlayIAPService _instance;
        public static GooglePlayIAPService Instance => _instance ??= new GooglePlayIAPService();

        // ── State ────────────────────────────────────────────────────────────
        public bool IsInitialized { get; private set; }

        private IStoreController   _storeController;
        private IExtensionProvider _extensionProvider;
        private Action<bool>       _initCallback;
        private readonly Dictionary<string, Action<bool, string>> _pendingCallbacks
            = new Dictionary<string, Action<bool, string>>();

        // ── IAPService: Initialize ───────────────────────────────────────────
        public void Initialize(Action<bool> onComplete = null)
        {
            if (IsInitialized)
            {
                onComplete?.Invoke(true);
                return;
            }

            _initCallback = onComplete;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // ── Register products ────────────────────────────────────────────
            // Non-consumables (owned permanently)
            builder.AddProduct(ShopProductIds.DisableAds, ProductType.NonConsumable);

            // Consumables (can be bought multiple times)
            builder.AddProduct(ShopProductIds.Coins1500,   ProductType.Consumable);
            builder.AddProduct(ShopProductIds.Coins6250,   ProductType.Consumable);
            builder.AddProduct(ShopProductIds.Coins13500,  ProductType.Consumable);
            builder.AddProduct(ShopProductIds.Coins25000,  ProductType.Consumable);
            builder.AddProduct(ShopProductIds.Coins100000, ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
            Debug.Log("[GooglePlayIAPService] Initializing Unity IAP…");
        }

        // ── IAPService: Purchase ─────────────────────────────────────────────
        public void PurchaseProduct(string productId, Action<bool, string> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(false, "IAP not initialized.");
                return;
            }

            var product = _storeController.products.WithID(productId);
            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(false, $"Product '{productId}' not available.");
                return;
            }

            if (_pendingCallbacks.ContainsKey(productId))
                _pendingCallbacks[productId] = onComplete;
            else
                _pendingCallbacks.Add(productId, onComplete);

            Debug.Log($"[GooglePlayIAPService] Initiating purchase: {productId}");
            _storeController.InitiatePurchase(product);
        }

        // ── IAPService: Restore ──────────────────────────────────────────────
        /// <summary>
        /// Restores non-consumable purchases. Call from SettingsPanelUI Restore button.
        /// On Android, purchases are auto-restored at init; this triggers a manual refresh.
        /// </summary>
        public void RestorePurchases(Action<bool, string> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(false, "IAP not initialized.");
                return;
            }

            // On Android, Unity IAP auto-restores at initialization.
            // IAppleExtensions.RestoreTransactions() is iOS-only.
            // We re-check the NonConsumable ownership directly:
            bool adsOwned = IsProductOwned(ShopProductIds.DisableAds);
            if (adsOwned)
            {
                GameState.Instance?.RemoveAds();
                Debug.Log("[GooglePlayIAPService] Restore: DisableAds found — AdsRemoved=true.");
            }
            onComplete?.Invoke(true, adsOwned ? "Purchases restored!" : "No purchases to restore.");
        }

        // ── IAPService: IsProductOwned ───────────────────────────────────────
        public bool IsProductOwned(string productId)
        {
            if (!IsInitialized || _storeController == null) return false;
            var product = _storeController.products.WithID(productId);
            return product != null && product.hasReceipt;
        }

        // ── IStoreListener: OnInitialized ────────────────────────────────────
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController   = controller;
            _extensionProvider = extensions;
            IsInitialized      = true;
            Debug.Log("[GooglePlayIAPService] Unity IAP initialized successfully.");

            // Auto-restore non-consumables on init
            if (IsProductOwned(ShopProductIds.DisableAds))
            {
                GameState.Instance?.RemoveAds();
                Debug.Log("[GooglePlayIAPService] Auto-restored: DisableAds.");
            }

            _initCallback?.Invoke(true);
            _initCallback = null;
        }

        // ── IStoreListener: OnInitializeFailed ───────────────────────────────
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"[GooglePlayIAPService] IAP init failed: {error}");
            _initCallback?.Invoke(false);
            _initCallback = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[GooglePlayIAPService] IAP init failed: {error} — {message}");
            _initCallback?.Invoke(false);
            _initCallback = null;
        }

        // ── IStoreListener: ProcessPurchase ──────────────────────────────────
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string productId = args.purchasedProduct.definition.id;
            Debug.Log($"[GooglePlayIAPService] Purchase success: {productId}");

            bool   success = true;
            string message = "Purchase completed";

            switch (productId)
            {
                case ShopProductIds.DisableAds:
                    GameState.Instance?.RemoveAds();
                    GameState.Instance?.AddCoins(1000); // bonus 1 000 coins
                    message = "Disable Ads (+1,000 Coins) purchased!";
                    break;

                case ShopProductIds.Coins1500:
                    GameState.Instance?.AddCoins(1500);
                    message = "1,500 Coins added!";
                    break;

                case ShopProductIds.Coins6250:
                    GameState.Instance?.AddCoins(6250);
                    message = "6,250 Coins added!";
                    break;

                case ShopProductIds.Coins13500:
                    GameState.Instance?.AddCoins(13500);
                    message = "13,500 Coins added!";
                    break;

                case ShopProductIds.Coins25000:
                    GameState.Instance?.AddCoins(25000);
                    message = "25,000 Coins added!";
                    break;

                case ShopProductIds.Coins100000:
                    GameState.Instance?.AddCoins(100000);
                    message = "100,000 Coins added!";
                    break;

                default:
                    success = false;
                    message = $"Unknown product: {productId}";
                    Debug.LogWarning($"[GooglePlayIAPService] {message}");
                    break;
            }

            if (_pendingCallbacks.TryGetValue(productId, out var cb))
            {
                cb?.Invoke(success, message);
                _pendingCallbacks.Remove(productId);
            }

            return PurchaseProcessingResult.Complete;
        }

        // ── IStoreListener: OnPurchaseFailed ─────────────────────────────────
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string productId = product?.definition?.id ?? "unknown";
            string message   = $"Purchase failed: {failureReason}";
            Debug.LogError($"[GooglePlayIAPService] {message} for product: {productId}");

            if (_pendingCallbacks.TryGetValue(productId, out var cb))
            {
                cb?.Invoke(false, message);
                _pendingCallbacks.Remove(productId);
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            string productId = product?.definition?.id ?? "unknown";
            string message   = $"Purchase failed: {failureDescription?.reason} — {failureDescription?.message}";
            Debug.LogError($"[GooglePlayIAPService] {message} for product: {productId}");

            if (_pendingCallbacks.TryGetValue(productId, out var cb))
            {
                cb?.Invoke(false, message);
                _pendingCallbacks.Remove(productId);
            }
        }
    }
#else
    // ── Fallback when Unity IAP package is not yet installed ─────────────────
    // This stub prevents compile errors. Once com.unity.purchasing is in
    // Package Manager, the real implementation above will compile instead.
    public class GooglePlayIAPService : IAPService
    {
        private static GooglePlayIAPService _instance;
        public static GooglePlayIAPService Instance => _instance ??= new GooglePlayIAPService();

        public bool IsInitialized => false;

        public void Initialize(Action<bool> onComplete = null)
        {
            Debug.LogWarning("[GooglePlayIAPService] Unity IAP package not installed. Using stub.");
            onComplete?.Invoke(false);
        }

        public void PurchaseProduct(string productId, Action<bool, string> onComplete)
        {
            Debug.LogWarning("[GooglePlayIAPService] Unity IAP package not installed.");
            onComplete?.Invoke(false, "IAP package not installed.");
        }

        public bool IsProductOwned(string productId) => false;

        public void RestorePurchases(Action<bool, string> onComplete)
        {
            onComplete?.Invoke(false, "IAP package not installed.");
        }
    }
#endif
}

