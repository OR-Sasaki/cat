#nullable enable

using NUnit.Framework;

namespace Shop.RewardAd.Tests
{
    public class ShopProductCsvParserTests
    {
        [Test]
        public void ValidEightColumnLine_ParsesAllFields()
        {
            var ok = ShopProductCsvParser.TryParseLine("2,SmallBox,furniture,3,0,reward_ad,1,3", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(2u, row.Id);
            Assert.AreEqual("SmallBox", row.Name);
            Assert.AreEqual("furniture", row.ItemTypeRaw);
            Assert.AreEqual(3u, row.ItemId);
            Assert.AreEqual(0, row.Price);
            Assert.AreEqual("reward_ad", row.CurrencyTypeRaw);
            Assert.AreEqual(1, row.Amount);
            Assert.AreEqual(3, row.DailyCap);
        }

        [Test]
        public void FewerThanEightColumns_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void EmptyAmount_DefaultsToOne()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn,,", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, row.Amount);
        }

        [Test]
        public void EmptyDailyCap_IsNull()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn,,", out var row, out _);

            Assert.IsTrue(ok);
            Assert.IsNull(row.DailyCap);
        }

        [Test]
        public void InvalidAmount_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,0,reward_ad,abc,", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void InvalidDailyCap_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,0,reward_ad,1,xyz", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void InvalidId_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("abc,Body001,outfit,1,0,reward_ad,1,3", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void LegacyYarnRow_WithEmptyNewColumns_Parses()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn,,", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(100u, row.Id);
            Assert.AreEqual(1, row.Amount);
            Assert.IsNull(row.DailyCap);
        }

        [Test]
        public void TrailingCarriageReturn_IsTrimmed()
        {
            var ok = ShopProductCsvParser.TryParseLine("2,SmallBox,furniture,3,0,reward_ad,1,3\r", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(3, row.DailyCap);
        }
    }
}
