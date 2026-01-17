using System;
using System.Linq;
using Cat.Character;
using Home.State;
using Root.Service;
using Root.State;
using UnityEngine;
using VContainer.Unity;

namespace Home.Starter
{
    public class HomeStarter : IStartable
    {
        readonly CharacterView _characterView;
        readonly UserEquippedOutfitState _userEquippedOutfitState;
        readonly UserEquippedOutfitService _userEquippedOutfitService;
        readonly MasterDataState _masterDataState;
        readonly OutfitAssetState _outfitAssetState;

        public HomeStarter(
            CharacterView characterView,
            UserEquippedOutfitState userEquippedOutfitState,
            UserEquippedOutfitService userEquippedOutfitService,
            MasterDataState masterDataState,
            OutfitAssetState outfitAssetState)
        {
            _characterView = characterView;
            _userEquippedOutfitState = userEquippedOutfitState;
            _userEquippedOutfitService = userEquippedOutfitService;
            _masterDataState = masterDataState;
            _outfitAssetState = outfitAssetState;
        }

        public void Start()
        {
            if (_outfitAssetState.IsLoaded)
            {
                ApplyOutfits();
            }
            else
            {
                _outfitAssetState.OnLoaded += ApplyOutfits;
            }
        }

        void ApplyOutfits()
        {
            try
            {
                ApplyDefaultOutfits();
                ApplyPlayerOutfits();
            }
            catch (Exception e)
            {
                Debug.LogError($"[HomeStarter] {e.Message}\n{e.StackTrace}");
            }
        }

        void ApplyDefaultOutfits()
        {
            var csv = Resources.Load<TextAsset>("default_outfits");
            if (csv is null) return;

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            if (lines.Count == 0) return;

            var hasNewEquip = false;

            foreach (var line in lines)
            {
                var columns = line.Split(',');
                var outfitName = columns[1].Trim();

                var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Name == outfitName);
                if (masterOutfit is null) continue;

                var outfit = _outfitAssetState.Get(outfitName);
                if (outfit is null) continue;

                if (_userEquippedOutfitState.GetEquippedOutfitId(outfit.OutfitType) is null)
                {
                    _userEquippedOutfitService.Equip(outfit.OutfitType, masterOutfit.Id);
                    hasNewEquip = true;
                }
            }

            if (hasNewEquip)
            {
                _userEquippedOutfitService.Save();
            }
        }

        void ApplyPlayerOutfits()
        {
            var equippedOutfits = _userEquippedOutfitState.GetAllEquippedOutfitIds();
            if (equippedOutfits.Count == 0) return;

            foreach (var (_, outfitId) in equippedOutfits)
            {
                var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Id == outfitId);
                if (masterOutfit is null) continue;

                var outfit = _outfitAssetState.Get(masterOutfit.Name);
                if (outfit is null) continue;

                _characterView.SetOutfit(outfit);
            }
        }
    }
}

