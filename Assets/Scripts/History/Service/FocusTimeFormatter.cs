#nullable enable

// 画面全体で一貫した秒→分変換 (切り捨て) を提供する。

namespace History.Service
{
    /// 集中時間 (秒) を分単位に切り捨て変換し、表示用の文字列を組み立てる純粋関数 static クラス。
    public static class FocusTimeFormatter
    {
        /// 秒値を分単位に切り捨てて返す。負値入力は 0 を返す。
        public static int SecondsToMinutes(int seconds)
        {
            if (seconds < 0)
            {
                return 0;
            }

            return seconds / 60;
        }

        /// 秒値を分単位に切り捨てた上で "{N}分" 形式の文字列にして返す。
        public static string FormatMinutes(int seconds)
        {
            int minutes = SecondsToMinutes(seconds);
            return $"{minutes}分";
        }
    }
}
