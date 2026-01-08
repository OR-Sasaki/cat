using System;

namespace Root.State
{
    public class MasterDataState
    {
        public bool IsImported { get; set; }
        public Outfit[] Outfits;
        public Furniture[] Furnitures;
    }

    [Serializable]
    public class Outfit
    {
        public uint Id;
        public string Type;
        public string Name;
    }

    [Serializable]
    public class Furniture
    {
        public uint Id;
        public string Type;
        public string Name;
    }
}
