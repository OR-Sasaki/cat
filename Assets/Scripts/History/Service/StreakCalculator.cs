#nullable enable

using System;
using Root.Service;
using VContainer;

namespace History.Service
{
    /// 集中時間記録から「当日基準で過去方向に連続記録のある日数」を算出する。
    /// 当日未記録 = ストリーク 0 と判定する (要件 5.3)。
    /// IClock のローカル日付を起点とし、走査上限は 10 年とする。
    public class StreakCalculator
    {
        readonly ITimerRecordService _records;
        readonly IClock _clock;

        /// 走査上限 (10 年 = 3650 日)。無限ループ防止用の安全弁。
        const int MaxScanDays = 3650;

        [Inject]
        public StreakCalculator(ITimerRecordService records, IClock clock)
        {
            _records = records;
            _clock = clock;
        }

        /// 当日基準で「過去方向に連続して集中時間記録のある日数」を返す。
        /// 当日が未記録なら 0 (途切れたとみなす)。
        public int Calculate()
        {
            var today = _clock.UtcNow.LocalDateTime.Date;

            // 要件 5.3: 当日未記録ならストリークは 0
            if (_records.GetSeconds(today) <= 0)
            {
                return 0;
            }

            var count = 0;
            var date = today;
            for (var i = 0; i < MaxScanDays; i++)
            {
                if (_records.GetSeconds(date) <= 0)
                {
                    break;
                }
                count++;
                date = date.AddDays(-1);
            }

            return count;
        }
    }
}
