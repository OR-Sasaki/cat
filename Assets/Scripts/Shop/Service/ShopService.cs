#nullable enable

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Service;
using Root.State;
using Root.View;
using Shop.RewardAd;
using Shop.View;
using Shop.State;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Shop.Service
{
    public class ShopService : ITickable
    {
        static readonly System.Random Rng = new();

        readonly ShopState _state;
        readonly IUserPointService _userPointService;
        readonly IUserItemInventoryService _userItemInventoryService;
        readonly MasterDataState _masterDataState;
        readonly IDialogService _dialogService;
        readonly SceneLoader _sceneLoader;
        readonly IClock _clock;
        readonly IRewardedAdService _rewardedAdService;
        readonly PlayerPrefsService _playerPrefsService;
        readonly RewardedAdConfig _rewardedAdConfig;

        Furniture[]? _cachedFurnitureSource;
        Dictionary<uint, Furniture>? _furnitureLookup;
        Outfit[]? _cachedOutfitSource;
        Dictionary<uint, Outfit>? _outfitLookup;

        // リワード広告商品の日次視聴消化回数（productId -> 当日カウント）と、それが属する JST 日付
        readonly Dictionary<uint, int> _dailyCountByProductId = new();
        readonly Dictionary<uint, ShopProduct> _rewardAdProductById = new();
        string _currentJstDate = string.Empty;

        // 視聴セッション進行中の productId（多重タップ防止、要件 5-2）
        uint? _processingProductId;

        bool _isInitialized;

        [Inject]
        public ShopService(
            ShopState state,
            IUserPointService userPointService,
            IUserItemInventoryService userItemInventoryService,
            MasterDataState masterDataState,
            IDialogService dialogService,
            SceneLoader sceneLoader,
            IClock clock,
            IRewardedAdService rewardedAdService,
            PlayerPrefsService playerPrefsService,
            RewardedAdConfig rewardedAdConfig)
        {
            _state = state;
            _userPointService = userPointService;
            _userItemInventoryService = userItemInventoryService;
            _masterDataState = masterDataState;
            _dialogService = dialogService;
            _sceneLoader = sceneLoader;
            _clock = clock;
            _rewardedAdService = rewardedAdService;
            _playerPrefsService = playerPrefsService;
            _rewardedAdConfig = rewardedAdConfig;
        }

        public void Initialize()
        {
            BuildRewardAdProductList();
            LoadRewardAdDailyCount();

            var snapshot = TimedShopCycleCalculator.Calculate(_clock.UtcNow, TimedShopConstants.UpdateInterval);
            RebuildTimedShop(snapshot);
            _isInitialized = true;
        }

        // マスターから CurrencyType.RewardAd の商品を抽出し ProductId 昇順で RewardAdProductList を構築する。
        // 通常 (Yarn) 商品の購入フロー・時限ショップ抽選には混入させない。
        void BuildRewardAdProductList()
        {
            _state.RewardAdProductList.Clear();

            var shopProducts = _masterDataState.ShopProducts ?? System.Array.Empty<ShopProduct>();
            var rewardAdProducts = new List<ShopProduct>();
            for (var i = 0; i < shopProducts.Length; i++)
            {
                if (shopProducts[i].CurrencyType == CurrencyType.RewardAd)
                    rewardAdProducts.Add(shopProducts[i]);
            }

            rewardAdProducts.Sort((a, b) => a.Id.CompareTo(b.Id));

            for (var i = 0; i < rewardAdProducts.Count; i++)
            {
                var data = BuildProductDataFromShopProduct(rewardAdProducts[i]);
                if (data != null)
                    _state.RewardAdProductList.Add(data);
            }
        }

        public void Tick()
        {
            if (!_isInitialized) return;
            // 毎フレームのコストを抑える: 次回更新時刻に到達するまでスナップショット計算を省略する
            if (_clock.UtcNow < _state.NextUpdateAt) return;

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
            SplitShopProductsForTimedShop(out var furnitureSource, out var outfitSource);

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

        void SplitShopProductsForTimedShop(out List<ShopProduct> furniture, out List<ShopProduct> outfit)
        {
            var shopProducts = _masterDataState.ShopProducts ?? System.Array.Empty<ShopProduct>();
            furniture = new List<ShopProduct>();
            outfit = new List<ShopProduct>();

            for (var i = 0; i < shopProducts.Length; i++)
            {
                var product = shopProducts[i];
                if (product.CurrencyType == CurrencyType.RewardAd) continue;

                switch (product.ItemType)
                {
                    case ItemType.Furniture: furniture.Add(product); break;
                    case ItemType.Outfit: outfit.Add(product); break;
                    case ItemType.Point: break;
                }
            }
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

        ProductData? BuildProductDataFromShopProduct(ShopProduct product)
        {
            string iconPath;
            string displayName;

            switch (product.ItemType)
            {
                case ItemType.Furniture:
                {
                    var lookup = GetFurnitureLookup();
                    if (lookup is null || !lookup.TryGetValue(product.ItemId, out var furniture))
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
                    if (lookup is null || !lookup.TryGetValue(product.ItemId, out var outfit))
                    {
                        Debug.LogWarning($"[ShopService] Outfit master not found for item_id={product.ItemId} (product_id={product.Id})");
                        return null;
                    }
                    displayName = string.IsNullOrEmpty(product.Name) ? outfit.Name : product.Name;
                    iconPath = ResolveOutfitIconPath(outfit);
                    break;
                }
                case ItemType.Point:
                {
                    displayName = string.IsNullOrEmpty(product.Name) ? "毛糸" : product.Name;
                    iconPath = ResolvePointIconPath(product);
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
                ItemId: product.ItemId,
                Amount: product.Amount
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

        static string ResolvePointIconPath(ShopProduct product)
        {
            return $"Points/{product.Name}";
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
                CurrencyType.RewardAd => data.ProductId.HasValue && IsRewardAdAvailable(data.ProductId.Value),
                _ => false,
            };
        }

        public bool IsSoldOut(ProductData data)
        {
            // リワード広告商品は当日残数 0 でも売り切れ扱い（要件 3-5）
            if (data.CurrencyType == CurrencyType.RewardAd
                && data.ProductId.HasValue
                && GetDailyRemainingCount(data.ProductId.Value) <= 0)
                return true;

            return data.ItemType switch
            {
                ItemType.Outfit => data.ItemId.HasValue && _userItemInventoryService.HasOutfit(data.ItemId.Value),
                ItemType.Furniture => false,
                ItemType.Point => false,
                _ => false,
            };
        }

        // 当該商品の本日残り視聴回数（下限 0）。日付跨ぎを検知したら遡及リセットする（要件 10-6）。
        public int GetDailyRemainingCount(uint productId)
        {
            EnsureFreshDate();
            var count = _dailyCountByProductId.TryGetValue(productId, out var c) ? c : 0;
            var dailyCap = _rewardAdProductById.TryGetValue(productId, out var product) ? product.DailyCap : null;
            return RewardAdDailyCount.ComputeRemaining(count, dailyCap, RewardAdShopConstants.DefaultDailyCap);
        }

        // 視聴可能 = 広告が視聴可能状態かつ当日残数 1 以上
        public bool IsRewardAdAvailable(uint productId)
        {
            return _rewardedAdService.IsReady && GetDailyRemainingCount(productId) >= 1;
        }

        void LoadRewardAdDailyCount()
        {
            BuildRewardAdMasterLookup();

            var currentJstDate = JstDateHelper.ToJstDateString(_clock.UtcNow);

            RewardAdDailyCountSnapshot? snapshot = null;
            try
            {
                snapshot = _playerPrefsService.Load<RewardAdDailyCountSnapshot>(PlayerPrefsKey.RewardAdDailyCount);
            }
            catch (System.Exception e)
            {
                // 初回起動（空文字）や破損時は例外になりうるため握りつぶしてリセット扱いにする
                Debug.LogWarning($"[ShopService] RewardAdDailyCount load failed, resetting: {e.Message}");
            }

            var result = RewardAdDailyCount.Reconcile(snapshot, currentJstDate, _rewardAdProductById.Keys);

            _dailyCountByProductId.Clear();
            foreach (var kv in result.Counts)
                _dailyCountByProductId[kv.Key] = kv.Value;
            _currentJstDate = currentJstDate;

            if (result.RequiresPersist)
                SaveRewardAdDailyCount();
        }

        // マスターから RewardAd 商品の (productId -> ShopProduct) 参照表を再構築する。
        // マスター再インポートで配列が差し替わっても最新集合で同定できるようにする（要件 10-7）。
        void BuildRewardAdMasterLookup()
        {
            _rewardAdProductById.Clear();
            var shopProducts = _masterDataState.ShopProducts ?? System.Array.Empty<ShopProduct>();
            for (var i = 0; i < shopProducts.Length; i++)
            {
                var product = shopProducts[i];
                if (product.CurrencyType != CurrencyType.RewardAd) continue;
                _rewardAdProductById[product.Id] = product;
            }
        }

        void SaveRewardAdDailyCount()
        {
            try
            {
                var snapshot = RewardAdDailyCount.BuildSnapshot(_dailyCountByProductId, _currentJstDate);
                _playerPrefsService.Save(PlayerPrefsKey.RewardAdDailyCount, snapshot);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShopService] {e.Message}\n{e.StackTrace}");
            }
        }

        // JST 日付が変わっていれば全カウントを 0 にリセットして永続化する。
        // リセットが発生したら true を返す。
        bool EnsureFreshDate()
        {
            var currentJstDate = JstDateHelper.ToJstDateString(_clock.UtcNow);
            if (currentJstDate == _currentJstDate)
                return false;

            var keys = new List<uint>(_dailyCountByProductId.Keys);
            for (var i = 0; i < keys.Count; i++)
                _dailyCountByProductId[keys[i]] = 0;

            _currentJstDate = currentJstDate;
            SaveRewardAdDailyCount();
            return true;
        }

        // 報酬付与成功時にカウントを 1 加算して即時永続化する。
        void IncrementDailyCount(uint productId)
        {
            EnsureFreshDate();
            _dailyCountByProductId.TryGetValue(productId, out var current);
            _dailyCountByProductId[productId] = current + 1;
            SaveRewardAdDailyCount();
        }

        public bool IsTimedShopProduct(ProductData data)
        {
            // 通常カテゴリ廃止後はサイクル更新で ProductData が再生成される前提のため、
            // 参照等価ではなく ProductId（マスタ ID）で同定する。
            if (!data.ProductId.HasValue) return false;

            for (var i = 0; i < _state.TimedFurnitureProductList.Count; i++)
            {
                if (_state.TimedFurnitureProductList[i].ProductId == data.ProductId) return true;
            }
            for (var i = 0; i < _state.TimedOutfitProductList.Count; i++)
            {
                if (_state.TimedOutfitProductList[i].ProductId == data.ProductId) return true;
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
                await OnRewardAdProductTappedAsync(data, ct);
                return;
            }

            if (data.CurrencyType == CurrencyType.RealMoney)
            {
                // 課金フローは本機能の対象外。CSV では yarn / reward_ad のみ許容しているため通常は到達しない防御コード。
                Debug.LogWarning("[ShopService] RealMoney purchase flow is not implemented in this phase.");
                return;
            }

            if (data.CurrencyType == CurrencyType.Yarn && _userPointService.GetYarnBalance() < data.Price)
                return;

            if (IsSoldOut(data))
                return;

            // タップ時刻直近の最新サイクル ID を保持し、確認ダイアログ後に再評価することで
            // Tick() 反映前の境界跨ぎでも更新を検知できるようにする。
            long? cycleAtTap = IsTimedShopProduct(data) ? GetCurrentCycleSnapshot().CycleId : null;

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

            if (cycleAtTap.HasValue && cycleAtTap.Value != GetCurrentCycleSnapshot().CycleId)
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

        // リワード広告商品の視聴フロー: 確認ダイアログ → 視聴 → 結果分岐 → 付与 → 結果メッセージ
        public async UniTask OnRewardAdProductTappedAsync(ProductData data, CancellationToken ct)
        {
            if (!data.ProductId.HasValue)
                return;

            var productId = data.ProductId.Value;

            // 視聴可否 (IsReady かつ 残数 >= 1) を確認
            if (!IsRewardAdAvailable(productId))
                return;

            // 多重タップ防止: 別セッション進行中なら弾く
            if (_processingProductId.HasValue)
                return;

            _processingProductId = productId;
            try
            {
                var confirmResult = await _dialogService.OpenAsync<CommonConfirmDialog, CommonConfirmDialogArgs>(
                    new CommonConfirmDialogArgs(
                        Title: "視聴確認",
                        Message: $"広告を視聴して「{data.Name}」を獲得しますか？"
                    ),
                    ct
                );

                if (confirmResult != DialogResult.Ok)
                    return;

                // 確認中に状態が変化している可能性があるため再評価
                if (!IsRewardAdAvailable(productId))
                {
                    await ShowRewardAdMessageAsync("広告を再生できませんでした", ct);
                    return;
                }

                var result = await _rewardedAdService.ShowAsync(_rewardedAdConfig.DefaultPlacementName, ct);

                switch (result)
                {
                    case RewardedAdResult.Rewarded:
                        await HandleRewardedAsync(data, productId, ct);
                        break;
                    case RewardedAdResult.Dismissed:
                        // 誘導文を含めない (要件 6-3)
                        await ShowRewardAdMessageAsync("広告の視聴が中断されました", ct);
                        break;
                    case RewardedAdResult.DisplayFailed:
                        await ShowRewardAdMessageAsync("広告を再生できませんでした", ct);
                        break;
                    case RewardedAdResult.NotReady:
                        await ShowRewardAdMessageAsync("広告を再生できませんでした", ct);
                        break;
                }
            }
            finally
            {
                _processingProductId = null;
            }
        }

        // 報酬獲得確定時の付与処理。付与成功時のみ日次カウントを加算する (要件 10-2)。
        async UniTask HandleRewardedAsync(ProductData data, uint productId, CancellationToken ct)
        {
            var grantSucceeded = TryGrantPurchasedItem(data);
            if (grantSucceeded)
                IncrementDailyCount(productId);

            var message = grantSucceeded
                ? $"「{data.Name}」を獲得しました！"
                : $"「{data.Name}」を獲得しました！\n（アイテムの付与に失敗しました）";

            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "獲得完了",
                    Message: message
                ),
                ct
            );
        }

        async UniTask ShowRewardAdMessageAsync(string message, CancellationToken ct)
        {
            await _dialogService.OpenAsync<CommonMessageDialog, CommonMessageDialogArgs>(
                new CommonMessageDialogArgs(
                    Title: "お知らせ",
                    Message: message
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
                case ItemType.Point:
                {
                    var result = _userPointService.AddYarn(data.Amount);
                    if (!result.IsSuccess)
                        Debug.LogError($"[ShopService] AddYarn failed (amount={data.Amount}): {result.Error}");
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
            return EnsureLookup(_masterDataState.Furnitures, ref _cachedFurnitureSource, ref _furnitureLookup, f => f.Id);
        }

        Dictionary<uint, Outfit>? GetOutfitLookup()
        {
            return EnsureLookup(_masterDataState.Outfits, ref _cachedOutfitSource, ref _outfitLookup, o => o.Id);
        }

        // マスターデータが再インポートされて配列参照が差し替わった場合のみ Lookup を再構築する
        static Dictionary<uint, T>? EnsureLookup<T>(
            T[]? source,
            ref T[]? cachedSource,
            ref Dictionary<uint, T>? cachedLookup,
            System.Func<T, uint> keySelector)
        {
            if (source is null) return null;

            if (!ReferenceEquals(source, cachedSource))
            {
                cachedSource = source;
                cachedLookup = new Dictionary<uint, T>(source.Length);
                foreach (var item in source)
                {
                    cachedLookup[keySelector(item)] = item;
                }
            }
            return cachedLookup;
        }
    }
}
