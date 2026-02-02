#nullable enable
using System;
using Shop.State;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Shop.View
{
    /// 汎用商品セルの表示（アイテム/毛糸パック共用）
    public class ProductCellView : MonoBehaviour
    {
        [SerializeField] Image? _icon;
        [SerializeField] TMP_Text? _nameText;
        [SerializeField] TMP_Text? _priceText;
        [SerializeField] Button? _button;

        public ProductData? Data { get; private set; }

        /// セルがタップされた時のイベント
        public event Action<ProductData>? OnTapped;

        AsyncOperationHandle<Sprite>? _iconHandle;

        void Start()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);

            ReleaseIconAsset();
        }

        public void Setup(ProductData data)
        {
            Data = data;

            if (_nameText != null)
                _nameText.text = data.Name;

            if (_priceText != null)
            {
                var currencySymbol = data.CurrencyType == CurrencyType.Yarn ? "毛糸" : "¥";
                _priceText.text = $"{currencySymbol} {data.Price:N0}";
            }

            LoadIconAsync(data.IconPath);
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        void OnButtonClicked()
        {
            if (Data != null)
                OnTapped?.Invoke(Data);
        }

        void LoadIconAsync(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath) || _icon == null)
                return;

            ReleaseIconAsset();

            var handle = Addressables.LoadAssetAsync<Sprite>(iconPath);
            _iconHandle = handle;

            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
                {
                    if (_icon != null)
                        _icon.sprite = h.Result;
                }
                else
                {
                    Debug.LogError($"[ProductCellView] Failed to load icon: {iconPath}");
                }
            };
        }

        void ReleaseIconAsset()
        {
            if (_iconHandle.HasValue && _iconHandle.Value.IsValid())
            {
                Addressables.Release(_iconHandle.Value);
                _iconHandle = null;
            }
        }
    }
}
