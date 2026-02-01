#nullable enable
using System;
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
        [SerializeField] Color _selectedTabColor = Color.white;
        [SerializeField] Color _unselectedTabColor = Color.gray;

        [Header("Yarn Balance Display")]
        [SerializeField] TMP_Text? _yarnBalanceText;

        // セルリストはタスク4以降で追加

        public event Action? OnBackButtonClicked;
        public event Action<ShopTab>? OnTabSelected;

        ShopState? _state;

        [Inject]
        public void Construct(ShopState state)
        {
            _state = state;
        }

        void Start()
        {
            SetupTabButtons();
            SetupBackButton();
            SubscribeToStateEvents();

            // デフォルトでアイテムタブを選択
            UpdateTabVisuals(ShopTab.Item);
            ShowContent(ShopTab.Item);
        }

        void OnDestroy()
        {
            UnsubscribeFromStateEvents();
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

        void OnTabChanged(ShopTab tab)
        {
            UpdateTabVisuals(tab);
            ShowContent(tab);
        }

        void UpdateTabVisuals(ShopTab tab)
        {
            var isItemTab = tab == ShopTab.Item;
            if (_itemTabImage != null)
                _itemTabImage.color = isItemTab ? _selectedTabColor : _unselectedTabColor;
            if (_pointTabImage != null)
                _pointTabImage.color = isItemTab ? _unselectedTabColor : _selectedTabColor;
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