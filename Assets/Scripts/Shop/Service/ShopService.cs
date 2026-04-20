#nullable enable

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Root.State;
using Root.View;
using Shop.View;
using Shop.State;
using UnityEngine;
using VContainer;

namespace Shop.Service
{
    public class ShopService
    {
        static readonly System.Random Rng = new();

        readonly ShopState _state;
        readonly IUserPointService _userPointService;
        readonly IUserItemInventoryService _userItemInventoryService;
        readonly MasterDataState _masterDataState;
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;

        Furniture[]? _cachedFurnitureSource;
        Dictionary<uint, Furniture>? _furnitureLookup;

        [Inject]
        public ShopService(
            ShopState state,
            IUserPointService userPointService,
            IUserItemInventoryService userItemInventoryService,
            MasterDataState masterDataState,
            IDialogService dialogService,
            SceneLoader sceneLoader)
        {
            _state = state;
            _userPointService = userPointService;
            _userItemInventoryService = userItemInventoryService;
            _masterDataState = masterDataState;
            _dialogService = dialogService;
            _sceneLoader = sceneLoader;
        }

        public void Initialize()
        {
            InitializeMockData();
        }

        void InitializeMockData()
        {
            _state.GachaList.Clear();
            _state.GachaList.Add(new GachaData(
                SinglePrice: 300,
                TenPrice: 2700,
                RewardFurnitureIds: new uint[] { 1u, 2u, 3u, 4u, 5u }
            ));

            _state.ItemProductList.Clear();
            _state.ItemProductList.Add(new ProductData("経験値ブースト", "ShopProducts/shop_Possession_Paid.png", 100, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("時間短縮チケット", "ShopProducts/shop_Possession_Paid.png", 150, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("レアドロップUP", "ShopProducts/shop_Possession_Paid.png", 200, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("スタミナ回復薬", "ShopProducts/shop_Possession_Paid.png", 80, CurrencyType.Yarn, ProductType.Item));
            _state.ItemProductList.Add(new ProductData("ゴールドブースト", "ShopProducts/shop_Possession_Paid.png", 120, CurrencyType.Yarn, ProductType.Item));

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
            UpdateGachaCellInteractable(cell, data, _userPointService.GetYarnBalance());
        }

        public void SetupProductCell(ProductCellView cell, ProductData data)
        {
            cell.Setup(data);
            UpdateProductCellInteractable(cell, data, _userPointService.GetYarnBalance());
        }

        // 残高変更時に呼ばれる軽量パス。Setup を再実行するとアドレッサブルアイコンが
        // リロードされてしまうため、interactable の更新のみに限定する。
        public void RefreshGachaCellInteractable(GachaCellView cell, int index, int balance)
        {
            if (index < 0 || index >= _state.GachaList.Count)
                return;

            UpdateGachaCellInteractable(cell, _state.GachaList[index], balance);
        }

        public void RefreshProductCellInteractable(ProductCellView cell, ProductData data, int balance)
        {
            UpdateProductCellInteractable(cell, data, balance);
        }

        public int GetYarnBalance() => _userPointService.GetYarnBalance();

        void UpdateGachaCellInteractable(GachaCellView cell, GachaData data, int balance)
        {
            cell.SetButtonsInteractable(balance >= data.SinglePrice, balance >= data.TenPrice);
        }

        void UpdateProductCellInteractable(ProductCellView cell, ProductData data, int balance)
        {
            var interactable = data.CurrencyType == CurrencyType.RealMoney || balance >= data.Price;
            cell.SetInteractable(interactable);
        }

        public void GoBack()
        {
            _sceneLoader.Load(Const.SceneName.Home);
        }

        public async UniTask OnProductCellTappedAsync(ProductData data, CancellationToken ct)
        {
            if (data.CurrencyType == CurrencyType.Yarn && _userPointService.GetYarnBalance() < data.Price)
            {
                await ShowYarnInsufficientAsync("購入できません", ct);
                return;
            }

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

            if (data.CurrencyType == CurrencyType.Yarn)
            {
                var spendOk = await TrySpendYarnAsync(data.Price, "購入できません", ct);
                if (!spendOk)
                    return;
            }

            var yarnPackAddFailed = data.ProductType == ProductType.YarnPack && !TryAddYarnPack(data);

            var completeMessage = yarnPackAddFailed
                ? $"{data.Name}を購入しました！\n（毛糸の加算に失敗しました）"
                : $"{data.Name}を購入しました！";
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "購入完了",
                    Message: completeMessage
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

            if (_userPointService.GetYarnBalance() < price)
            {
                await ShowYarnInsufficientAsync("ガチャを引けません", ct);
                return;
            }

            var confirmResult = await _dialogService.OpenAsync<CommonConfirmDialog, CommonConfirmDialogArgs>(
                new CommonConfirmDialogArgs(
                    Title: "ガチャ確認",
                    Message: $"{count}連ガチャを{price:N0}毛糸で引きますか？"
                ),
                ct
            );

            if (confirmResult != DialogResult.Ok)
                return;

            var spendOk = await TrySpendYarnAsync(price, "ガチャを引けません", ct);
            if (!spendOk)
                return;

            var furnitureIds = ExecuteGacha(gachaData, count);

            // UnknownId / InvalidArgument は部分失敗として該当 ID のみスキップし、残りの結果処理を継続する
            var grantedNames = new List<string>(furnitureIds.Count);
            var failedIds = new List<uint>();
            foreach (var furnitureId in furnitureIds)
            {
                var addResult = _userItemInventoryService.AddFurniture(furnitureId, 1);
                if (addResult.IsSuccess)
                {
                    grantedNames.Add(ResolveFurnitureName(furnitureId));
                }
                else
                {
                    Debug.LogError($"[ShopService] AddFurniture failed (id={furnitureId}): {addResult.Error}");
                    failedIds.Add(furnitureId);
                }
            }

            var resultMessage = BuildGachaResultMessage(grantedNames, failedIds);
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "ガチャ結果",
                    Message: resultMessage
                ),
                ct
            );
        }

        static string BuildGachaResultMessage(List<string> grantedNames, List<uint> failedIds)
        {
            var hasGranted = grantedNames.Count > 0;
            var hasFailed = failedIds.Count > 0;

            var message = hasGranted
                ? $"以下の家具を獲得しました！\n{string.Join("\n", grantedNames)}"
                : "家具の付与に失敗しました。";

            if (hasFailed && hasGranted)
            {
                message += $"\n\n以下の家具は付与に失敗しました: {string.Join(", ", failedIds)}";
            }

            return message;
        }

        async UniTask<bool> TrySpendYarnAsync(int price, string insufficientTitle, CancellationToken ct)
        {
            var result = _userPointService.SpendYarn(price);
            if (result.IsSuccess)
                return true;

            if (result.Error == PointOperationErrorCode.Insufficient)
            {
                await ShowYarnInsufficientAsync(insufficientTitle, ct);
            }
            else
            {
                Debug.LogError($"[ShopService] SpendYarn failed: {result.Error}");
            }
            return false;
        }

        bool TryAddYarnPack(ProductData data)
        {
            if (!data.YarnAmount.HasValue || data.YarnAmount.Value <= 0)
            {
                Debug.LogError($"[ShopService] AddYarn skipped: YarnAmount is null or non-positive ({data.YarnAmount})");
                return true;
            }

            var result = _userPointService.AddYarn(data.YarnAmount.Value);
            if (result.IsSuccess)
                return true;

            Debug.LogError($"[ShopService] AddYarn failed: {result.Error}");
            return result.Error != PointOperationErrorCode.Overflow;
        }

        UniTask ShowYarnInsufficientAsync(string title, CancellationToken ct)
        {
            return _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: title,
                    Message: "毛糸が足りません。"
                ),
                ct
            );
        }

        List<uint> ExecuteGacha(GachaData gachaData, int count)
        {
            var results = new List<uint>(count);
            for (var i = 0; i < count; i++)
            {
                var index = Rng.Next(gachaData.RewardFurnitureIds.Count);
                results.Add(gachaData.RewardFurnitureIds[index]);
            }
            return results;
        }

        string ResolveFurnitureName(uint furnitureId)
        {
            var lookup = GetFurnitureLookup();
            if (lookup != null && lookup.TryGetValue(furnitureId, out var furniture))
                return furniture.Name;
            return furnitureId.ToString();
        }

        Dictionary<uint, Furniture>? GetFurnitureLookup()
        {
            var source = _masterDataState.Furnitures;
            if (source == null)
                return null;

            // マスターデータのインポートで配列が再生成された場合に再構築する
            if (!ReferenceEquals(source, _cachedFurnitureSource))
            {
                _cachedFurnitureSource = source;
                _furnitureLookup = new Dictionary<uint, Furniture>(source.Length);
                foreach (var furniture in source)
                {
                    _furnitureLookup[furniture.Id] = furniture;
                }
            }
            return _furnitureLookup;
        }
    }
}
