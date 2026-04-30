#nullable enable
using System;
using System.Collections.Generic;
using Root.Service;
using Shop.Service;
using Shop.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Shop.View
{
    public class ShopView : MonoBehaviour
    {
        [Header("Tab Buttons (legacy, kept for scene compatibility)")]
        [SerializeField] Button? _itemTabButton;
        [SerializeField] Button? _pointTabButton;

        [Header("Tab Content (legacy, kept for scene compatibility)")]
        [SerializeField] GameObject? _itemContent;
        [SerializeField] GameObject? _pointContent;

        [Header("Navigation")]
        [SerializeField] Button? _backButton;

        [Header("Tab Visual (legacy, kept for scene compatibility)")]
        [SerializeField] Image? _itemTabImage;
        [SerializeField] Image? _pointTabImage;
        [SerializeField] Sprite? _tabSelectedSprite;
        [SerializeField] Sprite? _tabUnselectedSprite;
        [SerializeField] TMP_Text? _itemTabText;
        [SerializeField] TMP_Text? _pointTabText;
        [SerializeField] Color _tabSelectedTextColor = Color.white;
        [SerializeField] Color _tabUnselectedTextColor = Color.gray;

        [Header("Yarn Balance Display")]
        [SerializeField] TMP_Text? _yarnBalanceText;

        [Header("Gacha Cells")]
        [SerializeField] List<GachaCellView> _gachaCells = new();

        [Header("Item Cells (legacy, kept for scene compatibility)")]
        [SerializeField] List<ProductCellView> _itemCells = new();

        [Header("Point Cells (legacy, kept for scene compatibility)")]
        [SerializeField] List<ProductCellView> _pointCells = new();

        [Header("Reward-Ad Cells (placeholder, 0 cells in this phase)")]
        [SerializeField] List<ProductCellView> _rewardAdCells = new();

        [Header("Timed Shop Furniture Cells")]
        [SerializeField] List<ProductCellView> _timedFurnitureCells = new();

        [Header("Timed Shop Outfit Cells")]
        [SerializeField] List<ProductCellView> _timedOutfitCells = new();

        public event Action? OnBackButtonClicked;

        ShopState? _state;
        ShopService? _shopService;
        IUserPointService? _userPointService;
        IUserItemInventoryService? _userItemInventoryService;
        bool _isProcessing;

        [Inject]
        public void Construct(
            ShopState state,
            ShopService shopService,
            IUserPointService userPointService,
            IUserItemInventoryService userItemInventoryService)
        {
            _state = state;
            _shopService = shopService;
            _userPointService = userPointService;
            _userItemInventoryService = userItemInventoryService;
        }

        void Start()
        {
            SetupBackButton();
            SetupCells();
            SubscribeToStateEvents();
            SubscribeToCellEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromStateEvents();
            UnsubscribeFromCellEvents();

            if (_backButton != null)
                _backButton.onClick.RemoveListener(OnBackClicked);
        }

        void SetupBackButton()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        void OnBackClicked()
        {
            _shopService?.GoBack();
            OnBackButtonClicked?.Invoke();
        }

        void SetupCells()
        {
            if (_shopService == null) return;

            for (var i = 0; i < _gachaCells.Count; i++)
            {
                _shopService.SetupGachaCell(_gachaCells[i], i);
            }

            if (_state == null) return;

            SetupCategoryCells(_rewardAdCells, _state.RewardAdProductList);
            SetupCategoryCells(_timedFurnitureCells, _state.TimedFurnitureProductList);
            SetupCategoryCells(_timedOutfitCells, _state.TimedOutfitProductList);

            RefreshAllCellsAppearance();
        }

        void SetupCategoryCells(List<ProductCellView> cells, IReadOnlyList<ProductData> list)
        {
            var dataCount = list.Count;
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell == null) continue;

                if (i < dataCount)
                {
                    cell.gameObject.SetActive(true);
                    cell.Setup(list[i]);
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
            }
        }

        void SubscribeToStateEvents()
        {
            if (_state != null)
            {
                _state.OnTimedShopUpdated += OnTimedShopUpdated;
            }

            if (_userPointService != null)
            {
                _userPointService.YarnBalanceChanged += OnYarnBalanceChanged;
                UpdateYarnBalanceDisplay(_userPointService.GetYarnBalance());
            }

            if (_userItemInventoryService != null)
            {
                _userItemInventoryService.OutfitChanged += OnOutfitChanged;
            }
        }

        void UnsubscribeFromStateEvents()
        {
            if (_state != null)
            {
                _state.OnTimedShopUpdated -= OnTimedShopUpdated;
            }

            if (_userPointService != null)
                _userPointService.YarnBalanceChanged -= OnYarnBalanceChanged;

            if (_userItemInventoryService != null)
                _userItemInventoryService.OutfitChanged -= OnOutfitChanged;
        }

        void OnYarnBalanceChanged(int balance)
        {
            UpdateYarnBalanceDisplay(balance);
            RefreshAllCellsAppearance();
        }

        void OnOutfitChanged(uint _)
        {
            RefreshAllCellsAppearance();
        }

        void OnTimedShopUpdated()
        {
            SetupCells();
        }

        void RefreshAllCellsAppearance()
        {
            if (_shopService == null || _state == null || _userPointService == null) return;

            var balance = _userPointService.GetYarnBalance();

            for (var i = 0; i < _gachaCells.Count; i++)
            {
                _shopService.RefreshGachaCellInteractable(_gachaCells[i], i, balance);
            }

            RefreshCategoryAppearance(_rewardAdCells, _state.RewardAdProductList, balance);
            RefreshCategoryAppearance(_timedFurnitureCells, _state.TimedFurnitureProductList, balance);
            RefreshCategoryAppearance(_timedOutfitCells, _state.TimedOutfitProductList, balance);
        }

        void RefreshCategoryAppearance(
            List<ProductCellView> cells,
            IReadOnlyList<ProductData> list,
            int balance)
        {
            if (_shopService == null) return;

            var count = Math.Min(cells.Count, list.Count);
            for (var i = 0; i < count; i++)
            {
                var cell = cells[i];
                if (cell == null) continue;

                var data = list[i];
                var isSoldOut = _shopService.IsSoldOut(data);
                var isAffordable = _shopService.IsAffordable(data, balance);

                cell.SetSoldOut(isSoldOut);
                cell.SetDimmed(!isAffordable);
                cell.SetInteractable(isAffordable);
            }
        }

        void SubscribeToCellEvents()
        {
            foreach (var cell in _gachaCells)
                cell.OnGachaTapped += OnGachaCellTapped;

            foreach (var cell in _rewardAdCells)
            {
                if (cell != null) cell.OnTapped += OnProductCellTapped;
            }

            foreach (var cell in _timedFurnitureCells)
            {
                if (cell != null) cell.OnTapped += OnProductCellTapped;
            }

            foreach (var cell in _timedOutfitCells)
            {
                if (cell != null) cell.OnTapped += OnProductCellTapped;
            }
        }

        void UnsubscribeFromCellEvents()
        {
            foreach (var cell in _gachaCells)
                cell.OnGachaTapped -= OnGachaCellTapped;

            foreach (var cell in _rewardAdCells)
            {
                if (cell != null) cell.OnTapped -= OnProductCellTapped;
            }

            foreach (var cell in _timedFurnitureCells)
            {
                if (cell != null) cell.OnTapped -= OnProductCellTapped;
            }

            foreach (var cell in _timedOutfitCells)
            {
                if (cell != null) cell.OnTapped -= OnProductCellTapped;
            }
        }

        async void OnGachaCellTapped(int index, int count)
        {
            if (_isProcessing || _shopService == null) return;
            _isProcessing = true;
            try
            {
                await _shopService.OnGachaTappedAsync(index, count, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // オブジェクト破棄時のキャンセルは正常動作
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopView] {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        async void OnProductCellTapped(ProductData data)
        {
            if (_isProcessing || _shopService == null) return;
            _isProcessing = true;
            try
            {
                await _shopService.OnProductCellTappedAsync(data, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // オブジェクト破棄時のキャンセルは正常動作
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopView] {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        void UpdateYarnBalanceDisplay(int balance)
        {
            if (_yarnBalanceText != null)
                _yarnBalanceText.text = balance.ToString("N0");
        }
    }
}
