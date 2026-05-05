using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using History.State;

namespace History.View
{
    /// 履歴カレンダーの年月見出しと月送りボタン。
    /// 前月/次月ボタン操作で HistoryCalendarState の DisplayMonth を更新する。
    /// 年跨ぎ (1 月 ⇄ 12 月) の正規化は本 View の責務。
    public class MonthHeaderView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _yearMonthText;
        [SerializeField] Button _prevButton;
        [SerializeField] Button _nextButton;

        HistoryCalendarState _state;

        [Inject]
        public void Construct(HistoryCalendarState state)
        {
            _state = state;
        }

        void Start()
        {
            _prevButton.onClick.AddListener(OnPrevClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _state.OnDisplayMonthChanged += OnDisplayMonthChanged;

            /// HistoryStarter 後に Awake/Start が呼ばれる前提だが、安全のため初期表示を行う。
            /// state.DisplayYear が 0 (未初期化) の可能性に備え DisplayMonth でガード。
            if (_state.DisplayMonth >= 1)
            {
                OnDisplayMonthChanged(_state.DisplayYear, _state.DisplayMonth);
            }
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnDisplayMonthChanged -= OnDisplayMonthChanged;
        }

        void OnPrevClicked()
        {
            int year = _state.DisplayYear;
            int month = _state.DisplayMonth;
            (int prevYear, int prevMonth) = month == 1 ? (year - 1, 12) : (year, month - 1);
            _state.SetDisplayMonth(prevYear, prevMonth);
        }

        void OnNextClicked()
        {
            int year = _state.DisplayYear;
            int month = _state.DisplayMonth;
            (int nextYear, int nextMonth) = month == 12 ? (year + 1, 1) : (year, month + 1);
            _state.SetDisplayMonth(nextYear, nextMonth);
        }

        void OnDisplayMonthChanged(int year, int month)
        {
            _yearMonthText.text = $"{year}/{month}";
        }
    }
}
