using System.Collections.Generic;
using UnityEngine;

namespace Home.State
{
    // IsoGridのセル状態を保持するState
    public class IsoGridState
    {
        public IsoGridCell[,] FloorCells { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        // UserFurnitureIDからフットプリント開始位置へのマッピング
        public Dictionary<int, Vector2Int> ObjectFootprintStartPositions { get; } = new();

        // セル配列を初期化
        public void Initialize(int gridWidth, int gridHeight)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            FloorCells = new IsoGridCell[gridWidth, gridHeight];
            ObjectFootprintStartPositions.Clear();
        }
    }
}
