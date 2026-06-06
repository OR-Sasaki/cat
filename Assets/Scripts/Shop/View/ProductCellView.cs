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
        [SerializeField] GameObject? _dimOverlay;
        [SerializeField] GameObject? _soldOutOverlay;

        public ProductData? Data { get; private set; }

        /// セルがタップされた時のイベント
        public event Action<ProductData>? OnTapped;

        AsyncOperationHandle? _iconHandle;
        bool? _isSoldOut;
        bool? _lastDimmed;
        bool? _lastInteractable;
        bool _hasWarnedMissingDimOverlay;
        bool _hasWarnedMissingSoldOutOverlay;

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

            LoadIconAsync(data);
        }

        // 売り切れ ＞ タップ無効の優先度を保証するため、interactable は _isSoldOut で常に上書きされる。
        // 呼出側は SetSoldOut の後に SetInteractable を呼ぶ契約。
        public void SetInteractable(bool interactable)
        {
            var effective = interactable && !_isSoldOut.GetValueOrDefault();
            if (_lastInteractable == effective) return;
            _lastInteractable = effective;

            if (_button != null)
                _button.interactable = effective;
        }

        public void SetDimmed(bool isDimmed)
        {
            if (_lastDimmed == isDimmed) return;
            _lastDimmed = isDimmed;

            if (_dimOverlay == null)
            {
                if (!_hasWarnedMissingDimOverlay)
                {
                    Debug.LogWarning("[ProductCellView] _dimOverlay is not assigned. SetDimmed is ignored.");
                    _hasWarnedMissingDimOverlay = true;
                }
                return;
            }

            _dimOverlay.SetActive(isDimmed);
        }

        public void SetSoldOut(bool isSoldOut)
        {
            if (_isSoldOut == isSoldOut) return;
            _isSoldOut = isSoldOut;

            if (_soldOutOverlay == null)
            {
                if (!_hasWarnedMissingSoldOutOverlay)
                {
                    Debug.LogWarning("[ProductCellView] _soldOutOverlay is not assigned. SetSoldOut overlay update is ignored.");
                    _hasWarnedMissingSoldOutOverlay = true;
                }
                return;
            }

            _soldOutOverlay.SetActive(isSoldOut);
        }

        void OnButtonClicked()
        {
            if (Data != null)
                OnTapped?.Invoke(Data);
        }

        void LoadIconAsync(ProductData data)
        {
            if (string.IsNullOrEmpty(data.IconPath) || _icon == null)
                return;

            ReleaseIconAsset();

            switch (data.ItemType)
            {
                case ItemType.Furniture:
                    LoadThumbnail<Cat.Furniture.Furniture>(data.IconPath, f => f.Thumbnail);
                    break;
                case ItemType.Outfit:
                    LoadThumbnail<Cat.Character.Outfit>(data.IconPath, o => o.Thumbnail);
                    break;
            }
        }

        void LoadThumbnail<T>(string address, Func<T, Sprite?> thumbnailSelector) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            _iconHandle = handle;

            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded && h.Result != null)
                {
                    var sprite = thumbnailSelector(h.Result);
                    if (_icon != null && sprite != null)
                        _icon.sprite = sprite;
                }
                else if (h.OperationException is Exception e)
                {
                    Debug.LogError($"[ProductCellView] {e.Message}\n{e.StackTrace}");
                }
                else
                {
                    Debug.LogError($"[ProductCellView] Failed to load icon: {address}");
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
