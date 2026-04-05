using System.Collections.Generic;
using UnityEngine;

namespace Home.State
{
    public enum WallSide
    {
        Left,
        Right,
    }

    public struct WallObjectPosition
    {
        public WallSide Side;
        public Vector2Int Position;
    }

    // IsoGridのセル状態を保持するState
    public class IsoGridState
    {
        // 床グリッド
        public IsoGridCell[,] FloorCells { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        // 壁グリッド
        public IsoGridCell[,] LeftWallCells { get; private set; }
        public IsoGridCell[,] RightWallCells { get; private set; }
        public int WallHeight { get; private set; }

        // UserFurnitureIDからフットプリント開始位置へのマッピング（床）
        public Dictionary<int, Vector2Int> ObjectFootprintStartPositions { get; } = new();

        // UserFurnitureIDからフットプリント開始位置へのマッピング（壁）
        public Dictionary<int, WallObjectPosition> WallObjectFootprintStartPositions { get; } = new();

        // セル配列を初期化
        public void Initialize(int gridWidth, int gridHeight, int wallHeight)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            WallHeight = wallHeight;

            FloorCells = new IsoGridCell[gridWidth, gridHeight];
            LeftWallCells = new IsoGridCell[gridHeight, wallHeight];  // (y, z)
            RightWallCells = new IsoGridCell[gridWidth, wallHeight];  // (x, z)

            ObjectFootprintStartPositions.Clear();
            WallObjectFootprintStartPositions.Clear();
        }
    }
}
