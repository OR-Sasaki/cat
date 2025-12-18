using System;
using System.Collections.Generic;
using Cat.Character;

namespace Root.State
{
    public class PlayerOutfitState
    {
        readonly Dictionary<OutfitType, uint> _equippedOutfitIds = new();

        public uint? GetEquippedOutfitId(OutfitType type)
        {
            return _equippedOutfitIds.TryGetValue(type, out var id) ? id : null;
        }

        public IReadOnlyDictionary<OutfitType, uint> GetAllEquippedOutfitIds()
        {
            return _equippedOutfitIds;
        }

        public void Equip(OutfitType type, uint outfitId)
        {
            _equippedOutfitIds[type] = outfitId;
        }

        public void Unequip(OutfitType type)
        {
            _equippedOutfitIds.Remove(type);
        }
    }

    [Serializable]
    public class PlayerOutfitData
    {
        public PlayerOutfit[] Outfits;
    }

    [Serializable]
    public class PlayerOutfit
    {
        public OutfitType Type;
        public uint OutfitId;
    }
}

