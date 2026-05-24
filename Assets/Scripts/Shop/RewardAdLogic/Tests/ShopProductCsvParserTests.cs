#nullable enable

using NUnit.Framework;

namespace Shop.RewardAd.Tests
{
    public class ShopProductCsvParserTests
    {
        [Test]
        [Description("正常な 8 カラム行はすべてのフィールドを正しく解釈する")]
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
        [Description("カラム数が 8 未満の行はスキップ対象として失敗を返す")]
        public void FewerThanEightColumns_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("amount 欄が空文字なら既定の 1 として解釈する")]
        public void EmptyAmount_DefaultsToOne()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn,,", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, row.Amount);
        }

        [Test]
        [Description("daily_cap 欄が空文字なら null（既定値適用）として解釈する")]
        public void EmptyDailyCap_IsNull()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn,,", out var row, out _);

            Assert.IsTrue(ok);
            Assert.IsNull(row.DailyCap);
        }

        [Test]
        [Description("amount が数値として不正な行はスキップ対象として失敗を返す")]
        public void InvalidAmount_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,0,reward_ad,abc,", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("daily_cap が数値として不正な行はスキップ対象として失敗を返す")]
        public void InvalidDailyCap_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,0,reward_ad,1,xyz", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("id が数値として不正な行はスキップ対象として失敗を返す")]
        public void InvalidId_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("abc,Body001,outfit,1,0,reward_ad,1,3", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("price が負値の行はスキップ対象として失敗を返す")]
        public void NegativePrice_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,-1,reward_ad,1,3", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("amount が 0 の行はスキップ対象として失敗を返す")]
        public void ZeroAmount_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,0,reward_ad,0,3", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("amount が負値の行はスキップ対象として失敗を返す")]
        public void NegativeAmount_Fails()
        {
            var ok = ShopProductCsvParser.TryParseLine("3,Body001,outfit,1,0,reward_ad,-5,3", out _, out var error);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Description("既存 Yarn 行（新カラム amount/daily_cap が空）も後方互換で正常に解釈する")]
        public void LegacyYarnRow_WithEmptyNewColumns_Parses()
        {
            var ok = ShopProductCsvParser.TryParseLine("100,Body001,outfit,1,100,yarn,,", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(100u, row.Id);
            Assert.AreEqual(1, row.Amount);
            Assert.IsNull(row.DailyCap);
        }

        [Test]
        [Description("行末の CR（CRLF 由来）は各カラムの Trim で除去され、最終カラムも正しく解釈される")]
        public void TrailingCarriageReturn_IsTrimmed()
        {
            var ok = ShopProductCsvParser.TryParseLine("2,SmallBox,furniture,3,0,reward_ad,1,3\r", out var row, out _);

            Assert.IsTrue(ok);
            Assert.AreEqual(3, row.DailyCap);
        }
    }
}
