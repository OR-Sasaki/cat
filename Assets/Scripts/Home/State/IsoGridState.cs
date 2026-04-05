using System.Collections.Generic;
using UnityEngine;

namespace Home.State
{
    public enum WallSide
    {
        Left,
        Right,
    }

    // IsoGridのセル状態を保持するState
    public class IsoGridState
    {
        public GridEntry Floor { get; private set; }
        public GridEntry LeftWall { get; private set; }
        public GridEntry RightWall { get; private set; }

        // 親家具ID → FragmentedGrid の GridEntry
        public Dictionary<int, GridEntry> FragmentedGrids { get; } = new();

        // セル配列を初期化
        public void Initialize(int gridWidth, int gridHeight, int wallHeight)
        {
            Floor = new GridEntry(new Vector2Int(gridWidth, gridHeight));
            LeftWall = new GridEntry(new Vector2Int(gridHeight, wallHeight));
            RightWall = new GridEntry(new Vector2Int(gridWidth, wallHeight));

            FragmentedGrids.Clear();
        }

        public IEnumerable<GridEntry> EnumerateRootGrids()
        {
            yield return Floor;
            yield return LeftWall;
            yield return RightWall;
        }

        public IEnumerable<GridEntry> EnumerateAllGrids()
        {
            foreach (var g in EnumerateRootGrids()) yield return g;
            foreach (var g in FragmentedGrids.Values) yield return g;
        }
    }
}
