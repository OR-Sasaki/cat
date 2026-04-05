using System;
using UnityEngine;

namespace Root.State
{
    public class UserState
    {
        public User User; // ユーザー情報
        public UserOutfit[] UserOutfits; // ユーザーが所持している服
        public UserFurniture[] UserFurnitures; // ユーザーが所持している家具
        public IsoGridSaveData IsoGridSaveData; // IsoGridの配置情報
    }

    [Serializable]
    public class IsoGridSaveData
    {
        public GridSaveEntry Floor;
        public GridSaveEntry LeftWall;
        public GridSaveEntry RightWall;
        public FragmentedGridSaveEntry[] FragmentedGrids;
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

    [Serializable]
    public class UserOutfit
    {
        public uint OutfitID;
    }

    [Serializable]
    public class UserFurniture
    {
        public int Id;
        public uint FurnitureID;
    }
}
