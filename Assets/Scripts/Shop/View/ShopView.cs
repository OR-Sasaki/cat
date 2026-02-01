#nullable enable
using System;
using System.Collections.Generic;
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
        public event Action<ShopTab>? OnTabSelected;

        ShopState? _state;
        ShopService? _shopService;

        [Inject]
        public void Construct(ShopState state, ShopService shopService)
        {
            _state = state;
            _shopService = shopService;
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
        }

        void SetupTabButtons()
        {
            if (_itemTabButton != null)
                _itemTabButton.onClick.AddListener(() => OnTabSelected?.Invoke(ShopTab.Item));
            if (_pointTabButton != null)
                _pointTabButton.onClick.AddListener(() => OnTabSelected?.Invoke(ShopTab.Point));
        }

        void SetupBackButton()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
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
                for (var i = 0; i < _itemCells.Count && i < _state.ItemProductList.Count; i++)
                {
                    _shopService.SetupProductCell(_itemCells[i], _state.ItemProductList[i]);
                }

                // 毛糸パックセルをセットアップ
                for (var i = 0; i < _pointCells.Count && i < _state.PointProductList.Count; i++)
                {
                    _shopService.SetupProductCell(_pointCells[i], _state.PointProductList[i]);
                }
            }
        }

        void SubscribeToStateEvents()
        {
            if (_state == null) return;

            _state.OnTabChanged += OnTabChanged;
            _state.OnYarnBalanceChanged += UpdateYarnBalanceDisplay;

            // 初期残高を表示
            UpdateYarnBalanceDisplay(_state.YarnBalance);
        }

        void UnsubscribeFromStateEvents()
        {
            if (_state == null) return;

            _state.OnTabChanged -= OnTabChanged;
            _state.OnYarnBalanceChanged -= UpdateYarnBalanceDisplay;
        }

        void SubscribeToCellEvents()
        {
            foreach (var cell in _gachaCells)
            {
                cell.OnGachaTapped += OnGachaCellTapped;
            }

            foreach (var cell in _itemCells)
            {
                cell.OnTapped += OnItemCellTapped;
            }

            foreach (var cell in _pointCells)
            {
                cell.OnTapped += OnPointCellTapped;
            }
        }

        void UnsubscribeFromCellEvents()
        {
            foreach (var cell in _gachaCells)
            {
                cell.OnGachaTapped -= OnGachaCellTapped;
            }

            foreach (var cell in _itemCells)
            {
                cell.OnTapped -= OnItemCellTapped;
            }

            foreach (var cell in _pointCells)
            {
                cell.OnTapped -= OnPointCellTapped;
            }
        }

        void OnGachaCellTapped(int index, int count)
        {
            // タスク8で実装予定: _shopService?.OnGachaTappedAsync(index, count, destroyCancellationToken);
        }

        void OnItemCellTapped(ProductData data)
        {
            // タスク8で実装予定: _shopService?.OnProductCellTappedAsync(data, destroyCancellationToken);
        }

        void OnPointCellTapped(ProductData data)
        {
            // タスク8で実装予定: _shopService?.OnProductCellTappedAsync(data, destroyCancellationToken);
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