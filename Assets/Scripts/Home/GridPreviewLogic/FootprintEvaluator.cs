#nullable enable

using System;
using System.Collections.Generic;

namespace Home.GridPreviewLogic
{
    /// グリッド上の 1 セル座標
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public int X { get; }
        public int Y { get; }

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X}, {Y})";
        public static bool operator ==(GridCell a, GridCell b) => a.Equals(b);
        public static bool operator !=(GridCell a, GridCell b) => !a.Equals(b);
    }

    /// 家具設置判定の結果
    public readonly struct FootprintEvaluation
    {
        /// 設置可能かどうか
        public bool CanPlace { get; }

        /// 範囲内セルのみ（表示用）
        public IReadOnlyList<GridCell> VisibleCells { get; }

        public FootprintEvaluation(bool canPlace, IReadOnlyList<GridCell> visibleCells)
        {
            CanPlace = canPlace;
            VisibleCells = visibleCells;
        }
    }

    /// 家具フットプリントの設置可否を判定する純粋計算ロジック。UnityEngine 非依存
    public static class FootprintEvaluator
    {
        /// footprintStart を起点に footprintSize 分のセルを走査し、範囲外なし・占有なしなら設置可能とする
        public static FootprintEvaluation Evaluate(
            GridCell footprintStart,
            GridCell footprintSize,
            int selfUserFurnitureId,
            Func<GridCell, bool> isInRange,
            Func<GridCell, int> occupantIdOf)
        {
            var visibleCells = new List<GridCell>(footprintSize.X * footprintSize.Y);
            var canPlace = true;

            for (var x = 0; x < footprintSize.X; x++)
            {
                for (var y = 0; y < footprintSize.Y; y++)
                {
                    var cell = new GridCell(footprintStart.X + x, footprintStart.Y + y);

                    if (!isInRange(cell))
                    {
                        canPlace = false;
                        continue;
                    }

                    visibleCells.Add(cell);

                    var occupantId = occupantIdOf(cell);
                    if (occupantId != 0 && occupantId != selfUserFurnitureId)
                        canPlace = false;
                }
            }

            return new FootprintEvaluation(canPlace, visibleCells);
        }
    }
}
