#nullable enable

using System;
using NUnit.Framework;

namespace Shop.RewardAd.Tests
{
    public class JstDateHelperTests
    {
        static DateTimeOffset Utc(int year, int month, int day, int hour, int minute)
        {
            return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        }

        [Test]
        [Description("UTC 14:59 は JST 23:59 となり、同じ JST 日付に留まる")]
        public void Utc1459_StaysSameJstDate()
        {
            Assert.AreEqual("2026-05-23", JstDateHelper.ToJstDateString(Utc(2026, 5, 23, 14, 59)));
        }

        [Test]
        [Description("UTC 15:00 は JST 翌日 0:00 となり、JST 日付が翌日へ繰り上がる")]
        public void Utc1500_RollsToNextJstDate()
        {
            Assert.AreEqual("2026-05-24", JstDateHelper.ToJstDateString(Utc(2026, 5, 23, 15, 0)));
        }

        [Test]
        [Description("月末境界: UTC 14:59 は当月末、UTC 15:00 は翌月 1 日へ繰り上がる")]
        public void MonthEndBoundary()
        {
            Assert.AreEqual("2026-05-31", JstDateHelper.ToJstDateString(Utc(2026, 5, 31, 14, 59)));
            Assert.AreEqual("2026-06-01", JstDateHelper.ToJstDateString(Utc(2026, 5, 31, 15, 0)));
        }

        [Test]
        [Description("年末境界: UTC 14:59 は当年末、UTC 15:00 は翌年 1 月 1 日へ繰り上がる")]
        public void YearEndBoundary()
        {
            Assert.AreEqual("2026-12-31", JstDateHelper.ToJstDateString(Utc(2026, 12, 31, 14, 59)));
            Assert.AreEqual("2027-01-01", JstDateHelper.ToJstDateString(Utc(2026, 12, 31, 15, 0)));
        }

        [Test]
        [Description("ToJstDate は時刻成分を切り捨てた JST の日付（DateTime）を返す")]
        public void ToJstDate_ReturnsDateOnlyComponent()
        {
            var date = JstDateHelper.ToJstDate(Utc(2026, 5, 23, 15, 30));
            Assert.AreEqual(new DateTime(2026, 5, 24), date);
        }
    }
}
