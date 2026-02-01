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
        public IsoGridObjectPosition[] ObjectPositions;
        public IsoGridWallObjectPosition[] WallObjectPositions;
    }

    [Serializable]
    public class IsoGridObjectPosition
    {
        public int UserFurnitureId;
        public int X;
        public int Y;
    }

    [Serializable]
    public class IsoGridWallObjectPosition
    {
        public int UserFurnitureId;
        public int Side; // 0=Left, 1=Right
        public int X;
        public int Z;
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
