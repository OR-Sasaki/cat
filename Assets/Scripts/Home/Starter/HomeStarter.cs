using System;
using System.Collections.Generic;
using System.Linq;
using Cat.Character;
using Root.Service;
using Root.State;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Home.Starter
{
    public class HomeStarter : IStartable
    {
        readonly CharacterView _characterView;
        readonly PlayerOutfitState _playerOutfitState;
        readonly PlayerOutfitService _playerOutfitService;
        readonly MasterDataState _masterDataState;

        public HomeStarter(
            CharacterView characterView,
            PlayerOutfitState playerOutfitState,
            PlayerOutfitService playerOutfitService,
            MasterDataState masterDataState)
        {
            _characterView = characterView;
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
            ApplyDefaultOutfits(() => ApplyPlayerOutfits());
        }

        void ApplyDefaultOutfits(Action onComplete)
        {
            var csv = Resources.Load<TextAsset>("default_outfits");
            if (csv is null)
            {
                onComplete?.Invoke();
                return;
            }

            var lines = csv.text.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            if (lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            var hasNewEquip = false;
            var pendingCount = lines.Count;

            foreach (var line in lines)
            {
                var columns = line.Split(',');
                var outfitName = columns[1].Trim();

                // マスターデータからOutfitを取得
                var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Name == outfitName);
                if (masterOutfit is null)
                {
                    pendingCount--;
                    if (pendingCount <= 0) CompleteDefaultOutfits(hasNewEquip, onComplete);
                    continue;
                }

                // Addressablesからロード
                var address = $"{masterOutfit.Type}/{outfitName}.asset";
                var handle = Addressables.LoadAssetAsync<Cat.Character.Outfit>(address);
                var capturedMasterOutfit = masterOutfit;
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded && h.Result is not null)
                    {
                        var outfit = h.Result;
                        // 同じOutfitTypeがすでに装備されている場合はスキップ
                        if (_playerOutfitState.GetEquippedOutfitId(outfit.OutfitType) is null)
                        {
                            _playerOutfitService.Equip(outfit.OutfitType, capturedMasterOutfit.Id);
                            hasNewEquip = true;
                        }
                    }

                    pendingCount--;
                    if (pendingCount <= 0) CompleteDefaultOutfits(hasNewEquip, onComplete);
                };
            }
        }

        void CompleteDefaultOutfits(bool hasNewEquip, Action onComplete)
        {
            if (hasNewEquip)
            {
                _playerOutfitService.Save();
            }
            onComplete?.Invoke();
        }

        void ApplyPlayerOutfits()
        {
            var equippedOutfits = _playerOutfitState.GetAllEquippedOutfitIds();
            if (equippedOutfits.Count == 0) return;

            foreach (var (_, outfitId) in equippedOutfits)
            {
                var masterOutfit = _masterDataState.Outfits?.FirstOrDefault(o => o.Id == outfitId);
                if (masterOutfit is null) continue;

                // Addressablesからロード
                var address = $"{masterOutfit.Type}/{masterOutfit.Name}.asset";
                var handle = Addressables.LoadAssetAsync<Cat.Character.Outfit>(address);
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded && h.Result is not null)
                    {
                        _characterView.SetOutfit(h.Result);
                    }
                };
            }
        }
    }
}

