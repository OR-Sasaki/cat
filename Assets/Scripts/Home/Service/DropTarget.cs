#nullable enable
using Home.State;
using Home.View;
using UnityEngine;
namespace Home.Service
{
    public enum DropSurface { Floor, Wall, Fragmented }

    /// ドラッグ中の家具を今離した場合の落下先。IsoDragService だけが生成する
    public readonly struct DropTarget
    {
        public DropSurface Surface { get; }
        public WallSide Side { get; }              // Surface == Wall のとき有効
        public FragmentedIsoGrid? Grid { get; }    // Surface == Fragmented のとき非 null
        public Vector2Int FootprintStart { get; }  // 各面のグリッド座標系
        public bool CanPlace { get; }              // IsoGridService.CanPlace* の結果

        public DropTarget(DropSurface surface, WallSide side, FragmentedIsoGrid? grid, Vector2Int footprintStart, bool canPlace)
        {
            Surface = surface;
            Side = side;
            Grid = grid;
            FootprintStart = footprintStart;
            CanPlace = canPlace;
        }
    }
}
