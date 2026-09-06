#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Home.OutfitEffectLogic.Tests
{
    public class CloudCoveragePlannerTests
    {
        [Test]
        [Description("全てのサンプル点がいずれかの配置の内接円に含まれ、キャラ全体を覆い尽くす")]
        public void Plan_CoversAllSamplePoints()
        {
            const float height = 2f;
            var placements = CloudCoveragePlanner.Plan(-1f, -1f, 1f, 1f, height, new Random(1));

            const float step = 0.07f * height;
            for (var y = -1f; y <= 1f; y += step)
            {
                for (var x = -1f; x <= 1f; x += step)
                {
                    var covered = false;
                    foreach (var p in placements)
                    {
                        var dx = x - p.X;
                        var dy = y - p.Y;
                        if (dx * dx + dy * dy <= p.InnerRadius * p.InnerRadius)
                        {
                            covered = true;
                            break;
                        }
                    }

                    Assert.IsTrue(covered, $"point ({x},{y}) not covered");
                }
            }
        }

        [Test]
        [Description("配置数は安全上限を超えない")]
        public void Plan_CountDoesNotExceedCap()
        {
            var placements = CloudCoveragePlanner.Plan(-1f, -1f, 1f, 1f, 2f, new Random(2));
            Assert.LessOrEqual(placements.Count, 40);
        }

        [Test]
        [Description("内接半径は許容範囲 (0.18h-0.34h) に収まる")]
        public void Plan_InnerRadiusWithinRange()
        {
            const float height = 3f;
            var placements = CloudCoveragePlanner.Plan(-1f, -1f, 1f, 1f, height, new Random(3));

            foreach (var p in placements)
            {
                Assert.GreaterOrEqual(p.InnerRadius, 0.18f * height);
                Assert.LessOrEqual(p.InnerRadius, 0.34f * height);
            }
        }

        [Test]
        [Description("同じシードなら同じ結果を返す")]
        public void Plan_SameSeed_ProducesSameResult()
        {
            var a = CloudCoveragePlanner.Plan(-1f, -1f, 1f, 1f, 2f, new Random(42));
            var b = CloudCoveragePlanner.Plan(-1f, -1f, 1f, 1f, 2f, new Random(42));

            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, b[i].X);
                Assert.AreEqual(a[i].Y, b[i].Y);
                Assert.AreEqual(a[i].InnerRadius, b[i].InnerRadius);
            }
        }

        [Test]
        [Description("退化した (サイズ0の) bounds でも最低1件の配置を返す")]
        public void Plan_DegenerateBounds_ReturnsAtLeastOnePlacement()
        {
            var placements = CloudCoveragePlanner.Plan(0f, 0f, 0f, 0f, 0f, new Random(4));
            Assert.GreaterOrEqual(placements.Count, 1);
        }
    }
}
