#nullable enable

using System;
using System.Collections.Generic;

namespace Root.Service
{
    /// 日別集中時間 (秒) の単一ソースとなるサービス契約
    public interface ITimerRecordService
    {
        /// 呼出時点のローカル日付に集中時間 (秒) を加算する。0 以下は無視する。
        void AddSeconds(int seconds);

        /// 指定日の集中時間 (秒) を取得する。記録がない場合は 0 を返す。
        int GetSeconds(DateTime date);

        /// 指定月 (年, 月) の合計集中時間 (秒) を取得する。
        int GetMonthTotalSeconds(int year, int month);

        /// 当日 (IClock 基準のローカル日付) の合計集中時間 (秒) を取得する。
        int GetTodayTotalSeconds();

        /// 全記録のイミュータブル写像を返す。日付昇順は保証しない。
        IReadOnlyDictionary<DateTime, int> GetAllRecords();

        /// 集中時間が加算されたとき発火 (日付, 加算秒数, 当該日付の合計秒数)
        event Action<DateTime, int, int>? FocusSecondsAdded;
    }
}
