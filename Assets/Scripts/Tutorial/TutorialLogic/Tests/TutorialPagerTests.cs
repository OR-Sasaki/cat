#nullable enable

using System;
using NUnit.Framework;

namespace Tutorial.TutorialLogic.Tests
{
    public class TutorialPagerTests
    {
        [Test]
        [Description("1 ページのみなら生成直後に最終ページ到達済みになり、ナビゲーションは不要")]
        public void SinglePage_ReachesLastImmediately()
        {
            var pager = new TutorialPager(1);

            Assert.IsTrue(pager.HasReachedLast);
            Assert.IsFalse(pager.ShowsNavigation);
        }

        [Test]
        [Description("複数ページでは生成直後は最終ページ未到達で、ナビゲーションを表示する")]
        public void MultiPage_DoesNotReachLastImmediately()
        {
            var pager = new TutorialPager(3);

            Assert.IsFalse(pager.HasReachedLast);
            Assert.IsTrue(pager.ShowsNavigation);
        }

        [Test]
        [Description("先頭ページで Previous は false を返し Index は変わらない")]
        public void Previous_AtFirst_ReturnsFalse()
        {
            var pager = new TutorialPager(3);

            Assert.IsFalse(pager.Previous());
            Assert.AreEqual(0, pager.Index);
        }

        [Test]
        [Description("最終ページで Next は false を返し Index は変わらない")]
        public void Next_AtLast_ReturnsFalse()
        {
            var pager = new TutorialPager(2);
            pager.Next();

            Assert.IsFalse(pager.Next());
            Assert.AreEqual(1, pager.Index);
        }

        [Test]
        [Description("最終ページ到達後に戻っても HasReachedLast は true のまま")]
        public void HasReachedLast_StaysTrueAfterPrevious()
        {
            var pager = new TutorialPager(2);
            pager.Next();

            Assert.IsTrue(pager.Previous());

            Assert.IsTrue(pager.HasReachedLast);
        }

        [Test]
        [Description("範囲外への JumpTo は false を返し Index は変わらない")]
        public void JumpTo_OutOfRange_ReturnsFalse()
        {
            var pager = new TutorialPager(3);

            Assert.IsFalse(pager.JumpTo(-1));
            Assert.IsFalse(pager.JumpTo(3));
            Assert.AreEqual(0, pager.Index);
        }

        [Test]
        [Description("最終ページへの JumpTo で HasReachedLast が true になる")]
        public void JumpTo_Last_SetsHasReachedLast()
        {
            var pager = new TutorialPager(3);

            Assert.IsTrue(pager.JumpTo(2));

            Assert.IsTrue(pager.HasReachedLast);
        }

        [Test]
        [Description("ページ数 0 では ArgumentOutOfRangeException を投げる")]
        public void Constructor_ZeroPages_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TutorialPager(0));
        }
    }
}
