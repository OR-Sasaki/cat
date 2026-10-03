#nullable enable

using System;

namespace Root.Service
{
    /// タイマー完了時の毛糸玉報酬のレートと日次上限を扱う純粋ロジック。
    /// 日付境界の判定は Shop.RewardAd.JstDateHelper に委ね、ここでは個数計算のみを担う。
    public static class TimerYarnRewardRules
    {
        /// 集中 1 分あたりに獲得できる毛糸玉の数
        public const int YarnPerFocusMinute = 1;

        /// 1 日に獲得できる毛糸玉の上限数
        public const int DailyCap = 30;

        /// 集中秒数から上限適用前の獲得数を求める。1 分未満の端数は切り捨てる。
        /// ここで上限を適用してしまうと、上限に到達した回かどうかを
        /// 付与数との比較で判定できなくなるため、丸めは ClampToDailyCap に委ねる
        public static int CalculateEarnable(float focusSeconds)
        {
            if (focusSeconds <= 0f || !float.IsFinite(focusSeconds)) return 0;

            var minutes = (int)Math.Floor(focusSeconds / 60f);
            return minutes <= 0 ? 0 : minutes * YarnPerFocusMinute;
        }

        /// 当日の既獲得数と突き合わせ、実際に付与できる数を求める
        public static int ClampToDailyCap(int earnable, int earnedToday)
        {
            if (earnable <= 0) return 0;

            var remaining = CalculateRemaining(earnedToday);
            if (remaining <= 0) return 0;

            return Math.Min(earnable, remaining);
        }

        /// 当日の残り獲得可能数を求める
        public static int CalculateRemaining(int earnedToday)
        {
            return Math.Max(0, DailyCap - Math.Max(0, earnedToday));
        }
    }
}
