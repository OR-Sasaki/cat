#nullable enable
using System;
using TMPro;
using UnityEngine;
using VContainer;

namespace History.View
{
    /// 履歴カレンダーのフッター。選択日 / 選択日集中時間 / 選択月合計を分単位で表示する。
    public class SelectedDateFooterView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _selectedDateText;   // "11/14" 形式
        [SerializeField] TextMeshProUGUI _selectedFocusText;  // 選択日の集中時間 (分)
        [SerializeField] TextMeshProUGUI _monthTotalText;     // 選択月の合計集中時間 (分)

        History.State.HistoryCalendarState? _state;
        Root.Service.ITimerRecordService? _records;

        [Inject]
        public void Construct(
            History.State.HistoryCalendarState state,
            Root.Service.ITimerRecordService records)
        {
            _state = state;
            _records = records;
        }

        void Start()
        {
            if (_state == null) return;

            _state.OnSelectedDateChanged += OnSelectedDateChanged;
            _state.OnDisplayMonthChanged += OnDisplayMonthChanged;

            /// 初期描画: HistoryStarter で SelectedDate / DisplayMonth が初期化済みのため、現在値で全項目を更新する。
            Refresh();
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnSelectedDateChanged -= OnSelectedDateChanged;
            _state.OnDisplayMonthChanged -= OnDisplayMonthChanged;
        }

        void OnSelectedDateChanged(DateTime date)
        {
            Refresh();
        }

        void OnDisplayMonthChanged(int year, int month)
        {
            Refresh();
        }

        void Refresh()
        {
            if (_state == null || _records == null) return;

            var selected = _state.SelectedDate;
            _selectedDateText.text = $"{selected.Month}/{selected.Day}";

            var selectedSeconds = _records.GetSeconds(selected);
            _selectedFocusText.text = History.Service.FocusTimeFormatter.FormatMinutes(selectedSeconds);

            var monthSeconds = _records.GetMonthTotalSeconds(_state.DisplayYear, _state.DisplayMonth);
            _monthTotalText.text = History.Service.FocusTimeFormatter.FormatMinutes(monthSeconds);
        }
    }
}
