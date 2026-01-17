using System;
using System.Linq;
using Cat.Character;
using Root.State;

namespace Root.Service
{
    public class UserEquippedOutfitService
    {
        readonly UserEquippedOutfitState _userEquippedOutfitState;
        readonly PlayerPrefsService _playerPrefsService;

        public UserEquippedOutfitService(UserEquippedOutfitState userEquippedOutfitState, PlayerPrefsService playerPrefsService)
        {
            _userEquippedOutfitState = userEquippedOutfitState;
            _playerPrefsService = playerPrefsService;
            Load();
        }

        public void Equip(OutfitType type, uint outfitId)
        {
            _userEquippedOutfitState.Equip(type, outfitId);
        }

        public void Unequip(OutfitType type)
        {
            _userEquippedOutfitState.Unequip(type);
        }

        public void Save()
        {
            var data = new UserEquippedOutfitData
            {
                Outfits = _userEquippedOutfitState.GetAllEquippedOutfitIds().Select(kvp => new UserEquippedOutfit
                {
                    Type = kvp.Key,
                    OutfitId = kvp.Value
                }).ToArray()
            };
            _playerPrefsService.Save(PlayerPrefsKey.UserEquippedOutfit, data);
        }

        void Load()
        {
            var data = _playerPrefsService.Load<UserEquippedOutfitData>(PlayerPrefsKey.UserEquippedOutfit);
            if (data?.Outfits is null) return;

            foreach (var outfit in data.Outfits)
            {
                _userEquippedOutfitState.Equip(outfit.Type, outfit.OutfitId);
            }
        }

        [Serializable]
        public class UserEquippedOutfitData
        {
            public UserEquippedOutfit[] Outfits;
        }

        [Serializable]
        public class UserEquippedOutfit
        {
            public OutfitType Type;
            public uint OutfitId;
        }
    }
}

