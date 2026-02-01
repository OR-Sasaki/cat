#nullable enable
using System;
using Shop.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop.View
{
    /// ガチャセルの表示（サムネイル、ガチャ名、1連/10連ボタン）
    public class GachaCellView : MonoBehaviour
    {
        [SerializeField] Image? _thumbnail;
        [SerializeField] TMP_Text? _nameText;
        [SerializeField] Button? _singleButton;
        [SerializeField] Button? _tenButton;
        [SerializeField] TMP_Text? _singlePriceText;
        [SerializeField] TMP_Text? _tenPriceText;

        public int Index { get; private set; }

        /// ガチャボタンがタップされた時のイベント（index, count）
        public event Action<int, int>? OnGachaTapped;

        GachaData? _data;

        void Start()
        {
            if (_singleButton != null)
                _singleButton.onClick.AddListener(OnSingleButtonClicked);
            if (_tenButton != null)
                _tenButton.onClick.AddListener(OnTenButtonClicked);
        }

        void OnDestroy()
        {
            if (_singleButton != null)
                _singleButton.onClick.RemoveListener(OnSingleButtonClicked);
            if (_tenButton != null)
                _tenButton.onClick.RemoveListener(OnTenButtonClicked);
        }

        public void Setup(int index, GachaData data)
        {
            Index = index;
            _data = data;

            if (_nameText != null)
                _nameText.text = data.Name;
            if (_singlePriceText != null)
                _singlePriceText.text = $"1回 {data.SinglePrice:N0}";
            if (_tenPriceText != null)
                _tenPriceText.text = $"10回 {data.TenPrice:N0}";

            // サムネイルはプレースホルダーとして設定済み（Addressablesでの読み込みは後続タスク）
        }

        public void SetButtonsInteractable(bool canAffordSingle, bool canAffordTen)
        {
            if (_singleButton != null)
                _singleButton.interactable = canAffordSingle;
            if (_tenButton != null)
                _tenButton.interactable = canAffordTen;
        }

        void OnSingleButtonClicked()
        {
            OnGachaTapped?.Invoke(Index, 1);
        }

        void OnTenButtonClicked()
        {
            OnGachaTapped?.Invoke(Index, 10);
        }
    }
}
