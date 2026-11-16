// ============================================================
// MockIAPService.cs  –  Arrow Slide Puzzle
// Mock implementation of IAPService for development/testing.
// Adds coins to GameState and marks ads removed.
//
// PRD Section 7: Assets/_Game/Scripts/Core/MockIAPService.cs
// ============================================================

using System;
using UnityEngine;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.Core
{
    public class MockIAPService : IAPService
    {
        private static MockIAPService _instance;
        public static MockIAPService Instance => _instance ??= new MockIAPService();

        public static event Action<string, bool> OnPurchaseProcessed;

        public bool IsInitialized { get; private set; } = true;

        public void Initialize(Action<bool> onComplete = null)
        {
            IsInitialized = true;
            onComplete?.Invoke(true);
        }

        public void PurchaseProduct(string productId, Action<bool, string> onComplete)
        {
            Debug.Log($"[MockIAPService] Processing purchase for product: '{productId}'");

            bool success = true;
            string message = "Purchase completed";

            switch (productId)
            {
                case ShopProductIds.DisableAds:
                    GameState.Instance?.RemoveAds();
                    GameState.Instance?.AddCoins(1000); // 1000 coin bonus
                    message = "Disable Ads (+1,000 Coins) purchased!";
                    Debug.Log("[MockIAPService] Disable Ads purchased! AdsRemoved=true, +1000 Coins.");
                    break;

                case ShopProductIds.Coins1500:
                    GameState.Instance?.AddCoins(1500);
                    message = "1,500 Coins added!";
                    Debug.Log("[MockIAPService] 1,500 Coins added.");
                    break;

                case ShopProductIds.Coins6250:
                    GameState.Instance?.AddCoins(6250);
                    message = "6,250 Coins added!";
                    Debug.Log("[MockIAPService] 6,250 Coins added.");
                    break;

                case ShopProductIds.Coins13500:
                    GameState.Instance?.AddCoins(13500);
                    message = "13,500 Coins added!";
                    Debug.Log("[MockIAPService] 13,500 Coins added.");
                    break;

                case ShopProductIds.Coins25000:
                    GameState.Instance?.AddCoins(25000);
                    message = "25,000 Coins added!";
                    Debug.Log("[MockIAPService] 25,000 Coins added.");
                    break;

                case ShopProductIds.Coins100000:
                    GameState.Instance?.AddCoins(100000);
                    message = "100,000 Coins added!";
                    Debug.Log("[MockIAPService] 100,000 Coins added.");
                    break;

                default:
                    success = false;
                    message = $"Unknown product ID: {productId}";
                    Debug.LogWarning($"[MockIAPService] {message}");
                    break;
            }

            OnPurchaseProcessed?.Invoke(productId, success);
            onComplete?.Invoke(success, message);
        }

        public bool IsProductOwned(string productId)
        {
            if (productId == ShopProductIds.DisableAds)
            {
                return GameState.Instance != null && GameState.Instance.AdsRemoved;
            }
            return false;
        }

        public void RestorePurchases(Action<bool, string> onComplete)
        {
            Debug.Log("[MockIAPService] Restore Purchases — mock (checks AdsRemoved flag).");
            bool adsOwned = IsProductOwned(ShopProductIds.DisableAds);
            onComplete?.Invoke(true, adsOwned ? "Restored: Disable Ads." : "Nothing to restore.");
        }
    }
}

