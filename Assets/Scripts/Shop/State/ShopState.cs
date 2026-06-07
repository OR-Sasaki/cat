#nullable enable
using System;
using System.Collections.Generic;

namespace Shop.State
{
    public enum ShopTab
    {
        Item,
        Point
    }

    public enum CurrencyType
    {
        Yarn,
        RealMoney,
        RewardAd
    }

    public enum ProductType
    {
        Item,
        YarnPack
    }

    /// ガチャデータ（価格と排出情報のみ。表示用のテキスト・画像はシーン上に固定配置）
    public record GachaData(
        int SinglePrice,
        int TenPrice,
        IReadOnlyList<uint> RewardFurnitureIds
    );

    public record ProductData(
        string Name,
        string IconPath,
        int Price,
        CurrencyType CurrencyType,
        ProductType ProductType,
        ItemType ItemType,
        uint? ProductId,
        uint? ItemId,
        int? YarnAmount = null, // 毛糸パックの場合のみ使用
        int Amount = 1 // 付与数（RewardAd/Point用）
    );

    public class ShopState
    {
        public ShopTab CurrentTab { get; private set; } = ShopTab.Item;

        public event Action<ShopTab>? OnTabChanged;

        public List<GachaData> GachaList { get; } = new();

        public List<ProductData> RewardAdProductList { get; } = new();
        public List<ProductData> TimedFurnitureProductList { get; } = new();
        public List<ProductData> TimedOutfitProductList { get; } = new();

        public long CurrentCycleId { get; private set; }
        public DateTimeOffset NextUpdateAt { get; private set; }

        public event Action? OnTimedShopUpdated;

        /// リワード広告商品の残り視聴回数が変化したときに発火（視聴成立・JST 日付境界リセット）
        public event Action? OnRewardAdCountsChanged;

        public void NotifyRewardAdCountsChanged()
        {
            OnRewardAdCountsChanged?.Invoke();
        }

        public void SetCurrentTab(ShopTab tab)
        {
            if (tab == CurrentTab)
                return;

            CurrentTab = tab;
            OnTabChanged?.Invoke(tab);
        }

        public void ApplyTimedShopUpdate(
            long cycleId,
            DateTimeOffset nextUpdateAt,
            IReadOnlyList<ProductData> timedFurniture,
            IReadOnlyList<ProductData> timedOutfit)
        {
            TimedFurnitureProductList.Clear();
            TimedFurnitureProductList.AddRange(timedFurniture);

            TimedOutfitProductList.Clear();
            TimedOutfitProductList.AddRange(timedOutfit);

            CurrentCycleId = cycleId;
            NextUpdateAt = nextUpdateAt;

            OnTimedShopUpdated?.Invoke();
        }
    }
}