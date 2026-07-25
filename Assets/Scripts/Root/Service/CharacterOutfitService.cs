#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cat.Character;
using Cysharp.Threading.Tasks;
using Root.State;
using UnityEngine;

namespace Root.Service
{
    /// 装備中の Outfit を CharacterView へ反映する
    /// Home / Timer など CharacterView を持つシーンから共通で利用し、見た目をシーン間で一致させる
    /// Cat.Character.Outfit と Root.State.Outfit が同名のため、型名を直接書かず var で受ける
    public class CharacterOutfitService
    {
        const string DefaultOutfitsCsvName = "default_outfits";

        readonly OutfitAssetState _outfitAssetState;
        readonly OutfitAssetService _outfitAssetService;
        readonly UserEquippedOutfitState _userEquippedOutfitState;
        readonly UserEquippedOutfitService _userEquippedOutfitService;
        readonly MasterDataState _masterDataState;

        public CharacterOutfitService(
            OutfitAssetState outfitAssetState,
            OutfitAssetService outfitAssetService,
            UserEquippedOutfitState userEquippedOutfitState,
            UserEquippedOutfitService userEquippedOutfitService,
            MasterDataState masterDataState)
        {
            _outfitAssetState = outfitAssetState;
            _outfitAssetService = outfitAssetService;
            _userEquippedOutfitState = userEquippedOutfitState;
            _userEquippedOutfitService = userEquippedOutfitService;
            _masterDataState = masterDataState;
        }

        /// 装備中の Outfit（未装備の部位はデフォルト）を characterView へ適用する
        public async UniTask ApplyEquippedAsync(CharacterView characterView, CancellationToken cancellationToken)
        {
            if (characterView == null) return;

            // デフォルト装備の判定には該当アセットの OutfitType が必要なため、装備分とデフォルト分の両方をロードする
            var defaultOutfitNames = LoadDefaultOutfitNames();
            var targetNames = GetEquippedOutfitNames().Concat(defaultOutfitNames).ToArray();

            await _outfitAssetService.LoadAsync(targetNames, cancellationToken);

            EquipDefaultsForEmptySlots(defaultOutfitNames);
            Apply(characterView);
        }

        /// 装備中の OutfitId をマスタ経由でアセット名へ解決する
        string[] GetEquippedOutfitNames()
        {
            var masterOutfits = _masterDataState.Outfits;
            if (masterOutfits is null) return Array.Empty<string>();

            return _userEquippedOutfitState.GetAllEquippedOutfitIds().Values
                .Select(id => masterOutfits.FirstOrDefault(o => o.Id == id))
                .Where(masterOutfit => masterOutfit is not null)
                .Select(masterOutfit => masterOutfit!.Name)
                .ToArray();
        }

        /// default_outfits.csv の outfit_id 列（アセット名）を読み出す
        static string[] LoadDefaultOutfitNames()
        {
            var csv = Resources.Load<TextAsset>(DefaultOutfitsCsvName);
            if (csv is null)
            {
                Debug.LogError($"[CharacterOutfitService] {DefaultOutfitsCsvName}.csv not found");
                return Array.Empty<string>();
            }

            return csv.text.Split('\n')
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(','))
                .Where(columns => columns.Length > 1)
                .Select(columns => columns[1].Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();
        }

        /// 未装備の部位にデフォルトを装備させる（初回起動時に裸にならないようにする）
        void EquipDefaultsForEmptySlots(IReadOnlyList<string> defaultOutfitNames)
        {
            var masterOutfits = _masterDataState.Outfits;
            if (masterOutfits is null) return;

            var hasNewEquip = false;

            foreach (var outfitName in defaultOutfitNames)
            {
                var masterOutfit = masterOutfits.FirstOrDefault(o => o.Name == outfitName);
                if (masterOutfit is null) continue;

                var outfit = _outfitAssetState.Get(outfitName);
                if (outfit is null) continue;

                if (_userEquippedOutfitState.GetEquippedOutfitId(outfit.OutfitType) is not null) continue;

                _userEquippedOutfitService.Equip(outfit.OutfitType, masterOutfit.Id);
                hasNewEquip = true;
            }

            if (hasNewEquip)
            {
                _userEquippedOutfitService.Save();
            }
        }

        void Apply(CharacterView characterView)
        {
            var masterOutfits = _masterDataState.Outfits;
            if (masterOutfits is null) return;

            foreach (var outfitId in _userEquippedOutfitState.GetAllEquippedOutfitIds().Values)
            {
                var masterOutfit = masterOutfits.FirstOrDefault(o => o.Id == outfitId);
                if (masterOutfit is null) continue;

                var outfit = _outfitAssetState.Get(masterOutfit.Name);
                if (outfit is null) continue;

                characterView.SetOutfit(outfit);
            }
        }
    }
}
