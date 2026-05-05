#nullable enable

using History.State;
using Root.Service;
using VContainer;
using VContainer.Unity;

namespace History.Starter
{
    /// 履歴シーン起動時に表示中年月と選択日を当日に初期化する。
    /// 各 View はこの初期化後の状態変更通知を受けて初回描画を行う。
    public class HistoryStarter : IStartable
    {
        readonly HistoryCalendarState _calendarState;
        readonly IClock _clock;

        [Inject]
        public HistoryStarter(HistoryCalendarState calendarState, IClock clock)
        {
            _calendarState = calendarState;
            _clock = clock;
        }

        public void Start()
        {
            var today = _clock.UtcNow.LocalDateTime.Date;
            _calendarState.SetDisplayMonth(today.Year, today.Month);
            _calendarState.SetSelectedDate(today);
        }
    }
}
