using Home.State;
using Root.State;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer.Unity;

namespace Home.Starter
{
    public class FurnitureAssetStarter : IStartable
    {
        readonly FurnitureAssetState _furnitureAssetState;
        readonly MasterDataState _masterDataState;

        int _pendingLoadCount;

        public FurnitureAssetStarter(
            FurnitureAssetState furnitureAssetState,
            MasterDataState masterDataState)
        {
            _furnitureAssetState = furnitureAssetState;
            _masterDataState = masterDataState;
        }

        public void Start()
        {
            LoadAllFurnitures();
        }

        void LoadAllFurnitures()
        {
            if (_masterDataState.Furnitures is null || _masterDataState.Furnitures.Length == 0)
            {
                Debug.LogError("[FurnitureAssetStarter] MasterDataState.Furnitures is null or empty");
                _furnitureAssetState.NotifyLoaded();
                return;
            }

            _pendingLoadCount = _masterDataState.Furnitures.Length;

            foreach (var masterFurniture in _masterDataState.Furnitures)
            {
                var furnitureName = masterFurniture.Name;
                var address = $"Furnitures/{furnitureName}";
                var handle = Addressables.LoadAssetAsync<Cat.Furniture.Furniture>(address);
                handle.Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded && h.Result is not null)
                    {
                        _furnitureAssetState.Add(furnitureName, h.Result);
                    }

                    _pendingLoadCount--;
                    if (_pendingLoadCount <= 0)
                    {
                        _furnitureAssetState.NotifyLoaded();
                    }
                };
            }
        }
    }
}
