using System.Collections.Generic;
using Root.Service;
using Shop.View;
using Shop.State;

namespace Shop.Service
{
    public class ShopService
    {
        readonly ShopState _state;
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;

        public ShopService(ShopState state, IDialogService dialogService, SceneLoader sceneLoader)
        {
            _state = state;
            _dialogService = dialogService;
            _sceneLoader = sceneLoader;
        }

        public void Initialize()
        {
            InitializeMockData();
        }

        void InitializeMockData()
        {
            // ガチャデータ（表示用テキスト・画像はシーン上に固定配置）
            _state.GachaList.Clear();
            _state.GachaList.Add(new GachaData(
                SinglePrice: 300,
                TenPrice: 2700,
                RewardFurnitureIds: new List<string> { "chair_01", "table_01", "lamp_01", "sofa_01", "carpet_01" }
            ));

            // アイテムデータ（アイテムタブ用）
            _state.ItemProductList.Clear();
            _state.ItemProductList.Add(new ProductData("経験値ブースト", "Textures/Shop/item_exp_boost", 100, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("時間短縮チケット", "Textures/Shop/item_time_ticket", 150, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("レアドロップUP", "Textures/Shop/item_rare_drop", 200, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("スタミナ回復薬", "Textures/Shop/item_stamina", 80, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("ゴールドブースト", "Textures/Shop/item_gold_boost", 120, CurrencyType.Yarn, ProductType.Item));

            // 毛糸パックデータ（ポイントタブ用）
            _state.PointProductList.Clear();
            _state.PointProductList.Add(new ProductData("毛糸パック S", "Textures/Shop/yarn_pack_s", 120, CurrencyType.RealMoney, ProductType.YarnPack, YarnAmount: 100));
            _state.PointProductList.Add(new ProductData("毛糸パック M", "Textures/Shop/yarn_pack_m", 480, CurrencyType.RealMoney, ProductType.YarnPack, YarnAmount: 500));
            _state.PointProductList.Add(new ProductData("毛糸パック L", "Textures/Shop/yarn_pack_l", 960, CurrencyType.RealMoney, ProductType.YarnPack, YarnAmount: 1200));
        }

        public void SetCurrentTab(ShopTab tab)
        {
            _state.SetCurrentTab(tab);
        }

        public void SetupGachaCell(GachaCellView cell, int index)
        {
            if (index < 0 || index >= _state.GachaList.Count)
                return;

            var data = _state.GachaList[index];
            cell.Setup(index);
            UpdateGachaCellInteractable(cell, data);
        }

        public void SetupProductCell(ProductCellView cell, ProductData data)
        {
            cell.Setup(data);
            UpdateProductCellInteractable(cell, data);
        }

        void UpdateGachaCellInteractable(GachaCellView cell, GachaData data)
        {
            var canAffordSingle = _state.YarnBalance >= data.SinglePrice;
            var canAffordTen = _state.YarnBalance >= data.TenPrice;
            cell.SetButtonsInteractable(canAffordSingle, canAffordTen);
        }

        void UpdateProductCellInteractable(ProductCellView cell, ProductData data)
        {
            // 毛糸通貨の場合のみ残高チェック、リアルマネーは常にinteractable
            var interactable = data.CurrencyType == CurrencyType.RealMoney || _state.YarnBalance >= data.Price;
            cell.SetInteractable(interactable);
        }

        public void GoBack()
        {
            _sceneLoader.Load(Const.SceneName.Home);
        }
    }
}