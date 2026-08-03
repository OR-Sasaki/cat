#nullable enable

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.View
{
    /// ON/OFF を切り替えるピル型スイッチ
    /// トラックの色とノブの左右位置で状態を表現する
    public class MenuSwitchView : MonoBehaviour
    {
        [SerializeField] Button _button = null!;
        [SerializeField] Image _track = null!;
        [SerializeField] RectTransform _knob = null!;
        [SerializeField] Color _onColor = new(0.902f, 0.773f, 0.451f);
        [SerializeField] Color _offColor = new(0.333f, 0.310f, 0.412f);
        [SerializeField] float _knobOffsetX = 24f;
        [SerializeField] float _duration = 0.15f;

        Tween? _knobTween;

        public bool IsOn { get; private set; }

        /// ユーザー操作で値が変わったときのみ発火する (SetValueWithoutNotify では発火しない)
        public event Action<bool>? ValueChanged;

        void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        /// 初期化用。イベントを発火せずアニメーションなしで表示を更新する
        public void SetValueWithoutNotify(bool isOn)
        {
            Apply(isOn, false);
        }

        void OnButtonClicked()
        {
            Apply(!IsOn, true);
            ValueChanged?.Invoke(IsOn);
        }

        void Apply(bool isOn, bool animate)
        {
            IsOn = isOn;

            var targetColor = isOn ? _onColor : _offColor;
            var targetX = isOn ? _knobOffsetX : -_knobOffsetX;

            _knobTween?.Kill();

            if (animate)
            {
                _knobTween = DOTween.Sequence()
                    .Join(_knob.DOAnchorPosX(targetX, _duration).SetEase(Ease.OutCubic))
                    .Join(_track.DOColor(targetColor, _duration))
                    .SetLink(gameObject);
                return;
            }

            _knob.anchoredPosition = new Vector2(targetX, _knob.anchoredPosition.y);
            _track.color = targetColor;
        }

        void OnDestroy()
        {
            _knobTween?.Kill();
            _button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
