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

    /// FragmentedIsoGrid上のオブジェクトの位置とDepth情報
    public struct FragmentedObjectData
    {
        public Vector2Int Position;
        public int Depth;
    }

    /// FragmentedIsoGridごとのセル占有状態と配置オブジェクト情報
    public class FragmentedGridStateEntry
    {
        public Vector2Int Size;
        public int[,] Cells;
        public Dictionary<int, FragmentedObjectData> ObjectPositions { get; } = new();
    }

    // IsoGridのセル状態を保持するState
    public class IsoGridState
    {
        // 床グリッド
        public int[,] FloorCells { get; private set; }
        public int GridWidth { get; private set; }
        public int GridHeight { get; private set; }

        // 壁グリッド
        public int[,] LeftWallCells { get; private set; }
        public int[,] RightWallCells { get; private set; }
        public int WallHeight { get; private set; }

        // UserFurnitureIDからフットプリント開始位置へのマッピング（床）
        public Dictionary<int, Vector2Int> ObjectFootprintStartPositions { get; } = new();

        // UserFurnitureIDからフットプリント開始位置へのマッピング（壁）
        public Dictionary<int, WallObjectPosition> WallObjectFootprintStartPositions { get; } = new();

        // FragmentedIsoGridごとのセル占有状態と配置オブジェクト
        // 親家具ID → FragmentedGridStateEntry
        public Dictionary<int, FragmentedGridStateEntry> FragmentedGrids { get; } = new();

        // === 新フィールド（並行稼働） ===
        public GridEntry Floor { get; private set; }
        public GridEntry LeftWall { get; private set; }
        public GridEntry RightWall { get; private set; }
        // 名前衝突回避のため一時的に V2。手順7 で FragmentedGrids にリネーム
        public Dictionary<int, GridEntry> FragmentedGridsV2 { get; } = new();

        // セル配列を初期化
        public void Initialize(int gridWidth, int gridHeight, int wallHeight)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            WallHeight = wallHeight;

            FloorCells = new int[gridWidth, gridHeight];
            LeftWallCells = new int[gridHeight, wallHeight];  // (y, z)
            RightWallCells = new int[gridWidth, wallHeight];  // (x, z)

            // 新フィールドを「同一配列共有」で初期化
            Floor = new GridEntry(new Vector2Int(gridWidth, gridHeight), FloorCells);
            LeftWall = new GridEntry(new Vector2Int(gridHeight, wallHeight), LeftWallCells);
            RightWall = new GridEntry(new Vector2Int(gridWidth, wallHeight), RightWallCells);

            ObjectFootprintStartPositions.Clear();
            WallObjectFootprintStartPositions.Clear();
            FragmentedGrids.Clear();
            FragmentedGridsV2.Clear();
        }
    }
}
