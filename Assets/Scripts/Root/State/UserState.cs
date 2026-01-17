using System;

namespace Root.State
{
    public class UserState
    {
        public User User; // ユーザー情報
        public UserOutfit[] UserOutfits; // ユーザーが所持している服
        public UserFurniture[] UserFurnitures; // ユーザーが所持している家具
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
        public uint FurnitureID;
    }
}
