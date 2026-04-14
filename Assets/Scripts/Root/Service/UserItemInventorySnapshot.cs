#nullable enable

using System;

namespace Root.Service
{
    /// 所持アイテム (家具・着せ替え) の永続化スナップショット
    /// Version != CurrentVersion の場合は破棄して空状態で初期化する
    /// JsonUtility 制約により Dictionary/HashSet は配列で保持
    [Serializable]
    public class UserItemInventorySnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;
        public FurnitureHoldingEntry[] Furnitures = Array.Empty<FurnitureHoldingEntry>();
        public uint[] OwnedOutfitIds = Array.Empty<uint>();
    }

    [Serializable]
    public class FurnitureHoldingEntry
    {
        public uint FurnitureId;
        public int Count;
    }
}
