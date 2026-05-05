#nullable enable

using System;

namespace Root.Service
{
    /// 日別集中時間の永続化スナップショット
    /// Version != CurrentVersion の場合は破棄して空集合で初期化する
    /// JsonUtility 制約により Dictionary は配列で保持する
    [Serializable]
    public class TimerRecordSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;
        public TimerRecordEntry[] Entries = Array.Empty<TimerRecordEntry>();
    }

    /// 1 日分の集中時間 (秒) エントリ
    [Serializable]
    public class TimerRecordEntry
    {
        /// "yyyy-MM-dd" 形式のローカル日付
        public string Date = string.Empty;

        /// 当日の集中時間 (秒)
        public int Seconds;
    }
}
