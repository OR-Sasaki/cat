#nullable enable

using NUnit.Framework;

namespace Root.AudioLogic.Tests
{
    public class SeSourcePickerTests
    {
        [Test]
        [Description("空きソースがあれば最小インデックスの空きを返す")]
        public void Pick_HasFreeSource_ReturnsSmallestFreeIndex()
        {
            var isPlaying = new[] { true, false, false };
            var startOrder = new[] { 1, 2, 3 };

            Assert.AreEqual(1, SeSourcePicker.Pick(isPlaying, startOrder));
        }

        [Test]
        [Description("全ソース使用中は startOrder が最小（最古）のインデックスを返す")]
        public void Pick_AllPlaying_ReturnsOldestIndex()
        {
            var isPlaying = new[] { true, true, true };
            var startOrder = new[] { 3, 1, 2 };

            Assert.AreEqual(1, SeSourcePicker.Pick(isPlaying, startOrder));
        }

        [Test]
        [Description("単一ソースで空きなら 0 を返す")]
        public void Pick_SingleSourceFree_ReturnsZero()
        {
            var isPlaying = new[] { false };
            var startOrder = new[] { 0 };

            Assert.AreEqual(0, SeSourcePicker.Pick(isPlaying, startOrder));
        }

        [Test]
        [Description("単一ソースで使用中でも 0 を返す")]
        public void Pick_SingleSourcePlaying_ReturnsZero()
        {
            var isPlaying = new[] { true };
            var startOrder = new[] { 5 };

            Assert.AreEqual(0, SeSourcePicker.Pick(isPlaying, startOrder));
        }

        [Test]
        [Description("長さ 0 のときは -1 を返す")]
        public void Pick_Empty_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, SeSourcePicker.Pick(System.Array.Empty<bool>(), System.Array.Empty<int>()));
        }
    }
}
