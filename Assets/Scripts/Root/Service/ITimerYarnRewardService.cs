#nullable enable

namespace Root.Service
{
    /// タイマー完了時の毛糸玉報酬を、日次上限付きで付与するサービス契約
    public interface ITimerYarnRewardService
    {
        /// タイマー完了時の報酬を付与する。日次上限はこの呼び出し時点の
        /// 日付 (ショップと同じ JST 境界) を基準に判定する
        TimerYarnRewardResult GrantForCompletedSession(float focusSeconds);
    }

    /// 報酬付与の結果
    public readonly struct TimerYarnRewardResult
    {
        /// 実際に付与された毛糸玉の数
        public int GrantedYarn { get; }

        /// 日次上限を適用する前の獲得数 (集中時間だけで決まる数)
        public int EarnableYarn { get; }

        /// 付与後の当日累計獲得数
        public int EarnedToday { get; }

        /// 1 日の上限数
        public int DailyCap { get; }

        /// 日次上限によって獲得数が削られたか
        public bool IsCapped => GrantedYarn < EarnableYarn;

        public TimerYarnRewardResult(int grantedYarn, int earnableYarn, int earnedToday, int dailyCap)
        {
            GrantedYarn = grantedYarn;
            EarnableYarn = earnableYarn;
            EarnedToday = earnedToday;
            DailyCap = dailyCap;
        }
    }
}
