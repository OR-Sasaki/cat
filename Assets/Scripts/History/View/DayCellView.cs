#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace History.View
{
    /// カレンダーグリッドの 1 日分セル。Prefab 化され CalendarGridView から複数 Instantiate される。
    /// 純粋な View として Bind 経由で全状態を受け取り、自身では集中時間記録や状態を直接参照しない。
    public class DayCellView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _dayText = null!;
        [SerializeField] Image _iconImage = null!;
        [SerializeField] GameObject _highlightObject = null!;
        [SerializeField] Button _button = null!;
        [SerializeField] Color _dayTextTint = Color.white;

        DateTime _currentDate;
        Action<DateTime>? _onClickCallback;

        void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        void OnClicked()
        {
            _onClickCallback?.Invoke(_currentDate);
        }

        /// 1 セル分の状態をまとめて受け取って表示を更新する。
        /// 集中時間 → Sprite 解決などのロジックは呼び出し側 (CalendarGridView) で済ませた状態で渡される。
        public void Bind(
            DateTime date,
            int seconds,
            Sprite? icon,
            bool isInMonth,
            bool isSelected,
            Color outOfMonthTint,
            Action<DateTime> onClickCallback)
        {
            _currentDate = date;
            _onClickCallback = onClickCallback;

            _dayText.text = date.Day.ToString();

            _iconImage.sprite = icon;
            _iconImage.enabled = (icon != null);

            _highlightObject.SetActive(isSelected);

            // 月内/月外の区別 (要件 3.7) のための一般的な Tint。
            // 段階表現 (集中量による色変化) は Sprite 切替のみで実現するため要件 4.6 とは衝突しない。
            var tint = isInMonth ? Color.white : outOfMonthTint;
            _iconImage.color = tint;
            _dayText.color = isInMonth
                ? _dayTextTint
                : _dayTextTint * new Color(outOfMonthTint.r, outOfMonthTint.g, outOfMonthTint.b, outOfMonthTint.a * 0.7f);
        }
    }
}
