using System;
using System.Collections.Generic;
using Cat.Character;

namespace Root.State
{
    public class UserEquippedOutfitState
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
}

