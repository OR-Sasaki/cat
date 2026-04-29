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
using VContainer.Unity;

namespace Shop.Service
{
    public class ShopService : ITickable
    {
        const int NormalFurnitureSlotCount = 6;
        const int NormalOutfitSlotCount = 6;

        static readonly System.Random Rng = new();

        readonly ShopState _state;
        readonly IUserPointService _userPointService;
        readonly IUserItemInventoryService _userItemInventoryService;
        readonly MasterDataState _masterDataState;
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;
        readonly IClock _clock;

        Furniture[]? _cachedFurnitureSource;
        Dictionary<uint, Furniture>? _furnitureLookup;
        Outfit[]? _cachedOutfitSource;
        Dictionary<uint, Outfit>? _outfitLookup;

        bool _isInitialized;

        [Inject]
        public ShopService(
            ShopState state,
            IUserPointService userPointService,
            IUserItemInventoryService userItemInventoryService,
            MasterDataState masterDataState,
            IDialogService dialogService,
            SceneLoader sceneLoader,
            IClock clock)
        {
            _state = state;
            _userPointService = userPointService;
            _userItemInventoryService = userItemInventoryService;
            _masterDataState = masterDataState;
            _dialogService = dialogService;
            _sceneLoader = sceneLoader;
            _clock = clock;
        }

        public void Initialize()
        {
            BuildNormalCategoryLists();

            var snapshot = TimedShopCycleCalculator.Calculate(_clock.UtcNow, TimedShopConstants.UpdateInterval);
            RebuildTimedShop(snapshot);
            _isInitialized = true;
        }

        public void Tick()
        {
            if (!_isInitialized) return;

            var snapshot = TimedShopCycleCalculator.Calculate(_clock.UtcNow, TimedShopConstants.UpdateInterval);
            if (snapshot.CycleId == _state.CurrentCycleId) return;

            RebuildTimedShop(snapshot);
        }

        public TimedShopCycleSnapshot GetCurrentCycleSnapshot()
        {
            return TimedShopCycleCalculator.Calculate(_clock.UtcNow, TimedShopConstants.UpdateInterval);
        }

        void RebuildTimedShop(TimedShopCycleSnapshot snapshot)
        {
            var shopProducts = _masterDataState.ShopProducts ?? System.Array.Empty<ShopProduct>();

            var furnitureSource = new List<ShopProduct>();
            var outfitSource = new List<ShopProduct>();
            for (var i = 0; i < shopProducts.Length; i++)
            {
                var product = shopProducts[i];
                if (product.CurrencyType == CurrencyType.RewardAd)
                    continue;

                if (product.ItemType == ItemType.Furniture) furnitureSource.Add(product);
                else if (product.ItemType == ItemType.Outfit) outfitSource.Add(product);
            }

            if (furnitureSource.Count == 0)
                Debug.LogWarning("[ShopService] No timed-shop furniture master rows.");
            if (outfitSource.Count == 0)
                Debug.LogWarning("[ShopService] No timed-shop outfit master rows.");

            var drawnFurniture = TimedShopLottery.DrawTimedProducts(
                furnitureSource, TimedShopConstants.TimedFurnitureSlotCount, snapshot.Seed);
            var drawnOutfit = TimedShopLottery.DrawTimedProducts(
                outfitSource, TimedShopConstants.TimedOutfitSlotCount, snapshot.Seed);

            var timedFurniture = ProjectToProductDataList(drawnFurniture);
            var timedOutfit = ProjectToProductDataList(drawnOutfit);

            _state.ApplyTimedShopUpdate(snapshot.CycleId, snapshot.NextUpdateAtUtc, timedFurniture, timedOutfit);
        }

        List<ProductData> ProjectToProductDataList(IReadOnlyList<ShopProduct> products)
        {
            var result = new List<ProductData>(products.Count);
            for (var i = 0; i < products.Count; i++)
            {
                var data = BuildProductDataFromShopProduct(products[i]);
                if (data != null) result.Add(data);
            }
            return result;
        }

