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
        public void Utc1459_StaysSameJstDate()
        {
            // UTC 14:59 → JST 23:59 同日
            Assert.AreEqual("2026-05-23", JstDateHelper.ToJstDateString(Utc(2026, 5, 23, 14, 59)));
        }

        [Test]
        public void Utc1500_RollsToNextJstDate()
        {
            // UTC 15:00 → JST 翌日 00:00
            Assert.AreEqual("2026-05-24", JstDateHelper.ToJstDateString(Utc(2026, 5, 23, 15, 0)));
        }

        [Test]
        public void MonthEndBoundary()
        {
            Assert.AreEqual("2026-05-31", JstDateHelper.ToJstDateString(Utc(2026, 5, 31, 14, 59)));
            Assert.AreEqual("2026-06-01", JstDateHelper.ToJstDateString(Utc(2026, 5, 31, 15, 0)));
        }

        [Test]
        public void YearEndBoundary()
        {
            Assert.AreEqual("2026-12-31", JstDateHelper.ToJstDateString(Utc(2026, 12, 31, 14, 59)));
            Assert.AreEqual("2027-01-01", JstDateHelper.ToJstDateString(Utc(2026, 12, 31, 15, 0)));
        }

        [Test]
        public void ToJstDate_ReturnsDateOnlyComponent()
        {
            var date = JstDateHelper.ToJstDate(Utc(2026, 5, 23, 15, 30));
            Assert.AreEqual(new DateTime(2026, 5, 24), date);
        }
    }
}
