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
        int? YarnAmount = null // 毛糸パックの場合のみ使用
    );

    public class ShopState
    {
        public ShopTab CurrentTab { get; private set; } = ShopTab.Item;

        public event Action<ShopTab>? OnTabChanged;

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
    }
}