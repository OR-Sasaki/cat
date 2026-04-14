#nullable enable

using System.Collections.Generic;

namespace Root.State
{
    public class UserItemInventoryState
    {
        readonly Dictionary<uint, int> _furnitureCounts = new();
        readonly HashSet<uint> _ownedOutfitIds = new();

        public int GetFurnitureCount(uint furnitureId)
        {
            return _furnitureCounts.TryGetValue(furnitureId, out var count) ? count : 0;
        }

        public IReadOnlyDictionary<uint, int> GetAllFurnitureCounts()
        {
            return _furnitureCounts;
        }

        public bool HasOutfit(uint outfitId)
        {
            return _ownedOutfitIds.Contains(outfitId);
        }

        public IReadOnlyCollection<uint> GetAllOwnedOutfitIds()
        {
            return _ownedOutfitIds;
        }

        internal void SetFurnitureCount(uint furnitureId, int count)
        {
            if (count <= 0)
            {
                _furnitureCounts.Remove(furnitureId);
                return;
            }
            _furnitureCounts[furnitureId] = count;
        }

        internal void AddOwnedOutfit(uint outfitId)
        {
            _ownedOutfitIds.Add(outfitId);
        }

        internal void Clear()
        {
            _furnitureCounts.Clear();
            _ownedOutfitIds.Clear();
        }
    }
}
