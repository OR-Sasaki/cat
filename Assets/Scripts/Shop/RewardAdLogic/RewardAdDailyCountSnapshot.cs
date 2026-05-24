#nullable enable

using System;

namespace Shop.RewardAd
{
    /// 日次視聴消化回数の永続化スナップショット。
    /// Version != CurrentVersion の場合は破棄して全リセットする。
    [Serializable]
    public class RewardAdDailyCountSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;
        public string JstDate = string.Empty; // yyyy-MM-dd
        public DailyCountEntry[] Entries = Array.Empty<DailyCountEntry>();

        [Serializable]
        public class DailyCountEntry
        {
            public uint ProductId;
            public int Count;
        }
    }
}
