#nullable enable
using System;
using Shop.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop.View
{
    /// リワード広告商品セル専用ビュー。
    /// 商品名は Prefab に直書きする想定なのでこのクラスは扱わない。残り回数「(あとN回)」のみ動的更新し、
    /// 視聴不可（売り切れ/広告未準備）はグレーアウトオーバーレイの切替で表現する。
    public class RewardAdProductCellView : MonoBehaviour
    {
        [SerializeField] TMP_Text? _remainingCountText;
        [SerializeField] Button? _button;
        [SerializeField] GameObject? _grayOutOverlay;

        public ProductData? Data { get; private set; }

        public event Action<ProductData>? OnTapped;

        bool? _lastGrayedOut;
        bool _hasWarnedMissingGrayOutOverlay;
        bool _hasWarnedMissingRemainingCountText;

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
        }

        public void SetRemainingCount(int remaining)
        {
            if (_remainingCountText == null)
            {
                if (!_hasWarnedMissingRemainingCountText)
                {
                    Debug.LogWarning("[RewardAdProductCellView] _remainingCountText is not assigned. SetRemainingCount is ignored.");
                    _hasWarnedMissingRemainingCountText = true;
                }
                return;
            }

            _remainingCountText.text = $"(あと{remaining}回)";
        }

        // 視聴不可（残り 0 回 or 広告未準備）でオーバーレイ表示とボタン無効化を同時に切替える。
        public void SetGrayedOut(bool grayedOut)
        {
            if (_lastGrayedOut == grayedOut) return;
            _lastGrayedOut = grayedOut;

            if (_button != null)
                _button.interactable = !grayedOut;

            if (_grayOutOverlay == null)
            {
                if (!_hasWarnedMissingGrayOutOverlay)
                {
                    Debug.LogWarning("[RewardAdProductCellView] _grayOutOverlay is not assigned. SetGrayedOut overlay update is ignored.");
                    _hasWarnedMissingGrayOutOverlay = true;
                }
                return;
            }

            _grayOutOverlay.SetActive(grayedOut);
        }

        void OnButtonClicked()
        {
            if (Data != null)
                OnTapped?.Invoke(Data);
        }
    }
}
