using System.Collections.Generic;
using UnityEngine;

namespace Home.State
{
    /// 配置領域の単位（Floor / LeftWall / RightWall / Fragmented すべてに共通）
    /// - Cells: セル占有状態（値は UserFurnitureId、0 は空き）
    /// - ObjectPositions: 配置オブジェクトのフットプリント開始位置とDepth
    /// GridEntry 自身は種別（床か壁かFragmentedか）を知らない。
    /// 座標変換は呼び出し側の責務。
    public class GridEntry
    {
        public Vector2Int Size { get; }
        public int[,] Cells { get; }
        public Dictionary<int, ObjectPlacement> ObjectPositions { get; } = new();

        public GridEntry(Vector2Int size)
        {
            Size = size;
            Cells = new int[size.x, size.y];
        }

        /// 既存の int[,] を共有して構築する（並行稼働期間のため）
        public GridEntry(Vector2Int size, int[,] sharedCells)
        {
            Size = size;
            Cells = sharedCells;
        }
    }
}
