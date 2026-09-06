#nullable enable
using System;
using System.Collections.Generic;

namespace Home.OutfitEffectLogic
{
    /// 雲配置1件。UnityEngine非依存 (Vector3の代わりにX/Y)
    public readonly struct CloudPlacement
    {
        public readonly float X;
        public readonly float Y;
        public readonly float InnerRadius;

        public CloudPlacement(float x, float y, float innerRadius)
        {
            X = x;
            Y = y;
            InnerRadius = innerRadius;
        }
    }

    /// OutfitChangeEffect デモの BuildCloudCoverage を UnityEngine 非依存に移植したもの
    public static class CloudCoveragePlanner
    {
        const float Spacing = 0.07f;
        const float OffsetRange = 0.08f;
        const float InnerRadiusMin = 0.18f;
        const float InnerRadiusMax = 0.34f;

        /// 想定超過時に打ち切る安全上限 (通常は12-25件程度)
        const int MaxPlacements = 40;

        /// bounds をグリッドサンプリングし、シャッフル順に未カバーの点から雲を配置して全体を覆い尽くす配置を求める
        public static List<CloudPlacement> Plan(float minX, float minY, float maxX, float maxY, float height, Random random)
        {
            var step = Spacing * height;
            if (step <= 0f)
            {
                // 退化した入力 (高さ0など) は単一配置でフォールバック
                return new List<CloudPlacement> { new(minX, minY, InnerRadiusMax) };
            }

            var samples = new List<(float x, float y)>();
            for (var y = minY; y <= maxY; y += step)
            {
                for (var x = minX; x <= maxX; x += step)
                {
                    samples.Add((x, y));
                }
            }

            // Fisher-Yates シャッフルで走査順をランダム化し、機械的な並びを避ける
            for (var i = samples.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (samples[i], samples[j]) = (samples[j], samples[i]);
            }

            var covered = new bool[samples.Count];
            var placements = new List<CloudPlacement>();

            for (var i = 0; i < samples.Count; i++)
            {
                if (covered[i])
                {
                    continue;
                }

                if (placements.Count >= MaxPlacements)
                {
                    break;
                }

                var offsetX = ((float)random.NextDouble() * 2f - 1f) * OffsetRange * height;
                var offsetY = ((float)random.NextDouble() * 2f - 1f) * OffsetRange * height;
                var spawnX = samples[i].x + offsetX;
                var spawnY = samples[i].y + offsetY;
                var innerRadius = (InnerRadiusMin + (float)random.NextDouble() * (InnerRadiusMax - InnerRadiusMin)) * height;
                placements.Add(new CloudPlacement(spawnX, spawnY, innerRadius));

                for (var j = 0; j < samples.Count; j++)
                {
                    if (covered[j])
                    {
                        continue;
                    }

                    var dx = samples[j].x - spawnX;
                    var dy = samples[j].y - spawnY;
                    if (dx * dx + dy * dy <= innerRadius * innerRadius)
                    {
                        covered[j] = true;
                    }
                }
            }

            return placements;
        }
    }
}
