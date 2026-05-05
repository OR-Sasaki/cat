#nullable enable
using System;
using UnityEngine;

namespace History.State
{
    /// 履歴カレンダー画面の表示状態 (表示中年月 / 選択日) を保持し、
    /// 変更時にイベント通知する単一データソース。
    /// 月送りで選択日を維持し、表示外でも値を保持し続ける。
    public class HistoryCalendarState
    {
        public int DisplayYear { get; private set; }
        public int DisplayMonth { get; private set; }
        public DateTime SelectedDate { get; private set; }

        public event Action<int, int>? OnDisplayMonthChanged;
        public event Action<DateTime>? OnSelectedDateChanged;

        /// 表示中の年月を更新する。
        /// month は [1..12]、year は 1 以上を期待。範囲外の場合は状態を変更せず early return。
        public void SetDisplayMonth(int year, int month)
        {
            if (year < 1)
            {
                Debug.LogError($"[HistoryCalendarState] Invalid year: {year}. year must be >= 1.");
                return;
            }
            if (month < 1 || month > 12)
            {
                Debug.LogError($"[HistoryCalendarState] Invalid month: {month}. month must be in [1..12].");
                return;
            }

            if (DisplayYear == year && DisplayMonth == month) return;

            DisplayYear = year;
            DisplayMonth = month;
            FireDisplayMonthChanged(year, month);
        }

        /// 選択日を更新する。
        /// 内部で時刻部分 00:00、Kind=Unspecified に正規化する。
        public void SetSelectedDate(DateTime date)
        {
            var normalizedDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);

            if (SelectedDate.Date == normalizedDate.Date) return;

            SelectedDate = normalizedDate;
            FireSelectedDateChanged(normalizedDate);
        }

        void FireDisplayMonthChanged(int year, int month)
        {
            var handlers = OnDisplayMonthChanged;
            if (handlers is null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<int, int>)handler)(year, month);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HistoryCalendarState] {e.Message}\n{e.StackTrace}");
                }
            }
        }

        void FireSelectedDateChanged(DateTime date)
        {
            var handlers = OnSelectedDateChanged;
            if (handlers is null) return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<DateTime>)handler)(date);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HistoryCalendarState] {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}
