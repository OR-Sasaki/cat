#nullable enable

using System;
using System.Globalization;

namespace Shop.RewardAd
{
    /// UTC から JST(UTC+9)の日付を求める純粋ヘルパ。
    /// Unity の .NET Standard 2.1 には DateOnly が無いため、日付は yyyy-MM-dd 文字列で表現する。
    public static class JstDateHelper
    {
        public static readonly TimeSpan JstOffset = TimeSpan.FromHours(9);
        public const string DateFormat = "yyyy-MM-dd";

        /// UTC 時刻に対応する JST 日付（時刻成分 00:00、Kind=Unspecified）を返す
        public static DateTime ToJstDate(DateTimeOffset utcNow)
        {
            return utcNow.ToOffset(JstOffset).Date;
        }

        /// UTC 時刻に対応する JST 日付を yyyy-MM-dd 文字列で返す
        public static string ToJstDateString(DateTimeOffset utcNow)
        {
            return ToJstDate(utcNow).ToString(DateFormat, CultureInfo.InvariantCulture);
        }
    }
}
