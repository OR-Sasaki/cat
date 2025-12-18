using System.Linq;
using Cat.Character;
using Root.State;

namespace Root.Service
{
    public class PlayerOutfitService
    {
        readonly PlayerOutfitState _playerOutfitState;
        readonly PlayerPrefsService _playerPrefsService;

        public PlayerOutfitService(PlayerOutfitState playerOutfitState, PlayerPrefsService playerPrefsService)
        {
            _playerOutfitState = playerOutfitState;
            _playerPrefsService = playerPrefsService;
            Load();
        }

        public void Equip(OutfitType type, uint outfitId)
        {
            _playerOutfitState.Equip(type, outfitId);
        }

        public void Unequip(OutfitType type)
        {
            _playerOutfitState.Unequip(type);
        }

        public void Save()
        {
            var data = new PlayerOutfitData
            {
                Outfits = _playerOutfitState.GetAllEquippedOutfitIds().Select(kvp => new PlayerOutfit
                {
                    Type = kvp.Key,
                    OutfitId = kvp.Value
                }).ToArray()
            };
            _playerPrefsService.Save(PlayerPrefsKey.PlayerOutfit, data);
        }

        void Load()
        {
            var data = _playerPrefsService.Load<PlayerOutfitData>(PlayerPrefsKey.PlayerOutfit);
            if (data?.Outfits is null) return;

            foreach (var outfit in data.Outfits)
            {
                _playerOutfitState.Equip(outfit.Type, outfit.OutfitId);
            }
        }
    }
}