        void BuildNormalCategoryLists()
        {
            _state.FurnitureProductList.Clear();
            _state.OutfitProductList.Clear();
            _state.RewardAdProductList.Clear();

            var shopProducts = _masterDataState.ShopProducts ?? System.Array.Empty<ShopProduct>();

            var furnitureCount = 0;
            var outfitCount = 0;
            for (var i = 0; i < shopProducts.Length; i++)
            {
                var product = shopProducts[i];
                if (product.ItemType == ItemType.Furniture && furnitureCount < NormalFurnitureSlotCount)
                {
                    var data = BuildProductDataFromShopProduct(product);
                    if (data != null)
                    {
                        _state.FurnitureProductList.Add(data);
                        furnitureCount++;
                    }
                }
                else if (product.ItemType == ItemType.Outfit && outfitCount < NormalOutfitSlotCount)
                {
                    var data = BuildProductDataFromShopProduct(product);
                    if (data != null)
                    {
                        _state.OutfitProductList.Add(data);
                        outfitCount++;
                    }
                }
            }
        }

        ProductData? BuildProductDataFromShopProduct(ShopProduct product)
        {
            string iconPath;
            string displayName;

            switch (product.ItemType)
            {
                case ItemType.Furniture:
                {
                    var lookup = GetFurnitureLookup();
                    if (lookup == null || !lookup.TryGetValue(product.ItemId, out var furniture))
                    {
                        Debug.LogWarning($"[ShopService] Furniture master not found for item_id={product.ItemId} (product_id={product.Id})");
                        return null;
                    }
                    displayName = string.IsNullOrEmpty(product.Name) ? furniture.Name : product.Name;
                    iconPath = ResolveFurnitureIconPath(furniture);
                    break;
                }
                case ItemType.Outfit:
                {
                    var lookup = GetOutfitLookup();
                    if (lookup == null || !lookup.TryGetValue(product.ItemId, out var outfit))
                    {
                        Debug.LogWarning($"[ShopService] Outfit master not found for item_id={product.ItemId} (product_id={product.Id})");
                        return null;
                    }
                    displayName = string.IsNullOrEmpty(product.Name) ? outfit.Name : product.Name;
                    iconPath = ResolveOutfitIconPath(outfit);
                    break;
                }
                default:
                    Debug.LogWarning($"[ShopService] Unsupported ItemType={product.ItemType} (product_id={product.Id})");
                    return null;
            }

            return new ProductData(
                Name: displayName,
                IconPath: iconPath,
                Price: product.Price,
                CurrencyType: product.CurrencyType,
                ProductType: ProductType.Item,
                ItemType: product.ItemType,
                ProductId: product.Id,
                ItemId: product.ItemId
            );
        }

        static string ResolveFurnitureIconPath(Furniture furniture)
        {
            return $"Furnitures/{furniture.Name}";
        }

        static string ResolveOutfitIconPath(Outfit outfit)
        {
            return $"Outfits/{outfit.Name}";
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
            var interactable = !IsSoldOut(data) && IsAffordable(data, balance);
            cell.SetInteractable(interactable);
        }

        public bool IsAffordable(ProductData data, int balance)
        {
            return data.CurrencyType switch
            {
                CurrencyType.Yarn => balance >= data.Price,
                CurrencyType.RealMoney => true,
                CurrencyType.RewardAd => false,
                _ => false,
            };
        }

        public bool IsSoldOut(ProductData data)
        {
            return data.ItemType switch
            {
                ItemType.Outfit => data.ItemId.HasValue && _userItemInventoryService.HasOutfit(data.ItemId.Value),
                ItemType.Furniture => false,
                ItemType.Point => false,
                _ => false,
            };
        }

        public bool IsTimedShopProduct(ProductData data)
        {
            for (var i = 0; i < _state.TimedFurnitureProductList.Count; i++)
            {
                if (ReferenceEquals(_state.TimedFurnitureProductList[i], data)) return true;
            }
            for (var i = 0; i < _state.TimedOutfitProductList.Count; i++)
            {
                if (ReferenceEquals(_state.TimedOutfitProductList[i], data)) return true;
            }
            return false;
        }

        public void GoBack()
        {
            _sceneLoader.Load(Const.SceneName.Home);
        }

