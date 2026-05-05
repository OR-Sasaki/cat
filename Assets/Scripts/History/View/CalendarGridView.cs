#nullable enable

using System;
using System.Collections.Generic;
using History.Service;
using History.State;
using Root.Service;
using UnityEngine;
using VContainer;

namespace History.View
{
    /// 7 列 × 6 行 = 42 セル固定 Pool のカレンダーグリッド。
    /// HistoryCalendarState の表示中年月 / 選択日変更で再描画する。
    /// 各セルには TimerRecordService の集中時間を渡し、
    /// HistoryCalendarSettings 経由で 4 段階 Sprite を解決して割り当てる。
    public class CalendarGridView : MonoBehaviour
    {
        const int CellCount = 42; // 7 列 × 6 行

        [SerializeField] DayCellView _dayCellPrefab = null!;
        [SerializeField] RectTransform _gridContainer = null!;

        readonly List<DayCellView> _cells = new(CellCount);

        HistoryCalendarState? _state;
        ITimerRecordService? _records;
        HistoryCalendarSettings? _settings;
        Action<DateTime>? _cellClickCallback;

        [Inject]
        public void Construct(
            HistoryCalendarState state,
            ITimerRecordService records,
            HistoryCalendarSettings settings)
        {
            _state = state;
            _records = records;
            _settings = settings;
        }

        void Awake()
        {
            for (var i = 0; i < CellCount; i++)
            {
                var cell = Instantiate(_dayCellPrefab, _gridContainer);
                _cells.Add(cell);
            }
            _cellClickCallback = OnCellClicked;
        }

        void Start()
        {
            if (_state == null) return;

            _state.OnDisplayMonthChanged += OnDisplayMonthChanged;
            _state.OnSelectedDateChanged += OnSelectedDateChanged;

            // HistoryStarter による初期化が済んでいる前提で初回描画
            if (_state.DisplayMonth >= 1)
            {
                RenderCells();
            }
        }

        void OnDestroy()
        {
            if (_state == null) return;
            _state.OnDisplayMonthChanged -= OnDisplayMonthChanged;
            _state.OnSelectedDateChanged -= OnSelectedDateChanged;
        }

        void OnDisplayMonthChanged(int year, int month) => RenderCells();
        void OnSelectedDateChanged(DateTime date) => RenderCells();

        void OnCellClicked(DateTime date)
        {
            _state?.SetSelectedDate(date);
        }

        void RenderCells()
        {
            if (_state == null || _records == null || _settings == null) return;
            if (_cellClickCallback is null) return;

            var year = _state.DisplayYear;
            var month = _state.DisplayMonth;
            var selected = _state.SelectedDate.Date;
            var outOfMonthTint = _settings.OutOfMonthTint;

            // 表示中月 1 日の曜日 (0 = 日, 6 = 土) を取得し、グリッド開始日 (前月末を含む) を決める
            var firstOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var firstDayOfWeek = (int)firstOfMonth.DayOfWeek;
            var gridStart = firstOfMonth.AddDays(-firstDayOfWeek);

            for (var i = 0; i < CellCount; i++)
            {
                var date = gridStart.AddDays(i);
                var seconds = _records.GetSeconds(date);
                var sprite = _settings.GetSpriteForSeconds(seconds);
                var isInMonth = date.Year == year && date.Month == month;
                // 要件 6.8: 選択ハイライトは表示中月内のセルにのみ描画する
                var isSelected = isInMonth && date.Date == selected;

                _cells[i].Bind(
                    date,
                    seconds,
                    sprite,
                    isInMonth,
                    isSelected,
                    outOfMonthTint,
                    _cellClickCallback);
            }
        }
    }
}
