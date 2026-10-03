#nullable enable

using System;

namespace Root.Service
{
    /// タイマー報酬の日次獲得数の永続化スナップショット。
    /// Version != CurrentVersion の場合は破棄して当日 0 個から数え直す
    [Serializable]
    public class TimerYarnRewardSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;

        /// 獲得数が属する JST 日付 (yyyy-MM-dd)
        public string JstDate = string.Empty;

        /// 上記日付における累計獲得数
        public int EarnedYarn;
    }
}
