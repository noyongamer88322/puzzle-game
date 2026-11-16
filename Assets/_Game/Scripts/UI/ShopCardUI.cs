// ============================================================
// ShopCardUI.cs  –  Arrow Slide Puzzle
// Individual card displayed in the Shop horizontal scroll view.
//
// Elements:
//   • Badge tag ("Best Seller", "Save", etc.)
//   • Item artwork (shop_noads.png / shop_pack_1..5.png)
//   • Title label (e.g. "1 500 Coins")
//   • Subtitle bonus label (e.g. "+1 000 Coins Bonus")
//   • Glossy green price button ($2.99, etc.)
//
// PRD Section 7: Assets/_Game/Scripts/UI/ShopCardUI.cs
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowPuzzle.Core;
using ArrowPuzzle.Systems;

namespace ArrowPuzzle.UI
{
    [DisallowMultipleComponent]
    public class ShopCardUI : MonoBehaviour
    {
        [Header("Product Configuration")]
        public string productId;
        public string displayTitle;
        public string subtitle;
        public string priceText;
        public int coinReward;
        public bool isDisableAds;

        [Header("UI References")]
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI subtitleLabel;
        public Image itemImage;
        public Button priceButton;
        public TextMeshProUGUI priceLabel;
        public GameObject badgeRoot;
        public TextMeshProUGUI badgeLabel;

        [Header("Animation")]
        public float pressScale = 0.92f;
        public float pressDuration = 0.10f;

        public event Action<ShopCardUI> OnPurchased;

        private RectTransform _btnRect;
        private Coroutine _bounceCoroutine;

        private void Awake()
        {
            if (priceButton != null)
            {
                _btnRect = priceButton.GetComponent<RectTransform>();
                priceButton.onClick.AddListener(OnPriceButtonClicked);
            }
        }

        public void Setup(string id, string title, string price, Sprite icon,
                          string badge = null, bool disableAds = false, int coins = 0, string sub = null)
        {
            productId    = id;
            displayTitle = title;
            priceText    = price;
            isDisableAds = disableAds;
            coinReward   = coins;
            subtitle     = sub;

            if (titleLabel != null) titleLabel.text = title;
            if (subtitleLabel != null)
            {
                bool hasSub = !string.IsNullOrEmpty(sub);
                subtitleLabel.gameObject.SetActive(hasSub);
                if (hasSub) subtitleLabel.text = sub;
            }

            if (priceLabel != null) priceLabel.text = price;
            if (itemImage != null && icon != null) itemImage.sprite = icon;

            if (badgeRoot != null)
            {
                bool hasBadge = !string.IsNullOrEmpty(badge);
                badgeRoot.SetActive(hasBadge);
                if (hasBadge && badgeLabel != null)
                {
                    badgeLabel.text = badge;
                }
            }

            RefreshState();
        }

        public void RefreshState()
        {
            if (isDisableAds && IAPManager.Service.IsProductOwned(productId))
            {
                if (priceLabel != null) priceLabel.text = "OWNED";
                if (priceButton != null) priceButton.interactable = false;
            }
            else
            {
                if (priceLabel != null) priceLabel.text = priceText;
                if (priceButton != null) priceButton.interactable = true;
            }
        }

        private void OnPriceButtonClicked()
        {
            if (_bounceCoroutine != null) StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = StartCoroutine(ButtonPressBounce());

            AudioManager.Instance?.PlayButton();

            IAPManager.Service.PurchaseProduct(productId, (success, msg) =>
            {
                if (success)
                {
                    RefreshState();
                    OnPurchased?.Invoke(this);
                }
            });
        }

        private IEnumerator ButtonPressBounce()
        {
            if (_btnRect == null) yield break;

            Vector3 orig = Vector3.one;
            Vector3 squish = Vector3.one * pressScale;
            float t = 0f;

            while (t < pressDuration)
            {
                t += Time.unscaledDeltaTime;
                _btnRect.localScale = Vector3.Lerp(orig, squish, t / pressDuration);
                yield return null;
            }

            t = 0f;
            float bounceDur = pressDuration * 1.5f;
            Vector3 overshoot = Vector3.one * 1.05f;

            while (t < bounceDur)
            {
                t += Time.unscaledDeltaTime;
                float r = t / bounceDur;
                _btnRect.localScale = Vector3.Lerp(squish, r < 0.6f ? overshoot : orig, Mathf.SmoothStep(0f, 1f, r));
                yield return null;
            }

            _btnRect.localScale = orig;
            _bounceCoroutine = null;
        }
    }
}

