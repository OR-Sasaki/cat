using System;

namespace Root.State
{
    public class UserState
    {
        public User User;
        public UserOutfit[] UserOutfits;
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
}
