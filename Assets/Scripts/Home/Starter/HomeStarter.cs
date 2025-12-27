using System;
using System.Linq;
using Cat.Character;
using Root.Service;
using Root.State;
using UnityEngine;
using VContainer.Unity;

namespace Home.Starter
{
    public class HomeStarter : IStartable
    {
        readonly CharacterView _characterView;
        readonly OutfitSetting _outfitSetting;
        readonly PlayerOutfitState _playerOutfitState;
        readonly PlayerOutfitService _playerOutfitService;
        readonly MasterDataState _masterDataState;

        public HomeStarter(
            CharacterView characterView,
            OutfitSetting outfitSetting,
            PlayerOutfitState playerOutfitState,
            PlayerOutfitService playerOutfitService,
            MasterDataState masterDataState)
        {
            _characterView = characterView;
            _outfitSetting = outfitSetting;
            _playerOutfitState = playerOutfitState;
            _playerOutfitService = playerOutfitService;
            _masterDataState = masterDataState;
        }

        public void Start()
        {
            try
            {
                ApplyOutfits();
            }
            catch (Exception e)
            {
                Debug.LogError($"[HomeStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        void ApplyOutfits()
        {
            ApplyDefaultOutfits();
            ApplyPlayerOutfits();
        }

        void ApplyDefaultOutfits()
        {
            var csv = Resources.Load<TextAsset>("default_outfits");

            var hasNewEquip = false;
            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
            foreach (var line in lines)
            {
                var columns = line.Split(',');
                var outfitName = columns[1].Trim();

                var outfit = _outfitSetting.Outfits?.FirstOrDefault(o => o.name == outfitName);
                if (outfit is null) continue;

                // 同じOutfitTypeがすでに装備されている場合はスキップ
                if (_playerOutfitState.GetEquippedOutfitId(outfit.OutfitType) is not null) continue;

                // マスターデータからOutfitIdを取得
                var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Name == outfitName);
                if (masterOutfit is null) continue;

                _playerOutfitService.Equip(outfit.OutfitType, masterOutfit.Id);
                hasNewEquip = true;
            }

            if (hasNewEquip)
            {
                _playerOutfitService.Save();
            }
        }

        void ApplyPlayerOutfits()
        {
            var equippedOutfits = _playerOutfitState.GetAllEquippedOutfitIds();

            foreach (var (_, outfitId) in equippedOutfits)
            {
                var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Id == outfitId);
                if (masterOutfit is null) continue;

                var outfit = _outfitSetting.Outfits?.FirstOrDefault(o => o.name == masterOutfit.Name);
                if (outfit is null) continue;

                _characterView.SetOutfit(outfit);
            }
        }
    }
}

