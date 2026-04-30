#nullable enable

using System;
using Root.Service;
using Shop.State;
using TMPro;
using UnityEngine;
using VContainer;

namespace Shop.View
{
    public class TimedShopTimerView : MonoBehaviour
    {
        const string PlaceholderText = "--:--";

        [SerializeField] TextMeshProUGUI? _remainingText;

        IClock? _clock;
        ShopState? _state;
        bool _hasWarnedMissingText;
        // 秒単位でしか表示が変わらないため、毎フレームの string 生成と TMP の dirty 化を避ける。
        int _lastSecondsRendered = -1;
        bool _isShowingPlaceholder;

        [Inject]
        public void Construct(IClock clock, ShopState state)
        {
            _clock = clock;
            _state = state;
        }

        void Update()
        {
            if (_remainingText == null)
            {
                if (!_hasWarnedMissingText)
                {
                    Debug.LogWarning("[TimedShopTimerView] _remainingText is not assigned. Update is no-op.");
                    _hasWarnedMissingText = true;
                }
                return;
            }

            if (_clock is null || _state is null || _state.NextUpdateAt == default)
            {
                ShowPlaceholder();
                return;
            }

            var remaining = _state.NextUpdateAt - _clock.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            var totalSeconds = (int)remaining.TotalSeconds;
            if (!_isShowingPlaceholder && totalSeconds == _lastSecondsRendered) return;

            _isShowingPlaceholder = false;
            _lastSecondsRendered = totalSeconds;
            _remainingText.text = Format(remaining);
        }

        void ShowPlaceholder()
        {
            if (_isShowingPlaceholder) return;
            _isShowingPlaceholder = true;
            _lastSecondsRendered = -1;

            if (_remainingText != null)
                _remainingText.text = PlaceholderText;
        }

        static string Format(TimeSpan remaining)
        {
            return remaining < TimeSpan.FromHours(1)
                ? $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}"
                : $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
    }
}