        public async UniTask OnProductCellTappedAsync(ProductData data, CancellationToken ct)
        {
            if (data.CurrencyType == CurrencyType.RewardAd)
            {
                // 本フェーズ未対応。将来の Unity Ads 統合点として分岐を残す。
                return;
            }

            if (data.CurrencyType == CurrencyType.Yarn && _userPointService.GetYarnBalance() < data.Price)
                return;

            if (IsSoldOut(data))
                return;

            var isTimed = IsTimedShopProduct(data);
            var cycleAtTap = isTimed ? _state.CurrentCycleId : 0L;

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

            if (isTimed && cycleAtTap != _state.CurrentCycleId)
            {
                await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                    new CommonMessageDialogArgs(
                        Title: "購入できません",
                        Message: "時限ショップが更新されました。"
                    ),
                    ct
                );
                return;
            }

            if (data.CurrencyType == CurrencyType.Yarn)
            {
                var spendOk = TrySpendYarn(data.Price);
                if (!spendOk)
                    return;
            }

            var grantFailed = !TryGrantPurchasedItem(data);
            var yarnPackAddFailed = data.ProductType == ProductType.YarnPack && !TryAddYarnPack(data);

            string completeMessage;
            if (grantFailed)
                completeMessage = $"{data.Name}を購入しました！\n（アイテムの付与に失敗しました）";
            else if (yarnPackAddFailed)
                completeMessage = $"{data.Name}を購入しました！\n（毛糸の加算に失敗しました）";
            else
                completeMessage = $"{data.Name}を購入しました！";

            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "購入完了",
                    Message: completeMessage
                ),
                ct
            );
        }

        bool TryGrantPurchasedItem(ProductData data)
        {
            if (!data.ItemId.HasValue)
                return true;

            switch (data.ItemType)
            {
                case ItemType.Furniture:
                {
                    var result = _userItemInventoryService.AddFurniture(data.ItemId.Value, 1);
                    if (!result.IsSuccess)
                        Debug.LogError($"[ShopService] AddFurniture failed (id={data.ItemId.Value}): {result.Error}");
                    return result.IsSuccess;
                }
                case ItemType.Outfit:
                {
                    var result = _userItemInventoryService.GrantOutfit(data.ItemId.Value);
                    if (!result.IsSuccess)
                        Debug.LogError($"[ShopService] GrantOutfit failed (id={data.ItemId.Value}): {result.Error}");
                    return result.IsSuccess;
                }
                default:
                    return true;
            }
        }

        public async UniTask OnGachaTappedAsync(int gachaIndex, int count, CancellationToken ct)
        {
            if (gachaIndex < 0 || gachaIndex >= _state.GachaList.Count)
                return;

            var gachaData = _state.GachaList[gachaIndex];
            var price = count == 1 ? gachaData.SinglePrice : gachaData.TenPrice;

            if (_userPointService.GetYarnBalance() < price)
            {
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

            var spendOk = TrySpendYarn(price);
            if (!spendOk)
                return;

            var furnitureIds = ExecuteGacha(gachaData, count);

            var grantedNames = new List<string>(furnitureIds.Count);
            var failedCount = 0;
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
                    failedCount++;
                }
            }

            var resultMessage = BuildGachaResultMessage(grantedNames, failedCount);
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "ガチャ結果",
                    Message: resultMessage
                ),
                ct
            );
        }

        static string BuildGachaResultMessage(List<string> grantedNames, int failedCount)
        {
            if (grantedNames.Count == 0)
                return "家具の付与に失敗しました。";

            var message = $"以下の家具を獲得しました！\n{string.Join("\n", grantedNames)}";
            if (failedCount > 0)
                message += "\n\n（一部の家具の付与に失敗しました）";

            return message;
        }

        bool TrySpendYarn(int price)
        {
            var result = _userPointService.SpendYarn(price);
            if (result.IsSuccess)
                return true;

            Debug.LogError($"[ShopService] SpendYarn failed: {result.Error}");
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

        Dictionary<uint, Outfit>? GetOutfitLookup()
        {
            var source = _masterDataState.Outfits;
            if (source == null)
                return null;

            if (!ReferenceEquals(source, _cachedOutfitSource))
            {
                _cachedOutfitSource = source;
                _outfitLookup = new Dictionary<uint, Outfit>(source.Length);
                foreach (var outfit in source)
                {
                    _outfitLookup[outfit.Id] = outfit;
                }
            }
            return _outfitLookup;
        }
    }
}
