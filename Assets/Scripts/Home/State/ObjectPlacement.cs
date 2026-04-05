using UnityEngine;

namespace Home.State
{
    /// グリッド上に配置されたオブジェクトの位置とDepth情報
    /// Floor/LeftWall/RightWall 上のオブジェクト: Depth = 0
    /// ルート上の家具のFragmentedGridに載った家具: Depth = 1
    /// さらにその上: Depth = 2 ...
    public struct ObjectPlacement
    {
        public Vector2Int Position;
        public int Depth;
    }
}
