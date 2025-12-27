using System;
using Home.State;
using Root.State;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Home.Starter
{
    public class OutfitAssetStarter : IStartable
    {
        readonly OutfitAssetState _outfitAssetState;
        readonly MasterDataState _masterDataState;

        int _pendingLoadCount;

        public OutfitAssetStarter(
            OutfitAssetState outfitAssetState,
            MasterDataState masterDataState)
        {
            _outfitAssetState = outfitAssetState;
            _masterDataState = masterDataState;
        }

        public void Start()
        {
            LoadAllOutfits();
        }

        void LoadAllOutfits()
        {
            if (_masterDataState.Outfits is null || _masterDataState.Outfits.Length == 0)
            {
                Debug.LogError("[OutfitAssetStarter] MasterDataState.Outfits is null or empty");
                _outfitAssetState.NotifyLoaded();
                return;
            }

            _pendingLoadCount = _masterDataState.Outfits.Length;

            foreach (var masterOutfit in _masterDataState.Outfits)
            {
                var outfitName = masterOutfit.Name;
                var address = $"{masterOutfit.Type}/{outfitName}.asset";
                var handle = Addressables.LoadAssetAsync<Cat.Character.Outfit>(address);
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded && h.Result is not null)
                    {
                        _outfitAssetState.Add(outfitName, h.Result);
                    }

                    _pendingLoadCount--;
                    if (_pendingLoadCount <= 0)
                    {
                        _outfitAssetState.NotifyLoaded();
                    }
                };
            }
        }
    }
}