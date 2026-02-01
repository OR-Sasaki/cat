#nullable enable
using System;
using Shop.State;
using TMPro;
using UnityEngine;
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

        void Start()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);
        }

        void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
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

            // アイコンはプレースホルダーとして設定済み（Addressablesでの読み込みは後続タスク）
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
    }
}
