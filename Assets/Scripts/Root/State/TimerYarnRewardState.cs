#nullable enable

namespace Root.State
{
    /// タイマー報酬の当日獲得数と、それが属する JST 日付を保持する
    public class TimerYarnRewardState
    {
        /// 獲得数が属する JST 日付 (yyyy-MM-dd)。未初期化は空文字
        public string JstDate { get; private set; } = string.Empty;

        /// 上記日付における累計獲得数
        public int EarnedYarn { get; private set; }

        internal void Set(string jstDate, int earnedYarn)
        {
            JstDate = jstDate;
            EarnedYarn = earnedYarn;
        }
    }
}
