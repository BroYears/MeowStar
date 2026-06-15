using UnityEngine;
using UnityEngine.UI;
using Nyangsta.Economy;
using Nyangsta.Ads;
using Nyangsta.Save;

namespace Nyangsta.UI
{
    /// <summary>
    /// Offline-earnings settlement modal shown on launch when idle income has
    /// accrued. Claim it, or watch a rewarded ad to double it (graceful 1x fallback
    /// if the ad fails). Built on demand into the controller's modal layer.
    /// </summary>
    public static class OfflinePopup
    {
        public static void Present(RectTransform modalLayer, double gold)
        {
            if (modalLayer == null || gold <= 0) return;

            var root = UIFactory.Rect("OfflinePopup", modalLayer);
            UIFactory.FillParent(root);
            var dim = UIFactory.Image("Dim", root, UITheme.Square, new Color(0, 0, 0, 0.6f));
            UIFactory.FillParent(dim.rectTransform);   // raycastTarget blocks the game behind

            var card = UIFactory.Panel("Card", root, UITheme.Panel, out _, shadow: true, outline: true);
            UIFactory.SetAnchors(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Size(card, 680, 560);

            var banner = UIFactory.Image("Banner", card, UITheme.Rounded, UITheme.GoldDeep, Image.Type.Sliced);
            UIFactory.SetAnchors(banner.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            UIFactory.Size(banner.rectTransform, 680, 120);
            UIFactory.Text("Title", banner.transform, "다녀오신 사이…", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.pivot = new Vector2(0.5f, 0.5f);

            UIFactory.Text("Sub", card, "직원들이 식당을 지키며 골드를 모았어요!", 25, UITheme.InkSoft, TextAnchor.UpperCenter)
                .rectTransform.anchoredPosition = new Vector2(0, -150);

            var coin = UIFactory.Image("Coin", card, UITheme.Circle, UITheme.Gold);
            UIFactory.SetAnchors(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UIFactory.Size(coin.rectTransform, 110, 110);
            coin.rectTransform.anchoredPosition = new Vector2(-150, 40);
            UIFactory.Text("G", coin.transform, "G", 56, UITheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var amount = UIFactory.Text("Amount", card, $"+{Num.Short(gold)}", 64, UITheme.GoldDeep, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.SetAnchors(amount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f));
            UIFactory.Size(amount.rectTransform, 360, 90);
            amount.rectTransform.anchoredPosition = new Vector2(-80, 40);

            bool claimed = false;

            void Close()
            {
                if (root != null) UITween.FadeOutDestroy(root.gameObject, 0.2f);
            }

            var claimBtn = UIFactory.Button("Claim", card, "수령하기", UITheme.LeafDark, null, 30);
            UIFactory.SetAnchors(claimBtn.rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(claimBtn.rect, 600, 92);
            claimBtn.rect.anchoredPosition = new Vector2(0, 40);

            var doubleBtn = UIFactory.Button("Double", card, "광고 보고 2배 받기", UITheme.GoldDeep, null, 30);
            UIFactory.SetAnchors(doubleBtn.rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            UIFactory.Size(doubleBtn.rect, 600, 96);
            doubleBtn.rect.anchoredPosition = new Vector2(0, 144);

            claimBtn.button.onClick.AddListener(() =>
            {
                if (claimed) return;
                claimed = true;
                ClaimOffline(gold, false);
                Audio.Sfx.Coin();
                Close();
            });

            doubleBtn.button.onClick.AddListener(() =>
            {
                if (claimed) return;
                var ad = AdManager.Instance;
                System.Action<bool> reward = ok =>
                {
                    if (claimed) return;
                    claimed = true;
                    ClaimOffline(gold, ok);
                    Audio.Sfx.Purchase();
                    if (ok) Toast.Show("오프라인 수익 2배 획득!", UITheme.Gold);
                    Close();
                };
                if (ad != null) ad.ShowRewarded(reward); else reward(true);
            });

            UITween.PopIn(card, 0.34f, 0.8f);
            Audio.Sfx.Ding();
        }

        private static void ClaimOffline(double gold, bool doubled)
        {
            var idle = IdleIncomeManager.Instance;
            if (idle != null)
            {
                idle.ClaimOffline(gold, doubled);
            }
            else
            {
                EconomyManager.Instance?.AddGold(doubled ? gold * 2 : gold);
            }

            SaveManager.Instance?.Save();
        }
    }
}
