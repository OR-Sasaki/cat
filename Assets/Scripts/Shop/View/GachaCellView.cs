#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shop.View
{
    /// ガチャセルの表示（1連/10連ボタンのイベント処理のみ）
    public class GachaCellView : MonoBehaviour
    {
        [SerializeField] Button? _singleButton;
        [SerializeField] Button? _tenButton;

        public int Index { get; private set; }

        /// ガチャボタンがタップされた時のイベント（index, count）
        public event Action<int, int>? OnGachaTapped;

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

        public void Setup(int index)
        {
            Index = index;
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