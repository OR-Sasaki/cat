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
        RealMoney
    }

    public enum ProductType
    {
        Item,
        YarnPack
    }

    public record GachaData(
        string Name,
        string ThumbnailPath,
        int SinglePrice,
        int TenPrice,
        List<string> RewardFurnitureIds
    );

    public record ProductData(
        string Name,
        string IconPath,
        int Price,
        CurrencyType CurrencyType,
        ProductType ProductType,
        int? YarnAmount = null // 毛糸パックの場合のみ使用
    );

    public class ShopState
    {
        public ShopTab CurrentTab { get; private set; } = ShopTab.Item;

        // モック用初期値（開発用）
        public int YarnBalance { get; private set; } = 10000;

        public event Action<ShopTab>? OnTabChanged;
        public event Action<int>? OnYarnBalanceChanged;

        // モックデータリスト - ShopService.Initialize()でテストデータを設定
        public List<GachaData> GachaList { get; } = new();
        public List<ProductData> ItemProductList { get; } = new();
        public List<ProductData> PointProductList { get; } = new();

        public void SetCurrentTab(ShopTab tab)
        {
            if (tab == CurrentTab)
                return;

            CurrentTab = tab;
            OnTabChanged?.Invoke(tab);
        }

        public void ConsumeYarn(int amount)
        {
            if (amount <= 0)
                return;

            YarnBalance = Math.Max(0, YarnBalance - amount);
            OnYarnBalanceChanged?.Invoke(YarnBalance);
        }

        public void AddYarn(int amount)
        {
            if (amount <= 0)
                return;

            YarnBalance += amount;
            OnYarnBalanceChanged?.Invoke(YarnBalance);
        }
    }
}