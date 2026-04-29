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
        [Header("Tab Buttons")]
        [SerializeField] Button? _itemTabButton;
        [SerializeField] Button? _pointTabButton;

        [Header("Tab Content")]
        [SerializeField] GameObject? _itemContent;
        [SerializeField] GameObject? _pointContent;

        [Header("Navigation")]
        [SerializeField] Button? _backButton;

        [Header("Tab Visual")]
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

        [Header("Item Cells")]
        [SerializeField] List<ProductCellView> _itemCells = new();

        [Header("Point Cells")]
        [SerializeField] List<ProductCellView> _pointCells = new();

        public event Action? OnBackButtonClicked;

        ShopState? _state;
        ShopService? _shopService;
        IUserPointService? _userPointService;
        bool _isProcessing;

        [Inject]
        public void Construct(ShopState state, ShopService shopService, IUserPointService userPointService)
        {
            _state = state;
            _shopService = shopService;
            _userPointService = userPointService;
        }

        void Start()
        {
            SetupTabButtons();
            SetupBackButton();
            SetupCells();
            SubscribeToStateEvents();
            SubscribeToCellEvents();

            // デフォルトでアイテムタブを選択
            UpdateTabVisuals(ShopTab.Item);
            ShowContent(ShopTab.Item);
        }

        void OnDestroy()
        {
            UnsubscribeFromStateEvents();
            UnsubscribeFromCellEvents();

            if (_itemTabButton != null)
                _itemTabButton.onClick.RemoveListener(OnItemTabClicked);
            if (_pointTabButton != null)
                _pointTabButton.onClick.RemoveListener(OnPointTabClicked);
            if (_backButton != null)
                _backButton.onClick.RemoveListener(OnBackClicked);
        }

        void SetupTabButtons()
        {
            if (_itemTabButton != null)
                _itemTabButton.onClick.AddListener(OnItemTabClicked);
            if (_pointTabButton != null)
                _pointTabButton.onClick.AddListener(OnPointTabClicked);
        }

        void SetupBackButton()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        void OnItemTabClicked()
        {
            _shopService?.SetCurrentTab(ShopTab.Item);
        }

        void OnPointTabClicked()
        {
            _shopService?.SetCurrentTab(ShopTab.Point);
        }

        void OnBackClicked()
        {
            _shopService?.GoBack();
            OnBackButtonClicked?.Invoke();
        }

        void SetupCells()
        {
            if (_shopService == null) return;

            // ガチャセルをセットアップ
            for (var i = 0; i < _gachaCells.Count; i++)
            {
                _shopService.SetupGachaCell(_gachaCells[i], i);
            }

            // アイテムセルをセットアップ
            if (_state != null)
            {
                for (var i = 0; i < _itemCells.Count && i < _state.FurnitureProductList.Count; i++)
                {
                    _shopService.SetupProductCell(_itemCells[i], _state.FurnitureProductList[i]);
                }

                for (var i = 0; i < _pointCells.Count && i < _state.OutfitProductList.Count; i++)
                {
                    _shopService.SetupProductCell(_pointCells[i], _state.OutfitProductList[i]);
                }
            }
        }

        void SubscribeToStateEvents()
        {
            if (_state != null)
                _state.OnTabChanged += OnTabChanged;

            if (_userPointService != null)
            {
                _userPointService.YarnBalanceChanged += OnYarnBalanceChanged;
                // 初期残高を表示
                UpdateYarnBalanceDisplay(_userPointService.GetYarnBalance());
            }
        }

        void UnsubscribeFromStateEvents()
        {
            if (_state != null)
                _state.OnTabChanged -= OnTabChanged;

            if (_userPointService != null)
                _userPointService.YarnBalanceChanged -= OnYarnBalanceChanged;
        }

        void OnYarnBalanceChanged(int balance)
        {
            UpdateYarnBalanceDisplay(balance);
            UpdateAllCellsInteractable(balance);
        }

        void UpdateAllCellsInteractable(int balance)
        {
            if (_shopService == null || _state == null) return;

            for (var i = 0; i < _gachaCells.Count; i++)
            {
                _shopService.RefreshGachaCellInteractable(_gachaCells[i], i, balance);
            }

            for (var i = 0; i < _itemCells.Count && i < _state.FurnitureProductList.Count; i++)
            {
                _shopService.RefreshProductCellInteractable(_itemCells[i], _state.FurnitureProductList[i], balance);
            }

            for (var i = 0; i < _pointCells.Count && i < _state.OutfitProductList.Count; i++)
            {
                _shopService.RefreshProductCellInteractable(_pointCells[i], _state.OutfitProductList[i], balance);
            }
        }

        void SubscribeToCellEvents()
        {
            foreach (var cell in _gachaCells)
                cell.OnGachaTapped += OnGachaCellTapped;

            foreach (var cell in _itemCells)
                cell.OnTapped += OnProductCellTapped;

            foreach (var cell in _pointCells)
                cell.OnTapped += OnProductCellTapped;
        }

        void UnsubscribeFromCellEvents()
        {
            foreach (var cell in _gachaCells)
                cell.OnGachaTapped -= OnGachaCellTapped;

            foreach (var cell in _itemCells)
                cell.OnTapped -= OnProductCellTapped;

            foreach (var cell in _pointCells)
                cell.OnTapped -= OnProductCellTapped;
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

        void OnTabChanged(ShopTab tab)
        {
            UpdateTabVisuals(tab);
            ShowContent(tab);
        }

        void UpdateTabVisuals(ShopTab tab)
        {
            var isItemTab = tab == ShopTab.Item;

            if (_itemTabImage != null)
                _itemTabImage.sprite = isItemTab ? _tabSelectedSprite : _tabUnselectedSprite;
            if (_pointTabImage != null)
                _pointTabImage.sprite = isItemTab ? _tabUnselectedSprite : _tabSelectedSprite;

            if (_itemTabText != null)
                _itemTabText.color = isItemTab ? _tabSelectedTextColor : _tabUnselectedTextColor;
            if (_pointTabText != null)
                _pointTabText.color = isItemTab ? _tabUnselectedTextColor : _tabSelectedTextColor;
        }

        void ShowContent(ShopTab tab)
        {
            if (_itemContent != null)
                _itemContent.SetActive(tab == ShopTab.Item);
            if (_pointContent != null)
                _pointContent.SetActive(tab == ShopTab.Point);
        }

        void UpdateYarnBalanceDisplay(int balance)
        {
            if (_yarnBalanceText != null)
                _yarnBalanceText.text = balance.ToString("N0");
        }
    }
}