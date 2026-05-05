#nullable enable

using System;
using System.Collections.Generic;

namespace Root.State
{
    /// 日別集中時間 (秒) を保持する状態。
    /// キーは時刻部分 00:00 / Kind=Unspecified に正規化された DateTime。
    public class TimerRecordState
    {
        readonly Dictionary<DateTime, int> _records = new();

        public int Get(DateTime date)
        {
            var key = Normalize(date);
            return _records.TryGetValue(key, out var seconds) ? seconds : 0;
        }

        public IReadOnlyDictionary<DateTime, int> GetAll() => _records;

        internal void Set(DateTime date, int seconds)
        {
            var key = Normalize(date);
            if (seconds <= 0)
            {
                _records.Remove(key);
                return;
            }
            _records[key] = seconds;
        }

        internal void Clear()
        {
            _records.Clear();
        }

        static DateTime Normalize(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }
}
