#nullable enable

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Root.View;
using Shop.View;
using Shop.State;
using VContainer;

namespace Shop.Service
{
    public class ShopService
    {
        readonly ShopState _state;
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;

        [Inject]
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
                RewardFurnitureIds: new uint[] { 1u, 2u, 3u, 4u, 5u }
            ));

            // アイテムデータ（アイテムタブ用）
            _state.ItemProductList.Clear();
            _state.ItemProductList.Add(new ProductData("経験値ブースト", "ShopProducts/shop_Possession_Paid.png", 100, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("時間短縮チケット", "ShopProducts/shop_Possession_Paid.png", 150, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("レアドロップUP", "ShopProducts/shop_Possession_Paid.png", 200, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("スタミナ回復薬", "ShopProducts/shop_Possession_Paid.png", 80, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("ゴールドブースト", "ShopProducts/shop_Possession_Paid.png", 120, CurrencyType.Yarn, ProductType.Item));

            // 毛糸パックデータ（ポイントタブ用）
            _state.PointProductList.Clear();
            _state.PointProductList.Add(new ProductData("毛糸パック S", "ShopProducts/shop_Possession_Paid.png", 120, CurrencyType.RealMoney, ProductType.YarnPack, YarnAmount: 100));
            _state.PointProductList.Add(new ProductData("毛糸パック M", "ShopProducts/shop_Possession_Paid.png", 480, CurrencyType.RealMoney, ProductType.YarnPack, YarnAmount: 500));
            _state.PointProductList.Add(new ProductData("毛糸パック L", "ShopProducts/shop_Possession_Paid.png", 960, CurrencyType.RealMoney, ProductType.YarnPack, YarnAmount: 1200));
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

        public async UniTask OnProductCellTappedAsync(ProductData data, CancellationToken ct)
        {
            // 毛糸通貨の場合は残高チェック
            if (data.CurrencyType == CurrencyType.Yarn && _state.YarnBalance < data.Price)
            {
                await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                    new CommonMessageDialogArgs(
                        Title: "購入できません",
                        Message: "毛糸が足りません。"
                    ),
                    ct
                );
                return;
            }

            // 購入確認ダイアログを表示
            var currencyLabel = data.CurrencyType == CurrencyType.Yarn ? "毛糸" : "円";
            var confirmResult = await _dialogService.OpenAsync<CommonConfirmDialog, CommonConfirmDialogArgs>(
                new CommonConfirmDialogArgs(
                    Title: "購入確認",
                    Message: $"{data.Name}を{data.Price:N0}{currencyLabel}で購入しますか？"
                ),
                ct
            );

            if (confirmResult != DialogResult.Ok)
                return;

            // 購入処理（モック）
            if (data.CurrencyType == CurrencyType.Yarn)
            {
                _state.ConsumeYarn(data.Price);
            }

            // 毛糸パックの場合は毛糸を追加
            if (data.ProductType == ProductType.YarnPack && data.YarnAmount.HasValue)
            {
                _state.AddYarn(data.YarnAmount.Value);
            }

            // 購入完了メッセージを表示
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "購入完了",
                    Message: $"{data.Name}を購入しました！"
                ),
                ct
            );
        }

        public async UniTask OnGachaTappedAsync(int gachaIndex, int count, CancellationToken ct)
        {
            if (gachaIndex < 0 || gachaIndex >= _state.GachaList.Count)
                return;

            var gachaData = _state.GachaList[gachaIndex];
            var price = count == 1 ? gachaData.SinglePrice : gachaData.TenPrice;

            // 残高チェック
            if (_state.YarnBalance < price)
            {
                await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                    new CommonMessageDialogArgs(
                        Title: "ガチャを引けません",
                        Message: "毛糸が足りません。"
                    ),
                    ct
                );
                return;
            }

            // ガチャ確認ダイアログを表示
            var confirmResult = await _dialogService.OpenAsync<CommonConfirmDialog, CommonConfirmDialogArgs>(
                new CommonConfirmDialogArgs(
                    Title: "ガチャ確認",
                    Message: $"{count}連ガチャを{price:N0}毛糸で引きますか？"
                ),
                ct
            );

            if (confirmResult != DialogResult.Ok)
                return;

            // 毛糸を消費
            _state.ConsumeYarn(price);

            // ガチャ実行（モック）- ランダムに家具を選出
            var results = ExecuteGacha(gachaData, count);

            // ガチャ結果を表示
            var resultMessage = $"以下の家具を獲得しました！\n{string.Join("\n", results)}";
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "ガチャ結果",
                    Message: resultMessage
                ),
                ct
            );
        }

        List<uint> ExecuteGacha(GachaData gachaData, int count)
        {
            var results = new List<uint>();
            var random = new System.Random();

            for (int i = 0; i < count; i++)
            {
                var index = random.Next(gachaData.RewardFurnitureIds.Count);
                results.Add(gachaData.RewardFurnitureIds[index]);
            }

            return results;
        }
    }
}
