using System;
using Shop.State;

namespace Root.State
{
    public class MasterDataState
    {
        public bool IsImported { get; set; }
        public Outfit[] Outfits;
        public Furniture[] Furnitures;
        public ShopProduct[] ShopProducts = Array.Empty<ShopProduct>();
    }

    [Serializable]
    public class Outfit
    {
        public uint Id;
        public string Type;
        /// アセット名 (Addressables のキー兼 CSV 間の参照キー)
        public string Name;
        /// UI 表示用の名前。CSV の display_name 列が空なら Name で代替される
        public string DisplayName;
    }

    [Serializable]
    public class Furniture
    {
        public uint Id;
        public string Type;
        /// アセット名 (Addressables のキー兼 CSV 間の参照キー)
        public string Name;
        /// UI 表示用の名前。CSV の display_name 列が空なら Name で代替される
        public string DisplayName;
    }
}
