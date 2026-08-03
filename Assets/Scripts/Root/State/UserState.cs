using System;
using UnityEngine;

namespace Root.State
{
    public class UserState
    {
        public User User; // ユーザー情報
        public IsoGridSaveData IsoGridSaveData; // IsoGridの配置情報
        // 所持アイテムは UserItemInventoryService (PlayerPrefs) が単一ソース
    }

    [Serializable]
    public class IsoGridSaveData
    {
        public GridSaveEntry Floor;
        public GridSaveEntry LeftWall;
        public GridSaveEntry RightWall;
        public FragmentedGridSaveEntry[] FragmentedGrids;
        /// 配置中 Base の UserFurnitureId。-1 = 未配置 sentinel。
        /// 旧セーブデータには本フィールドが存在しないため JsonUtility は 0 で復元する。
        /// 復元側 (IsoGridLoadService.LoadBaseObject) は <= 0 をすべて未設定扱いする前提で運用する。
        public int BaseUserFurnitureId = -1;
    }

    [Serializable]
    public class GridSaveEntry
    {
        public ObjectPlacementSaveEntry[] ObjectPositions;
    }

    [Serializable]
    public class FragmentedGridSaveEntry
    {
        public int ParentUserFurnitureId; // FragmentedIsoGridを持つ親家具のID
        public ObjectPlacementSaveEntry[] ObjectPositions;
    }

    [Serializable]
    public class ObjectPlacementSaveEntry
    {
        public int UserFurnitureId;
        public int X;
        public int Y;
        public int Depth; // ルート=0, Fragmented=1以上（ロード順序用）
    }

    [Serializable]
    public class User
    {
        public string Name;
    }
}
