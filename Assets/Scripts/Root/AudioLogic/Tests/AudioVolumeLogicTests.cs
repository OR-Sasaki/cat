#nullable enable

using NUnit.Framework;

namespace Root.AudioLogic.Tests
{
    public class AudioVolumeLogicTests
    {
        [Test]
        [Description("0〜1 の範囲内の値はそのまま返す")]
        public void Clamp_WithinRange_ReturnsAsIs()
        {
            Assert.AreEqual(0.5f, AudioVolumeLogic.Clamp(0.5f));
        }

        [Test]
        [Description("負値は 0 に丸める")]
        public void Clamp_Negative_ReturnsZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.Clamp(-0.3f));
        }

        [Test]
        [Description("1 を超える値は 1 に丸める")]
        public void Clamp_OverOne_ReturnsOne()
        {
            Assert.AreEqual(1f, AudioVolumeLogic.Clamp(1.5f));
        }

        [Test]
        [Description("NaN は 0 に丸める")]
        public void Clamp_NaN_ReturnsZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.Clamp(float.NaN));
        }

        [Test]
        [Description("有効時は基準音量と BGM 音量の積を返す")]
        public void CalcBgmVolume_Enabled_ReturnsProduct()
        {
            Assert.AreEqual(0.4f, AudioVolumeLogic.CalcBgmVolume(true, 0.8f, 0.5f), 0.0001f);
        }

        [Test]
        [Description("基準音量または BGM 音量のどちらかが 0 なら 0 を返す")]
        public void CalcBgmVolume_EitherZero_ReturnsZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcBgmVolume(true, 0f, 0.5f));
            Assert.AreEqual(0f, AudioVolumeLogic.CalcBgmVolume(true, 0.8f, 0f));
        }

        [Test]
        [Description("サウンド無効時は常に 0 を返す")]
        public void CalcBgmVolume_Disabled_AlwaysZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcBgmVolume(false, 0.8f, 0.5f));
        }

        [Test]
        [Description("基準音量が負値なら 0 として計算する")]
        public void CalcBgmVolume_NegativeClipBaseVolume_ClampedToZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcBgmVolume(true, -0.5f, 0.5f));
        }

        [Test]
        [Description("BGM 音量が 1 を超えるなら 1 として計算する")]
        public void CalcBgmVolume_BgmVolumeOverOne_ClampedToOne()
        {
            Assert.AreEqual(0.8f, AudioVolumeLogic.CalcBgmVolume(true, 0.8f, 1.5f), 0.0001f);
        }

        [Test]
        [Description("基準音量が NaN なら 0 として計算する")]
        public void CalcBgmVolume_ClipBaseVolumeNaN_ClampedToZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcBgmVolume(true, float.NaN, 0.5f));
        }

        [Test]
        [Description("BGM 音量が NaN なら 0 として計算する")]
        public void CalcBgmVolume_BgmVolumeNaN_ClampedToZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcBgmVolume(true, 0.8f, float.NaN));
        }

        [Test]
        [Description("有効時は SE 音量をそのまま返す")]
        public void CalcSeVolume_Enabled_ReturnsAsIs()
        {
            Assert.AreEqual(0.6f, AudioVolumeLogic.CalcSeVolume(true, 0.6f));
        }

        [Test]
        [Description("SE 音量が 0 なら 0 を返す")]
        public void CalcSeVolume_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcSeVolume(true, 0f));
        }

        [Test]
        [Description("サウンド無効時は常に 0 を返す")]
        public void CalcSeVolume_Disabled_AlwaysZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcSeVolume(false, 0.6f));
        }

        [Test]
        [Description("SE 音量が負値なら 0 に丸める")]
        public void CalcSeVolume_Negative_ClampedToZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcSeVolume(true, -0.6f));
        }

        [Test]
        [Description("SE 音量が 1 を超えるなら 1 に丸める")]
        public void CalcSeVolume_OverOne_ClampedToOne()
        {
            Assert.AreEqual(1f, AudioVolumeLogic.CalcSeVolume(true, 1.6f));
        }

        [Test]
        [Description("SE 音量が NaN なら 0 に丸める")]
        public void CalcSeVolume_NaN_ClampedToZero()
        {
            Assert.AreEqual(0f, AudioVolumeLogic.CalcSeVolume(true, float.NaN));
        }
    }
}
